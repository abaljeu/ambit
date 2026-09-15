namespace Gambol.Server

open System
open System.IO
open Microsoft.AspNetCore.Http

[<RequireQualifiedAccess>]
module RouteAppShell =

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
            if File.Exists path then File.GetLastWriteTimeUtc path
            else DateTime.MinValue
        [
            assets.GambolHtml
            Path.Combine(webRoot, "Program.js")
            Path.Combine(webRoot, "Program.bundle.js")
            Path.Combine(webRoot, "Update.js")
            Path.Combine(webRoot, "style.css")
            assets.DefaultUserCss
            assets.CommandDockSvg
        ]
        |> List.map fileWriteUtc
        |> List.max

    let createBuildStamps (this: AmbitApp) : RouteAssets * BuildStamps =
        let webRoot = this.WebRootPath
        let assets = createRouteAssets webRoot
        let readPageArtifactUtc () = pageArtifactUtc webRoot assets
        let serverAssemblyPath =
            System.Reflection.Assembly.GetExecutingAssembly().Location
        if String.IsNullOrWhiteSpace serverAssemblyPath then
            failwith "Could not determine server assembly path for build timestamp."
        if not (File.Exists serverAssemblyPath) then
            failwithf
                "Could not read server assembly timestamp: missing file at '%s'."
                serverAssemblyPath
        // Deploy stamps freeze at startup; page stamps re-read wwwroot mtimes (Fable watch).
        let deployUtc =
            max (File.GetLastWriteTimeUtc(serverAssemblyPath)) (readPageArtifactUtc ())
        let processStartUtc = DateTime.UtcNow
        let torontoTz = TimeZoneInfo.FindSystemTimeZoneById("America/Toronto")
        let formatStamp (utc: DateTime) =
            TimeZoneInfo.ConvertTimeFromUtc(utc, torontoTz).ToString("yyyy-MM-dd HH:mm:ss")
            + " ET"
        let pageUtc () =
            let artifactUtc = readPageArtifactUtc ()
            if artifactUtc > DateTime.MinValue then artifactUtc else deployUtc
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
                    let artifactUtc = readPageArtifactUtc ()
                    if artifactUtc > DateTime.MinValue then formatStamp artifactUtc
                    else "unknown"
            PageBuildEpochSec = fun () -> pageUtc () |> epochSec
            DeployEpochSec = fun () -> epochSec processStartUtc
            InlineCommandDockSprite = inlineCommandDockSprite
        }

    let private dbStatusText (status: DatabaseSetup.DbStatus) =
        match status with
        | DatabaseSetup.DbStatus.Ok -> "ok"
        | DatabaseSetup.DbStatus.Mismatch1 -> "mismatch1"
        | DatabaseSetup.DbStatus.Mismatch2 -> "mismatch2"
        | DatabaseSetup.DbStatus.Absent -> "absent"

    let private serveUserCss (shell: AppShellContext) =
        let dataDir = shell.Persistence.DataDir
        let defaultUserCss = shell.Assets.DefaultUserCss
        let userPath = Path.Combine(Bookkeeping.systemDir dataDir, "user.css")
        let path = if File.Exists(userPath) then userPath else defaultUserCss
        if File.Exists(path) then Results.File(path, "text/css")
        else Results.NoContent()

    let private renderGambolHtml (shell: AppShellContext) (programFile: string) =
        let publicAssetBaseOpt = shell.AmbitApp.PublicAssetBaseOpt
        let assets = shell.Assets
        let stamps = shell.Stamps
        let dbStatus = shell.Persistence.DbStatus
        let raw = File.ReadAllText(assets.GambolHtml)
        let pageEpoch = stamps.PageBuildEpochSec ()
        let basePrefix =
            match publicAssetBaseOpt with None -> "" | Some url -> url
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
            | None -> sprintf "/ambit/%s?v=%d" programFile pageEpoch
            | Some baseUrl ->
                sprintf "%s/ambit/%s?v=%d" baseUrl programFile pageEpoch
        raw
            .Replace("href=\"/ambit/style.css\"", sprintf "href=\"%s\"" styleHref)
            .Replace("href=\"/ambit/user.css\"", sprintf "href=\"%s\"" userHref)
            .Replace("</head>", script)
            .Replace("<!-- command-dock-sprite -->", stamps.InlineCommandDockSprite ())
            .Replace("src=\"/ambit/Program.js\"", sprintf "src=\"%s\"" programSrc)

    let private registerAppShellRoute (shell: AppShellContext) =
        let this = shell.AmbitApp
        let persistence = shell.Persistence
        let serveAmbitHtml (ctx: HttpContext) =
            ctx.Response.Headers.CacheControl <- "no-cache, no-store, must-revalidate"
            ctx.Response.Headers.Pragma <- "no-cache"
            ctx.Response.Headers.Expires <- "0"
            let programFile =
                match ctx.Request.Query.TryGetValue("debug") with
                | true, value when value.ToString() = "1" -> "Program.js"
                | _ -> "Program.bundle.js"
            let html = renderGambolHtml shell programFile
            Results.Content(html, "text/html")
        let serveAmbitApp (ctx: HttpContext) : IResult =
            if this.Auth.IsAuthenticated ctx.Request then
                serveAmbitHtml ctx
            elif this.Auth.ExpectedUser = "" && this.Auth.ExpectedPass = "" then
                match
                    RouteAuthentication.loginThenSetCookie
                        persistence.Core.host
                        this.Auth
                        ctx.Response
                    |> Async.RunSynchronously
                with
                | Ok () -> serveAmbitHtml ctx
                | Error _ -> Results.Redirect("/ambit/login")
            else
                Results.Redirect("/ambit/login")
        this.MapGet("/ambit", Func<HttpContext, IResult>(serveAmbitApp))
        |> ignore

    let registerCssAndShellRoutes (shell: AppShellContext) =
        shell.AmbitApp.MapGet("/ambit/user.css", Func<IResult>(fun () ->
            serveUserCss shell
        )) |> ignore
        registerAppShellRoute shell
