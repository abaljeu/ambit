namespace Gambol.Server

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Gambol.Shared

[<RequireQualifiedAccess>]
module RouteRegistration =

    type Authentication =
        {
            ExpectedUser: string
            ExpectedPass: string
            Disabled: bool
            GitToken: string
            IsAuthenticated: HttpRequest -> bool
            /// Smart HTTP only: Basic username + git PAT (not browser cookie).
            IsGitAuthenticated: HttpRequest -> bool
            SetCookie: HttpResponse -> unit
            ClearCookie: HttpResponse -> unit
        }

    type PersistenceContext =
        {
            DataDir: string
            Mode: DatabaseSetup.PersistenceMode
            DbStatus: DatabaseSetup.DbStatus
            GetHandle: string -> AgentHandle
            GetOrCreateFileAgent: string -> FileAgent
        }

    type RouteAssets =
        {
            GambolHtml: string
            DefaultUserCss: string
            CommandDockSvg: string
        }

    type BuildStamps =
        {
            DeployStamp: unit -> string
            PageBuildStamp: unit -> string
            PageBuildEpochSec: unit -> int
            DeployEpochSec: unit -> int
            InlineCommandDockSprite: unit -> string
        }

    let createAuthentication (config: IConfiguration) =
        let expectedUser = config.["Auth:Username"] |> Option.ofObj |> Option.defaultValue ""
        let expectedPass = config.["Auth:Password"] |> Option.ofObj |> Option.defaultValue ""
        let validToken = AuthToken.deriveToken expectedUser expectedPass
        let gitToken = AuthToken.deriveGitToken expectedUser expectedPass
        let authDisabled = expectedUser = "" && expectedPass = ""
        let isAuthenticated (req: HttpRequest) =
            if authDisabled then true
            else
                match req.Cookies.TryGetValue(AuthToken.cookieName) with
                | true, cookie -> cookie = validToken
                | _ -> false
        let isGitAuthenticated (req: HttpRequest) =
            if authDisabled then true
            else
                match req.Headers.TryGetValue("Authorization") with
                | true, values ->
                    match AuthToken.tryParseBasicAuth (string values.[0]) with
                    | Some(user, pass) ->
                        user = expectedUser && pass = gitToken
                    | None -> false
                | _ -> false
        let setAuthCookie (resp: HttpResponse) =
            let opts =
                CookieOptions(
                    HttpOnly = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = Nullable(DateTimeOffset.UtcNow.AddYears(10)))
            resp.Cookies.Append(AuthToken.cookieName, validToken, opts)
        let clearAuthCookie (resp: HttpResponse) =
            resp.Cookies.Delete(AuthToken.cookieName)
        {
            ExpectedUser = expectedUser
            ExpectedPass = expectedPass
            Disabled = authDisabled
            GitToken = gitToken
            IsAuthenticated = isAuthenticated
            IsGitAuthenticated = isGitAuthenticated
            SetCookie = setAuthCookie
            ClearCookie = clearAuthCookie
        }

    let private errorTemplate (message: string) =
        sprintf """<!DOCTYPE html>
<html><head><title>Server Error</title>
<style>body{font-family:sans-serif;padding:2rem}pre{background:#f4f4f4;padding:1rem;overflow:auto}</style>
</head><body>
<h1>Server failed to start</h1>
<pre>%s</pre>
</body></html>""" message

    let private registerStartupError (app: WebApplication) (message: string) =
        let errorHtml = errorTemplate message
        let writeError (ctx: HttpContext) =
            ctx.Response.StatusCode <- 500
            ctx.Response.ContentType <- "text/html; charset=utf-8"
            ctx.Response.WriteAsync(errorHtml)
        app.Use(fun ctx (_next: RequestDelegate) -> writeError ctx) |> ignore
        app.MapFallback(fun (ctx: HttpContext) -> writeError ctx) |> ignore

    let private createPersistenceContext
        (config: IConfiguration)
        (dataDir: string)
        (persistenceMode: DatabaseSetup.PersistenceMode)
        =
        let mutable currentFileAgent: (string * FileAgent) option = None
        let fileAgentLock = obj ()
        let getOrCreateFileAgent (filename: string) : FileAgent =
            lock fileAgentLock (fun () ->
                match currentFileAgent with
                | Some (name, agent) when name = filename -> agent
                | Some (_, agent) ->
                    FileAgent.dispose agent
                    let newAgent = FileAgent.create dataDir filename
                    currentFileAgent <- Some (filename, newAgent)
                    newAgent
                | None ->
                    let newAgent = FileAgent.create dataDir filename
                    currentFileAgent <- Some (filename, newAgent)
                    newAgent)
        let dbConnString = config.["DB_CONNECTION_STRING"] |> Option.ofObj |> Option.defaultValue ""
        let dbStatus = DatabaseSetup.resolveDbConnection persistenceMode dbConnString dataDir
        let getHandle (filename: string) : AgentHandle =
            match persistenceMode, dbStatus with
            | DatabaseSetup.PersistenceMode.Db, DatabaseSetup.DbStatus.Ok ->
                AgentHandle.ofDb (DatabaseSetup.getOrCreateDbAgent dbConnString dataDir filename)
            | DatabaseSetup.PersistenceMode.File, DatabaseSetup.DbStatus.Ok ->
                let fileAgent = getOrCreateFileAgent filename
                let dbAgent = DatabaseSetup.getOrCreateDbAgent dbConnString dataDir filename
                AgentHandle.ofFileWithDbMirror fileAgent (Some dbAgent)
            | _ ->
                AgentHandle.ofFile (getOrCreateFileAgent filename)
        {
            DataDir = dataDir
            Mode = persistenceMode
            DbStatus = dbStatus
            GetHandle = getHandle
            GetOrCreateFileAgent = getOrCreateFileAgent
        }

    let private stripXmlDeclaration (text: string) =
        if text.StartsWith("<?xml") then
            match text.IndexOf("?>") with
            | -1 -> text
            | i -> text.Substring(i + 2).TrimStart()
        else text

    let private createRouteAssets (webRoot: string) : RouteAssets =
        {
            GambolHtml = Path.Combine(webRoot, "gambol.template.html")
            DefaultUserCss = Path.Combine(webRoot, "user.css")
            CommandDockSvg = Path.Combine(webRoot, "command-dock.svg")
        }

    let private pageArtifactUtc (webRoot: string) (assets: RouteAssets) =
        let fileWriteUtc path =
            if File.Exists path then File.GetLastWriteTimeUtc path else DateTime.MinValue
        [
            assets.GambolHtml
            Path.Combine(webRoot, "Program.js")
            Path.Combine(webRoot, "Update.js")
            Path.Combine(webRoot, "style.css")
            assets.DefaultUserCss
            assets.CommandDockSvg
        ]
        |> List.map fileWriteUtc
        |> List.max

    let private createBuildStamps (app: WebApplication) : RouteAssets * BuildStamps =
        let webRoot = app.Environment.WebRootPath
        let assets = createRouteAssets webRoot
        let pageArtifactUtc = pageArtifactUtc webRoot assets
        let serverAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location
        if String.IsNullOrWhiteSpace serverAssemblyPath then
            failwith "Could not determine server assembly path for build timestamp."
        if not (File.Exists serverAssemblyPath) then
            failwithf "Could not read server assembly timestamp: missing file at '%s'." serverAssemblyPath
        let deployUtc = max (File.GetLastWriteTimeUtc(serverAssemblyPath)) pageArtifactUtc
        let torontoTz = TimeZoneInfo.FindSystemTimeZoneById("America/Toronto")
        let formatStamp (utc: DateTime) =
            TimeZoneInfo.ConvertTimeFromUtc(utc, torontoTz).ToString("yyyy-MM-dd HH:mm:ss") + " ET"
        let pageUtc () =
            if pageArtifactUtc > DateTime.MinValue then pageArtifactUtc else deployUtc
        let epochSec (utc: DateTime) =
            int (utc.Subtract(DateTime.UnixEpoch).TotalSeconds)
        let inlineCommandDockSprite () =
            if not (File.Exists assets.CommandDockSvg) then ""
            else stripXmlDeclaration (File.ReadAllText assets.CommandDockSvg)
        assets,
        {
            DeployStamp = fun () -> formatStamp deployUtc
            PageBuildStamp =
                fun () ->
                    if pageArtifactUtc > DateTime.MinValue then formatStamp pageArtifactUtc
                    else "unknown"
            PageBuildEpochSec = fun () -> pageUtc () |> epochSec
            DeployEpochSec = fun () -> epochSec deployUtc
            InlineCommandDockSprite = inlineCommandDockSprite
        }

    let private registerAuthRoutes (app: WebApplication) (auth: Authentication) =
        let loginHtml = Path.Combine(app.Environment.WebRootPath, "login.html")
        app.MapGet("/ambit/login", Func<IResult>(fun () ->
            Results.File(loginHtml, "text/html")
        )) |> ignore
        app.MapPost("/ambit/login", Func<HttpRequest, Task<IResult>>(fun req -> task {
            let! form = req.ReadFormAsync()
            let username = string form.["username"]
            let password = string form.["password"]
            if username = auth.ExpectedUser && password = auth.ExpectedPass && username <> "" then
                auth.SetCookie req.HttpContext.Response
                return Results.Redirect("/ambit")
            else
                return Results.Redirect("/ambit/login?error=1")
        })) |> ignore
        app.MapGet("/ambit/logout", Func<HttpResponse, IResult>(fun resp ->
            auth.ClearCookie resp
            Results.Redirect("/ambit/login")
        )) |> ignore
        // Git PAT for smart HTTP (cookie session required; not the cookie itself).
        app.MapGet("/ambit/git-token", Func<HttpRequest, IResult>(fun req ->
            if auth.Disabled then
                Results.Json(
                    {| disabled = true; message = "Auth disabled; git gateway is open" |})
            elif not (auth.IsAuthenticated req) then
                Results.Unauthorized()
            else
                Results.Json(
                    {| username = auth.ExpectedUser; token = auth.GitToken |})
        )) |> ignore

    let private parseClientRev (req: HttpRequest) =
        match req.Query.TryGetValue "rev" with
        | true, value ->
            match Int32.TryParse(string value) with
            | true, revision -> revision
            | _ -> 0
        | _ -> 0

    /// Read X-Gambol-Client, store on HttpContext.Items, log when present.
    let private bindClientHint (req: HttpRequest) : string option =
        match req.Headers.TryGetValue(ClientIdentity.HeaderName) with
        | true, values ->
            match ClientIdentity.tryFromValues values with
            | Some hint ->
                req.HttpContext.Items[ClientIdentity.HeaderName] <- hint
                eprintfn "[Gambol] %s client=%s" (string req.Path) hint
                Some hint
            | None -> None
        | _ -> None

    let private registerStateRoutes
        (app: WebApplication)
        (auth: Authentication)
        (persistence: PersistenceContext)
        (stamps: BuildStamps)
        =
        app.MapGet("/ambit/state", Func<HttpRequest, Task<IResult>>(fun req -> task {
            if not (auth.IsAuthenticated req) then
                return Results.Unauthorized()
            else
                let handle = persistence.GetHandle "gambol"
                return! Api.getState handle |> Async.StartAsTask
        })) |> ignore
        app.MapGet("/ambit/poll", Func<HttpRequest, Task<IResult>>(fun req -> task {
            if not (auth.IsAuthenticated req) then
                return Results.Unauthorized()
            else
                let handle = persistence.GetHandle "gambol"
                let pageEpoch = stamps.PageBuildEpochSec ()
                let clientRev = parseClientRev req
                return!
                    Api.getPoll handle (stamps.DeployEpochSec ()) pageEpoch clientRev
                    |> Async.StartAsTask
        })) |> ignore
        app.MapPost("/ambit/changes", Func<HttpRequest, Task<IResult>>(fun req -> task {
            if not (auth.IsAuthenticated req) then
                return Results.Unauthorized()
            else
                bindClientHint req |> ignore
                use reader = new StreamReader(req.Body)
                let! body = reader.ReadToEndAsync()
                let handle = persistence.GetHandle "gambol"
                return! Api.postChange handle body |> Async.StartAsTask
        })) |> ignore

    let private prepareGitSave (persistence: PersistenceContext) () = async {
        let handle = persistence.GetHandle "gambol"
        let fileAgent = persistence.GetOrCreateFileAgent "gambol"
        return!
            SavePrep.syncDataDir
                persistence.Mode
                persistence.DbStatus
                (fun () -> handle.getState ())
                (fun () -> FileAgent.flushSnapshot fileAgent)
                (fun () -> FileAgent.getRevision fileAgent)
                persistence.DataDir
                "gambol"
    }

    let private registerSaveRoutes
        (app: WebApplication)
        (auth: Authentication)
        (persistence: PersistenceContext)
        =
        app.MapGet("/ambit/capabilities", Func<HttpRequest, IResult>(fun req ->
            if auth.Disabled || auth.IsAuthenticated req then
                Api.getCapabilities persistence.DataDir
            else
                Results.Unauthorized()
        )) |> ignore
        app.MapPost("/ambit/file-status", Func<HttpRequest, Task<IResult>>(fun req -> task {
            if not (auth.IsAuthenticated req) then
                return Results.Unauthorized()
            else
                use reader = new StreamReader(req.Body)
                let! body = reader.ReadToEndAsync()
                return Api.postFileStatus persistence.DataDir body
        })) |> ignore
        app.MapGet("/ambit/file", Func<HttpRequest, Task<IResult>>(fun req -> task {
            if not (auth.IsAuthenticated req) then
                return Results.Unauthorized()
            else
                match req.Query.TryGetValue("path") with
                | false, _ -> return Results.BadRequest({| error = "path is required" |})
                | true, value ->
                    return Api.getImportFile persistence.DataDir (string value)
        })) |> ignore
        app.MapPost("/ambit/file/parse", Func<HttpRequest, Task<IResult>>(fun req -> task {
            if not (auth.IsAuthenticated req) then
                return Results.Unauthorized()
            else
                use reader = new StreamReader(req.Body)
                let! body = reader.ReadToEndAsync()
                let handle = persistence.GetHandle "gambol"
                return!
                    Api.postParseFile handle persistence.DataDir body
                    |> Async.StartAsTask
        })) |> ignore
        app.MapPost("/ambit/save", Func<HttpRequest, Task<IResult>>(fun req -> task {
            if not (auth.IsAuthenticated req) then
                return Results.Unauthorized()
            else
                let clientHint = bindClientHint req
                let prepare = prepareGitSave persistence
                return!
                    Api.gitSave prepare persistence.DataDir clientHint
                    |> Async.StartAsTask
        })) |> ignore

    let private dbStatusText (status: DatabaseSetup.DbStatus) =
        match status with
        | DatabaseSetup.DbStatus.Ok -> "ok"
        | DatabaseSetup.DbStatus.Mismatch1 -> "mismatch1"
        | DatabaseSetup.DbStatus.Mismatch2 -> "mismatch2"
        | DatabaseSetup.DbStatus.Absent -> "absent"

    let private serveUserCss (dataDir: string) (defaultUserCss: string) =
        let userPath = Path.Combine(dataDir, "user.css")
        let path = if File.Exists(userPath) then userPath else defaultUserCss
        if File.Exists(path) then Results.File(path, "text/css")
        else Results.NoContent()

    let private renderGambolHtml
        (publicAssetBaseOpt: string option)
        (assets: RouteAssets)
        (stamps: BuildStamps)
        (dbStatus: DatabaseSetup.DbStatus)
        =
        let raw = File.ReadAllText(assets.GambolHtml)
        let pageEpoch = stamps.PageBuildEpochSec ()
        let basePrefix = match publicAssetBaseOpt with None -> "" | Some url -> url
        let styleHref = sprintf "%s/ambit/style.css?v=%d" basePrefix pageEpoch
        let userHref = sprintf "%s/ambit/user.css?v=%d" basePrefix pageEpoch
        let script =
            "    <script>window.__BUILD__ = \"" + stamps.DeployStamp ()
            + "\"; window.__PAGE_BUILD__ = \"" + stamps.PageBuildStamp ()
            + "\"; window.__BUILD_TS__ = " + string (stamps.DeployEpochSec ())
            + "; window.__PAGE_BUILD_TS__ = " + string pageEpoch
            + "; window.__DB_PRESENT__ = \"" + dbStatusText dbStatus
            + "\";</script>\n</head>"
        let programSrc =
            match publicAssetBaseOpt with
            | None -> sprintf "/ambit/Program.js?v=%d" pageEpoch
            | Some baseUrl -> sprintf "%s/ambit/Program.js?v=%d" baseUrl pageEpoch
        raw
            .Replace("href=\"/ambit/style.css\"", sprintf "href=\"%s\"" styleHref)
            .Replace("href=\"/ambit/user.css\"", sprintf "href=\"%s\"" userHref)
            .Replace("</head>", script)
            .Replace("<!-- command-dock-sprite -->", stamps.InlineCommandDockSprite ())
            .Replace("src=\"/ambit/Program.js\"", sprintf "src=\"%s\"" programSrc)

    let private registerAppShellRoute
        (app: WebApplication)
        (auth: Authentication)
        (publicAssetBaseOpt: string option)
        (assets: RouteAssets)
        (stamps: BuildStamps)
        (persistence: PersistenceContext)
        =
        let serveAmbitApp (ctx: HttpContext) : IResult =
            if auth.IsAuthenticated ctx.Request then
                ctx.Response.Headers.CacheControl <- "no-cache, no-store, must-revalidate"
                ctx.Response.Headers.Pragma <- "no-cache"
                ctx.Response.Headers.Expires <- "0"
                let html =
                    renderGambolHtml publicAssetBaseOpt assets stamps persistence.DbStatus
                Results.Content(html, "text/html")
            else
                Results.Redirect("/ambit/login")
        app.MapGet("/ambit", Func<HttpContext, IResult>(serveAmbitApp)) |> ignore

    let private registerCssAndShellRoutes
        (app: WebApplication)
        (auth: Authentication)
        (publicAssetBaseOpt: string option)
        (assets: RouteAssets)
        (stamps: BuildStamps)
        (persistence: PersistenceContext)
        =
        app.MapGet("/ambit/user.css", Func<IResult>(fun () ->
            serveUserCss persistence.DataDir assets.DefaultUserCss
        )) |> ignore
        registerAppShellRoute app auth publicAssetBaseOpt assets stamps persistence

    let registerPersistenceAndRoutes
        (config: IConfiguration)
        (auth: Authentication)
        (publicAssetBaseOpt: string option)
        (dataDirResult: Result<string, exn>)
        (app: WebApplication)
        =
        let persistenceModeResult =
            config.["Persistence:Mode"]
            |> Option.ofObj
            |> Option.defaultValue ""
            |> DatabaseSetup.resolvePersistenceMode
        match dataDirResult, persistenceModeResult with
        | Error ex, _ ->
            registerStartupError app (ex.ToString())
        | _, Error err ->
            registerStartupError app err
        | Ok dataDir, Ok persistenceMode ->
            let persistence = createPersistenceContext config dataDir persistenceMode
            let assets, stamps = createBuildStamps app
            registerAuthRoutes app auth
            registerStateRoutes app auth persistence stamps
            registerSaveRoutes app auth persistence
            let flushForGit () = async {
                let handle = persistence.GetHandle "gambol"
                let! flushResult =
                    SavePrep.syncGitArtifacts
                        persistence.Mode
                        persistence.DbStatus
                        (fun () -> handle.getState ())
                        (fun () ->
                            persistence.GetOrCreateFileAgent "gambol"
                            |> FileAgent.flushSnapshot)
                        (fun () ->
                            persistence.GetOrCreateFileAgent "gambol"
                            |> FileAgent.getRevision)
                        persistence.DataDir
                match flushResult with
                | Ok _ -> return Ok ()
                | Error err -> return Error err
            }
            let reconcileGitPush label changedPaths =
                LazyLoadReconciliationServer.reconcileChangedPaths
                    (persistence.GetHandle "gambol")
                    persistence.DataDir
                    label
                    changedPaths
            GitGateway.registerRoutes
                app
                auth.IsGitAuthenticated
                persistence.DataDir
                flushForGit
                reconcileGitPush
            LazyLoadReconciliationDiagnostics.registerRoute
                app
                auth.IsAuthenticated
            GitGatewayDiagnostics.registerRoute
                app
                auth.IsAuthenticated
            LazyLoadReconciliationServer.registerDirectoryRoute
                app
                auth.IsAuthenticated
                persistence.DataDir
                (fun () -> persistence.GetHandle "gambol")
            LazyLoadReconciliationServer.registerAddedRoute
                app
                auth.IsAuthenticated
                persistence.DataDir
                (fun () -> persistence.GetHandle "gambol")
            WorkspaceWebDav.registerRoutes
                app
                auth.IsAuthenticated
                persistence.DataDir
            registerCssAndShellRoutes app auth publicAssetBaseOpt assets stamps persistence
