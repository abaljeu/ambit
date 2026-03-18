namespace Gambol.Server

open System
open System.IO
open System.Security.Cryptography
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.StaticFiles
open Microsoft.Extensions.Configuration

module Main =

    let resolveDataDir (contentRoot: string) (config: IConfiguration) =
        let onAzure = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") |> Option.ofObj |> Option.isSome
        let dataDir =
            if onAzure then
                // Azure App Service: use persistent /home mount; ignore DataDir config
                let home = Environment.GetEnvironmentVariable("HOME") |> Option.ofObj |> Option.defaultValue "/home"
                Path.Combine(home, "data")
            else
                let relative = config.["DataDir"] |> Option.ofObj |> Option.defaultValue "../../data"
                if Path.IsPathRooted(relative) then relative
                else Path.Combine(contentRoot, relative) |> Path.GetFullPath
        Directory.CreateDirectory(dataDir) |> ignore
        dataDir

    let private cookieName = "gambol_auth"

    // Derive a stable token from credentials so authentication survives server restarts.
    // The token is an HMAC-SHA256 of the username keyed by the password, hex-encoded.
    let private deriveToken (username: string) (password: string) =
        use hmac = new HMACSHA256(Text.Encoding.UTF8.GetBytes(password))
        let hash = hmac.ComputeHash(Text.Encoding.UTF8.GetBytes(username))
        Convert.ToHexString(hash).ToLowerInvariant()

    [<EntryPoint>]
    let main args =
        let port = Environment.GetEnvironmentVariable("PORT") |> Option.ofObj

        // Dev: __SOURCE_DIRECTORY__ has wwwroot next to it (Fable output).
        // Published: wwwroot is copied into the publish output dir alongside the DLL.
        let contentRoot =
            let src = __SOURCE_DIRECTORY__
            if Directory.Exists(Path.Combine(src, "wwwroot")) then src
            else AppContext.BaseDirectory

        let options = WebApplicationOptions(
                        Args = args,
                        ContentRootPath = contentRoot,
                        WebRootPath = Path.Combine(contentRoot, "wwwroot"))
        let builder = WebApplication.CreateBuilder(options)
        // Env-specific appsettings.
        // In Azure, read from persistent /home (not the site wwwroot) so config can survive redeploys.
        let onAzure = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") |> Option.ofObj |> Option.isSome
        let homeDir = Environment.GetEnvironmentVariable("HOME") |> Option.ofObj |> Option.defaultValue "/home"
        let envFile = "appsettings." + builder.Environment.EnvironmentName + ".json"
        let envFilePath = if onAzure then Path.Combine(homeDir, envFile) else envFile
        builder.Configuration.AddJsonFile(envFilePath, optional = true)
            |> ignore
        let app = builder.Build()
        port |> Option.iter (fun p -> app.Urls.Add(sprintf "http://0.0.0.0:%s" p))

        // Production without appsettings.Production.json: start but show error to every request
        let expectedProductionConfigPath =
            let filename = "appsettings.Production.json"
            if onAzure then Path.Combine(homeDir, filename)
            else Path.Combine(app.Environment.ContentRootPath, filename)
        let productionConfigMissing =
            app.Environment.EnvironmentName = "Production"
            && not (File.Exists(expectedProductionConfigPath))
        if productionConfigMissing then
            app.Use(fun (ctx: HttpContext) (next: RequestDelegate) ->
                ctx.Response.StatusCode <- 500
                ctx.Response.ContentType <- "text/html; charset=utf-8"
                let errorHtmlPath = Path.Combine(app.Environment.WebRootPath, "missing-production-config.html")
                let errorHtml =
                    if File.Exists(errorHtmlPath) then
                        File.ReadAllText(errorHtmlPath)
                    else
                        sprintf "Missing appsettings.Production.json at %s" expectedProductionConfigPath
                let html = errorHtml.Replace("{{CONFIG_PATH}}", expectedProductionConfigPath)
                ctx.Response.WriteAsync(html)
            ) |> ignore

        let config = app.Configuration
        let dataDirResult =
            try Ok (resolveDataDir app.Environment.ContentRootPath config)
            with ex -> Error ex

        // ── Auth (stateless — token is derived from credentials, no in-memory state) ──
        let expectedUser = config.["Auth:Username"] |> Option.ofObj |> Option.defaultValue ""
        let expectedPass = config.["Auth:Password"] |> Option.ofObj |> Option.defaultValue ""
        let validToken = deriveToken expectedUser expectedPass

        let authDisabled = expectedUser = "" && expectedPass = ""
        let isAuthenticated (req: HttpRequest) =
            if authDisabled then true
            else
                match req.Cookies.TryGetValue(cookieName) with
                | true, cookie -> cookie = validToken
                | _ -> false

        let setAuthCookie (resp: HttpResponse) =
            let opts =
                CookieOptions(
                    HttpOnly = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = Nullable(DateTimeOffset.UtcNow.AddYears(10)))
            resp.Cookies.Append(cookieName, validToken, opts)

        let clearAuthCookie (resp: HttpResponse) =
            resp.Cookies.Delete(cookieName)

        app.UseDefaultFiles() |> ignore
        app.UseStaticFiles() |> ignore
        // Serve wwwroot under /ambit/ so assets like /ambit/Program.js work. Use /ambit/ not /ambit
        // so that GET /ambit (exact) falls through to MapGet rather than 404 from static files.
        // JS and source maps: no-cache so Reload button (needed on iPad Safari) fetches fresh modules.
        let ambitOpts = StaticFileOptions(
            RequestPath = PathString("/ambit"),
            OnPrepareResponse = Action<StaticFileResponseContext>(fun ctx ->
                let path = ctx.Context.Request.Path.Value
                if path.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".js.map", StringComparison.OrdinalIgnoreCase) then
                    ctx.Context.Response.Headers.CacheControl <- "no-cache, no-store, must-revalidate"
                    ctx.Context.Response.Headers.Pragma <- "no-cache"
                    ctx.Context.Response.Headers.Expires <- "0"))
        app.UseStaticFiles(ambitOpts) |> ignore

        // Development only: serve Client/Shared source files so Chrome DevTools can load mapped sources
        if app.Environment.EnvironmentName = "Development" then
            let contentRoot = app.Environment.ContentRootPath
            let clientDir = Path.GetFullPath(Path.Combine(contentRoot, "..", "Client"))
            let sharedDir = Path.GetFullPath(Path.Combine(contentRoot, "..", "Shared"))
            let serveSource (dir: string) (path: string) =
                let fullPath = Path.GetFullPath(Path.Combine(dir, path))
                if fullPath.StartsWith(dir, StringComparison.OrdinalIgnoreCase)
                   && File.Exists(fullPath)
                   && (path.EndsWith(".fs", StringComparison.OrdinalIgnoreCase)
                       || path.EndsWith(".fsx", StringComparison.OrdinalIgnoreCase))
                then Results.File(fullPath, "text/plain")
                else Results.NotFound()
            app.MapGet("/Client/{*path}", Func<string, IResult>(fun path -> serveSource clientDir path)) |> ignore
            app.MapGet("/Shared/{*path}", Func<string, IResult>(fun path -> serveSource sharedDir path)) |> ignore

        match dataDirResult with
        | Error ex ->
            let errorHtml =
                sprintf """<!DOCTYPE html>
<html><head><title>Server Error</title>
<style>body{font-family:sans-serif;padding:2rem}pre{background:#f4f4f4;padding:1rem;overflow:auto}</style>
</head><body>
<h1>Server failed to start</h1>
<pre>%s</pre>
</body></html>""" (ex.ToString())
            app.MapFallback(fun (ctx: HttpContext) ->
                ctx.Response.StatusCode <- 500
                ctx.Response.ContentType <- "text/html; charset=utf-8"
                ctx.Response.WriteAsync(errorHtml)
            ) |> ignore

        | Ok dataDir ->

            // ── File agent ─────────────────────────────────────────────────
            let mutable currentAgent: (string * FileAgent) option = None
            let agentLock = obj ()

            let getOrCreateAgent (filename: string) : FileAgent =
                lock agentLock (fun () ->
                    match currentAgent with
                    | Some (name, agent) when name = filename -> agent
                    | Some (_, agent) ->
                        FileAgent.dispose agent
                        let newAgent = FileAgent.create dataDir filename
                        currentAgent <- Some (filename, newAgent)
                        newAgent
                    | None ->
                        let newAgent = FileAgent.create dataDir filename
                        currentAgent <- Some (filename, newAgent)
                        newAgent
                )

            // GET /ambit/login → serve login.html
            let loginHtml = Path.Combine(app.Environment.WebRootPath, "login.html")
            app.MapGet("/ambit/login", Func<IResult>(fun () ->
                Results.File(loginHtml, "text/html")
            )) |> ignore

            // POST /ambit/login → validate credentials, set permanent cookie, redirect
            app.MapPost("/ambit/login", Func<HttpRequest, Task<IResult>>(fun req -> task {
                let! form = req.ReadFormAsync()
                let username = string form.["username"]
                let password = string form.["password"]
                if username = expectedUser && password = expectedPass && username <> "" then
                    setAuthCookie req.HttpContext.Response
                    return Results.Redirect("/ambit")
                else
                    return Results.Redirect("/ambit/login?error=1")
            })) |> ignore

            // GET /ambit/logout → clear session, redirect to login
            app.MapGet("/ambit/logout", Func<HttpResponse, IResult>(fun resp ->
                clearAuthCookie resp
                Results.Redirect("/ambit/login")
            )) |> ignore

            // Build stamps for client to detect server redeploy (used in state + gambol.html)
            let gambolHtml = Path.Combine(app.Environment.WebRootPath, "gambol.html")
            let programJs = Path.Combine(app.Environment.WebRootPath, "Program.js")
            let torontoTz = TimeZoneInfo.FindSystemTimeZoneById("America/Toronto")
            let torontoNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, torontoTz)
            let startupStamp = torontoNow.ToString("yyyy-MM-dd HH:mm:ss") + " ET"
            let pageBuildStamp () =
                let htmlTime = if File.Exists(gambolHtml) then File.GetLastWriteTimeUtc(gambolHtml) else DateTime.MinValue
                let jsTime = if File.Exists(programJs) then File.GetLastWriteTimeUtc(programJs) else DateTime.MinValue
                let latest = max htmlTime jsTime
                if latest > DateTime.MinValue then
                    TimeZoneInfo.ConvertTimeFromUtc(latest, torontoTz).ToString("yyyy-MM-dd HH:mm:ss") + " ET"
                else
                    "unknown"
            let pageBuildEpochSec () =
                let htmlTime = if File.Exists(gambolHtml) then File.GetLastWriteTimeUtc(gambolHtml) else DateTime.MinValue
                let jsTime = if File.Exists(programJs) then File.GetLastWriteTimeUtc(programJs) else DateTime.MinValue
                let latest = max htmlTime jsTime
                int (latest.Subtract(DateTime.UnixEpoch).TotalSeconds)
            let startupEpochSec = int (torontoNow.Subtract(DateTime.UnixEpoch).TotalSeconds)

            // GET /ambit/state → JSON { revision, graph } (full payload for initial load)
            app.MapGet("/ambit/state", Func<HttpRequest, Task<IResult>>(fun req -> task {
                if not (isAuthenticated req) then
                    return Results.Unauthorized()
                else
                    let agent = getOrCreateAgent "gambol"
                    return! Api.getState agent |> Async.StartAsTask
            })) |> ignore

            // GET /ambit/poll → JSON { r, b, p } (revision, buildEpochSec, pageBuildEpochSec — lightweight)
            app.MapGet("/ambit/poll", Func<HttpRequest, Task<IResult>>(fun req -> task {
                if not (isAuthenticated req) then
                    return Results.Unauthorized()
                else
                    let agent = getOrCreateAgent "gambol"
                    let pageEpoch = pageBuildEpochSec ()
                    return! Api.getPoll agent startupEpochSec pageEpoch |> Async.StartAsTask
            })) |> ignore

            // POST /ambit/changes → JSON { revision, graph } or 400
            app.MapPost("/ambit/changes", Func<HttpRequest, Task<IResult>>(fun req -> task {
                if not (isAuthenticated req) then
                    return Results.Unauthorized()
                else
                    use reader = new StreamReader(req.Body)
                    let! body = reader.ReadToEndAsync()
                    let agent = getOrCreateAgent "gambol"
                    return! Api.postChange agent body |> Async.StartAsTask
            })) |> ignore

            // GET /ambit/user.css → serve dataDir/user.css, falling back to wwwroot/user.css
            let defaultUserCss = Path.Combine(app.Environment.WebRootPath, "user.css")
            let serveUserCss () =
                let userPath = Path.Combine(dataDir, "user.css")
                let path = if File.Exists(userPath) then userPath else defaultUserCss
                if File.Exists(path) then Results.File(path, "text/css")
                else Results.NoContent()
            app.MapGet("/ambit/user.css", Func<IResult>(fun () -> serveUserCss ())) |> ignore

            // GET /ambit → serve gambol.html (protected) with startup stamp and page file stamp injected
            let gambolHtmlWithStamp () =
                let raw = File.ReadAllText(gambolHtml)
                let pageStamp = pageBuildStamp ()
                let pageEpoch = pageBuildEpochSec ()
                let snippet =
                    "    <script>window.__BUILD__ = \"" + startupStamp + "\"; window.__PAGE_BUILD__ = \"" + pageStamp
                    + "\"; window.__BUILD_TS__ = " + string startupEpochSec
                    + "; window.__PAGE_BUILD_TS__ = " + string pageEpoch + ";</script>\n</head>"
                let withStamp = raw.Replace("</head>", snippet)
                // Cache-bust Program.js so reload gets fresh assets when server redeploys
                withStamp.Replace("src=\"/ambit/Program.js\"", sprintf "src=\"/ambit/Program.js?v=%d\"" pageEpoch)
            app.MapGet("/ambit", Func<HttpContext, IResult>(fun ctx ->
                if isAuthenticated ctx.Request then
                    ctx.Response.Headers.CacheControl <- "no-cache, no-store, must-revalidate"
                    ctx.Response.Headers.Pragma <- "no-cache"
                    ctx.Response.Headers.Expires <- "0"
                    Results.Content(gambolHtmlWithStamp (), "text/html")
                else
                    Results.Redirect("/ambit/login")
            )) |> ignore

        app.Run()

        0 // Exit code
