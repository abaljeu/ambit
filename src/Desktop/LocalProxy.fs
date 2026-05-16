namespace Gambol.Desktop

open System
open System.Collections.Generic
open System.IO
open System.Net
open System.Net.Http
open System.Text.Json
open System.Threading.Tasks
open Gambol.Shared
open Thoth.Json.Newtonsoft
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Primitives

type LocalProxy =
    { LocalUrl: Uri
      Stop: unit -> Task<unit> }

[<RequireQualifiedAccess>]
module LocalProxy =
    let private hopByHopHeaders =
        [ "Connection"
          "Keep-Alive"
          "Proxy-Authenticate"
          "Proxy-Authorization"
          "Proxy-Connection"
          "TE"
          "Trailer"
          "Transfer-Encoding"
          "Upgrade" ]

    let private isHopByHopHeader name =
        hopByHopHeaders
        |> List.exists (fun header ->
            String.Equals(header, name, StringComparison.OrdinalIgnoreCase))

    let private isForwardedRequestHeader name =
        not (String.Equals(name, "Host", StringComparison.OrdinalIgnoreCase))
        && not (String.Equals(name, "Cookie", StringComparison.OrdinalIgnoreCase))
        && not (isHopByHopHeader name)

    let private isForwardedResponseHeader name =
        not (String.Equals(name, "Set-Cookie", StringComparison.OrdinalIgnoreCase))
        && not (isHopByHopHeader name)

    let private resolveTargetUri (cloudAppUrl: Uri) (path: PathString) (query: QueryString) =
        let origin = Uri(cloudAppUrl.GetLeftPart(UriPartial.Authority))
        let targetPath =
            if not path.HasValue || path.Value = "/" then cloudAppUrl.AbsolutePath
            else path.Value

        let builder = UriBuilder(origin)
        builder.Path <- targetPath
        builder.Query <- if query.HasValue then query.Value.TrimStart('?') else ""
        builder.Uri

    let private requestHasBody (request: HttpRequest) =
        request.ContentLength.HasValue || request.Headers.ContainsKey("Transfer-Encoding")

    let private addHeaders
        (tryAdd: string -> string array -> bool)
        (headers: IHeaderDictionary)
        =
        for header in headers do
            if isForwardedRequestHeader header.Key then
                tryAdd header.Key (header.Value |> Seq.toArray) |> ignore

    let private createProxyRequest (cloudAppUrl: Uri) (request: HttpRequest) =
        let targetUri = resolveTargetUri cloudAppUrl request.Path request.QueryString
        let proxyRequest = new HttpRequestMessage(HttpMethod(request.Method), targetUri)

        if requestHasBody request then
            proxyRequest.Content <- new StreamContent(request.Body)

        let addRequestHeader (key: string) (values: string array) =
            proxyRequest.Headers.TryAddWithoutValidation(key, Seq.ofArray values)

        addHeaders addRequestHeader request.Headers

        if not (isNull proxyRequest.Content) then
            let addContentHeader (key: string) (values: string array) =
                proxyRequest.Content.Headers.TryAddWithoutValidation(key, Seq.ofArray values)

            addHeaders addContentHeader request.Headers

        proxyRequest

    let private currentOrigin (request: HttpRequest) =
        Uri(request.Scheme + "://" + request.Host.Value)

    let private rewriteHeader
        (cloudAppUrl: Uri)
        (localUrl: Uri)
        (name: string)
        (values: seq<string>)
        =
        if String.Equals(name, "Location", StringComparison.OrdinalIgnoreCase) then
            values |> Seq.map (RedirectRewrite.rewriteLocation cloudAppUrl localUrl)
        else
            values

    let private copyHeaders
        (cloudAppUrl: Uri)
        (localUrl: Uri)
        (headers: IEnumerable<KeyValuePair<string, seq<string>>>)
        (response: HttpResponse)
        =
        for header in headers do
            if isForwardedResponseHeader header.Key then
                let values =
                    rewriteHeader cloudAppUrl localUrl header.Key header.Value
                    |> Seq.toArray

                response.Headers[header.Key] <- StringValues(values)

    let private createHttpClient () =
        let handler = new HttpClientHandler(AllowAutoRedirect = false)
        new HttpClient(handler, disposeHandler = true)

    let private isDesktopRequest (path: PathString) =
        path.StartsWithSegments(PathString "/_desktop")

    let private writeCapabilities (context: HttpContext) = task {
        context.Response.StatusCode <- StatusCodes.Status200OK
        context.Response.ContentType <- "application/json; charset=utf-8"

        do!
            context.Response.WriteAsync(
                DesktopCapabilities.importEnabledJson,
                context.RequestAborted)
    }

    let private quoteJson (text: string) =
        JsonSerializer.Serialize text

    let private writeJson (context: HttpContext) (json: string) = task {
        context.Response.StatusCode <- StatusCodes.Status200OK
        context.Response.ContentType <- "application/json; charset=utf-8"
        do! context.Response.WriteAsync(json, context.RequestAborted)
    }

    let private writeBadRequest (context: HttpContext) (message: string) = task {
        context.Response.StatusCode <- StatusCodes.Status400BadRequest
        context.Response.ContentType <- "application/json; charset=utf-8"
        let json = "{\"error\":" + quoteJson message + "}"
        do! context.Response.WriteAsync(json, context.RequestAborted)
    }

    let private readRequestBody (context: HttpContext) = task {
        use reader = new StreamReader(context.Request.Body)
        return! reader.ReadToEndAsync(context.RequestAborted)
    }

    let private decodePathRequest (body: string) : Result<string, string> =
        try
            use document = JsonDocument.Parse body
            let value = document.RootElement.GetProperty "path"

            if value.ValueKind <> JsonValueKind.String then
                Error "path must be a string"
            else
                match value.GetString() with
                | null -> Error "path is required"
                | path when path.Trim().Length = 0 -> Error "path is required"
                | path -> Ok path
        with
        | :? JsonException -> Error "invalid JSON"
        | :? KeyNotFoundException -> Error "path is required"
        | :? InvalidOperationException -> Error "path must be a string"

    let private hasInvalidPathChar (path: string) =
        path.IndexOfAny(Path.GetInvalidPathChars()) >= 0

    let private resolveLocalPath (path: string) : Result<string, string> =
        let trimmed = path.Trim()

        if trimmed.Length = 0 || hasInvalidPathChar trimmed then
            Error "invalid path"
        else
            try
                let combined =
                    if Path.IsPathFullyQualified trimmed then trimmed
                    else Path.Combine(Environment.CurrentDirectory, trimmed)

                Ok (Path.GetFullPath combined)
            with
            | :? ArgumentException -> Error "invalid path"
            | :? NotSupportedException -> Error "invalid path"
            | :? PathTooLongException -> Error "invalid path"

    let private fileStatusForPath (path: string) : DesktopFileStatus =
        match resolveLocalPath path with
        | Error _ -> InvalidPath
        | Ok fullPath when File.Exists fullPath -> ExistingFile
        | Ok fullPath when Directory.Exists fullPath -> ExistingFolder
        | Ok _ -> CreateFile

    let private localAppUrl (listenUrl: string) (cloudAppUrl: Uri) =
        let baseUrl =
            if listenUrl.EndsWith("/", StringComparison.Ordinal) then listenUrl
            else listenUrl + "/"

        Uri(Uri baseUrl, cloudAppUrl.AbsolutePath.TrimStart('/'))

    let private writeFileStatus (context: HttpContext) (path: string) = task {
        let status = fileStatusForPath path
        let json =
            "{\"path\":" + quoteJson path
            + ",\"status\":" + quoteJson (DesktopFileStatus.label status) + "}"
        do! writeJson context json
    }

    let private handleFileStatus (context: HttpContext) = task {
        let! body = readRequestBody context
        match decodePathRequest body with
        | Error message -> do! writeBadRequest context message
        | Ok path -> do! writeFileStatus context path
    }

    let private readDirectoryAsText (fullPath: string) : string =
        let dir = DirectoryInfo fullPath

        dir.EnumerateFileSystemInfos()
        |> Seq.sortBy (fun e -> e.Name.ToLowerInvariant())
        |> Seq.map (fun e ->
            let name =
                match e with
                | :? DirectoryInfo -> e.Name + "/"
                | _ -> e.Name

            let ts = e.LastWriteTime.ToString("yyyy-MM-dd HH:mm")
            sprintf "[[%s]] %s" name ts)
        |> String.concat "\n"

    let private handleImport (context: HttpContext) = task {
        let! body = readRequestBody context

        match decodePathRequest body with
        | Error message -> do! writeBadRequest context message
        | Ok path ->
            match resolveLocalPath path with
            | Error message -> do! writeBadRequest context message
            | Ok fullPath ->
                if not (File.Exists fullPath) && not (Directory.Exists fullPath) then
                    do! writeBadRequest context "file not found"
                else
                    try
                        let! text =
                            if Directory.Exists fullPath then
                                Task.FromResult(readDirectoryAsText fullPath)
                            else
                                File.ReadAllTextAsync(fullPath, context.RequestAborted)

                        match ImportText.buildPackage path text with
                        | Error message -> do! writeBadRequest context message
                        | Ok package ->
                            let json =
                                Encode.toString 0 (Serialization.encodeDesktopImportPackage package)

                            do! writeJson context json
                    with
                    | :? IOException as ex ->
                        do! writeBadRequest context ("read failed: " + ex.Message)
    }

    let private handleDesktopRequest (context: HttpContext) = task {
        if
            HttpMethods.IsGet context.Request.Method
            && context.Request.Path.Equals(PathString "/_desktop/capabilities")
        then
            do! writeCapabilities context
        elif
            HttpMethods.IsPost context.Request.Method
            && context.Request.Path.Equals(PathString "/_desktop/file-status")
        then
            do! handleFileStatus context
        elif
            HttpMethods.IsPost context.Request.Method
            && context.Request.Path.Equals(PathString "/_desktop/import")
        then
            do! handleImport context
        else
            context.Response.StatusCode <- StatusCodes.Status404NotFound
    }

    let private forward (client: HttpClient) (cloudAppUrl: Uri) (context: HttpContext) = task {
        use proxyRequest = createProxyRequest cloudAppUrl context.Request
        let localUrl = currentOrigin context.Request

        use! proxyResponse =
            client.SendAsync(
                proxyRequest,
                HttpCompletionOption.ResponseHeadersRead,
                context.RequestAborted)

        context.Response.StatusCode <- int proxyResponse.StatusCode
        copyHeaders cloudAppUrl localUrl proxyResponse.Headers context.Response
        copyHeaders cloudAppUrl localUrl proxyResponse.Content.Headers context.Response

        do! proxyResponse.Content.CopyToAsync(context.Response.Body, context.RequestAborted)
    }

    let start (cloudAppUrl: string) = task {
        let cloudUri = Uri cloudAppUrl
        let builder = WebApplication.CreateBuilder([||])
        builder.Services.Configure<HostOptions>(fun (options: HostOptions) ->
            options.ShutdownTimeout <- TimeSpan.FromSeconds 1.0)
        |> ignore

        builder.WebHost.ConfigureKestrel(fun options -> options.Listen(IPAddress.Loopback, 0))
        |> ignore

        let app = builder.Build()
        let client = createHttpClient ()
        app.Run(RequestDelegate(fun context ->
            if isDesktopRequest context.Request.Path then
                handleDesktopRequest context
            else
                forward client cloudUri context)) |> ignore

        do! app.StartAsync()

        let localUrl = localAppUrl (app.Urls |> Seq.exactlyOne) cloudUri

        let stop () = task {
            do! app.StopAsync()
            client.Dispose()
            do! app.DisposeAsync().AsTask()
        }

        return { LocalUrl = localUrl; Stop = stop }
    }
