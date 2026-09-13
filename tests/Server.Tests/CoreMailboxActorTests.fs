module Gambol.Server.Tests.CoreMailboxActorTests

open System
open System.Threading.Tasks
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

let private tester = Authority "tester"

let private testerSecret = Credential "tester-secret"

let private withCaller (host: MailboxHost) =
    CoreMailbox.addCaller host tester testerSecret

let private request name : StartActorRequest =
    { caller = tester
      secret = testerSecret
      name = ActorName name
      focus = Graph.rootId }

let private helloOnRoot (state: State) =
    let root = state.graph.nodes.[Graph.rootId]
    root.children
    |> List.exists (fun child ->
        match Map.tryFind child.id state.graph.nodes with
        | Some node -> node.text = "hello"
        | None -> false)

let private waitHello (host: MailboxHost) = task {
    let sw = Diagnostics.Stopwatch.StartNew()
    let rec loop () = task {
        let! state = CoreMailbox.getState host |> Async.StartAsTask
        let state = requireOk "state" state
        if helloOnRoot state then
            return ()
        elif sw.Elapsed > TimeSpan.FromSeconds 5.0 then
            Assert.Fail("hello child did not appear")
        else
            do! Task.Delay 20
            return! loop ()
    }
    return! loop ()
}

[<Fact>]
let ``StartActor without a live caller is auth-refused`` () = task {
    let dataDir = newTempDir ()
    let host = CoreMailbox.createFile dataDir
    try
        let! result =
            CoreMailbox.startActor host (request "test")
            |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, result)
        Assert.Empty(CoreMailbox.lifecycleEvents host)
    finally
        CoreMailbox.dispose host
}

[<Fact>]
let ``StartActor hands off to startActor and writes a live row`` () = task {
    let dataDir = newTempDir ()
    let host = CoreMailbox.createFile dataDir
    try
        withCaller host
        let entered = TaskCompletionSource<unit>()
        let block = TaskCompletionSource<unit>()
        host.pool.register (ActorName "test") (fun _ _ _ _ -> async {
            entered.TrySetResult() |> ignore
            do! Async.AwaitTask(block.Task :> Task)
        })
        let! started =
            CoreMailbox.startActor host (request "test")
            |> Async.StartAsTask
        let started = requireOk "start" started
        Assert.True(host.pool.isLive started.actor)
        Assert.Equal(Graph.rootId, started.focus)
        match CoreMailbox.lifecycleEvents host with
        | [ CoreEvent.ActorStarted ev ] ->
            Assert.Equal(started.actor, ev.actor)
        | other ->
            Assert.Fail($"expected one ActorStarted, got {other}")
        do! entered.Task.WaitAsync(TimeSpan.FromSeconds 5.0)
        let! state = CoreMailbox.getState host |> Async.StartAsTask
        ignore (requireOk "mailbox free" state)
        block.TrySetResult() |> ignore
    finally
        CoreMailbox.dispose host
}

[<Fact>]
let ``PostChange admits the Actor against the CoreActorPool table`` () =
    task {
        let dataDir = newTempDir ()
        let host = CoreMailbox.createFile dataDir
        try
            withCaller host
            let creds = TaskCompletionSource<Credential>()
            host.pool.register (ActorName "test") (fun _ _ cred _ -> async {
                creds.TrySetResult cred |> ignore
            })
            let! started =
                CoreMailbox.startActor host (request "test")
                |> Async.StartAsTask
            let started = requireOk "start" started
            let! cred = creds.Task.WaitAsync(TimeSpan.FromSeconds 5.0)
            let! state = CoreMailbox.getState host |> Async.StartAsTask
            let state = requireOk "state" state
            let parent = state.graph.nodes.[Graph.rootId]
            let childId = NodeId.New()
            let change =
                { id = state.revision.Value
                  changeId = Guid.NewGuid()
                  ops =
                    [ Op.NewNode(childId, "admitted")
                      Op.Replace(
                          Graph.rootId,
                          parent.children,
                          parent.children @ [ ChildNode.owner childId ]) ] }
            let! refused =
                CoreMailbox.postActorChange
                    host
                    started.actor
                    (Credential "wrong")
                    [ change ]
                |> Async.StartAsTask
            Assert.Equal(Error CoreAuth.refuse, refused)
            let! accepted =
                CoreMailbox.postActorChange
                    host
                    started.actor
                    cred
                    [ change ]
                |> Async.StartAsTask
            ignore (requireOk "admitted post" accepted)
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``TestActor hello posts through PostChange after admission`` () = task {
    let dataDir = newTempDir ()
    let host = CoreMailbox.createFile dataDir
    try
        withCaller host
        let creds = TaskCompletionSource<Credential>()
        host.pool.register (ActorName "test") (fun focus graph cred core ->
            async {
                creds.TrySetResult cred |> ignore
                return! TestActor.run "hello" focus graph cred core
            })
        let! started =
            CoreMailbox.startActor host (request "test")
            |> Async.StartAsTask
        let started = requireOk "start" started
        do! waitHello host
        let! cred = creds.Task.WaitAsync(TimeSpan.FromSeconds 5.0)
        let! stopped =
            CoreMailbox.actorStop
                host
                started.actor
                cred
                ActorSucceeded
            |> Async.StartAsTask
        ignore (requireOk "stop" stopped)
        Assert.False(host.pool.isLive started.actor)
        match CoreMailbox.lifecycleEvents host with
        | [ CoreEvent.ActorStarted _; CoreEvent.ActorFinished fin ] ->
            Assert.Equal(started.actor, fin.actor)
        | other ->
            Assert.Fail($"expected started then finished, got {other}")
        let childId = NodeId.New()
        let change =
            { id = 0
              changeId = Guid.NewGuid()
              ops =
                [ Op.NewNode(childId, "late")
                  Op.Replace(
                      Graph.rootId,
                      [],
                      [ ChildNode.owner childId ]) ] }
        let! late =
            CoreMailbox.postActorChange host started.actor cred [ change ]
            |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, late)
    finally
        CoreMailbox.dispose host
}
