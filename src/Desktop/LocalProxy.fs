namespace Gambol.Desktop

open System
open System.Collections.Generic
open System.IO
open System.Net
open System.Net.Http
open System.Text
open System.Text.Json
open System.Threading.Tasks
open Gambol.Server
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

    let private isContentHeader name =
        String.Equals(name, "Content-Type", StringComparison.OrdinalIgnoreCase)
        || String.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase)
        || String.Equals(name, "Content-Encoding", StringComparison.OrdinalIgnoreCase)

    let private isBrowserContextHeader name =
        String.Equals(name, "Origin", StringComparison.OrdinalIgnoreCase)
        || String.Equals(name, "Referer", StringComparison.OrdinalIgnoreCase)
        || name.StartsWith("Sec-", StringComparison.OrdinalIgnoreCase)

    let private cloudReferer (cloudAppUrl: Uri) =
        let path = cloudAppUrl.GetLeftPart(UriPartial.Path)
        if path.EndsWith("/", StringComparison.Ordinal) then path else path + "/"

    let private isForwardedRequestHeader name =
        not (String.Equals(name, "Host", StringComparison.OrdinalIgnoreCase))
        && not (String.Equals(name, "Cookie", StringComparison.OrdinalIgnoreCase))
        && not (isHopByHopHeader name)
        && not (isContentHeader name)
        && not (isBrowserContextHeader name)

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

    let private copyRequestBody (request: HttpRequest) = task {
        request.EnableBuffering() |> ignore
        use ms = new MemoryStream()
        do! request.Body.CopyToAsync(ms, request.HttpContext.RequestAborted)
        return new ByteArrayContent(ms.ToArray()) :> HttpContent
    }

    let private ensureContentHeaders (request: HttpRequest) (content: HttpContent) =
        let hasContentType =
            match content.Headers.ContentType with
            | null -> false
            | ct -> not (String.IsNullOrEmpty ct.MediaType)

        if not hasContentType then
            match request.ContentType with
            | null | "" -> ()
            | contentType ->
                content.Headers.TryAddWithoutValidation("Content-Type", contentType)
                |> ignore

    let private addCloudBrowserHeaders (cloudAppUrl: Uri) (proxyRequest: HttpRequestMessage) =
        let origin = cloudAppUrl.GetLeftPart(UriPartial.Authority)
        proxyRequest.Headers.TryAddWithoutValidation("Origin", origin) |> ignore
        proxyRequest.Headers.TryAddWithoutValidation("Referer", cloudReferer cloudAppUrl)
        |> ignore

    let private addAuthCookie
        (credentials: LoginForm.Credentials option)
        (tryAdd: string -> string array -> bool)
        =
        credentials
        |> Option.iter (fun creds ->
            let cookie = AuthToken.cookieHeaderValue creds.Username creds.Password
            tryAdd "Cookie" [| cookie |] |> ignore)

    let private createProxyRequest
        (cloudAppUrl: Uri)
        (request: HttpRequest)
        (bodyOverride: HttpContent option)
        (credentials: LoginForm.Credentials option)
        =
        let targetUri = resolveTargetUri cloudAppUrl request.Path request.QueryString
        let proxyRequest = new HttpRequestMessage(HttpMethod(request.Method), targetUri)

        match bodyOverride with
        | Some content -> proxyRequest.Content <- content
        | None -> ()

        let addRequestHeader (key: string) (values: string array) =
            proxyRequest.Headers.TryAddWithoutValidation(key, Seq.ofArray values)

        addAuthCookie credentials addRequestHeader
        addHeaders addRequestHeader request.Headers
        addCloudBrowserHeaders cloudAppUrl proxyRequest

        if not (isNull proxyRequest.Content) then
            ensureContentHeaders request proxyRequest.Content

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

    let private configPath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Gambol",
            "config.json")

    let private quoteJson (text: string) =
        JsonSerializer.Serialize text

    let private writeJson (context: HttpContext) (json: string) = task {
        context.Response.StatusCode <- StatusCodes.Status200OK
        context.Response.ContentType <- "application/json; charset=utf-8"
        do! context.Response.WriteAsync(json, context.RequestAborted)
    }

    let private writeCapabilities (canGit: bool) (context: HttpContext) = task {
        do! writeJson context (DesktopCapabilities.desktopEnabledJson canGit)
    }

    let private writeJsonWithStatus (context: HttpContext) (status: int) (json: string) = task {
        context.Response.StatusCode <- status
        context.Response.ContentType <- "application/json; charset=utf-8"
        do! context.Response.WriteAsync(json, context.RequestAborted)
    }

    let private writeBadRequest (context: HttpContext) (message: string) = task {
        context.Response.StatusCode <- StatusCodes.Status400BadRequest
        context.Response.ContentType <- "application/json; charset=utf-8"
        let json = "{\"error\":" + quoteJson message + "}"
        do! context.Response.WriteAsync(json, context.RequestAborted)
    }

    let private writeUnauthorized (context: HttpContext) = task {
        let json = "{\"error\":\"unauthorized\"}"
        do! writeJsonWithStatus context StatusCodes.Status401Unauthorized json
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

    let private resolveLocalPath
        (workspaceMap: Map<string, WorkspaceMapping>)
        (path: string)
        : Result<string, string>
        =
        let trimmed = path.Trim()

        if trimmed.Length = 0 then
            Error "invalid path"
        else
            match NodeDesktopPath.tryParseWorkspacePath trimmed with
            | Some (label, rel) ->
                WorkspaceLocalMapping.resolvePath workspaceMap label rel
            | None ->
                if hasInvalidPathChar trimmed then
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

    let private fileStatusForPath
        (workspaceMap: Map<string, WorkspaceMapping>)
        (path: string)
        : DesktopFileStatus * System.DateTime option
        =
        match resolveLocalPath workspaceMap path with
        | Error _ -> InvalidPath, None
        | Ok fullPath when File.Exists fullPath ->
            ExistingFile, Some (File.GetLastWriteTimeUtc fullPath)
        | Ok fullPath when Directory.Exists fullPath ->
            ExistingFolder, Some (Directory.GetLastWriteTimeUtc fullPath)
        | Ok _ -> CreateFile, None

    let private localAppUrl (listenUrl: string) (cloudAppUrl: Uri) =
        let baseUrl =
            if listenUrl.EndsWith("/", StringComparison.Ordinal) then listenUrl
            else listenUrl + "/"

        Uri(Uri baseUrl, cloudAppUrl.AbsolutePath.TrimStart('/'))

    let private writeFileStatus
        (workspaceMap: Map<string, WorkspaceMapping>)
        (context: HttpContext)
        (path: string)
        = task {
        let status, sourceModifiedUtc = fileStatusForPath workspaceMap path

        let sourceJson =
            match sourceModifiedUtc with
            | None -> ""
            | Some t ->
                ",\"sourceModifiedUtc\":"
                + string (t.ToUniversalTime().Ticks)

        let json =
            "{\"path\":" + quoteJson path
            + ",\"status\":" + quoteJson (NodeStatus.label status)
            + sourceJson
            + "}"

        do! writeJson context json
    }

    let private handleFileStatus
        (workspaceMap: Map<string, WorkspaceMapping>)
        (context: HttpContext)
        = task {
        let! body = readRequestBody context
        match decodePathRequest body with
        | Error message -> do! writeBadRequest context message
        | Ok path -> do! writeFileStatus workspaceMap context path
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

    let private handleImport
        (workspaceMap: Map<string, WorkspaceMapping>)
        (context: HttpContext)
        = task {
        let! body = readRequestBody context

        match decodePathRequest body with
        | Error message -> do! writeBadRequest context message
        | Ok path ->
            match resolveLocalPath workspaceMap path with
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

                        let packageResult =
                            if Directory.Exists fullPath then
                                ImportText.buildPackage path text
                                |> Result.map (fun package ->
                                    { package with isDirectory = true })
                            else
                                ImportDocument.buildFilePackage path text

                        match packageResult with
                        | Error message -> do! writeBadRequest context message
                        | Ok package ->
                            let json =
                                Encode.toString 0 (Serialization.encodeDesktopImportPackage package)

                            do! writeJson context json
                    with
                    | :? IOException as ex ->
                        do! writeBadRequest context ("read failed: " + ex.Message)
    }

    let private handleImportGet
        (workspaceMap: Map<string, WorkspaceMapping>)
        (context: HttpContext)
        = task {
        match context.Request.Query.TryGetValue("path") with
        | false, _ -> do! writeBadRequest context "path is required"
        | true, value ->
            let path = string value
            let wantContent =
                match context.Request.Query.TryGetValue("content") with
                | true, c -> string c = "1"
                | false, _ -> false

            match decodePathRequest ("{\"path\":" + quoteJson path + "}") with
            | Error message -> do! writeBadRequest context message
            | Ok validPath when wantContent ->
                match resolveLocalPath workspaceMap validPath with
                | Error message -> do! writeBadRequest context message
                | Ok fullPath when Directory.Exists fullPath ->
                    do! writeBadRequest context "path is a directory"
                | Ok fullPath when not (File.Exists fullPath) ->
                    do! writeBadRequest context "file not found"
                | Ok fullPath ->
                    try
                        let! text =
                            File.ReadAllTextAsync(fullPath, context.RequestAborted)
                        let json =
                            "{\"path\":"
                            + quoteJson validPath
                            + ",\"content\":"
                            + quoteJson text
                            + "}"
                        do! writeJson context json
                    with
                    | :? IOException as ex ->
                        do! writeBadRequest context ("read failed: " + ex.Message)
            | Ok validPath ->
                let body = "{\"path\":" + quoteJson validPath + "}"
                context.Request.Body <- new MemoryStream(Encoding.UTF8.GetBytes(body))
                do! handleImport workspaceMap context
    }

    let private handleExport
        (workspaceMap: Map<string, WorkspaceMapping>)
        (context: HttpContext)
        = task {
        let! body = readRequestBody context

        match Decode.fromString Serialization.decodeDesktopExportRequest body with
        | Error message -> do! writeBadRequest context message
        | Ok request ->
            match ExportText.validateExportContent request.content with
            | Error message -> do! writeBadRequest context message
            | Ok () ->
                match resolveLocalPath workspaceMap request.path with
                | Error message -> do! writeBadRequest context message
                | Ok fullPath ->
                    if Directory.Exists fullPath then
                        do! writeBadRequest context "cannot export to a directory"
                    else
                        try
                            do!
                                File.WriteAllTextAsync(
                                    fullPath,
                                    request.content,
                                    context.RequestAborted)

                            let response = { path = request.path }
                            let json =
                                Encode.toString 0 (Serialization.encodeDesktopExportResponse response)

                            do! writeJson context json
                        with
                        | :? IOException as ex ->
                            do! writeBadRequest context ("write failed: " + ex.Message)
    }

    let private handleDesktopRequest
        (workspaceMap: Map<string, WorkspaceMapping> ref)
        (client: HttpClient)
        (ambitBase: string)
        (canGit: bool)
        (session: ref<LoginForm.Credentials option>)
        (downloadManager: WorkspaceDownloadManager.Manager)
        (context: HttpContext)
        = task {
        let map = workspaceMap.Value
        if
            HttpMethods.IsGet context.Request.Method
            && context.Request.Path.Equals(PathString "/_desktop/capabilities")
        then
            do! writeCapabilities canGit context
        elif
            HttpMethods.IsPost context.Request.Method
            && context.Request.Path.Equals(PathString "/_desktop/file-status")
        then
            do! handleFileStatus map context
        elif
            HttpMethods.IsGet context.Request.Method
            && context.Request.Path.Equals(PathString "/_desktop/file")
        then
            do! handleImportGet map context
        elif
            HttpMethods.IsPost context.Request.Method
            && context.Request.Path.Equals(PathString "/_desktop/file")
        then
            do! handleExport map context
        else
            let! handledMapping =
                WorkspaceMappingEndpoints.tryHandle configPath workspaceMap context
            if handledMapping then
                ()
            else
                let! handledSync =
                    WorkspaceSyncEndpoints.tryHandle
                        map
                        client
                        ambitBase
                        session.Value
                        downloadManager
                        context
                if handledSync then
                    ()
                else
                    context.Response.StatusCode <- StatusCodes.Status404NotFound
    }

    let private isAmbitLoginPost (request: HttpRequest) =
        HttpMethods.IsPost request.Method
        && request.Path.StartsWithSegments(PathString "/ambit/login")

    let private isAmbitLogoutGet (request: HttpRequest) =
        HttpMethods.IsGet request.Method
        && request.Path.StartsWithSegments(PathString "/ambit/logout")

    let private responseLocations (response: HttpResponseMessage) =
        seq {
            if not (isNull response.Headers.Location) then
                yield string response.Headers.Location

            match response.Headers.TryGetValues("Location") with
            | true, values -> yield! values
            | _ -> ()
        }

    let private forward
        (client: HttpClient)
        (cloudAppUrl: Uri)
        (session: ref<LoginForm.Credentials option>)
        (context: HttpContext)
        = task {
        if isAmbitLogoutGet context.Request then
            AuthStore.clear()
            session .Value <- None

        let! bodyOverride, loginAttempt =
            if isAmbitLoginPost context.Request then
                task {
                    let! body = readRequestBody context
                    let parsed = LoginForm.tryParse body

                    let mediaType =
                        context.Request.ContentType
                        |> Option.ofObj
                        |> Option.defaultValue "application/x-www-form-urlencoded"

                    let content = new StringContent(body, Encoding.UTF8, mediaType)
                    return Some(content :> HttpContent), parsed
                }
            elif requestHasBody context.Request then
                task {
                    let! content = copyRequestBody context.Request
                    return Some content, None
                }
            else
                task { return None, None }

        use proxyRequest =
            createProxyRequest cloudAppUrl context.Request bodyOverride session.Value

        let localUrl = currentOrigin context.Request

        use! proxyResponse =
            client.SendAsync(
                proxyRequest,
                HttpCompletionOption.ResponseHeadersRead,
                context.RequestAborted)

        match loginAttempt, LoginRedirect.isSuccess (int proxyResponse.StatusCode) (responseLocations proxyResponse) with
        | Some creds, true ->
            AuthStore.save creds
            session .Value <- Some creds
        | _ -> ()

        context.Response.StatusCode <- int proxyResponse.StatusCode
        copyHeaders cloudAppUrl localUrl proxyResponse.Headers context.Response
        copyHeaders cloudAppUrl localUrl proxyResponse.Content.Headers context.Response

        do! proxyResponse.Content.CopyToAsync(context.Response.Body, context.RequestAborted)
    }

    let start (cloudAppUrl: string) = task {
        let cloudUri = Uri cloudAppUrl

        let workspaceMap =
            WorkspaceLocalMapping.loadFromFile configPath
            |> Result.defaultWith (fun _ -> { entries = [] })
            |> WorkspaceLocalMapping.toMap
            |> ref

        let ambitBase = cloudUri.GetLeftPart(UriPartial.Path).TrimEnd('/')
        let canGit = DesktopGit.isAvailable()

        let builder = WebApplication.CreateBuilder([||])
        builder.Services.Configure<HostOptions>(fun (options: HostOptions) ->
            options.ShutdownTimeout <- TimeSpan.FromSeconds 1.0)
        |> ignore

        builder.WebHost.ConfigureKestrel(fun options -> options.Listen(IPAddress.Loopback, 0))
        |> ignore

        let app = builder.Build()
        let client = createHttpClient ()
        let session = ref (AuthStore.load())
        let downloadManager =
            WorkspaceDownloadManager.create
                client
                ambitBase
                (session.Value
                 |> Option.map (fun c ->
                     AuthToken.cookieHeaderValue c.Username c.Password))
                (fun label ->
                    match WorkspaceLocalMapping.resolvePath workspaceMap.Value label "" with
                    | Ok path -> Ok path
                    | Error _ ->
                        Error(WorkspaceLocalMapping.missingMappingMessage label))

        app.Run(RequestDelegate(fun context ->
            if isDesktopRequest context.Request.Path then
                handleDesktopRequest
                    workspaceMap
                    client
                    ambitBase
                    canGit
                    session
                    downloadManager
                    context
            else
                forward client cloudUri session context)) |> ignore

        do! app.StartAsync()

        let localUrl = localAppUrl (app.Urls |> Seq.exactlyOne) cloudUri

        let stop () = task {
            do! app.StopAsync()
            client.Dispose()
            do! app.DisposeAsync().AsTask()
        }

        return { LocalUrl = localUrl; Stop = stop }
    }
