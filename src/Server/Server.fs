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
        let relative = config.["DataDir"] |> Option.ofObj |> Option.defaultValue "../../data"
        let dataDir = Path.Combine(contentRoot, relative) |> Path.GetFullPath
        Directory.CreateDirectory(dataDir) |> ignore
        dataDir

    [<EntryPoint>]
    let main args =
        let options = WebApplicationOptions(
                        Args = args,
                        ContentRootPath = __SOURCE_DIRECTORY__,
                        WebRootPath = Path.Combine(__SOURCE_DIRECTORY__, "wwwroot"))
        let builder = WebApplication.CreateBuilder(options)
        let app = builder.Build()

        let dataDir = resolveDataDir app.Environment.ContentRootPath app.Configuration

        // One agent at a time — keyed by filename
        let mutable currentAgent: (string * FileAgent) option = None
        let agentLock = obj ()

        let getOrCreateAgent (filename: string) =
            lock agentLock (fun () ->
                match currentAgent with
                | Some (name, agent) when name = filename -> agent
                | Some (_, agent) ->
                    (agent :> IDisposable).Dispose()
                    let newAgent = new FileAgent(dataDir, filename)
                    currentAgent <- Some (filename, newAgent)
                    newAgent
                | None ->
                    let newAgent = new FileAgent(dataDir, filename)
                    currentAgent <- Some (filename, newAgent)
                    newAgent
            )

        app.UseDefaultFiles() |> ignore
        app.UseStaticFiles() |> ignore

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
