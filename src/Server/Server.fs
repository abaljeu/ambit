namespace Gambol.Server

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Gambol.Shared

/// Marker type for WebApplicationFactory<Program> in tests.
type Program = class end

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

/// Mutable server state behind a lock.
type ServerState =
    { mutable state: State
      mutable revision: Revision
      dataDir: string
      mutable snapshotFile: string }

module ServerState =
    let defaultSnapshotFile = "gambol-snapshot.txt"

    let private sanitizeFilename (filename: string) =
        let name = Path.GetFileName(filename)
        if String.IsNullOrWhiteSpace(name) || name <> filename then
            None
        else
            Some name

    let private loadGraphForFile (dataDir: string) (snapshotFile: string) =
        let snapshotPath = Path.Combine(dataDir, snapshotFile)
        if File.Exists(snapshotPath) then
            let text = File.ReadAllText(snapshotPath)
            Snapshot.read text
        else
            Graph.create ()

    let resolveDataDir (contentRoot: string) (config: IConfiguration) =
        let relative = config.["DataDir"] |> Option.ofObj |> Option.defaultValue "../../data"
        let dataDir = Path.Combine(contentRoot, relative) |> Path.GetFullPath
        Directory.CreateDirectory(dataDir) |> ignore
        dataDir

    let create (dataDir: string) (snapshotFile: string) =
        let graph = loadGraphForFile dataDir snapshotFile

        { state = { graph = graph; history = History.empty }
          revision = Revision 0
          dataDir = dataDir
          snapshotFile = snapshotFile }

    let lock' = obj ()

    let withLock f state =
        lock lock' (fun () -> f state)

    let switchSnapshotFile (filename: string) (serverState: ServerState) : bool =
        match sanitizeFilename filename with
        | None -> false
        | Some safeName ->
            let graph = loadGraphForFile serverState.dataDir safeName
            serverState.state <- { graph = graph; history = History.empty }
            serverState.revision <- Revision.Zero
            serverState.snapshotFile <- safeName
            true

module Api =
    let private encodeStateJson (serverState: ServerState) =
        Encode.toString 0 (
            Thoth.Json.Core.Encode.object
                [ "revision", Serialization.encodeRevision serverState.revision
                  "graph", Serialization.encodeGraph serverState.state.graph ]
        )

    let private jsonResult json =
        Results.Content(json, "application/json")

    let private submitDecoder =
        Thoth.Json.Core.Decode.object (fun get ->
            get.Required.Field "clientRevision" Serialization.decodeRevision,
            get.Required.Field "change" Serialization.decodeChange)

    let getState (serverState: ServerState) =
        encodeStateJson serverState |> jsonResult

    let submit (body: string) (serverState: ServerState) =
        match Decode.fromString submitDecoder body with
        | Error err ->
            Results.BadRequest({| error = $"Invalid JSON: {err}" |})
        | Ok (_clientRevision, change) ->
            match History.applyChange change serverState.state with
            | ApplyResult.Invalid(_, msg) ->
                Results.BadRequest({| error = msg |})
            | ApplyResult.Unchanged _ ->
                encodeStateJson serverState |> jsonResult
            | ApplyResult.Changed newState ->
                serverState.state <- newState
                serverState.revision <- Revision(serverState.revision.Value + 1)
                encodeStateJson serverState |> jsonResult

    let private saveDecoder =
        Thoth.Json.Core.Decode.object (fun get ->
            get.Optional.Field "filename" Thoth.Json.Core.Decode.string)

    let save (body: string) (serverState: ServerState) =
        let filenameOpt =
            if String.IsNullOrWhiteSpace(body) then None
            else
                match Decode.fromString saveDecoder body with
                | Error _ -> None
                | Ok opt -> opt

        let filename = filenameOpt |> Option.defaultValue serverState.snapshotFile
        let path = Path.Combine(serverState.dataDir, filename)

        try
            let text = Snapshot.write serverState.state.graph
            File.WriteAllText(path, text)
            serverState.snapshotFile <- filename

            Encode.toString 0 (
                Thoth.Json.Core.Encode.object
                    [ "success", Thoth.Json.Core.Encode.bool true
                      "snapshotFile", Thoth.Json.Core.Encode.string filename ])
            |> jsonResult
        with ex ->
            Results.Json({| success = false; error = ex.Message |}, statusCode = 500)

module Main =
    [<EntryPoint>]
    let main args =
        let builder = WebApplication.CreateBuilder(args)
        let app = builder.Build()

        let dataDir = ServerState.resolveDataDir app.Environment.ContentRootPath app.Configuration
        let snapshotFile =
            app.Configuration.["SnapshotFile"]
            |> Option.ofObj
            |> Option.defaultValue ServerState.defaultSnapshotFile
        let state = ServerState.create dataDir snapshotFile

        app.UseDefaultFiles() |> ignore
        app.UseStaticFiles() |> ignore

        app.MapGet("/state", Func<IResult>(fun () ->
            ServerState.withLock Api.getState state
        )) |> ignore

        app.MapPost("/submit", Func<HttpRequest, Task<IResult>>(fun req -> task {
            use reader = new StreamReader(req.Body)
            let! body = reader.ReadToEndAsync()
            return ServerState.withLock (Api.submit body) state
        })) |> ignore

        app.MapPost("/save", Func<HttpRequest, Task<IResult>>(fun req -> task {
            use reader = new StreamReader(req.Body)
            let! body = reader.ReadToEndAsync()
            return ServerState.withLock (Api.save body) state
        })) |> ignore

        app.MapGet("/{filename}", Func<string, IResult>(fun filename ->
            let switched = ServerState.withLock (ServerState.switchSnapshotFile filename) state
            if not switched then
                Results.BadRequest({| error = "Invalid filename" |})
            else
                let indexPath = Path.Combine(app.Environment.WebRootPath, "index.html")
                Results.File(indexPath, "text/html")
        )) |> ignore

        app.Run()

        0 // Exit code
