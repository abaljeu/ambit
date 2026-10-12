namespace Gambol.Server

open Gambol.Shared

[<RequireQualifiedAccess>]
type GithubTransportOperation =
    | Load
    | Save

type GithubTransportGit =
    { withWorkTreeGate:
        string ->
        (unit -> Result<WorkspaceTrackedBranch * string, string>) ->
        Result<WorkspaceTrackedBranch * string, string>
      trackedBranch: string -> Result<WorkspaceTrackedBranch, string>
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
        CoreMailbox.startPeerActor
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

    let private pullTracked git workspaceRoot =
        git.withWorkTreeGate workspaceRoot (fun () ->
            git.trackedBranch workspaceRoot
            |> Result.bind (fun tracked ->
                git.pullTracked workspaceRoot tracked
                |> Result.map (fun output -> tracked, output)))

    let private saveTracked git workspaceRoot =
        git.withWorkTreeGate workspaceRoot (fun () ->
            git.commitAll
                workspaceRoot
                "gambol: git Save"
                None)
        |> Result.bind (fun (tracked, commitOutput) ->
            git.pushTracked workspaceRoot tracked commitOutput)

    /// Changed list after a pull. None when HEAD cannot be read.
    let private changedAfterPull workspaceRoot before =
        match before, WorkspaceGit.tryHead workspaceRoot with
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
                return! workspace.handle.load workspaceId
        }

    /// A successful pull marks received files, then posts that Workspace.
    /// A failed pull is the actor result.
    let private runLoad
        dependencies
        dataDir
        changes
        workspaceId
        workspaceRoot
        =
        async {
            let before = WorkspaceGit.tryHead workspaceRoot
            match pullTracked dependencies.git workspaceRoot with
            | Error err -> return Error err
            | Ok _ ->
                let changed =
                    changedAfterPull workspaceRoot before
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

    let private runOperation
        operation
        dependencies
        dataDir
        changes
        workspaceId
        root
        =
        match operation with
        | GithubTransportOperation.Load ->
            runLoad
                dependencies
                dataDir
                changes
                workspaceId
                root
        | GithubTransportOperation.Save ->
            async.Return(
                saveTracked dependencies.git root
                |> Result.map ignore)

    let private runBody dataDir operation dependencies input coreChanges =
        async {
            let actorChanges =
                coreChanges.asCaller(actorCaller input)
            let! state = actorChanges.getState ()
            match state with
            | Error err -> return ActorFailed err
            | Ok current ->
                match
                    workspaceFromFocus
                        dataDir current.graph input.focusId
                with
                | Error err -> return ActorFailed err
                | Ok(workspaceId, root) ->
                    match!
                        runOperation
                            operation
                            dependencies
                            dataDir
                            actorChanges
                            workspaceId
                            root
                    with
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
                    coreChanges.asCaller(actorCaller input)
                let! _ = actorChanges.actorStop result
                return ()
            }

    let workspaceGit : GithubTransportGit =
        { withWorkTreeGate = WorkspaceGit.withWorkTreeGate
          trackedBranch = WorkspaceGit.trackedBranch
          pullTracked = WorkspaceGit.pullTrackedBranch
          commitAll = WorkspaceGit.commitTracked
          pushTracked = WorkspaceGit.pushTrackedBranch }

    let productionDependencies =
        { git = workspaceGit }
