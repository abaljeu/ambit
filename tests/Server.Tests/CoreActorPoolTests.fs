module Gambol.Server.Tests.CoreActorPoolTests

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

let private addChild (handle: CoreChanges) (text: string) = async {
    let! state = handle.getState ()
    let state = requireOk "state" state
    let parent = state.graph.nodes.[Graph.rootId]
    let childId = NodeId.New()
    let change =
        { id = state.revision.Value
          changeId = Guid.NewGuid()
          ops =
            [ Op.NewNode(childId, text)
              Op.Replace(
                  Graph.rootId,
                  parent.children,
                  parent.children @ [ ChildNode.owner childId ]) ] }
    let! accepted = handle.postChange [ change ]
    ignore (requireOk "post" accepted)
    return childId
}

let private spanOf (graph: Graph) (childId: NodeId) : NodeRange =
    let start =
        graph.nodes.[Graph.rootId].children
        |> List.findIndex (fun c -> c.id = childId)
    { pnode = Graph.rootId; start = start; endd = start + 1 }

let private waitActor (started: TaskCompletionSource<Graph * Credential>) =
    started.Task.WaitAsync(TimeSpan.FromSeconds 5.0)

[<Fact>]
let ``launch starts Actor with subgraph and credential and returns a public number``
    () =
    task {
        let dataDir = newTempDir ()
        let agent = FileAgent.create dataDir
        try
            let handle = FileAgent.coreChanges agent
            let credentials = CoreCredentials.create ()
            let pool = CoreActorPool.create credentials
            let started = TaskCompletionSource<Graph * Credential>()
            let canFinish = TaskCompletionSource<unit>()
            pool.register (ActorName "test") (fun subgraph cred _ -> async {
                started.TrySetResult(subgraph, cred) |> ignore
                do! canFinish.Task |> Async.AwaitTask
            })
            let! childId = addChild handle "span" |> Async.StartAsTask
            let! state = handle.getState () |> Async.StartAsTask
            let state = requireOk "state" state
            let request =
                { name = ActorName "test"
                  revision = state.revision
                  span = spanOf state.graph childId }
            let! launched =
                pool.launch handle request |> Async.StartAsTask
            let (PublicNumber number) = requireOk "launch" launched
            Assert.True(number > 0)
            let! subgraph, cred = waitActor started
            Assert.True(Map.containsKey childId subgraph.nodes)
            let! live = credentials.contains cred |> Async.StartAsTask
            Assert.True(live)
            canFinish.TrySetResult() |> ignore
        finally
            FileAgent.dispose agent
    }

[<Fact>]
let ``public numbers are never reused`` () = task {
    let dataDir = newTempDir ()
    let agent = FileAgent.create dataDir
    try
        let handle = FileAgent.coreChanges agent
        let pool = CoreActorPool.create (CoreCredentials.create ())
        pool.register (ActorName "test") (fun _ _ _ -> async.Return())
        let! a = addChild handle "a" |> Async.StartAsTask
        let! b = addChild handle "b" |> Async.StartAsTask
        let! state = handle.getState () |> Async.StartAsTask
        let state = requireOk "state" state
        let! first =
            pool.launch handle
                { name = ActorName "test"
                  revision = state.revision
                  span = spanOf state.graph a }
            |> Async.StartAsTask
        let! second =
            pool.launch handle
                { name = ActorName "test"
                  revision = state.revision
                  span = spanOf state.graph b }
            |> Async.StartAsTask
        let (PublicNumber n1) = requireOk "first" first
        let (PublicNumber n2) = requireOk "second" second
        Assert.NotEqual(n1, n2)
    finally
        FileAgent.dispose agent
}

[<Fact>]
let ``launch that shares a NodeId with a live span is refused`` () = task {
    let dataDir = newTempDir ()
    let agent = FileAgent.create dataDir
    try
        let handle = FileAgent.coreChanges agent
        let pool = CoreActorPool.create (CoreCredentials.create ())
        let canFinish = TaskCompletionSource<unit>()
        pool.register (ActorName "test") (fun _ _ _ -> async {
            do! canFinish.Task |> Async.AwaitTask
        })
        let! childId = addChild handle "held" |> Async.StartAsTask
        let! state = handle.getState () |> Async.StartAsTask
        let state = requireOk "state" state
        let request =
            { name = ActorName "test"
              revision = state.revision
              span = spanOf state.graph childId }
        let! first = pool.launch handle request |> Async.StartAsTask
        ignore (requireOk "first" first)
        let! second = pool.launch handle request |> Async.StartAsTask
        Assert.Equal(Error CoreActorPool.overlap, second)
        Assert.Equal(
            Error(CoreAdmissionError.text CoreAdmissionError.Overlap),
            second)
        Assert.False(CoreAuth.isAuthRefuse CoreActorPool.overlap)
        canFinish.TrySetResult() |> ignore
    finally
        FileAgent.dispose agent
}

[<Fact>]
let ``live span Nodes show lock-present; History and agent graph do not`` () =
    task {
        let dataDir = newTempDir ()
        let agent = FileAgent.create dataDir
        try
            let handle = FileAgent.coreChanges agent
            let pool = CoreActorPool.create (CoreCredentials.create ())
            let canFinish = TaskCompletionSource<unit>()
            pool.register (ActorName "test") (fun _ _ _ -> async {
                do! canFinish.Task |> Async.AwaitTask
            })
            let! childId = addChild handle "lock" |> Async.StartAsTask
            let! before = handle.getChangesSince (Revision 0) |> Async.StartAsTask
            let! state = handle.getState () |> Async.StartAsTask
            let state = requireOk "state" state
            let! launched =
                pool.launch handle
                    { name = ActorName "test"
                      revision = state.revision
                      span = spanOf state.graph childId }
                |> Async.StartAsTask
            ignore (requireOk "launch" launched)
            let! raw = handle.getState () |> Async.StartAsTask
            let raw = requireOk "raw" raw
            Assert.False(raw.graph.nodes.[childId].lockPresent)
            let! viewed =
                pool.withLocks(handle).getState () |> Async.StartAsTask
            let viewed = requireOk "viewed" viewed
            Assert.True(viewed.graph.nodes.[childId].lockPresent)
            Assert.False(viewed.graph.nodes.[Graph.rootId].lockPresent)
            let! after = handle.getChangesSince (Revision 0) |> Async.StartAsTask
            Assert.Equal<Guid list>(
                before |> List.map _.changeId,
                after |> List.map _.changeId)
            let historyJson =
                after
                |> List.map ChangeLog.encodeChange
                |> String.concat ""
            Assert.DoesNotContain("lockPresent", historyJson)
            canFinish.TrySetResult() |> ignore
        finally
            FileAgent.dispose agent
    }

[<Fact>]
let ``unknown actor is refused`` () = task {
    let dataDir = newTempDir ()
    let agent = FileAgent.create dataDir
    try
        let handle = FileAgent.coreChanges agent
        let pool = CoreActorPool.create (CoreCredentials.create ())
        let! childId = addChild handle "x" |> Async.StartAsTask
        let! state = handle.getState () |> Async.StartAsTask
        let state = requireOk "state" state
        let! launched =
            pool.launch handle
                { name = ActorName "missing"
                  revision = state.revision
                  span = spanOf state.graph childId }
            |> Async.StartAsTask
        Assert.Equal(Error CoreActorPool.unknownActor, launched)
        Assert.Equal(
            Error(CoreAdmissionError.text CoreAdmissionError.UnknownActor),
            launched)
        Assert.False(CoreAuth.isAuthRefuse CoreActorPool.unknownActor)
    finally
        FileAgent.dispose agent
}

[<Fact>]
let ``query by public number identifies the registered Actor`` () = task {
    let dataDir = newTempDir ()
    let agent = FileAgent.create dataDir
    try
        let handle = FileAgent.coreChanges agent
        let pool = CoreActorPool.create (CoreCredentials.create ())
        let canFinish = TaskCompletionSource<unit>()
        pool.register (ActorName "test") (fun _ _ _ -> async {
            do! canFinish.Task |> Async.AwaitTask
        })
        let! childId = addChild handle "span" |> Async.StartAsTask
        let! state = handle.getState () |> Async.StartAsTask
        let state = requireOk "state" state
        let request =
            { name = ActorName "test"
              revision = state.revision
              span = spanOf state.graph childId }
        let! launched = pool.launch handle request |> Async.StartAsTask
        let number = requireOk "launch" launched
        let! found = pool.query number |> Async.StartAsTask
        Assert.Equal(Ok request, found)
        canFinish.TrySetResult() |> ignore
    finally
        FileAgent.dispose agent
}

[<Fact>]
let ``query does not return a job result or job Error`` () = task {
    let dataDir = newTempDir ()
    let agent = FileAgent.create dataDir
    try
        let handle = FileAgent.coreChanges agent
        let credentials = CoreCredentials.create ()
        let started = TaskCompletionSource<Graph * Credential>()
        let canFinish = TaskCompletionSource<unit>()
        let pool = CoreActorPool.create credentials
        pool.register (ActorName "test") (fun subgraph cred _ -> async {
            started.TrySetResult(subgraph, cred) |> ignore
            do! canFinish.Task |> Async.AwaitTask
        })
        let! childId = addChild handle "span" |> Async.StartAsTask
        let! state = handle.getState () |> Async.StartAsTask
        let state = requireOk "state" state
        let request =
            { name = ActorName "test"
              revision = state.revision
              span = spanOf state.graph childId }
        let! launched = pool.launch handle request |> Async.StartAsTask
        let number = requireOk "launch" launched
        let! _, cred = waitActor started
        let! live = credentials.contains cred |> Async.StartAsTask
        Assert.True(live)
        let! found = pool.query number |> Async.StartAsTask
        match found with
        | Error err ->
            Assert.Fail($"query returned Error, not identity: {err}")
        | Ok job ->
            Assert.Equal(ActorName "test", job.name)
            Assert.Equal(state.revision, job.revision)
            Assert.Equal(request.span, job.span)
        canFinish.TrySetResult() |> ignore
    finally
        FileAgent.dispose agent
}

[<Fact>]
let ``query uses the public number, not a span NodeId`` () = task {
    let dataDir = newTempDir ()
    let agent = FileAgent.create dataDir
    try
        let handle = FileAgent.coreChanges agent
        let pool = CoreActorPool.create (CoreCredentials.create ())
        let canFinish = TaskCompletionSource<unit>()
        pool.register (ActorName "first") (fun _ _ _ -> async {
            do! canFinish.Task |> Async.AwaitTask
        })
        pool.register (ActorName "second") (fun _ _ _ -> async {
            do! canFinish.Task |> Async.AwaitTask
        })
        let! a = addChild handle "a" |> Async.StartAsTask
        let! b = addChild handle "b" |> Async.StartAsTask
        let! state = handle.getState () |> Async.StartAsTask
        let state = requireOk "state" state
        let! first =
            pool.launch handle
                { name = ActorName "first"
                  revision = state.revision
                  span = spanOf state.graph a }
            |> Async.StartAsTask
        let! second =
            pool.launch handle
                { name = ActorName "second"
                  revision = state.revision
                  span = spanOf state.graph b }
            |> Async.StartAsTask
        let n1 = requireOk "first" first
        let n2 = requireOk "second" second
        let! q1 = pool.query n1 |> Async.StartAsTask
        let! q2 = pool.query n2 |> Async.StartAsTask
        let job1 = requireOk "query first" q1
        let job2 = requireOk "query second" q2
        Assert.Equal(ActorName "first", job1.name)
        Assert.Equal(ActorName "second", job2.name)
        Assert.Equal(spanOf state.graph a, job1.span)
        Assert.Equal(spanOf state.graph b, job2.span)
        let! missing = pool.query (PublicNumber 0) |> Async.StartAsTask
        Assert.Equal(Error CoreActorPool.unknownJob, missing)
        Assert.Equal(
            Error(CoreAdmissionError.text CoreAdmissionError.UnknownJob),
            missing)
        Assert.False(CoreAuth.isAuthRefuse CoreActorPool.unknownJob)
        canFinish.TrySetResult() |> ignore
    finally
        FileAgent.dispose agent
}

[<Fact>]
let ``Actor post through launch handle is admitted with the job credential`` () =
    task {
        let dataDir = newTempDir ()
        let agent = FileAgent.create dataDir
        try
            let handle = FileAgent.coreChanges agent
            let credentials = CoreCredentials.create ()
            let pool = CoreActorPool.create credentials
            let posted =
                TaskCompletionSource<Result<CoreChangesAccepted, string>>()
            pool.register (ActorName "test") (fun subgraph _ core -> async {
                let parent = subgraph.nodes.[Graph.rootId]
                let childId = NodeId.New()
                let! rev = core.getRevision ()
                let change =
                    { id = rev.Value
                      changeId = Guid.NewGuid()
                      ops =
                        [ Op.NewNode(childId, "from actor")
                          Op.Replace(
                              Graph.rootId,
                              parent.children,
                              parent.children @ [ ChildNode.owner childId ]) ] }
                let! result = core.postChange [ change ]
                posted.TrySetResult(result) |> ignore
            })
            let! childId = addChild handle "span" |> Async.StartAsTask
            let! state = handle.getState () |> Async.StartAsTask
            let state = requireOk "state" state
            let! launched =
                pool.launch handle
                    { name = ActorName "test"
                      revision = state.revision
                      span = spanOf state.graph childId }
                |> Async.StartAsTask
            ignore (requireOk "launch" launched)
            let! actorResult = posted.Task.WaitAsync(TimeSpan.FromSeconds 5.0)
            ignore (requireOk "actor post" actorResult)
        finally
            FileAgent.dispose agent
    }

[<Fact>]
let ``Actor handle wrap refuses a different inactive credential`` () = task {
    let dataDir = newTempDir ()
    let agent = FileAgent.create dataDir
    try
        let handle = FileAgent.coreChanges agent
        let credentials = CoreCredentials.create ()
        let bound =
            CoreAuth.bindHandle credentials (Credential "inactive") handle
        let! state = handle.getState () |> Async.StartAsTask
        let state = requireOk "state" state
        let change =
            { id = state.revision.Value
              changeId = Guid.NewGuid()
              ops =
                [ Op.NewNode(NodeId.New(), "nope")
                  Op.Replace(Graph.rootId, [], [ ChildNode.owner (NodeId.New()) ]) ] }
        let! result = bound.postChange [ change ] |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, result)
    finally
        FileAgent.dispose agent
}

[<Fact>]
let ``Actor finish removes public number so query fails`` () = task {
    let dataDir = newTempDir ()
    let agent = FileAgent.create dataDir
    try
        let handle = FileAgent.coreChanges agent
        let pool = CoreActorPool.create (CoreCredentials.create ())
        let finished = TaskCompletionSource<unit>()
        pool.register (ActorName "test") (fun _ _ _ -> async {
            finished.TrySetResult() |> ignore
        })
        let! childId = addChild handle "span" |> Async.StartAsTask
        let! state = handle.getState () |> Async.StartAsTask
        let state = requireOk "state" state
        let! launched =
            pool.launch handle
                { name = ActorName "test"
                  revision = state.revision
                  span = spanOf state.graph childId }
            |> Async.StartAsTask
        let number = requireOk "launch" launched
        let! _ = finished.Task.WaitAsync(TimeSpan.FromSeconds 5.0)
        do! Task.Delay(100)
        let! missing = pool.query number |> Async.StartAsTask
        Assert.Equal(Error CoreActorPool.unknownJob, missing)
    finally
        FileAgent.dispose agent
}

[<Fact>]
let ``Actor finish removes credential from the set`` () = task {
    let dataDir = newTempDir ()
    let agent = FileAgent.create dataDir
    try
        let handle = FileAgent.coreChanges agent
        let credentials = CoreCredentials.create ()
        let pool = CoreActorPool.create credentials
        let captured = TaskCompletionSource<Credential>()
        let canFinish = TaskCompletionSource<unit>()
        pool.register (ActorName "test") (fun _ cred _ -> async {
            captured.TrySetResult(cred) |> ignore
            do! canFinish.Task |> Async.AwaitTask
        })
        let! childId = addChild handle "span" |> Async.StartAsTask
        let! state = handle.getState () |> Async.StartAsTask
        let state = requireOk "state" state
        let! launched =
            pool.launch handle
                { name = ActorName "test"
                  revision = state.revision
                  span = spanOf state.graph childId }
            |> Async.StartAsTask
        ignore (requireOk "launch" launched)
        let! cred = captured.Task.WaitAsync(TimeSpan.FromSeconds 5.0)
        let! liveBefore = credentials.contains cred |> Async.StartAsTask
        Assert.True(liveBefore)
        canFinish.TrySetResult() |> ignore
        do! Task.Delay(100)
        let! liveAfter = credentials.contains cred |> Async.StartAsTask
        Assert.False(liveAfter)
    finally
        FileAgent.dispose agent
}

[<Fact>]
let ``Actor finish clears lock-present from span Nodes`` () = task {
    let dataDir = newTempDir ()
    let agent = FileAgent.create dataDir
    try
        let handle = FileAgent.coreChanges agent
        let pool = CoreActorPool.create (CoreCredentials.create ())
        let started = TaskCompletionSource<unit>()
        let canFinish = TaskCompletionSource<unit>()
        pool.register (ActorName "test") (fun _ _ _ -> async {
            started.TrySetResult() |> ignore
            do! canFinish.Task |> Async.AwaitTask
        })
        let! childId = addChild handle "span" |> Async.StartAsTask
        let! state = handle.getState () |> Async.StartAsTask
        let state = requireOk "state" state
        let! launched =
            pool.launch handle
                { name = ActorName "test"
                  revision = state.revision
                  span = spanOf state.graph childId }
            |> Async.StartAsTask
        ignore (requireOk "launch" launched)
        let! _ = started.Task.WaitAsync(TimeSpan.FromSeconds 5.0)
        let! viewedBefore =
            pool.withLocks(handle).getState () |> Async.StartAsTask
        let viewedBefore = requireOk "viewed before" viewedBefore
        Assert.True(viewedBefore.graph.nodes.[childId].lockPresent)
        canFinish.TrySetResult() |> ignore
        do! Task.Delay(100)
        let! viewedAfter =
            pool.withLocks(handle).getState () |> Async.StartAsTask
        let viewedAfter = requireOk "viewed after" viewedAfter
        Assert.False(viewedAfter.graph.nodes.[childId].lockPresent)
    finally
        FileAgent.dispose agent
}

[<Fact>]
let ``Actor Posts apply before delete-actor in FIFO order`` () = task {
    let dataDir = newTempDir ()
    let agent = FileAgent.create dataDir
    try
        let handle = FileAgent.coreChanges agent
        let credentials = CoreCredentials.create ()
        let pool = CoreActorPool.create credentials
        let posted = TaskCompletionSource<unit>()
        pool.register (ActorName "test") (fun subgraph cred core -> async {
            let parent = subgraph.nodes.[Graph.rootId]
            let childId = NodeId.New()
            let! rev = core.getRevision ()
            let change =
                { id = rev.Value
                  changeId = Guid.NewGuid()
                  ops =
                    [ Op.NewNode(childId, "from actor")
                      Op.Replace(
                          Graph.rootId,
                          parent.children,
                          parent.children @ [ ChildNode.owner childId ]) ] }
            let! result = core.postChange [ change ]
            ignore (requireOk "actor post" result)
            posted.TrySetResult() |> ignore
        })
        let! childId = addChild handle "span" |> Async.StartAsTask
        let! state = handle.getState () |> Async.StartAsTask
        let state = requireOk "state" state
        let! launched =
            pool.launch handle
                { name = ActorName "test"
                  revision = state.revision
                  span = spanOf state.graph childId }
            |> Async.StartAsTask
        let number = requireOk "launch" launched
        let! _ = posted.Task.WaitAsync(TimeSpan.FromSeconds 5.0)
        do! Task.Delay(100)
        let! finalState = handle.getState () |> Async.StartAsTask
        let finalState = requireOk "final" finalState
        let root = finalState.graph.nodes.[Graph.rootId]
        let hasActorChild =
            root.children
            |> List.exists (fun c ->
                match finalState.graph.nodes.TryGetValue(c.id) with
                | true, node -> node.text = "from actor"
                | false, _ -> false)
        Assert.True(hasActorChild)
        let! missing = pool.query number |> Async.StartAsTask
        Assert.Equal(Error CoreActorPool.unknownJob, missing)
    finally
        FileAgent.dispose agent
}
