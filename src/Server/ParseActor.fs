namespace Gambol.Server

open System.Threading
open Gambol.Shared

/// Dependencies for the long-lived Parse actor loop (outside Core).
type ParseActorDeps =
    { dataDir: string
      consumer: unit -> NodeId
      getGraph: unit -> Async<Result<Graph, string>>
      postOps: Op list -> Async<Result<unit, string>> }

/// One long-lived Parse actor: pull stack, run planParseFile, post ops.
[<RequireQualifiedAccess>]
module ParseActor =

    let private parseOne (deps: ParseActorDeps) (fileId: NodeId) =
        async {
            let! graphResult = deps.getGraph ()
            match graphResult with
            | Error _ -> return ()
            | Ok graph ->
                match
                    DocumentPersistWrite.planParseFile
                        deps.dataDir
                        graph
                        fileId
                        None
                with
                | Error _ -> return ()
                | Ok [] -> return ()
                | Ok ops ->
                    let! _ = deps.postOps ops
                    return ()
        }

    let private loop (deps: ParseActorDeps) =
        while true do
            let fileId = deps.consumer ()
            parseOne deps fileId |> Async.RunSynchronously

    /// Hosts the perpetual consumer on a background thread.
    let start (deps: ParseActorDeps) =
        let thread = Thread(ThreadStart(fun () -> loop deps))
        thread.IsBackground <- true
        thread.Start()

    let postParseOps
        (handle: CoreChanges)
        (ops: Op list)
        : Async<Result<unit, string>> =
        async {
            let event = GraphOnlyChangePost.mint "Parse" ops
            match! handle.postGraphOnly event with
            | Ok _ -> return Ok ()
            | Error err -> return Error err
        }

    let graphFromHost
        (host: MailboxHost)
        : unit -> Async<Result<Graph, string>> =
        fun () ->
            async {
                match! CoreMailbox.tryGetState host with
                | Error err -> return Error err
                | Ok state -> return Ok state.graph
            }
