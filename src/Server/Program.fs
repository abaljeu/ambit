namespace Gambol.Server

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Gambol.Shared

/// Marker type for WebApplicationFactory<Program> in tests.
type Program = class end

module Encode = Thoth.Json.Newtonsoft.Encode

/// Mutable server state behind a lock.
type ServerState =
    { mutable graph: Graph
      mutable revision: Revision
      mutable transactionLog: Change list }

module ServerState =
    let snapshotFilename = "gambol-snapshot.txt"

    let resolveDataDir (contentRoot: string) (config: IConfiguration) =
        let relative = config.["DataDir"] |> Option.ofObj |> Option.defaultValue "../../data"
        let dataDir = Path.Combine(contentRoot, relative) |> Path.GetFullPath
        Directory.CreateDirectory(dataDir) |> ignore
        dataDir

    let create (dataDir: string) =
        let snapshotPath = Path.Combine(dataDir, snapshotFilename)
        let graph =
            if File.Exists(snapshotPath) then
                let text = File.ReadAllText(snapshotPath)
                Snapshot.read text
            else
                Graph.create ()

        { graph = graph
          revision = Revision 0
          transactionLog = [] }

    let lock' = obj ()

    let withLock f state =
        lock lock' (fun () -> f state)

module Api =
    let getState (state: ServerState) =
        Encode.toString 0 (
            Thoth.Json.Core.Encode.object
                [ "revision", Serialization.encodeRevision state.revision
                  "graph", Serialization.encodeGraph state.graph ]
        )

module Main =
    [<EntryPoint>]
    let main args =
        let builder = WebApplication.CreateBuilder(args)
        let app = builder.Build()

        let dataDir = ServerState.resolveDataDir app.Environment.ContentRootPath app.Configuration
        let state = ServerState.create dataDir

        app.UseDefaultFiles() |> ignore
        app.UseStaticFiles() |> ignore

        app.MapGet("/state", Func<IResult>(fun () ->
            let json = ServerState.withLock Api.getState state
            Results.Content(json, "application/json")
        )) |> ignore

        app.Run()

        0 // Exit code
