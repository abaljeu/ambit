namespace Gambol.Server

open System
open System.IO
open System.Security.Cryptography
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
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
        // Env-specific appsettings: required in Production (fail if missing), optional elsewhere
        let envFile = "appsettings." + builder.Environment.EnvironmentName + ".json"
        builder.Configuration.AddJsonFile(envFile, optional = (builder.Environment.EnvironmentName <> "Production"))
        let app = builder.Build()
        port |> Option.iter (fun p -> app.Urls.Add(sprintf "http://0.0.0.0:%s" p))

        let config = app.Configuration
        let dataDirResult =
            try Ok (resolveDataDir app.Environment.ContentRootPath config)
            with ex -> Error ex

        // ── Auth (stateless — token is derived from credentials, no in-memory state) ──
        let expectedUser = config.["Auth:Username"] |> Option.ofObj |> Option.defaultValue ""
        let expectedPass = config.["Auth:Password"] |> Option.ofObj |> Option.defaultValue ""
        let validToken = deriveToken expectedUser expectedPass

        let isAuthenticated (req: HttpRequest) =
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

            // GET /login → serve login.html
            let loginHtml = Path.Combine(app.Environment.WebRootPath, "login.html")
            app.MapGet("/login", Func<IResult>(fun () ->
                Results.File(loginHtml, "text/html")
            )) |> ignore

            // POST /login → validate credentials, set permanent cookie, redirect
            app.MapPost("/login", Func<HttpRequest, Task<IResult>>(fun req -> task {
                let! form = req.ReadFormAsync()
                let username = string form.["username"]
                let password = string form.["password"]
                if username = expectedUser && password = expectedPass && username <> "" then
                    setAuthCookie req.HttpContext.Response
                    return Results.Redirect("/amble")
                else
                    return Results.Redirect("/login?error=1")
            })) |> ignore

            // GET /logout → clear session, redirect to login
            app.MapGet("/logout", Func<HttpResponse, IResult>(fun resp ->
                clearAuthCookie resp
                Results.Redirect("/login")
            )) |> ignore

            // GET /amble/state → JSON { revision, graph }
            app.MapGet("/amble/state", Func<HttpRequest, Task<IResult>>(fun req -> task {
                if not (isAuthenticated req) then
                    return Results.Unauthorized()
                else
                    let agent = getOrCreateAgent "gambol"
                    return! Api.getState agent |> Async.StartAsTask
            })) |> ignore

            // POST /amble/changes → JSON { revision, graph } or 400
            app.MapPost("/amble/changes", Func<HttpRequest, Task<IResult>>(fun req -> task {
                if not (isAuthenticated req) then
                    return Results.Unauthorized()
                else
                    use reader = new StreamReader(req.Body)
                    let! body = reader.ReadToEndAsync()
                    let agent = getOrCreateAgent "gambol"
                    return! Api.postChange agent body |> Async.StartAsTask
            })) |> ignore

            // GET /amble/user.css → serve dataDir/user.css, falling back to wwwroot/user.css
            let defaultUserCss = Path.Combine(app.Environment.WebRootPath, "user.css")
            app.MapGet("/amble/user.css", Func<IResult>(fun () ->
                let userPath = Path.Combine(dataDir, "user.css")
                let path = if File.Exists(userPath) then userPath else defaultUserCss
                if File.Exists(path) then Results.File(path, "text/css")
                else Results.NoContent()
            )) |> ignore

            // GET /amble → serve gambol.html (protected) with startup stamp and page file stamp injected
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
            let gambolHtmlWithStamp () =
                let raw = File.ReadAllText(gambolHtml)
                let pageStamp = pageBuildStamp ()
                let snippet = "    <script>window.__BUILD__ = \"" + startupStamp + "\"; window.__PAGE_BUILD__ = \"" + pageStamp + "\";</script>\n</head>"
                raw.Replace("</head>", snippet)
            app.MapGet("/amble", Func<HttpRequest, IResult>(fun req ->
                if isAuthenticated req then
                    Results.Content(gambolHtmlWithStamp (), "text/html")
                else
                    Results.Redirect("/login")
            )) |> ignore

        app.Run()

        0 // Exit code
