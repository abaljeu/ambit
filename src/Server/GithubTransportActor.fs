namespace Gambol.Server

open Gambol.Shared

[<RequireQualifiedAccess>]
type GithubTransportOperation =
    | Load
    | Save

type GithubTransportGit =
    { remoteExists: string -> Result<bool, string>
      fetchTracked:
        string -> WorkspaceTrackedBranch -> Result<string, string>
      pullTracked:
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

    /// Save includes ROOT. ROOT's repository is the data directory.
    let private saveWorkspace dataDir graph focusId =
        match GraphQuery.enclosingWorkspace graph focusId with
        | None -> Error "Focus is not in a Workspace."
        | Some workspaceId ->
            match
                DocumentPersistPath.repositoryRoot dataDir graph focusId
            with
            | Some root -> Ok(workspaceId, root)
            | None -> Error "Focus does not name a Workspace work tree."

    type private LoadPullHeads =
        { beforeHead: Result<string option, string>
          afterHead: Result<string option, string> }

    /// Actor changes, data directory, and Workspace id for one Git Load.
    type internal WorkspaceTarget =
        { changes: ActorChanges
          dataDir: string
          workspaceId: NodeId }

    type internal LockedWorkspace = {
        graph: Graph
        workspaceId: NodeId
        root: string
    }

    /// Graph at the event, then the focus work tree. None when the focus
    /// has no workspace work tree. A graph read error stays an error.
    let private readWorkspace
        resolve
        dataDir
        focusId
        (changes: ActorChanges)
        =
        async {
            match! changes.graphAtEvent () with
            | Error err -> return Error err
            | Ok current ->
                match resolve dataDir current.graph focusId with
                | Error _ -> return Ok None
                | Ok (workspaceId, root) ->
                    return
                        Ok (Some {
                            graph = current.graph
                            workspaceId = workspaceId
                            root = root
                        })
        }

    let private readFocusWorkspace dataDir focusId changes =
        readWorkspace workspaceFromFocus dataDir focusId changes

    let private readSaveWorkspace dataDir focusId changes =
        readWorkspace saveWorkspace dataDir focusId changes

    /// One Workspace lock. The action releases when it returns.
    let internal withWorkspaceLock graph workspaceId action =
        NodeLockPool.withLockResult graph workspaceId action

    /// Commit holds the Workspace lock. The next step runs inside it.
    let private commitInLock git locked message next =
        withWorkspaceLock locked.graph locked.workspaceId (fun () ->
            git.commitAll locked.root message None
            |> Result.bind next)

    /// Commit under the Workspace lock, then fetch with no lock.
    let private commitFetched git locked message =
        match
            commitInLock git locked message (fun committed ->
                Ok committed)
        with
        | Error err -> Error err
        | Ok (tracked, output) ->
            git.fetchTracked locked.root tracked
            |> Result.map (fun _ -> tracked, output)

    let private mergeLocked git locked tracked =
        let beforeHead = WorkspaceGit.tryHead locked.root
        git.pullTracked locked.root tracked
        |> Result.map (fun _ ->
            { beforeHead = beforeHead
              afterHead = WorkspaceGit.tryHead locked.root })

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

    let private markChanged target label changedPaths =
        LazyLoadReconciliationServer.reconcileFromChanges
            target.changes
            target.dataDir
            label
            changedPaths

    let private startParse target =
        async {
            let! loaded = target.changes.loadNode target.workspaceId
            return loaded |> Result.map (fun _ -> ())
        }

    let private finishLoadOn target label changedPaths =
        async {
            match! markChanged target label changedPaths with
            | Error err -> return Error err
            | Ok _ -> return! startParse target
        }

    let private changedPaths heads root =
        changedAfterPull root heads |> Option.defaultValue []

    /// Named Workspace folder, or `.` for the data-directory repository.
    let private reconcileLabel (locked: LockedWorkspace) =
        if locked.workspaceId = Graph.rootId then
            "."
        else
            System.IO.Path.GetFileName locked.root

    /// Merge, then await the Unparsed mark, under one Workspace lock.
    let internal lockMergeAndMark git locked target tracked = async {
        let mergeAndMark = async {
            match mergeLocked git locked tracked with
            | Error err -> return Error err
            | Ok heads ->
                return!
                    markChanged
                        target
                        (reconcileLabel locked)
                        (changedPaths heads locked.root)
        }
        let! held =
            NodeLockPool.withLockAsync
                locked.graph
                locked.workspaceId
                mergeAndMark
        match held with
        | Error NodeLockRejection.HolderHoldsLock ->
            return Error "holder already holds a lock"
        | Ok result -> return result
    }

    /// Commit, unlocked fetch, then the locked merge and Unparsed mark.
    let private commitFetchAndMark git locked target message = async {
        match commitFetched git locked message with
        | Error err -> return Error err
        | Ok (tracked, output) ->
            let! merged =
                lockMergeAndMark git locked target tracked
            return merged |> Result.map (fun _ -> tracked, output)
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
                let! fetched =
                    commitFetchAndMark
                        dependencies.git
                        locked
                        target
                        "gambol: git Load"
                match fetched with
                | Error err -> return Error err
                | Ok _ -> return! startParse target
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

    /// A missing work tree has no repository. Git says the same with
    /// `not a git repository`. Any other message is a real git failure.
    let private gitHasNoRepository (root: string) (message: string) =
        not (System.IO.Directory.Exists root)
        || message.Contains(
            "not a git repository",
            System.StringComparison.OrdinalIgnoreCase)

    [<RequireQualifiedAccess>]
    type private GitRemoteArm =
        | Remote
        | Neither

    /// A remote is git. No remote, and a directory that is not a
    /// repository, are Neither. Any other git error fails the actor.
    let private gitRemoteArm git root =
        match git.remoteExists root with
        | Ok true -> Ok GitRemoteArm.Remote
        | Ok false -> Ok GitRemoteArm.Neither
        | Error message when gitHasNoRepository root message ->
            Ok GitRemoteArm.Neither
        | Error message -> Error message

    /// A remote pulls. No work tree, no remote, and a directory that is
    /// not a repository are Neither. Any other git error fails the actor.
    let private runGitOrNeither dependencies (root: string) target =
        let label = System.IO.Path.GetFileName root
        match gitRemoteArm dependencies.git root with
        | Ok GitRemoteArm.Remote -> runLoadOn dependencies root target
        | Ok GitRemoteArm.Neither -> runNeither target label
        | Error message -> async.Return (Error message)

    /// Workspace, Directory, and File share the work-tree remote check.
    let private runLoadChoice dependencies root target choice =
        match choice with
        | Some _ -> runGitOrNeither dependencies root target
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

    /// Mark the focus Parsed and Unpersisted. `NodeParsed` pushes that
    /// node on the persist stack through `persist.pushPersist`. Wait
    /// until the persist thread sets Persisted.
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

    let private saveCommitMessage = "gambol: git Save"

    /// After Persisted: commit when the work tree is a git repository.
    /// A missing repository skips the commit. An empty tree is success.
    let private commitSaveRepository graph workspaceId root =
        if not (WorkspaceGit.isRepo root) then
            Ok ()
        else
            withWorkspaceLock graph workspaceId (fun () ->
                GitSave.commitAll root saveCommitMessage
                |> Result.map ignore)

    let private pushAfterMark git root target tracked output =
        async {
            match! startParse target with
            | Error err -> return Error err
            | Ok () ->
                return
                    git.pushTracked root tracked output
                    |> Result.map ignore
        }

    let private saveFetched dataDir git focusChanges locked = async {
        let target = {
            changes = focusChanges
            dataDir = dataDir
            workspaceId = locked.workspaceId
        }
        let! committed =
            commitFetchAndMark git locked target saveCommitMessage
        match committed with
        | Error err -> return Error err
        | Ok (tracked, output) ->
            return! pushAfterMark git locked.root target tracked output
    }

    /// A remote commits, pulls, and pushes. Neither commits when the
    /// directory is a repository, and skips the commit when it is not.
    /// `remoteExists` reports no repository when this directory has
    /// no `.git`, so a parent repository is not this work tree.
    let private runSaveAfterPersist dependencies dataDir changes place =
        match gitRemoteArm dependencies.git place.root with
        | Ok GitRemoteArm.Remote ->
            saveFetched dataDir dependencies.git changes place
        | Ok GitRemoteArm.Neither ->
            async.Return (
                commitSaveRepository
                    place.graph
                    place.workspaceId
                    place.root)
        | Error message -> async.Return (Error message)

    /// Persist on the save stack, then commit, pull, and push when a
    /// remote exists. No work tree, a directory that is not a
    /// repository, and a repository with no remote are Neither.
    let private runSaveBody dataDir dependencies focusId changes =
        async {
            match! persistFocusWhenNotOrdinary changes focusId with
            | Error err -> return Error err
            | Ok () ->
                match! readSaveWorkspace dataDir focusId changes with
                | Error err -> return Error err
                | Ok None -> return Ok ()
                | Ok (Some place) ->
                    return!
                        runSaveAfterPersist
                            dependencies
                            dataDir
                            changes
                            place
        }

    let private runLoadBody dataDir dependencies focusId changes =
        async {
            match! readFocusWorkspace dataDir focusId changes with
            | Error err -> return Error err
            | Ok None ->
                return! runOutsideWorkspace changes dataDir focusId
            | Ok (Some place) ->
                return!
                    runLoadChoice
                        dependencies
                        place.root
                        { changes = changes
                          dataDir = dataDir
                          workspaceId = place.workspaceId }
                        (LoadSubject.enclosingSubject
                            place.graph
                            focusId)
        }

    let private actorFunction body : ActorFunction =
        fun _ changes -> finishActor changes (body changes)

    /// Load reads the enclosing Workspace, then loads that subject.
    let loadActorFunction dataDir dependencies focusId =
        actorFunction (runLoadBody dataDir dependencies focusId)

    /// Save persists a focus that is not an ordinary node.
    /// A remote commits, pulls, and pushes. Neither commits only
    /// when this directory is a repository.
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
        { remoteExists = WorkspaceGit.remoteExists
          fetchTracked = WorkspaceGit.fetchTrackedBranch
          pullTracked = WorkspaceGit.mergeFetchedBranch
          commitAll = WorkspaceGit.commitTracked
          pushTracked = WorkspaceGit.pushTrackedBranch }

    let productionDependencies =
        { git = workspaceGit }
