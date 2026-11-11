namespace Gambol.Server

open Gambol.Shared

[<RequireQualifiedAccess>]
type GithubTransportOperation =
    | Load
    | Save

type GithubTransportGit =
    { pullTracked:
        string -> WorkspaceTrackedBranch -> Result<string, string>
      commitAll:
        string ->
        string ->
        string option ->
        Result<WorkspaceTrackedBranch * string, string>
      pushTracked:
        string ->
        WorkspaceTrackedBranch ->
        string ->
        Result<string, string> }

type GithubTransportActorDependencies =
    { git: GithubTransportGit }

/// Git Load and Git Save for a person Load or Save.
[<RequireQualifiedAccess>]
module GithubTransportActor =

    let private transportActorName =
        function
        | GithubTransportOperation.Load -> "github-load"
        | GithubTransportOperation.Save -> "github-save"

    let peerName operation =
        PeerActorName(transportActorName operation)

    let private workspaceFromFocus dataDir graph focusId =
        let workspaceId = GraphQuery.enclosingWorkspace graph focusId
        match workspaceId with
        | None -> Error "Focus is not in a Workspace."
        | Some id ->
            match
                Map.tryFind id graph.nodes
                |> Option.bind (fun node -> Filename.tryValue node.name),
                DocumentPersistPath.workspaceRootFor dataDir graph focusId
            with
            | Some _, Some root -> Ok(id, root)
            | _ -> Error "Focus does not name a Workspace work tree."

    type private LoadPullHeads =
        { beforeHead: Result<string option, string>
          afterHead: Result<string option, string> }

    /// Actor changes, data directory, and Workspace id for one Git Load.
    type private WorkspaceTarget =
        { changes: ActorChanges
          dataDir: string
          workspaceId: NodeId }

    let private pullAndReadHeads git workspaceRoot tracked =
        let beforeHead = WorkspaceGit.tryHead workspaceRoot
        git.pullTracked workspaceRoot tracked
        |> Result.map (fun _ ->
            { beforeHead = beforeHead
              afterHead = WorkspaceGit.tryHead workspaceRoot })

    type private LockedWorkspace = {
        graph: Graph
        workspaceId: NodeId
        root: string
    }

    /// Commit and the next step hold the Workspace lock. Push stays outside.
    let private commitInLock git locked message next =
        NodeLockPool.withLockResult locked.graph locked.workspaceId (fun () ->
            git.commitAll locked.root message None
            |> Result.bind next)

    let private pullTracked git locked =
        commitInLock git locked "gambol: git Load" (fun (tracked, _) ->
            pullAndReadHeads git locked.root tracked)

    /// Paths the pull changed. None when either head cannot be read.
    let private changedAfterPull workspaceRoot heads =
        match heads.beforeHead, heads.afterHead with
        | Ok oldHead, Ok(Some newHead) ->
            match
                WorkspaceGit.changedPathsBetween
                    workspaceRoot
                    oldHead
                    newHead
            with
            | Ok paths -> Some paths
            | Error _ -> None
        | _ -> None

    let private finishLoadOn target label changedPaths =
        async {
            match!
                LazyLoadReconciliationServer.reconcileFromChanges
                    target.changes
                    target.dataDir
                    label
                    changedPaths
            with
            | Error err -> return Error err
            | Ok _ ->
                let! loaded = target.changes.loadNode target.workspaceId
                return loaded |> Result.map (fun _ -> ())
        }

    /// Mark received files and containing Directories, then Load.
    let internal finishLoadPull
        (workspace: LazyLoadReconciliationServer.WorkspaceReconcile)
        workspaceId
        changedPaths
        =
        finishLoadOn
            { changes = CoreDoor.actorChanges workspace.handle
              dataDir = workspace.dataDir
              workspaceId = workspaceId }
            workspace.workspaceLabel
            changedPaths

    let private applyPulledChanges root target heads =
        let changed =
            changedAfterPull root heads
            |> Option.defaultValue []
        let label = System.IO.Path.GetFileName root
        finishLoadOn target label changed

    let private runLoadOn dependencies root target =
        async {
            let! state = target.changes.graphAtEvent ()
            match state with
            | Error err -> return Error err
            | Ok current ->
                let locked = {
                    graph = current.graph
                    workspaceId = target.workspaceId
                    root = root
                }
                match pullTracked dependencies.git locked with
                | Error err -> return Error err
                | Ok heads ->
                    return! applyPulledChanges root target heads
        }

    /// A successful pull marks received files, then posts that Workspace.
    /// A failed pull is the actor result.
    let internal runLoad
        dependencies
        dataDir
        changes
        workspaceId
        workspaceRoot
        =
        runLoadOn
            dependencies
            workspaceRoot
            { changes = CoreDoor.actorChanges changes
              dataDir = dataDir
              workspaceId = workspaceId }

    let private missingSubject =
        Error "Focus is not in a Workspace, Directory, or File."

    /// Neither. The arm is empty. Reconcile records no received paths.
    let private runNeither (target: WorkspaceTarget) (label: string) =
        finishLoadOn target label []

    /// A Git remote pulls. No remote, and a directory that is not a
    /// repository, are the Neither arm.
    let private runGitOrNeither dependencies root target =
        match WorkspaceGit.remoteExists root with
        | Ok true -> runLoadOn dependencies root target
        | Ok false ->
            runNeither target (System.IO.Path.GetFileName root)
        | Error _ ->
            runNeither target (System.IO.Path.GetFileName root)

    /// A Workspace Load pulls. A Directory or a File pulls when the work
    /// tree has a remote, and is Neither otherwise.
    let private runLoadChoice dependencies root target choice =
        match choice with
        | Some (_, LoadSubject.Workspace) ->
            runLoadOn dependencies root target
        | Some (_, LoadSubject.Directory)
        | Some (_, LoadSubject.File) ->
            runGitOrNeither dependencies root target
        | None -> async.Return missingSubject

    /// No workspace work tree: Neither, then reconcile. No git pull.
    /// An empty receipt still needs a sync root, so the label is SYSTEM.
    let private runOutsideWorkspace changes dataDir focusId =
        runNeither
            { changes = changes
              dataDir = dataDir
              workspaceId = focusId }
            "SYSTEM"

    let private actorOutcome =
        function
        | Ok () -> ActorSucceeded
        | Error err -> ActorFailed err

    let private finishActor
        (changes: ActorChanges)
        (work: Async<Result<unit, string>>)
        =
        async {
            let! step = work
            let! _ = changes.actorStop (actorOutcome step)
            return ()
        }

    let private readFocusNode (changes: ActorChanges) focusId =
        async {
            let! state = changes.graphAtEvent ()
            return
                state
                |> Result.bind (fun current ->
                    match Map.tryFind focusId current.graph.nodes with
                    | None -> Error "node not found"
                    | Some node ->
                        Ok(current.eventId, current.graph, node))
        }

    /// Parsed is required. A pulling file stays Unparsed.
    let private focusCanBeParsed (graph: Graph) (focusId: NodeId) =
        match GraphMutate.setParseState focusId ParseState.Parsed graph with
        | Error err -> Error err
        | Ok parsed ->
            match Map.tryFind focusId parsed.nodes with
            | Some node when node.parseState = ParseState.Parsed ->
                Ok ()
            | Some node when node.documentState.pullingFile ->
                Error
                    "focused node stays unparsed while a file is pulling"
            | _ -> Error "focused node was not marked parsed"

    let private failedPersistMessage focusId (event: Ev) =
        match event.body with
        | EventBody.Notice (Notice.PersistFailed (id, message))
            when id = focusId ->
            Some message
        | _ -> None

    /// Persisted is the graph state. PersistFailed is the event tail.
    let rec private waitUntilPersisted
        (changes: ActorChanges)
        (after: EventId)
        focusId
        =
        async {
            match! readFocusNode changes focusId with
            | Error err -> return Error err
            | Ok (_, _, node) when
                node.parseState = ParseState.Parsed
                && node.persistState = PersistState.Persisted ->
                return Ok ()
            | Ok (_, _, node) when
                node.parseState <> ParseState.Parsed ->
                return Error "focused node was not marked parsed"
            | Ok _ ->
                let! events = changes.eventsSince after
                match
                    List.tryPick (failedPersistMessage focusId) events
                with
                | Some message -> return Error message
                | None ->
                    return! waitUntilPersisted changes after focusId
        }

    /// The focus is marked Parsed and pushed on the persistence stack.
    /// NodeUnpersisted is posted so that the write runs.
    let private persistFocus
        (changes: ActorChanges)
        focusId
        eventId
        (graph: Graph)
        =
        async {
            match focusCanBeParsed graph focusId with
            | Error err -> return Error err
            | Ok () ->
                changes.postNotice (Notice.NodeUnpersisted focusId)
                changes.postNotice (Notice.NodeParsed focusId)
                return! waitUntilPersisted changes eventId focusId
        }

    /// The focus is persisted when it is not an ordinary node.
    let private persistFocusWhenNotOrdinary
        (changes: ActorChanges)
        focusId
        =
        async {
            match! readFocusNode changes focusId with
            | Error err -> return Error err
            | Ok (eventId, graph, node) when node.kind <> Normal ->
                return! persistFocus changes focusId eventId graph
            | Ok _ -> return Ok ()
        }

    type private SaveCommit =
        { tracked: WorkspaceTrackedBranch
          output: string
          heads: LoadPullHeads }

    /// Commit and pull stay inside the Workspace lock. Push stays outside.
    let private commitAndPull git locked =
        commitInLock git locked "gambol: git Save" (fun (tracked, output) ->
            pullAndReadHeads git locked.root tracked
            |> Result.map (fun heads ->
                { tracked = tracked
                  output = output
                  heads = heads }))

    let private pushAfterPull git root target commit =
        async {
            match! applyPulledChanges root target commit.heads with
            | Error err -> return Error err
            | Ok () ->
                return
                    git.pushTracked root commit.tracked commit.output
                    |> Result.map ignore
        }

    let private runSaveOnTree
        dataDir
        dependencies
        focusId
        changes
        graph
        workspaceId
        root
        =
        async {
            match! persistFocusWhenNotOrdinary changes focusId with
            | Error err -> return Error err
            | Ok () ->
                let locked = {
                    graph = graph
                    workspaceId = workspaceId
                    root = root
                }
                match commitAndPull dependencies.git locked with
                | Error err -> return Error err
                | Ok commit ->
                    let target = {
                        changes = changes
                        dataDir = dataDir
                        workspaceId = workspaceId
                    }
                    return!
                        pushAfterPull
                            dependencies.git
                            root
                            target
                            commit
        }

    /// No workspace work tree: the actor runs and does not pull or push.
    let private runSaveBody dataDir dependencies focusId changes =
        async {
            match! changes.graphAtEvent () with
            | Error err -> return Error err
            | Ok current ->
                match workspaceFromFocus dataDir current.graph focusId with
                | Error _ -> return Ok ()
                | Ok (workspaceId, root) ->
                    return!
                        runSaveOnTree
                            dataDir
                            dependencies
                            focusId
                            changes
                            current.graph
                            workspaceId
                            root
        }

    let private runLoadBody dataDir dependencies focusId changes =
        async {
            match! changes.graphAtEvent () with
            | Error err -> return Error err
            | Ok current ->
                match workspaceFromFocus dataDir current.graph focusId with
                | Error _ ->
                    return! runOutsideWorkspace changes dataDir focusId
                | Ok (workspaceId, root) ->
                    return!
                        runLoadChoice
                            dependencies
                            root
                            { changes = changes
                              dataDir = dataDir
                              workspaceId = workspaceId }
                            (LoadSubject.enclosingSubject
                                current.graph
                                focusId)
        }

    let private actorFunction body : ActorFunction =
        fun _ changes -> finishActor changes (body changes)

    /// Load reads the enclosing Workspace, then loads that subject.
    let loadActorFunction dataDir dependencies focusId =
        actorFunction (runLoadBody dataDir dependencies focusId)

    /// Save persists the focus if it is not an ordinary node, then commits, pulls, and pushes.
    let saveActorFunction dataDir dependencies focusId =
        actorFunction (runSaveBody dataDir dependencies focusId)

    /// Save on a door. Same body as the actor, for a direct caller.
    let internal runSave dependencies dataDir door focusId =
        runSaveBody
            dataDir
            dependencies
            focusId
            (CoreDoor.actorChanges door)

    let start host caller dataDir dependencies operation request =
        let actor =
            match operation with
            | GithubTransportOperation.Load ->
                loadActorFunction dataDir dependencies request.focusId
            | GithubTransportOperation.Save ->
                saveActorFunction dataDir dependencies request.focusId
        let placement : ActorOrderFromStart.FunctionActorPlacement =
            { request = request
              actor = actor
              actorName = transportActorName operation
              actorNodeIdOf = ActorOrderFromStart.focusIdOf }
        async {
            let! placed =
                CoreMailboxApi.coreMailboxStartActorFromFunction
                    host
                    caller
                    placement
            return Result.map ignore placed
        }

    let workspaceGit : GithubTransportGit =
        { pullTracked = WorkspaceGit.pullTrackedBranch
          commitAll = WorkspaceGit.commitTracked
          pushTracked = WorkspaceGit.pushTrackedBranch }

    let productionDependencies =
        { git = workspaceGit }
