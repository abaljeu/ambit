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
            fun root action ->
                steps.Add($"gate acquire {root}")
                try
                    action ()
                finally
                    steps.Add($"gate release {root}")
          trackedBranch =
            fun root ->
                steps.Add($"tracked {root}")
                Ok tracked
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
      commandId: NodeId
      focusId: NodeId }

let private seedOperation host operationName =
    let workspaceId, workspaceOps =
        FileNodeOps.planCreateWorkspace (Graph.create ()) "home"
    let commandId = NodeId.New()
    let focusId = NodeId.New()
    postGraph host workspaceOps
    CoreMailboxApi.addInMsg host (InMsg.ParseFinished workspaceId)
    CoreMailboxApi.getState host
    |> Async.RunSynchronously
    |> requireOk "parse workspace"
    |> ignore
    postGraph
        host
        [ Op.NewNode(commandId, operationName)
          Op.NewNode(focusId, "subnode")
          Op.Replace(
              workspaceId,
              [],
              [ ChildNode.owner commandId
                ChildNode.owner focusId ]) ]
    { workspaceId = workspaceId
      commandId = commandId
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
      commandId = seed.commandId
      graphIds =
        [ Graph.rootId
          Graph.workspacesId
          seed.workspaceId
          seed.commandId
          seed.focusId ]
      eventId = EventId.zero }

let private runActor operation dependencies operationName onPush body =
    task {
        let dataDir = newTempDir ()
        let pool = CoreActorPool.create ()
        let enqueue: (InMsg -> unit) ref = ref (fun (_: InMsg) -> ())
        pool.registerPeer
            (GithubTransportActor.peerName operation)
            (GithubTransportActor.actorFn
                dataDir
                operation
                dependencies
                (fun msg -> enqueue.Value msg))
        let host =
            CoreMailbox.hostWithParsePush
                pool
                (FileAgent.persist (FileAgent.create dataDir))
                admittedCredentials
                onPush
        enqueue.Value <- CoreMailboxApi.addInMsg host
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
            (fun _ _ _ -> Error "commit unused")
            (fun _ _ _ -> Error "push unused")
    withActor GithubTransportOperation.Load deps "load"
        (fun _ host _ _ seed -> task {
            try
                let! state =
                    CoreMailboxApi.getState host |> Async.StartAsTask
                let baseline = requireOk "state" state
                let! started =
                    GithubTransportActor.start
                        host testCaller GithubTransportOperation.Load
                        (request seed)
                    |> Async.StartAsTask
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
    let commit _ _ _ = Error "commit unused"
    let push _ _ _ = Error "push unused"
    let deps =
        dependencies steps pull commit push
    withActor GithubTransportOperation.Load deps "load"
        (fun dataDir host pool operation seed -> task {
            let! started =
                GithubTransportActor.start
                    host testCaller operation (request seed)
                |> Async.StartAsTask
            requireOk "start Load" started
            let! stopped = waitForStop host seed.focusId 1000
            Assert.Equal(Some ActorSucceeded, stopped)
            let root = Path.Combine(dataDir, "home")
            Assert.Equal<string list>(
                [ $"gate acquire {root}"
                  $"tracked {root}"
                  $"pull {root}"
                  $"gate release {root}" ],
                steps |> Seq.toList)
            Assert.False(
                Set.contains seed.focusId (pool.liveFocusIds ()))
        })

[<Fact>]
let ``git Load posts that Workspace id and does not reconcile`` () =
    let steps = ResizeArray<string>()
    let pushed = ResizeArray<NodeId>()
    let pull root _ =
        steps.Add($"pull {root}")
        Ok "pulled"
    let commit _ _ _ = Error "commit unused"
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
                GithubTransportActor.start
                    host testCaller operation (request seed)
                |> Async.StartAsTask
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
                    event.commandName = "Parse"
                    && match Ev.ops event with
                       | None -> false
                       | Some ops ->
                           List.exists
                               (function
                               | Op.SetDocumentState _ -> false
                               | _ -> true)
                               ops)
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
            (fun _ _ _ -> Error "commit unused")
            (fun _ _ _ -> Error "push unused")
    runActor
        GithubTransportOperation.Load
        deps
        "load"
        (fun id -> pushed.Add id)
        (fun _ host pool operation seed -> task {
            let! started =
                GithubTransportActor.start
                    host testCaller operation (request seed)
                |> Async.StartAsTask
            requireOk "start Load" started
            let! stopped = waitForStop host seed.focusId 1000
            Assert.Equal(Some(ActorFailed conflict), stopped)
            Assert.Empty(pushed)
            Assert.False(
                Set.contains seed.focusId (pool.liveFocusIds ()))
        })

[<Fact>]
let ``git Save gates commit then releases before push`` () =
    let steps = ResizeArray<string>()
    let pull _ _ = Error "pull unused"
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
                GithubTransportActor.start
                    host testCaller operation (request seed)
                |> Async.StartAsTask
            requireOk "start Save" started
            let! stopped = waitForStop host seed.focusId 1000
            Assert.Equal(Some ActorSucceeded, stopped)
            let root = Path.Combine(dataDir, "home")
            Assert.Equal<string list>(
                [ $"gate acquire {root}"
                  $"commit {root}"
                  $"gate release {root}"
                  $"push {root}" ],
                steps |> Seq.toList)
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
            (fun _ _ _ -> Error "commit unused")
            (fun _ _ _ -> Error "push unused")
    withActor GithubTransportOperation.Load deps "load"
        (fun _ host pool operation seed -> task {
            let! started =
                GithubTransportActor.start
                    host testCaller operation (request seed)
                |> Async.StartAsTask
            requireOk "start Load" started
            do! entered.Task.WaitAsync(TimeSpan.FromSeconds 1.0)
            Assert.True(
                Set.contains seed.focusId (pool.liveFocusIds ()))
            release.SetResult()
            let! stopped = waitForStop host seed.focusId 1000
            Assert.Equal(Some ActorSucceeded, stopped)
            Assert.False(
                Set.contains seed.focusId (pool.liveFocusIds ()))
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
        (fun _ host _ operation seed -> task {
            let! started =
                GithubTransportActor.start
                    host testCaller operation (request seed)
                |> Async.StartAsTask
            requireOk $"start {operationName}" started
            let! stopped = waitForStop host seed.focusId 1000
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
                CoreMailboxApi.startActor host testCaller (request seed)
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
