namespace Gambol.Desktop

open System
open System.Collections.Generic
open System.Net
open System.Net.Http
open System.Threading.Tasks
open Gambol.Shared
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
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

    let private copyHeaders
        (headers: IEnumerable<KeyValuePair<string, seq<string>>>)
        (response: HttpResponse)
        =
        for header in headers do
            if not (isHopByHopHeader header.Key) then
                response.Headers[header.Key] <- StringValues(header.Value |> Seq.toArray)

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
                DesktopCapabilities.disabledJson,
                context.RequestAborted)
    }

    let private handleDesktopRequest (context: HttpContext) = task {
        if
            HttpMethods.IsGet context.Request.Method
            && context.Request.Path.Equals(PathString "/_desktop/capabilities")
        then
            do! writeCapabilities context
        else
            context.Response.StatusCode <- StatusCodes.Status404NotFound
    }

    let private forward (client: HttpClient) (cloudAppUrl: Uri) (context: HttpContext) = task {
        use proxyRequest = createProxyRequest cloudAppUrl context.Request

        use! proxyResponse =
            client.SendAsync(
                proxyRequest,
                HttpCompletionOption.ResponseHeadersRead,
                context.RequestAborted)

        context.Response.StatusCode <- int proxyResponse.StatusCode
        copyHeaders proxyResponse.Headers context.Response
        copyHeaders proxyResponse.Content.Headers context.Response

        do! proxyResponse.Content.CopyToAsync(context.Response.Body, context.RequestAborted)
    }

    let start (cloudAppUrl: string) = task {
        let cloudUri = Uri cloudAppUrl
        let builder = WebApplication.CreateBuilder([||])
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

        let localUrl = app.Urls |> Seq.exactlyOne |> Uri

        let stop () = task {
            do! app.StopAsync()
            client.Dispose()
            do! app.DisposeAsync().AsTask()
        }

        return { LocalUrl = localUrl; Stop = stop }
    }
