namespace Gambol.Server

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration

module Main =

    let private sanitizeFilename (filename: string) =
        let name = Path.GetFileName(filename)
        if String.IsNullOrWhiteSpace(name) || name <> filename then
            None
        else
            Some name

    let resolveDataDir (contentRoot: string) (config: IConfiguration) =
        let onAzure = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") |> Option.ofObj |> Option.isSome
        let dataDir =
            if onAzure then
                // Azure App Service: use persistent /home mount; ignore DataDir config
                let home = Environment.GetEnvironmentVariable("HOME") |> Option.ofObj |> Option.defaultValue "/home"
                Path.Combine(home, "site", "data")
            else
                let relative = config.["DataDir"] |> Option.ofObj |> Option.defaultValue "../../data"
                if Path.IsPathRooted(relative) then relative
                else Path.Combine(contentRoot, relative) |> Path.GetFullPath
        Directory.CreateDirectory(dataDir) |> ignore
        dataDir

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
        let app = builder.Build()
        port |> Option.iter (fun p -> app.Urls.Add(sprintf "http://0.0.0.0:%s" p))

        let dataDirResult =
            try Ok (resolveDataDir app.Environment.ContentRootPath app.Configuration)
            with ex -> Error ex

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
            // One agent at a time — keyed by filename
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

            // GET /gambol/state → JSON { revision, graph }
            app.MapGet("/gambol/state", Func<Task<IResult>>(fun () -> task {
                let agent = getOrCreateAgent "gambol"
                return! Api.getState agent |> Async.StartAsTask
            })) |> ignore

            // POST /gambol/changes → JSON { revision, graph } or 400
            app.MapPost("/gambol/changes", Func<HttpRequest, Task<IResult>>(fun req -> task {
                use reader = new StreamReader(req.Body)
                let! body = reader.ReadToEndAsync()
                let agent = getOrCreateAgent "gambol"
                return! Api.postChange agent body |> Async.StartAsTask
            })) |> ignore

            // GET /gambol → serve gambol.html (URL stays as /gambol so client reads filename correctly)
            let gambolHtml = Path.Combine(app.Environment.WebRootPath, "gambol.html")
            app.MapGet("/gambol", Func<IResult>(fun () -> Results.File(gambolHtml, "text/html"))) |> ignore

        app.Run()

        0 // Exit code
