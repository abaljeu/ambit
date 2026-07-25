namespace Gambol.Server

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Server.Kestrel.Core
open Microsoft.AspNetCore.StaticFiles
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Gambol.Shared


module Main =

    type ServerLocation =
        {
            ContentRoot: string
            OnAzure: bool
            HomeDir: string
        }

    let resolveDataDir (contentRoot: string) (config: IConfiguration) =
        let onAzure = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") |> Option.ofObj |> Option.isSome
        let home = Environment.GetEnvironmentVariable("HOME") |> Option.ofObj |> Option.defaultValue "/home"
        let configured = config.["DataDir"] |> Option.ofObj
        DataDir.resolve contentRoot configured onAzure home

    let serverLocation () =
        // Dev: __SOURCE_DIRECTORY__ has wwwroot next to it (Fable output).
        // Published: wwwroot is copied into the publish output dir alongside the DLL.
        let src = __SOURCE_DIRECTORY__
        let contentRoot =
            if Directory.Exists(Path.Combine(src, "wwwroot")) then
                src
            else
                AppContext.BaseDirectory
        let onAzure =
            Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")
            |> Option.ofObj
            |> Option.isSome
        let homeDir =
            Environment.GetEnvironmentVariable("HOME")
            |> Option.ofObj
            |> Option.defaultValue "/home"
        { ContentRoot = contentRoot; OnAzure = onAzure; HomeDir = homeDir }

    let createBuilder args location =
        let options = WebApplicationOptions(
                        Args = args,
                        ContentRootPath = location.ContentRoot,
                        WebRootPath = Path.Combine(location.ContentRoot, "wwwroot"))
        WebApplication.CreateBuilder(options)

    /// Git smart HTTP receive-pack can exceed Kestrel's default 30 MB body limit.
    let configureKestrelLimits (builder: WebApplicationBuilder) =
        builder.Services.Configure<KestrelServerOptions>(fun (options: KestrelServerOptions) ->
            options.Limits.MaxRequestBodySize <- Nullable(100L * 1024L * 1024L))
        |> ignore

    let addAppSettings location (builder: WebApplicationBuilder) =
        // Env-specific appsettings.
        // In Azure, read from persistent /home (not the site wwwroot) so config can survive redeploys.
        if location.OnAzure then
            let settingsPath = Path.Combine(location.HomeDir, "appsettings.json")
            builder.Configuration.AddJsonFile(settingsPath, optional = true)
                |> ignore
        let envFile = "appsettings." + builder.Environment.EnvironmentName + ".json"
        let envFilePath =
            if location.OnAzure then Path.Combine(location.HomeDir, envFile)
            else envFile
        builder.Configuration.AddJsonFile(envFilePath, optional = true)
            |> ignore

    let bindConfiguredPort port (app: WebApplication) =
        port |> Option.iter (fun p -> app.Urls.Add(sprintf "http://0.0.0.0:%s" p))

    let logHeadPresence hasHead =
        eprintfn
            "Gambol: server hasHead=%b (Environment.UserInteractive=%b)."
            hasHead
            Environment.UserInteractive

    let registerProductionConfigGuard location (app: WebApplication) =
        // Production without appsettings.Production.json: start but show error to every request
        let expectedProductionConfigPath =
            let filename = "appsettings.Production.json"
            if location.OnAzure then Path.Combine(location.HomeDir, filename)
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

    let resolvePublicAssetBase location (config: IConfiguration) (app: WebApplication) =
        // Absolute origin for CSS + Program.js when the app page is served from another host.
        let publicAssetBaseOpt =
            let raw = config.["PublicAssetBase"] |> Option.ofObj |> Option.defaultValue ""
            let trimmed = raw.Trim().TrimEnd('/')
            let fromConfig = if String.IsNullOrWhiteSpace(trimmed) then None else Some trimmed
            match fromConfig with
            | Some b -> Some b
            | None ->
                if location.OnAzure && app.Environment.EnvironmentName = "Production" then
                    Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME")
                    |> Option.ofObj
                    |> Option.map (fun h -> ("https://" + h.Trim()).TrimEnd('/'))
                else None

        publicAssetBaseOpt

    let resolveJsModuleCorsOrigins (config: IConfiguration) publicAssetBaseOpt =
        // Comma-separated origins for Access-Control-Allow-Origin on /ambit/*.js (ES modules cross-origin).
        // If empty but PublicAssetBase is in effect, default to "*" so proxy + Azure works without extra config.
        let rawCors = config.["JsModuleCorsOrigins"] |> Option.ofObj |> Option.defaultValue ""
        let parsed =
            if String.IsNullOrWhiteSpace(rawCors) then
                [||]
            else
                rawCors.Split(',')
                |> Array.map (fun s -> s.Trim())
                |> Array.filter (fun s -> s.Length > 0)
        if parsed.Length > 0 then parsed
        elif Option.isSome publicAssetBaseOpt then [| "*" |]
        else [||]

    let applyNoCacheHeaders (resp: HttpResponse) =
        resp.Headers.CacheControl <- "no-cache, no-store, must-revalidate"
        resp.Headers.Pragma <- "no-cache"
        resp.Headers.Expires <- "0"

    let applyCorsHeaders (origins: string[]) (ctx: HttpContext) =
        if origins.Length > 0 then
            let allowAny = origins |> Array.exists (fun o -> o = "*")
            if allowAny then
                ctx.Response.Headers.Append("Access-Control-Allow-Origin", "*")
            else
                match ctx.Request.Headers.TryGetValue("Origin") with
                | true, originVals ->
                    let origin = originVals.ToString()
                    if origins |> Array.contains origin then
                        ctx.Response.Headers.Append("Access-Control-Allow-Origin", origin)
                        ctx.Response.Headers.Append("Vary", "Origin")
                | _ -> ()

    let useAmbitStaticFiles (jsModuleCorsOrigins: string[]) (app: WebApplication) =
        // Serve wwwroot under /ambit/ so assets like /ambit/Program.js work.
        let ambitOpts = StaticFileOptions(
            RequestPath = PathString("/ambit"),
            OnPrepareResponse = Action<StaticFileResponseContext>(fun ctx ->
                let path = ctx.Context.Request.Path.Value
                if path.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".js.map", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) then
                    applyNoCacheHeaders ctx.Context.Response
                    applyCorsHeaders jsModuleCorsOrigins ctx.Context))
        app.UseStaticFiles(ambitOpts) |> ignore

    let redirectAmbitSlash (app: WebApplication) =
        // Do not register both MapGet("/ambit") and MapGet("/ambit/") — routing treats them as ambiguous for the same request.
        // Normalize trailing slash to the canonical app URL.
        app.Use(fun (ctx: HttpContext) (next: RequestDelegate) ->
            if HttpMethods.IsGet(ctx.Request.Method) && ctx.Request.Path.Equals(PathString("/ambit/")) then
                ctx.Response.Redirect("/ambit", false)
                Task.CompletedTask
            else
                next.Invoke(ctx)
        )
        |> ignore

    let useDevelopmentLatency (app: WebApplication) =
        // Development only: handle first, then delay before completing the response.
        // Simulates RTT after work so concurrent uploads overlap delays.
        if app.Environment.EnvironmentName = "Development" then
            app.Use(fun (ctx: HttpContext) (next: RequestDelegate) ->
                task {
                    do! next.Invoke(ctx)
                    do! Task.Delay(1000)
                } :> Task)
            |> ignore

    let mapSourceFiles hasHead (app: WebApplication) =
        // Headed only: serve Client/Shared sources for browser source maps.
        if hasHead then
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

    let configureStaticAssetsAndSources
        hasHead
        location
        (config: IConfiguration)
        (app: WebApplication)
        =
        app.UseDefaultFiles() |> ignore
        app.UseStaticFiles() |> ignore
        let publicAssetBaseOpt = resolvePublicAssetBase location config app
        let jsModuleCorsOrigins = resolveJsModuleCorsOrigins config publicAssetBaseOpt

        useAmbitStaticFiles jsModuleCorsOrigins app
        redirectAmbitSlash app
        useDevelopmentLatency app
        mapSourceFiles hasHead app

        publicAssetBaseOpt

    let configureApplication hasHead location (app: WebApplication) =
        let config = app.Configuration
        let dataDirResult =
            try Ok (resolveDataDir app.Environment.ContentRootPath config)
            with ex -> Error ex
        // Early: capture status + body for errors / mutating / DAV / git.
        // Log file is wiped fresh on each process start; lives under dataDir/SYSTEM/.
        let httpLogFile =
            match dataDirResult with
            | Ok dataDir -> HttpResponseLog.register dataDir app
            | Error _ -> ""
        let auth = RouteRegistration.createAuthentication config
        let publicAssetBaseOpt =
            configureStaticAssetsAndSources hasHead location config app

        RouteRegistration.registerPersistenceAndRoutes
            config
            auth
            publicAssetBaseOpt
            dataDirResult
            app
            httpLogFile

    [<EntryPoint>]
    let main args =
        let port = Environment.GetEnvironmentVariable("PORT") |> Option.ofObj
        let hasHead = HeadPresence.detectHasHead ()
        let location = serverLocation ()
        let builder = createBuilder args location

        addAppSettings location builder
        configureKestrelLimits builder

        let app = builder.Build()
        bindConfiguredPort port app
        logHeadPresence hasHead
        registerProductionConfigGuard location app
        configureApplication hasHead location app
        app.Run()

        0 // Exit code
