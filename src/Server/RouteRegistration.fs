namespace Gambol.Server

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Gambol.Shared

[<RequireQualifiedAccess>]
module RouteRegistration =

    type AmbitApp with
        member this.CreateBoot persistenceMode : CoreBoot =
            let dataDir = this.DataDir
            let dbConnString =
                this.Config.["DB_CONNECTION_STRING"]
                |> Option.ofObj
                |> Option.defaultValue ""
            let dbStatus =
                DatabaseSetup.resolveDbConnection
                    persistenceMode
                    dbConnString
                    dataDir
            {
                PersistenceMode = persistenceMode
                DbStatus = dbStatus
                DbConnectionString = dbConnString
                DataDir = dataDir
                AuthUser = this.Auth.ExpectedUser
                AuthPass = this.Auth.ExpectedPass
                Actors = [ ActorName "test", TestActor.actorFn ]
            }

    let private errorTemplate (message: string) =
        sprintf """<!DOCTYPE html>
<html><head><title>Server Error</title>
<style>body{font-family:sans-serif;padding:2rem}pre{background:#f4f4f4;padding:1rem;overflow:auto}</style>
</head><body>
<h1>Server failed to start</h1>
<pre>%s</pre>
</body></html>""" message

    let private registerStartupError (this: AmbitApp) (message: string) =
        let errorHtml = errorTemplate message
        let writeError (ctx: HttpContext) =
            ctx.Response.StatusCode <- 500
            ctx.Response.ContentType <- "text/html; charset=utf-8"
            ctx.Response.WriteAsync(errorHtml)
        this.Use(fun ctx (_next: RequestDelegate) -> writeError ctx) |> ignore
        this.MapFallback(fun (ctx: HttpContext) -> writeError ctx) |> ignore

    let private registerErrorReportRoute (routes: AppShellContext) =
        HttpResponseLog.registerErrorReportRoute routes.AmbitApp

    let private createPersistenceContext
        (this: AmbitApp)
        persistenceMode
        =
        let boot = this.CreateBoot persistenceMode
        {
            DataDir = boot.DataDir
            Mode = boot.PersistenceMode
            DbStatus = boot.DbStatus
            Core = CoreRuntime.create boot
        }

    let private isWritable (persistence: PersistenceContext) =
        persistence.Mode <> DatabaseSetup.PersistenceMode.Db
        || persistence.DbStatus = DatabaseSetup.DbStatus.Ok

    let private boundChanges
        (persistence: PersistenceContext)
        (caller: Caller)
        : CoreChanges =
        let raw = CoreMailbox.coreChanges persistence.Core.host caller
        if isWritable persistence then raw
        else CoreRuntime.readOnly raw

    let private parseBound (persistence: PersistenceContext) =
        boundChanges persistence persistence.Core.parseCaller

    /// Missing cookie is 401 without Core. Present cookie uses mailbox admit.
    let private withBrowserChanges
        (persistence: PersistenceContext)
        (req: HttpRequest)
        (cont: CoreChanges -> Async<IResult>)
        : Async<IResult> =
        match BrowserRequestCreds.tryCookieCaller req with
        | None -> async.Return(Results.Unauthorized())
        | Some caller ->
            async {
                let! live =
                    CoreMailbox.isAdmitted persistence.Core.host caller
                if live then return! cont (boundChanges persistence caller)
                else return Results.Unauthorized()
            }

    let private registerAuthRoutes (routes: AppShellContext) =
        let this = routes.AmbitApp
        let persistence = routes.Persistence
        let loginHtml = Path.Combine(this.WebRootPath, "login.html")
        this.MapGet("/ambit/login", Func<IResult>(fun () ->
            Results.File(loginHtml, "text/html")
        )) |> ignore
        this.MapPost("/ambit/login", Func<HttpRequest, Task<IResult>>(fun req -> task {
            let! form = req.ReadFormAsync()
            let username = string form.["username"]
            let password = string form.["password"]
            if username = this.Auth.ExpectedUser
               && password = this.Auth.ExpectedPass then
                let! loginResult =
                    RouteAuthentication.loginThenSetCookie
                        persistence.Core.host
                        this.Auth
                        req.HttpContext.Response
                    |> Async.StartAsTask
                match loginResult with
                | Error _ -> return Results.Redirect("/ambit/login?error=1")
                | Ok () -> return Results.Redirect("/ambit")
            else
                return Results.Redirect("/ambit/login?error=1")
        })) |> ignore
        this.MapGet("/ambit/logout", Func<HttpContext, IResult>(fun ctx ->
            match BrowserRequestCreds.tryCookieCaller ctx.Request with
            | Some caller ->
                CoreMailbox.logout persistence.Core.host caller
                |> Async.RunSynchronously
                |> ignore
            | None -> ()
            this.Auth.ClearCookie ctx.Response
            Results.Redirect("/ambit/login")
        )) |> ignore
        // Git PAT for smart HTTP (cookie session required; not the cookie itself).
        this.MapGet("/ambit/git-token", Func<HttpRequest, IResult>(fun req ->
            if this.Auth.ExpectedUser = "" && this.Auth.ExpectedPass = "" then
                Results.Json(
                    {| disabled = true; message = "Auth disabled; git gateway is open" |})
            elif not (this.Auth.IsAuthenticated req) then
                Results.Unauthorized()
            else
                Results.Json(
                    {| username = this.Auth.ExpectedUser; token = this.Auth.GitToken |})
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

    let private registerStateRoutes (routes: AppShellContext) =
        let this = routes.AmbitApp
        let persistence = routes.Persistence
        let stamps = routes.Stamps
        this.MapGet("/ambit/state", Func<HttpRequest, Task<IResult>>(fun req -> task {
            try
                return!
                    withBrowserChanges persistence req (fun handle ->
                        Api.getState handle req)
                    |> Async.StartAsTask
            with ex ->
                let detail =
                    "Internal server error loading state (dataDir="
                    + persistence.DataDir
                    + "): "
                    + ex.Message
                return
                    Results.Content(
                        detail,
                        "text/plain; charset=utf-8",
                        statusCode = 500)
        })) |> ignore
        this.MapGet("/ambit/poll", Func<HttpRequest, Task<IResult>>(fun req -> task {
            let pageEpoch = stamps.PageBuildEpochSec ()
            let clientRev = parseClientRev req
            return!
                withBrowserChanges persistence req (fun handle ->
                    Api.getPoll
                        handle
                        (stamps.DeployEpochSec ())
                        pageEpoch
                        clientRev)
                |> Async.StartAsTask
        })) |> ignore
        this.MapPost("/ambit/load", Func<HttpRequest, Task<IResult>>(fun req -> task {
            use reader = new StreamReader(req.Body)
            let! body = reader.ReadToEndAsync()
            let pageEpoch = stamps.PageBuildEpochSec ()
            return!
                withBrowserChanges persistence req (fun handle ->
                    Api.postLoad
                        handle
                        (stamps.DeployEpochSec ())
                        pageEpoch
                        body)
                |> Async.StartAsTask
        })) |> ignore
        this.MapPost("/ambit/changes", Func<HttpRequest, Task<IResult>>(fun req -> task {
            bindClientHint req |> ignore
            use reader = new StreamReader(req.Body)
            let! body = reader.ReadToEndAsync()
            let pageEpoch = stamps.PageBuildEpochSec ()
            return!
                withBrowserChanges persistence req (fun handle ->
                    Api.postEvents
                        handle
                        (stamps.DeployEpochSec ())
                        pageEpoch
                        body)
                |> Async.StartAsTask
        })) |> ignore
        this.MapPost("/ambit/events", Func<HttpRequest, Task<IResult>>(fun req -> task {
            bindClientHint req |> ignore
            use reader = new StreamReader(req.Body)
            let! body = reader.ReadToEndAsync()
            let pageEpoch = stamps.PageBuildEpochSec ()
            return!
                withBrowserChanges persistence req (fun handle ->
                    Api.postEvents
                        handle
                        (stamps.DeployEpochSec ())
                        pageEpoch
                        body)
                |> Async.StartAsTask
        })) |> ignore
        this.MapPost("/ambit/command", Func<HttpRequest, Task<IResult>>(fun req -> task {
            bindClientHint req |> ignore
            use reader = new StreamReader(req.Body)
            let! body = reader.ReadToEndAsync()
            match BrowserRequestCreds.tryCookieCaller req with
            | None -> return Results.Unauthorized()
            | Some caller ->
                let! live =
                    CoreMailbox.isAdmitted persistence.Core.host caller
                    |> Async.StartAsTask
                if not live then
                    return Results.Unauthorized()
                else
                    return!
                        Api.postCommand
                            (fun request ->
                                CoreMailbox.startActor
                                    persistence.Core.host
                                    caller
                                    request)
                            (boundChanges persistence caller)
                            body
                        |> Async.StartAsTask
        })) |> ignore

    let private prepareGitSave (persistence: PersistenceContext) () = async {
        let handle = parseBound persistence
        return!
            SavePrep.syncDataDir
                persistence.Mode
                persistence.DbStatus
                (fun () -> handle.getState ())
                (fun () -> CoreMailbox.flushSnapshot persistence.Core.host)
                (fun () -> CoreMailbox.getEventId persistence.Core.host)
                persistence.DataDir
    }

    let private registerSaveRoutes (routes: AppShellContext) =
        let this = routes.AmbitApp
        let persistence = routes.Persistence
        this.MapGet("/ambit/capabilities", Func<HttpRequest, IResult>(fun req ->
            if this.Auth.IsAuthenticated req then
                Api.getCapabilities persistence.DataDir
            else
                Results.Unauthorized()
        )) |> ignore
        this.MapPost("/ambit/file-status", Func<HttpRequest, Task<IResult>>(fun req -> task {
            if not (this.Auth.IsAuthenticated req) then
                return Results.Unauthorized()
            else
                use reader = new StreamReader(req.Body)
                let! body = reader.ReadToEndAsync()
                return Api.postFileStatus persistence.DataDir body
        })) |> ignore
        this.MapGet("/ambit/file", Func<HttpRequest, Task<IResult>>(fun req -> task {
            if not (this.Auth.IsAuthenticated req) then
                return Results.Unauthorized()
            else
                match req.Query.TryGetValue("path") with
                | false, _ -> return Results.BadRequest({| error = "path is required" |})
                | true, value ->
                    return Api.getImportFile persistence.DataDir (string value)
        })) |> ignore
        this.MapPost("/ambit/file/parse", Func<HttpRequest, Task<IResult>>(fun req -> task {
            if not (this.Auth.IsAuthenticated req) then
                return Results.Unauthorized()
            else
                use reader = new StreamReader(req.Body)
                let! body = reader.ReadToEndAsync()
                return!
                    Api.postParseFile
                        (parseBound persistence)
                        persistence.DataDir
                        body
                    |> Async.StartAsTask
        })) |> ignore
        this.MapPost("/ambit/save", Func<HttpRequest, Task<IResult>>(fun req -> task {
            if not (this.Auth.IsAuthenticated req) then
                return Results.Unauthorized()
            else
                let clientHint = bindClientHint req
                let prepare = prepareGitSave persistence
                return!
                    Api.gitSave prepare persistence.DataDir clientHint
                    |> Async.StartAsTask
        })) |> ignore

    let private mailboxIsAuthenticated persistence req =
        match BrowserRequestCreds.tryCookieCaller req with
        | None -> false
        | Some caller ->
            CoreMailbox.isAdmitted
                persistence.Core.host
                caller
            |> Async.RunSynchronously

    let private withMailboxAdmit (this: AmbitApp) (persistence: PersistenceContext) =
        { this with
            Auth =
                { this.Auth with
                    IsAuthenticated = mailboxIsAuthenticated persistence } }
    let registerPersistenceAndRoutes (this: AmbitApp) : AmbitApp =
        let persistenceModeResult =
            this.Config.["Persistence:Mode"]
            |> Option.ofObj
            |> Option.defaultValue ""
            |> DatabaseSetup.resolvePersistenceMode
        match this.DataDirResult, persistenceModeResult with
        | Error ex, _ ->
            registerStartupError this (ex.ToString())
            this
        | _, Error err ->
            registerStartupError this err
            this
        | Ok _, Ok persistenceMode ->
            let persistence = createPersistenceContext this persistenceMode
            let this = withMailboxAdmit this persistence
            let assets, stamps = RouteAppShell.createBuildStamps this
            let routes =
                {
                    AmbitApp = this
                    Assets = assets
                    Stamps = stamps
                    Persistence = persistence
                }
            registerAuthRoutes routes
            registerStateRoutes routes
            registerSaveRoutes routes
            registerErrorReportRoute routes
            let flushForGit () = async {
                let handle = parseBound persistence
                let! flushResult =
                    SavePrep.syncGitArtifacts
                        persistence.Mode
                        persistence.DbStatus
                        (fun () -> handle.getState ())
                        (fun () ->
                            CoreMailbox.flushSnapshot persistence.Core.host)
                        (fun () ->
                            CoreMailbox.getEventId persistence.Core.host)
                        persistence.DataDir
                match flushResult with
                | Ok _ -> return Ok ()
                | Error err -> return Error err
            }
            let reconcileGitPush label changedPaths =
                LazyLoadReconciliationServer.reconcileChangedPaths
                    (parseBound persistence)
                    persistence.DataDir
                    label
                    changedPaths
            GitGateway.registerRoutes
                { shell = routes
                  flush = flushForGit
                  reconcile = reconcileGitPush }
            LazyLoadReconciliationDiagnostics.registerRoute this
            GitGatewayDiagnostics.registerRoute this
            LazyLoadReconciliationServer.registerDirectoryRoute
                this
                (fun () -> parseBound persistence)
            LazyLoadReconciliationServer.registerAddedRoute
                this
                (fun () -> parseBound persistence)
            WorkspaceWebDav.registerRoutes this
            RouteAppShell.registerCssAndShellRoutes routes
            DailyGitSave.register this
            this
