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

/// Server Peer Actor for person-started git Load and git Save.
[<RequireQualifiedAccess>]
module GithubTransportActor =

    let peerName =
        function
        | GithubTransportOperation.Load ->
            PeerActorName "github-load"
        | GithubTransportOperation.Save ->
            PeerActorName "github-save"

    let start host caller operation request =
        CoreMailboxApi.startPeerActor
            host caller (peerName operation) request

    let private actorCaller (input: ActorInput) : Caller =
        { authority = Authority "Actor"
          name = ""
          secret = input.secret }

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

    let private saveTracked git workspaceRoot =
        git.withWorkTreeGate.applyAction workspaceRoot (fun () ->
            git.commitAll
                workspaceRoot
                "gambol: git Save"
                None)
        |> Result.bind (fun (tracked, commitOutput) ->
            git.pushTracked workspaceRoot tracked commitOutput)

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

    /// Mark received files and containing Directories, then Load.
    let internal finishLoadPull
        (workspace: LazyLoadReconciliationServer.WorkspaceReconcile)
        workspaceId
        changedPaths
        =
        async {
            match!
                LazyLoadReconciliationServer.reconcileReceivedFiles
                    workspace
                    changedPaths
            with
            | Error err -> return Error err
            | Ok _ ->
                return! CoreDoor.loadNode workspace.handle workspaceId
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
        async {
            match pullTracked dependencies.git workspaceRoot with
            | Error err -> return Error err
            | Ok heads ->
                let changed =
                    changedAfterPull workspaceRoot heads
                    |> Option.defaultValue []
                return!
                    finishLoadPull
                        { handle = changes
                          dataDir = dataDir
                          workspaceLabel =
                            System.IO.Path.GetFileName
                                workspaceRoot }
                        workspaceId
                        changed
        }

    let private runSave dependencies root =
        saveTracked dependencies.git root |> Result.map ignore

    let private runBody dataDir operation dependencies input coreChanges =
        async {
            let actorChanges =
                CoreDoor.forCaller (actorCaller input) coreChanges
            let! state = CoreDoor.getState actorChanges
            match state with
            | Error err -> return ActorFailed err
            | Ok current ->
                match
                    workspaceFromFocus
                        dataDir current.graph input.focusId
                with
                | Error err -> return ActorFailed err
                | Ok(workspaceId, root) ->
                    match operation with
                    | GithubTransportOperation.Load ->
                        match!
                            runLoad
                                dependencies
                                dataDir
                                actorChanges
                                workspaceId
                                root
                        with
                        | Ok _ -> return ActorSucceeded
                        | Error err -> return ActorFailed err
                    | GithubTransportOperation.Save ->
                        match runSave dependencies root with
                        | Ok () -> return ActorSucceeded
                        | Error err -> return ActorFailed err
        }

    let actorFn
        dataDir
        operation
        dependencies
        : ActorFn =
        fun input coreChanges ->
            async {
                let! result =
                    runBody
                        dataDir operation dependencies input coreChanges
                let actorChanges =
                    CoreDoor.forCaller (actorCaller input) coreChanges
                let! _ = CoreDoor.actorStop actorChanges result
                return ()
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
