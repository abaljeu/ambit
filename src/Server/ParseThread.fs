namespace Gambol.Server

open System.Threading
open Gambol.Shared

/// Dependencies for the long-lived Parse consumer thread (outside Core).
type ParseThreadDeps =
    { dataDir: string
      consumer: unit -> NodeId
      getGraph: unit -> Async<Result<Graph, string>>
      postOps: Op list -> Async<Result<unit, string>>
      finishParse: NodeId -> unit }

/// One long-lived Parse consumer thread: pull stack, run planParseFile.
[<RequireQualifiedAccess>]
module ParseThread =

    let private postContent (deps: ParseThreadDeps) (ops: Op list) =
        async {
            if List.isEmpty ops then
                return Ok ()
            else
                return! deps.postOps ops
        }

    let private reportPost (err: string) =
        eprintfn "ParseThread: content post failed: %s" err

    let private parseOne (deps: ParseThreadDeps) (fileId: NodeId) =
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
                | Ok ops ->
                    let! posted = postContent deps ops
                    match posted with
                    | Error err -> return reportPost err
                    | Ok () -> return deps.finishParse fileId
        }

    let private loop (deps: ParseThreadDeps) =
        while true do
            let fileId = deps.consumer ()
            parseOne deps fileId |> Async.RunSynchronously

    /// Hosts the perpetual consumer on a background thread.
    let start (deps: ParseThreadDeps) =
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
