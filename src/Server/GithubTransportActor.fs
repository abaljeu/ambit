namespace Gambol.Server

open Gambol.Shared

[<RequireQualifiedAccess>]
type GithubTransportOperation =
    | Load
    | Save

type GithubTransportWorkTreeGate =
    abstract applyAction<'value> :
        string ->
        (unit -> Result<'value, string>) ->
        Result<'value, string>

type GithubTransportGit =
    { withWorkTreeGate: GithubTransportWorkTreeGate
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

    let peerName =
        function
        | GithubTransportOperation.Load ->
            PeerActorName "github-load"
        | GithubTransportOperation.Save ->
            PeerActorName "github-save"

    let private transportActorName =
        function
        | GithubTransportOperation.Load -> "github-load"
        | GithubTransportOperation.Save -> "github-save"

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

    [<RequireQualifiedAccess>]
    type private LoadCommit =
        | Committed of WorkspaceTrackedBranch
        | NothingToCommit of WorkspaceTrackedBranch
        | Failed of string

    type private LoadPullHeads =
        { beforeHead: Result<string option, string>
          afterHead: Result<string option, string> }

    let private readLoadCommit result =
        match result with
        | Error err -> LoadCommit.Failed err
        | Ok(tracked, output)
            when output = GitSave.nothingToCommitMessage ->
            LoadCommit.NothingToCommit tracked
        | Ok(tracked, _) -> LoadCommit.Committed tracked

    let private pullAndReadHeads git workspaceRoot tracked =
        let beforeHead = WorkspaceGit.tryHead workspaceRoot
        git.pullTracked workspaceRoot tracked
        |> Result.map (fun _ ->
            { beforeHead = beforeHead
              afterHead = WorkspaceGit.tryHead workspaceRoot })

    let private pullTracked git workspaceRoot =
        git.withWorkTreeGate.applyAction workspaceRoot (fun () ->
            match
                readLoadCommit (
                    git.commitAll
                        workspaceRoot
                        "gambol: git Load"
                        None)
            with
            | LoadCommit.Failed err -> Error err
            | LoadCommit.Committed tracked
            | LoadCommit.NothingToCommit tracked ->
                pullAndReadHeads git workspaceRoot tracked)

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

    let private finishLoadOn
        (changes: ActorChanges)
        dataDir
        workspaceLabel
        workspaceId
        changedPaths
        =
        async {
            match!
                LazyLoadReconciliationServer.reconcileFromChanges
                    changes
                    dataDir
                    workspaceLabel
                    changedPaths
            with
            | Error err -> return Error err
            | Ok _ ->
                let! loaded = changes.loadNode workspaceId
                return loaded |> Result.map (fun _ -> ())
        }

    /// Mark received files and containing Directories, then Load.
    let internal finishLoadPull
        (workspace: LazyLoadReconciliationServer.WorkspaceReconcile)
        workspaceId
        changedPaths
        =
        finishLoadOn
            (CoreDoor.actorChanges workspace.handle)
            workspace.dataDir
            workspace.workspaceLabel
            workspaceId
            changedPaths

    let private runLoadOn
        dependencies
        dataDir
        (changes: ActorChanges)
        workspaceId
        workspaceRoot
        =
        async {
            match pullTracked dependencies.git workspaceRoot with
            | Error err -> return Error err
            | Ok heads ->
                let changed =
                    changedAfterPull workspaceRoot heads
                    |> Option.defaultValue []
                let label =
                    System.IO.Path.GetFileName workspaceRoot
                return!
                    finishLoadOn
                        changes
                        dataDir
                        label
                        workspaceId
                        changed
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
            dataDir
            (CoreDoor.actorChanges changes)
            workspaceId
            workspaceRoot

    let private loadFocus (changes: ActorChanges) focusId =
        async {
            let! loaded = changes.loadNode focusId
            return loaded |> Result.map (fun _ -> ())
        }

    let private missingSubject =
        Error "Focus is not in a Workspace, Directory, or File."

    /// Workspace Load pulls. A Directory or File loads that node only.
    let private runLoadChoice
        dependencies
        dataDir
        changes
        choice
        workspaceId
        root
        =
        match choice with
        | Some (_, LoadSubject.Workspace) ->
            runLoadOn
                dependencies
                dataDir
                changes
                workspaceId
                root
        | Some (id, LoadSubject.Directory)
        | Some (id, LoadSubject.File) ->
            loadFocus changes id
        | None -> async.Return missingSubject

    let private runLoadStep
        dataDir
        changes
        graph
        focusId
        workspaceId
        dependencies
        root
        =
        runLoadChoice
            dependencies
            dataDir
            changes
            (LoadSubject.enclosingSubject graph focusId)
            workspaceId
            root

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

    let private readWorkspace
        dataDir
        (changes: ActorChanges)
        focusId
        =
        async {
            let! state = changes.graphAtEvent ()
            match state with
            | Error err -> return Error err
            | Ok current ->
                match workspaceFromFocus dataDir current.graph focusId with
                | Error err -> return Error err
                | Ok found -> return Ok(current.graph, found)
        }

    /// The focus node, or the document that holds it, written before git.
    let private persistFocus dataDir graph focusId =
        let targetId =
            match DocumentPartition.documentRootForNode graph focusId with
            | Some documentRootId -> documentRootId
            | None -> focusId
        DocumentPersistChange.persistNode dataDir graph targetId
        |> Result.map ignore

    /// Commit, then Load's pull resolution, then push. The gate covers commit and pull.
    let private commitPullPush git root =
        git.withWorkTreeGate.applyAction root (fun () ->
            git.commitAll root "gambol: git Save" None
            |> Result.bind (fun (tracked, commitOutput) ->
                git.pullTracked root tracked
                |> Result.map (fun _ -> tracked, commitOutput)))
        |> Result.bind (fun (tracked, commitOutput) ->
            git.pushTracked root tracked commitOutput)
        |> Result.map ignore

    let private runSaveBody
        dataDir
        dependencies
        focusId
        (changes: ActorChanges)
        =
        async {
            match! readWorkspace dataDir changes focusId with
            | Error err -> return Error err
            | Ok(graph, (_, root)) ->
                match persistFocus dataDir graph focusId with
                | Error err -> return Error err
                | Ok () -> return commitPullPush dependencies.git root
        }

    let private runLoadBody
        dataDir
        dependencies
        focusId
        (changes: ActorChanges)
        =
        async {
            match! readWorkspace dataDir changes focusId with
            | Error err -> return Error err
            | Ok(graph, (workspaceId, root)) ->
                return!
                    runLoadStep
                        dataDir
                        changes
                        graph
                        focusId
                        workspaceId
                        dependencies
                        root
        }

    let loadActorFunction dataDir dependencies focusId =
        fun _ changes ->
            finishActor
                changes
                (runLoadBody dataDir dependencies focusId changes)

    let saveActorFunction dataDir dependencies focusId =
        fun _ changes ->
            finishActor
                changes
                (runSaveBody dataDir dependencies focusId changes)

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
              actorNodeIdOf = fun item -> item.focusId }
        async {
            let! placed =
                CoreMailboxApi.coreMailboxStartActorFromFunction
                    host
                    caller
                    placement
            return Result.map ignore placed
        }

    let private workTreeGate =
        { new GithubTransportWorkTreeGate with
            member _.applyAction root action =
                WorkspaceGit.withWorkTreeGate root action }

    let workspaceGit : GithubTransportGit =
        { withWorkTreeGate = workTreeGate
          pullTracked = WorkspaceGit.pullTrackedBranch
          commitAll = WorkspaceGit.commitTracked
          pushTracked = WorkspaceGit.pushTrackedBranch }

    let productionDependencies =
        { git = workspaceGit }
