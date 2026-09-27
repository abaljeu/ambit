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
    { git: GithubTransportGit
      continueLoad:
        CoreChanges -> string -> Async<Result<unit, string>> }

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
            | Some label, Some root -> Ok(label, root)
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

    let private focusFileId (graph: Graph) (focusId: NodeId) =
        match DocumentPartition.documentRootForNode graph focusId with
        | None -> None
        | Some rootId ->
            match Map.tryFind rootId graph.nodes with
            | Some { kind = Special File } -> Some rootId
            | _ -> None

    /// After directory match, parse the focused File from DataDir.
    let private parseFocusFile dataDir (changes: CoreChanges) focusId =
        async {
            let! state = changes.getState ()
            match state with
            | Error err -> return Error err
            | Ok current ->
                match focusFileId current.graph focusId with
                | None -> return Ok ()
                | Some fileId ->
                    match
                        DocumentPersistWrite.planParseFile
                            dataDir
                            current.graph
                            fileId
                            None
                    with
                    | Error err -> return Error err
                    | Ok ops ->
                        return!
                            GraphOnlyChangePost.postChunks
                                changes.postGraphOnly
                                "Parse"
                                (GraphOnlyChangeChunks.split ops)
        }

    /// Load step 1 pulls. Step 2 corrects the graph even when pull
    /// failed or changed no files. Step 3 parses a focused File.
    /// Pull's error is the actor result.
    let private runLoad
        dependencies
        dataDir
        changes
        focusId
        label
        workspaceRoot
        =
        async {
            let pulled =
                pullTracked dependencies.git workspaceRoot
            let! matched =
                dependencies.continueLoad changes label
            let! parsed = parseFocusFile dataDir changes focusId
            match pulled with
            | Error err -> return Error err
            | Ok _ ->
                match matched with
                | Error err -> return Error err
                | Ok () -> return parsed
        }

    let private runOperation
        operation
        dependencies
        dataDir
        changes
        focusId
        label
        root
        =
        match operation with
        | GithubTransportOperation.Load ->
            runLoad
                dependencies
                dataDir
                changes
                focusId
                label
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
                | Ok(label, root) ->
                    match!
                        runOperation
                            operation
                            dependencies
                            dataDir
                            actorChanges
                            input.focusId
                            label
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

    let productionDependencies dataDir =
        { git = workspaceGit
          continueLoad =
            fun changes label ->
                async {
                    let! result =
                        LazyLoadReconciliationServer.reconcileWorkspace
                            changes dataDir label
                    return result |> Result.map ignore
                } }
