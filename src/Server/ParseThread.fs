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

    let private postContent
        (deps: ParseThreadDeps)
        (nodeId: NodeId)
        (ops: Op list)
        =
        deps.postOps nodeId ops

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
        (deps: ParseThreadDeps)
        (nodeId: NodeId)
        (push: NodeId list)
        (posted: Result<unit, string>)
        =
        match posted with
        | Error err -> reportPost err
        | Ok () ->
            push |> List.iter deps.markUnparsed
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
                let! posted = postContent deps fileId ops
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
                      directoryId = directoryId
                      discover =
                        DocumentPersistPath.discoverImmediateMembers }
            with
            | Error _ -> return ()
            | Ok planned ->
                let! posted = postContent deps directoryId planned.ops
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
        let rec run () =
            match deps.consumer () with
            | None -> ()
            | Some fileId ->
                parseOne deps fileId |> Async.RunSynchronously
                run ()
        run ()

    /// Hosts the consumer. Reset joins this thread after the stack stops.
    let start (deps: ParseThreadDeps) =
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

    /// Posts NodeParsing for this node, then the graph Change.
    let postParseOps
        (handle: CoreChangesDoor)
        (nodeId: NodeId)
        (ops: Op list)
        : Async<Result<unit, string>> =
        async {
            CoreMailboxApi.postNotice
                handle.host
                (Notice.NodeParsing nodeId)
            if List.isEmpty ops then
                return Ok ()
            else
                let event = GraphOnlyChangePost.mint "Parse" ops
                match! CoreDoor.postGraphOnly handle event with
                | Ok _ -> return Ok ()
                | Error err ->
                    CoreMailboxApi.postNotice
                        handle.host
                        (Notice.ParseFailed(nodeId, err))
                    return Error err
        }

    let graphFromHost
        (host: MailboxHost)
        : unit -> Async<Result<Graph, string>> =
        fun () ->
            async {
                match! CoreMailboxApi.tryGetState host with
                | Error err -> return Error err
                | Ok state -> return Ok state.graph
            }

    /// Builds the parse stack and starts the consumer. Callers do not receive push.
    let bootCore (coreBoot: CoreBoot) : CoreRuntime =
        let rawPush, consumer = ParseStack.create ()
        let core, push = CoreRuntime.createPublishing coreBoot rawPush
        let parseHandle =
            CoreDoor.create true core.host core.parseCaller
        start
            { dataDir = coreBoot.DataDir
              consumer = consumer
              push = push
              getGraph = graphFromHost core.host
              postOps = postParseOps parseHandle
              markUnparsed =
                fun nodeId ->
                    CoreMailboxApi.postNotice
                        core.host
                        (Notice.NodeUnparsed nodeId)
              finishParse =
                fun nodeId ->
                    CoreMailboxApi.postNotice
                        core.host
                        (Notice.NodeParsed nodeId) }
        core
