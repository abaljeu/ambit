module Gambol.Server.Tests.GithubTransportActorTests

open System
open System.IO
open System.Threading.Tasks
open Microsoft.FSharp.Reflection
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private tracked =
    { branch = "master"
      remote = "origin"
      upstream = "refs/heads/master" }

let private dependencies
    (steps: ResizeArray<string>)
    pull
    commit
    push
    =
    let git =
        { withWorkTreeGate =
            { new GithubTransportWorkTreeGate with
                member _.applyAction root action =
                    steps.Add($"gate acquire {root}")
                    try
                        action ()
                    finally
                        steps.Add($"gate release {root}") }
          pullTracked = pull
          commitAll = commit
          pushTracked = push }
    { git = git }

let private postGraph host ops =
    let event =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change ops }
    CoreMailboxApi.postGraphOnly host testCaller event
    |> Async.RunSynchronously
    |> requireOk "post graph"
    |> ignore

type private Seed =
    { workspaceId: NodeId
      runDefinitionId: NodeId
      focusId: NodeId }

let private markCurrent host nodeId =
    CoreMailboxApi.postNotice host (Notice.NodeParsed nodeId)
    let state =
        CoreMailboxApi.graphAtEvent host
        |> Async.RunSynchronously
        |> requireOk "mark current"
    match Map.tryFind nodeId state.graph.nodes with
    | Some node when Node.documentStateEnum node = Current ->
        state.graph
    | _ ->
        Assert.Fail("NodeParsed did not set Current")
        Unchecked.defaultof<_>

let private seedOperation host operationName =
    let workspaceId, workspaceOps =
        FileNodeOps.planCreateWorkspace (Graph.create ()) "home"
    let runDefinitionId = NodeId.New()
    let focusId = NodeId.New()
    postGraph host workspaceOps
    let graph = markCurrent host workspaceId
    let children = Graph.children graph workspaceId
    postGraph
        host
        [ Op.NewNode(runDefinitionId, operationName)
          Op.NewNode(focusId, "subnode")
          Op.Replace(
              workspaceId,
              children,
              children
              @ [ ChildNode.owner runDefinitionId
                  ChildNode.owner focusId ]) ]
    { workspaceId = workspaceId
      runDefinitionId = runDefinitionId
      focusId = focusId }

let rec private waitForStop host focusId remainingMs =
    task {
        let! history =
            CoreMailboxApi.eventHistory host
            |> Async.StartAsTask
        let stopped =
            history.events
            |> List.tryPick (fun event ->
                match event.body with
                | EventBody.ActorStop(id, result) when id = focusId ->
                    Some result
                | _ -> None)
        match stopped with
        | Some result -> return Some result
        | None when remainingMs <= 0 -> return None
        | None ->
            do! Task.Delay 10
            return! waitForStop host focusId (remainingMs - 10)
    }

let private request seed =
    { zoomId = seed.workspaceId
      focusId = seed.focusId
      runDefinitionId = seed.runDefinitionId
      graphIds =
        [ Graph.rootId
          Graph.workspacesId
          seed.workspaceId
          seed.runDefinitionId
          seed.focusId ]
      eventId = EventId.zero }

let private workspaceRequest seed =
    { request seed with focusId = seed.workspaceId }

let private startTransport dataDir host deps operation actorRequest =
    GithubTransportActor.start
        host
        testCaller
        dataDir
        deps
        operation
        actorRequest
    |> Async.StartAsTask

let private saveOrder root =
    [ $"gate acquire {root}"
      $"commit {root}"
      $"pull {root}"
      $"gate release {root}"
      $"push {root}" ]

let private runActor operation dependencies operationName onPush body =
    task {
        let dataDir = newTempDir ()
        let pool = CoreActorPool.create ()
        let host =
            CoreMailbox.hostWithParsePush
                pool
                (FileAgent.persist (FileAgent.create dataDir))
                admittedCredentials
                onPush
        try
            let seed = seedOperation host operationName
            do! body dataDir host pool operation seed
        finally
            CoreMailboxApi.dispose host
    }

let private withActor operation dependencies operationName body =
    runActor operation dependencies operationName ignore body

let private applyTail label events state =
    match SyncLogic.applyServerTail events state with
    | Ok next -> next
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private offersCancel focusId (state: ClientSyncState) =
    ActorLive.offersCancel focusId state.actorLiveFocusIds

[<Fact>]
let ``git Load tells the client the actor completed`` () =
    let entered =
        TaskCompletionSource<unit>(
            TaskCreationOptions.RunContinuationsAsynchronously)
    let release =
        TaskCompletionSource<unit>(
            TaskCreationOptions.RunContinuationsAsynchronously)
    let pull _ _ =
        entered.TrySetResult() |> ignore
        if not (release.Task.Wait 5000) then
            Error "pull wait timed out"
        else
            Ok "pulled"
    let deps =
        dependencies
            (ResizeArray())
            pull
            (fun _ _ _ -> Ok(tracked, GitSave.nothingToCommitMessage))
            (fun _ _ _ -> Error "push unused")
    withActor GithubTransportOperation.Load deps "load"
        (fun dataDir host _ _ seed -> task {
            try
                let! state =
                    CoreMailboxApi.graphAtEvent host |> Async.StartAsTask
                let baseline = requireOk "state" state
                let! started =
                    startTransport
                        dataDir
                        host
                        deps
                        GithubTransportOperation.Load
                        (request seed)
                requireOk "start Load" started
                do! entered.Task.WaitAsync(TimeSpan.FromSeconds 2.0)
                let! commandEvents =
                    CoreMailboxApi.getEventsSince host baseline.eventId
                    |> Async.StartAsTask
                let afterCommand =
                    applyTail
                        "command response"
                        commandEvents
                        (ClientSyncState.create
                            baseline.graph
                            baseline.eventId
                            (ClientHistory.clear ()))
                Assert.True(
                    offersCancel seed.focusId afterCommand,
                    "command response did not show Cancel")
                release.TrySetResult() |> ignore
                let! stopped = waitForStop host seed.focusId 2000
                Assert.Equal(Some ActorSucceeded, stopped)
                postGraph
                    host
                    [ Op.SetUpdateTime(
                        seed.focusId,
                        DateTime.UnixEpoch,
                        DateTime.UtcNow) ]
                let! pollEvents =
                    CoreMailboxApi.getEventsSince
                        host
                        afterCommand.eventId
                    |> Async.StartAsTask
                let afterPoll =
                    applyTail "poll" pollEvents afterCommand
                Assert.False(
                    offersCancel seed.focusId afterPoll,
                    "Cancel stays: client was not told the actor completed")
            finally
                release.TrySetResult() |> ignore
        })

[<Fact>]
let ``git Load gates pull from subnode Focus`` () =
    let steps = ResizeArray<string>()
    let pull root _ =
        steps.Add($"pull {root}")
        Ok "pulled"
    let commit root message clientHint =
        steps.Add($"commit {root}")
        Assert.Equal("gambol: git Load", message)
        Assert.Equal(None, clientHint)
        Ok(tracked, GitSave.nothingToCommitMessage)
    let push _ _ _ = Error "push unused"
    let deps =
        dependencies steps pull commit push
    withActor GithubTransportOperation.Load deps "load"
        (fun dataDir host pool operation seed -> task {
            let! started =
                startTransport
                    dataDir
                    host
                    deps
                    operation
                    (request seed)
            requireOk "start Load" started
            let! stopped = waitForStop host seed.focusId 1000
            Assert.Equal(Some ActorSucceeded, stopped)
            let root = Path.Combine(dataDir, "home")
            Assert.Equal<string list>(
                [ $"gate acquire {root}"
                  $"commit {root}"
                  $"pull {root}"
                  $"gate release {root}" ],
                steps |> Seq.toList)
            Assert.False(
                Set.contains seed.focusId (pool.liveActorNodeIds ()))
        })

[<Fact>]
let ``git Load posts that Workspace id and does not reconcile`` () =
    let steps = ResizeArray<string>()
    let pushed = ResizeArray<NodeId>()
    let pull root _ =
        steps.Add($"pull {root}")
        Ok "pulled"
    let commit _ _ _ = Ok(tracked, GitSave.nothingToCommitMessage)
    let push _ _ _ = Error "push unused"
    let deps =
        dependencies steps pull commit push
    runActor
        GithubTransportOperation.Load
        deps
        "load"
        (fun id -> pushed.Add id)
        (fun dataDir host _ operation seed -> task {
            let! started =
                startTransport
                    dataDir
                    host
                    deps
                    operation
                    (request seed)
            requireOk "start Load" started
            let! stopped = waitForStop host seed.focusId 1000
            Assert.Equal(Some ActorSucceeded, stopped)
            Assert.Equal<NodeId list>(
                [ seed.workspaceId ],
                pushed |> Seq.toList)
            Assert.DoesNotContain(seed.focusId, pushed)
            let! history =
                CoreMailboxApi.eventHistory host
                |> Async.StartAsTask
            let reconciled =
                history.events
                |> List.exists (fun event ->
                    event.commandName = "Parse")
            Assert.False(reconciled)
            let root = Path.Combine(dataDir, "home")
            Assert.Contains($"pull {root}", steps)
        })

[<Fact>]
let ``git Load ends when the pull fails and posts nothing`` () =
    let steps = ResizeArray<string>()
    let pushed = ResizeArray<NodeId>()
    let conflict = "Git conflict: notes/today.md"
    let pull _ _ = Error conflict
    let deps =
        dependencies
            steps
            pull
            (fun _ _ _ -> Ok(tracked, GitSave.nothingToCommitMessage))
            (fun _ _ _ -> Error "push unused")
    runActor
        GithubTransportOperation.Load
        deps
        "load"
        (fun id -> pushed.Add id)
        (fun dataDir host pool operation seed -> task {
            let! started =
                startTransport
                    dataDir
                    host
                    deps
                    operation
                    (request seed)
            requireOk "start Load" started
            let! stopped = waitForStop host seed.focusId 1000
            Assert.Equal(Some(ActorFailed conflict), stopped)
            Assert.Empty(pushed)
            Assert.False(
                Set.contains seed.focusId (pool.liveActorNodeIds ()))
        })

[<Fact>]
let ``git Save gates commit then releases before push`` () =
    let steps = ResizeArray<string>()
    let pull root _ =
        steps.Add($"pull {root}")
        Ok "pulled"
    let commit root message clientHint =
        steps.Add($"commit {root}")
        Assert.Equal("gambol: git Save", message)
        Assert.Equal(None, clientHint)
        Ok(tracked, "committed")
    let push root received commitOutput =
        steps.Add($"push {root}")
        Assert.Equal(tracked, received)
        Assert.Equal("committed", commitOutput)
        Ok "pushed"
    let deps =
        dependencies steps pull commit push
    withActor GithubTransportOperation.Save deps "save"
        (fun dataDir host _ operation seed -> task {
            let! started =
                startTransport
                    dataDir
                    host
                    deps
                    operation
                    (workspaceRequest seed)
            requireOk "start Save" started
            let! stopped = waitForStop host seed.workspaceId 1000
            Assert.Equal(Some ActorSucceeded, stopped)
            let root = Path.Combine(dataDir, "home")
            Assert.Equal<string list>(saveOrder root, steps |> Seq.toList)
        })

[<Fact>]
let ``git Save persists focus then commits pulls and pushes`` () =
    let steps = ResizeArray<string>()
    let persisted = ResizeArray<bool>()
    let pull root _ =
        steps.Add($"pull {root}")
        Ok "pulled"
    let commit root _ _ =
        steps.Add($"commit {root}")
        persisted.Add(File.Exists(Path.Combine(root, "note.txt")))
        Ok(tracked, "committed")
    let push root _ _ =
        steps.Add($"push {root}")
        Ok "pushed"
    let deps =
        dependencies steps pull commit push
    withActor GithubTransportOperation.Save deps "save"
        (fun dataDir host _ operation seed -> task {
            let! state =
                CoreMailboxApi.graphAtEvent host |> Async.StartAsTask
            let graph = (requireOk "state" state).graph
            let fileId, fileOps =
                FileNodeOps.planCreateOwnedFile
                    graph
                    seed.workspaceId
                    "note.txt"
            postGraph host fileOps
            let focusRequest = request seed
            let fileRequest =
                { focusRequest with
                    focusId = fileId
                    graphIds = focusRequest.graphIds @ [ fileId ] }
            let! started =
                startTransport
                    dataDir
                    host
                    deps
                    operation
                    fileRequest
            requireOk "start Save" started
            let! stopped = waitForStop host fileId 1000
            Assert.Equal(Some ActorSucceeded, stopped)
            Assert.Equal<bool list>([ true ], persisted |> Seq.toList)
            let! after =
                CoreMailboxApi.graphAtEvent host |> Async.StartAsTask
            let saved = (requireOk "saved" after).graph
            Assert.Equal(
                ParseState.Parsed,
                saved.nodes.[fileId].parseState)
            let root = Path.Combine(dataDir, "home")
            Assert.Equal<string list>(saveOrder root, steps |> Seq.toList)
        })

[<Fact>]
let ``git Save marks an unparsed file parsed and writes it before commit`` () =
    let steps = ResizeArray<string>()
    let persisted = ResizeArray<bool>()
    let pull root _ =
        steps.Add($"pull {root}")
        Ok "pulled"
    let commit root _ _ =
        steps.Add($"commit {root}")
        persisted.Add(File.Exists(Path.Combine(root, "note.txt")))
        Ok(tracked, "committed")
    let push root _ _ =
        steps.Add($"push {root}")
        Ok "pushed"
    let deps =
        dependencies steps pull commit push
    withActor GithubTransportOperation.Save deps "save"
        (fun dataDir host _ operation seed -> task {
            let! state =
                CoreMailboxApi.graphAtEvent host |> Async.StartAsTask
            let graph = (requireOk "state" state).graph
            let fileId, fileOps =
                FileNodeOps.planCreateOwnedFile
                    graph
                    seed.workspaceId
                    "note.txt"
            postGraph host fileOps
            CoreMailboxApi.postNotice host (Notice.NodeUnparsed fileId)
            let! marked =
                CoreMailboxApi.graphAtEvent host |> Async.StartAsTask
            let before = (requireOk "unparsed" marked).graph.nodes.[fileId]
            Assert.Equal(ParseState.Unparsed, before.parseState)
            Assert.Equal(PersistState.Persisted, before.persistState)
            let focusRequest = request seed
            let fileRequest =
                { focusRequest with
                    focusId = fileId
                    graphIds = focusRequest.graphIds @ [ fileId ] }
            let! started =
                startTransport
                    dataDir
                    host
                    deps
                    operation
                    fileRequest
            requireOk "start Save" started
            let! stopped = waitForStop host fileId 1000
            Assert.Equal(Some ActorSucceeded, stopped)
            Assert.Equal<bool list>([ true ], persisted |> Seq.toList)
            let! after =
                CoreMailboxApi.graphAtEvent host |> Async.StartAsTask
            let saved = (requireOk "saved" after).graph.nodes.[fileId]
            Assert.Equal(ParseState.Parsed, saved.parseState)
        })

let private isPersistNotice =
    function
    | EventBody.Notice (Notice.NodeParsed _)
    | EventBody.Notice (Notice.NodePersisted _)
    | EventBody.Notice (Notice.NodePersisting _)
    | EventBody.Notice (Notice.NodeUnpersisted _)
    | EventBody.Notice (Notice.PersistFailed _) -> true
    | _ -> false

[<Fact>]
let ``git Save on a Normal Node skips persist then commits pulls and pushes`` () =
    let steps = ResizeArray<string>()
    let pull root _ =
        steps.Add($"pull {root}")
        Ok "pulled"
    let commit root _ _ =
        steps.Add($"commit {root}")
        Ok(tracked, "committed")
    let push root _ _ =
        steps.Add($"push {root}")
        Ok "pushed"
    let deps =
        dependencies steps pull commit push
    withActor GithubTransportOperation.Save deps "save"
        (fun dataDir host _ operation seed -> task {
            let! state =
                CoreMailboxApi.graphAtEvent host |> Async.StartAsTask
            let before = requireOk "before" state
            let focus = before.graph.nodes.[seed.focusId]
            let workspace = before.graph.nodes.[seed.workspaceId]
            Assert.Equal(Normal, focus.kind)
            let! started =
                startTransport
                    dataDir
                    host
                    deps
                    operation
                    (request seed)
            requireOk "start Save" started
            let! stopped = waitForStop host seed.focusId 1000
            Assert.Equal(Some ActorSucceeded, stopped)
            let! events =
                CoreMailboxApi.getEventsSince host before.eventId
                |> Async.StartAsTask
            let persisted =
                events |> List.filter (fun event -> isPersistNotice event.body)
            Assert.Empty(persisted)
            let! afterState =
                CoreMailboxApi.graphAtEvent host |> Async.StartAsTask
            let after = requireOk "after" afterState
            Assert.Equal(focus, after.graph.nodes.[seed.focusId])
            Assert.Equal(
                workspace,
                after.graph.nodes.[seed.workspaceId])
            let root = Path.Combine(dataDir, "home")
            Assert.Equal<string list>(saveOrder root, steps |> Seq.toList)
        })

[<Fact>]
let ``git Save stops when the pull fails and does not push`` () =
    let steps = ResizeArray<string>()
    let conflict = "Git conflict: notes/today.md"
    let pull _ _ =
        steps.Add("pull")
        Error conflict
    let commit _ _ _ =
        steps.Add("commit")
        Ok(tracked, "committed")
    let push _ _ _ =
        steps.Add("push")
        Ok "pushed"
    let deps =
        dependencies steps pull commit push
    withActor GithubTransportOperation.Save deps "save"
        (fun dataDir host _ operation seed -> task {
            let! started =
                startTransport
                    dataDir
                    host
                    deps
                    operation
                    (workspaceRequest seed)
            requireOk "start Save" started
            let! stopped = waitForStop host seed.workspaceId 1000
            Assert.Equal(Some(ActorFailed conflict), stopped)
            Assert.Equal<string list>(
                [ "commit"; "pull" ],
                steps
                |> Seq.filter (fun step ->
                    step = "commit" || step = "pull" || step = "push")
                |> Seq.toList)
        })

[<Fact>]
let ``Peer Actor row is live only while git work runs`` () =
    let steps = ResizeArray<string>()
    let entered = TaskCompletionSource<unit>()
    let release = TaskCompletionSource<unit>()
    let pull _ _ =
        entered.SetResult()
        release.Task.Wait()
        Ok "pulled"
    let deps =
        dependencies
            steps
            pull
            (fun _ _ _ -> Ok(tracked, GitSave.nothingToCommitMessage))
            (fun _ _ _ -> Error "push unused")
    withActor GithubTransportOperation.Load deps "load"
        (fun dataDir host pool operation seed -> task {
            let! started =
                startTransport
                    dataDir
                    host
                    deps
                    operation
                    (request seed)
            requireOk "start Load" started
            do! entered.Task.WaitAsync(TimeSpan.FromSeconds 1.0)
            Assert.True(
                Set.contains seed.focusId (pool.liveActorNodeIds ()))
            release.SetResult()
            let! stopped = waitForStop host seed.focusId 1000
            Assert.Equal(Some ActorSucceeded, stopped)
            Assert.False(
                Set.contains seed.focusId (pool.liveActorNodeIds ()))
        })

[<Theory>]
[<InlineData("load")>]
[<InlineData("save")>]
let ``Load and Save return the same conflict reject shape`` operationName =
    let steps = ResizeArray<string>()
    let conflict = "Git conflict: notes/today.md"
    let pull _ _ = Error conflict
    let commit _ _ _ = Error conflict
    let push _ _ _ = Error "push unused"
    let deps =
        dependencies steps pull commit push
    let operation =
        if operationName = "load" then
            GithubTransportOperation.Load
        else
            GithubTransportOperation.Save
    withActor operation deps operationName
        (fun dataDir host _ operation seed -> task {
            let actorRequest =
                if operationName = "save" then
                    workspaceRequest seed
                else
                    request seed
            let! started =
                startTransport
                    dataDir
                    host
                    deps
                    operation
                    actorRequest
            requireOk $"start {operationName}" started
            let! stopped = waitForStop host actorRequest.focusId 1000
            Assert.Equal(Some(ActorFailed conflict), stopped)
            Assert.Contains("notes/today.md", conflict)
        })

[<Fact>]
let ``Peer Actor is not available through the Run actor table`` () =
    let steps = ResizeArray<string>()
    let deps =
        dependencies
            steps
            (fun _ _ -> Ok "pulled")
            (fun _ _ _ -> Ok(tracked, "committed"))
            (fun _ _ _ -> Ok "pushed")
    withActor GithubTransportOperation.Load deps "load"
        (fun _ host _ _ seed -> task {
            let! result =
                CoreMailboxApi.coreMailboxStartActor host testCaller (request seed)
                |> Async.StartAsTask
            match result with
            | Error err ->
                Assert.Contains("actor 'load' not registered", err)
            | Ok () -> Assert.Fail("Run must not start the Peer Actor")
            Assert.Empty(steps)
        })

[<Fact>]
let ``Peer Actor dependencies contain no credential field`` () =
    let fields recordType =
        FSharpType.GetRecordFields recordType
        |> Array.map (fun field -> field.Name.ToLowerInvariant())
    let names =
        Array.append
            (fields typeof<GithubTransportGit>)
            (fields typeof<GithubTransportActorDependencies>)
    Assert.DoesNotContain(
        names,
        fun name ->
            name.Contains("credential")
            || name.Contains("token")
            || name.Contains("secret"))
