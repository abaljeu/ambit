namespace Gambol.Server

open System.Threading
open Gambol.Shared

/// Dependencies for the long-lived Parse consumer thread (outside Core).
type ParseThreadDeps =
    { dataDir: string
      consumer: unit -> NodeId
      push: NodeId -> unit
      getGraph: unit -> Async<Result<Graph, string>>
      postOps: Op list -> Async<Result<unit, string>>
      finishParse: NodeId -> unit }

/// One long-lived Parse consumer thread: pull stack, run planParseFile
/// or Directory reconcile.
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

    let private alreadyParsed (graph: Graph) (nodeId: NodeId) =
        match Map.tryFind nodeId graph.nodes with
        | Some node when node.parseState = ParseState.Parsed -> true
        | _ -> false

    let private afterPost
        (deps: ParseThreadDeps)
        (nodeId: NodeId)
        (push: NodeId list)
        (posted: Result<unit, string>)
        =
        match posted with
        | Error err -> reportPost err
        | Ok () ->
            push |> List.iter deps.push
            deps.finishParse nodeId

    let private parseFile
        (deps: ParseThreadDeps)
        (graph: Graph)
        (fileId: NodeId)
        =
        async {
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
                return afterPost deps fileId [] posted
        }

    let private parseDirectory
        (deps: ParseThreadDeps)
        (graph: Graph)
        (directoryId: NodeId)
        =
        async {
            match
                DirectoryReconcile.planDirectoryReconcile
                    { dataDir = deps.dataDir
                      graph = graph
                      directoryId = directoryId }
            with
            | Error _ -> return ()
            | Ok planned ->
                let! posted = postContent deps planned.ops
                return afterPost deps directoryId planned.push posted
        }

    let private parseOne (deps: ParseThreadDeps) (nodeId: NodeId) =
        async {
            let! graphResult = deps.getGraph ()
            match graphResult with
            | Error _ -> return ()
            | Ok graph when alreadyParsed graph nodeId -> return ()
            | Ok graph ->
                match Map.tryFind nodeId graph.nodes with
                | Some { kind = Special File } ->
                    return! parseFile deps graph nodeId
                | Some { kind = Special (Directory | Workspace) } ->
                    return! parseDirectory deps graph nodeId
                | _ -> return ()
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
