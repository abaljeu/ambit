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

    /// Git pull, then changes.load on the Workspace.
    /// A failed pull is the actor result.
    let private runLoad
        dependencies
        changes
        workspaceId
        workspaceRoot
        =
        async {
            match pullTracked dependencies.git workspaceRoot with
            | Error err -> return Error err
            | Ok _ ->
                match! CoreDoor.loadNode changes workspaceId with
                | Error err -> return Error err
                | Ok () -> return Ok ()
        }

    let private runOperation
        operation
        dependencies
        dataDir
        changes
        focusId
        workspaceId
        root
        =
        match operation with
        | GithubTransportOperation.Load ->
            runLoad
                dependencies
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
                    match!
                        runOperation
                            operation
                            dependencies
                            dataDir
                            actorChanges
                            input.focusId
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
                    CoreDoor.forCaller (actorCaller input) coreChanges
                let! _ = CoreDoor.actorStop actorChanges result
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
