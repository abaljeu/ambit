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
    continueLoad
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
    { git = git; continueLoad = continueLoad }

let private postGraph host ops =
    let event =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change ops }
    CoreMailbox.postGraphOnly host testCaller event
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
    let ops =
        workspaceOps
        @ [ Op.NewNode(commandId, operationName)
            Op.NewNode(focusId, "subnode")
            Op.Replace(
                workspaceId,
                [],
                [ ChildNode.owner commandId
                  ChildNode.owner focusId ]) ]
    postGraph host ops
    { workspaceId = workspaceId
      commandId = commandId
      focusId = focusId }

let rec private waitForStop host focusId remainingMs =
    task {
        let! history =
            CoreMailbox.eventHistory host
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

let private withActor operation dependencies operationName body =
    task {
        let dataDir = newTempDir ()
        let pool = CoreActorPool.create ()
        pool.registerPeer
            (GithubTransportActor.peerName operation)
            (GithubTransportActor.actorFn
                dataDir operation dependencies)
        let host =
            CoreMailbox.host
                pool
                (FileAgent.persist (FileAgent.create dataDir))
                admittedCredentials
        try
            let seed = seedOperation host operationName
            do! body dataDir host pool operation seed
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``git Load gates pull then continues to Parse from subnode Focus`` () =
    let steps = ResizeArray<string>()
    let pull root _ =
        steps.Add($"pull {root}")
        Ok "pulled"
    let commit _ _ _ = Error "commit unused"
    let push _ _ _ = Error "push unused"
    let continueLoad _ label =
        steps.Add($"parse {label}")
        async.Return(Ok ())
    let deps =
        dependencies steps pull commit push continueLoad
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
                  $"gate release {root}"
                  "parse home" ],
                steps |> Seq.toList)
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
    let continueLoad _ _ = async.Return(Error "parse unused")
    let deps =
        dependencies steps pull commit push continueLoad
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
            (fun _ _ -> async.Return(Ok ()))
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
    let continueLoad _ _ = async.Return(Error "parse unused")
    let deps =
        dependencies steps pull commit push continueLoad
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
            (fun _ _ -> async.Return(Ok ()))
    withActor GithubTransportOperation.Load deps "load"
        (fun _ host _ _ seed -> task {
            let! result =
                CoreMailbox.startActor host testCaller (request seed)
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
