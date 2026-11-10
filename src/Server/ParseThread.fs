namespace Gambol.Server

open System.Threading
open Gambol.Shared

/// Dependencies for the long-lived Parse consumer thread (outside Core).
type ParseThreadDeps =
    { dataDir: string
      consumer: unit -> NodeId option
      push: NodeId -> unit
      getGraph: unit -> Async<Result<Graph, string>>
      postOps: NodeId -> Op list -> Async<Result<unit, string>>
      markUnparsed: NodeId -> unit
      finishParse: NodeId -> unit }

/// One long-lived Parse consumer thread: pull stack, run planParseFile
/// or Directory reconcile.
[<RequireQualifiedAccess>]
module ParseThread =

    type private InnerDeps =
        { dataDir: string
          consumer: unit -> NodeId option
          push: NodeId -> unit
          getGraph: unit -> Async<Result<Graph, string>>
          postOps: NodeId -> Op list -> Async<Result<unit, string>>
          finishParse: NodeId -> unit }

    let private reportPost (err: string) =
        eprintfn "ParseThread: content post failed: %s" err

    // Queue processing is currently wrong. It caused a whole-workspace
    // reconcile that exceeded the Azure Free plan CPU quota: 60%
    // short-window, 5% daily average. This is not a decision to drop the
    // requeue step. Re-enable once queue processing is fixed (pending
    // github-transport Load ticket: move Load onto a paced Parse thread).
    let private requeueOnUnparsed = false

    let private enqueueIfRequeue
        (push: NodeId -> unit)
        (ids: NodeId list)
        =
        // Queue processing is currently wrong. It caused a whole-workspace
        // reconcile that exceeded the Azure Free plan CPU quota: 60%
        // short-window, 5% daily average. This is not a decision to drop
        // the requeue step. Re-enable once queue processing is fixed
        // (pending github-transport Load ticket: move Load onto a paced
        // Parse thread). Unparsed is still set by the caller.
        if requeueOnUnparsed then
            ids |> List.iter push

    let private alreadyParsed (graph: Graph) (nodeId: NodeId) =
        match Map.tryFind nodeId graph.nodes with
        | Some node when
            node.documentState.parseState = ParseState.Parsed -> true
        | _ -> false

    let private afterPost
        (deps: InnerDeps)
        (nodeId: NodeId)
        (push: NodeId list)
        (posted: Result<unit, string>)
        =
        match posted with
        | Error err -> reportPost err
        | Ok () ->
            // requeueOnUnparsed is false: Unparsed stays set, and these
            // ids are not added to the Parse queue. Queue processing is
            // currently wrong. It caused a whole-workspace reconcile that
            // exceeded the Azure Free plan CPU quota: 60% short-window,
            // 5% daily average. This is not a decision to drop the requeue
            // step. Re-enable once queue processing is fixed (pending
            // github-transport Load ticket: move Load onto a paced Parse
            // thread).
            enqueueIfRequeue deps.push push
            deps.finishParse nodeId

    let private parseFile
        (deps: InnerDeps)
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
                let! posted = deps.postOps fileId ops
                return afterPost deps fileId [] posted
        }

    let private reconcileDirectory
        (deps: InnerDeps)
        (graph: Graph)
        (directoryId: NodeId)
        =
        match
            NodeLockPool.withLock graph directoryId (fun () ->
                DirectoryReconcile.planDirectoryReconcile
                    { dataDir = deps.dataDir
                      graph = graph
                      directoryId = directoryId
                      discover =
                        DocumentPersistPath.discoverImmediateMembers })
        with
        | Error NodeLockRejection.HolderHoldsLock ->
            Error "holder already holds a lock"
        | Ok result -> result

    let private parseDirectory
        (deps: InnerDeps)
        (graph: Graph)
        (directoryId: NodeId)
        =
        async {
            match reconcileDirectory deps graph directoryId with
            | Error _ -> return ()
            | Ok planned ->
                let! posted = deps.postOps directoryId planned.ops
                return afterPost deps directoryId planned.push posted
        }

    let private parseOne (deps: InnerDeps) (nodeId: NodeId) =
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

    let private loop (deps: InnerDeps) =
        let rec run () =
            match deps.consumer () with
            | None -> ()
            | Some fileId ->
                parseOne deps fileId |> Async.RunSynchronously
                run ()
        run ()

    /// Hosts the consumer. Reset joins this thread after the stack stops.
    let private startInner (deps: InnerDeps) =
        let thread = Thread(ThreadStart(fun () -> loop deps))
        thread.IsBackground <- true
        thread.Name <- "gambol-parse"
        thread.Start()
        let gate = obj ()
        let joined = ref false
        let join () =
            lock gate (fun () ->
                if not joined.Value then
                    joined.Value <- true
                    thread.Join())
        ServerRunRegistry.registerParseJoin join

    let start (deps: ParseThreadDeps) =
        startInner
            { dataDir = deps.dataDir
              consumer = deps.consumer
              push = deps.push
              getGraph = deps.getGraph
              postOps = deps.postOps
              finishParse = deps.finishParse }

    /// Posts NodeParsing for this node, then the graph Change.
    /// Notices go through postNotice, which boot hands in.
    let postParseOps
        (postNotice: Notice -> unit)
        (handle: CoreChangesDoor)
        (nodeId: NodeId)
        (ops: Op list)
        : Async<Result<unit, string>> =
        async {
            postNotice (Notice.NodeParsing nodeId)
            if List.isEmpty ops then
                return Ok ()
            else
                let event = GraphOnlyChangePost.mint "Parse" ops
                match! CoreDoor.postGraphOnly handle event with
                | Ok _ -> return Ok ()
                | Error err ->
                    postNotice (Notice.ParseFailed(nodeId, err))
                    return Error err
        }

    let graphFromHost
        (host: MailboxHost)
        : unit -> Async<Result<Graph, string>> =
        fun () ->
            async {
                match! CoreMailboxApi.tryGraphAtEvent host with
                | Error err -> return Error err
                | Ok state -> return Ok state.graph
            }

    /// Builds the parse stack and starts the consumer. Callers do not receive push.
    let bootCore (coreBoot: CoreBoot) : CoreRuntime =
        let rawPush, consumer = ParseStack.create ()
        let core, push = CoreRuntime.createPublishing coreBoot rawPush
        let parseHandle =
            CoreDoor.create true core.host core.parseCaller
        let postNotice = CoreMailboxApi.postNotice core.host
        startInner
            { dataDir = coreBoot.DataDir
              consumer = consumer
              push = push
              getGraph = graphFromHost core.host
              postOps = postParseOps postNotice parseHandle
              finishParse =
                fun nodeId ->
                    postNotice (Notice.NodeParsed nodeId) }
        core
