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
            pool.register (ActorName "test") (fun subgraph cred _ -> async {
                started.TrySetResult(subgraph, cred) |> ignore
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
        pool.register (ActorName "test") (fun _ _ _ -> async.Return())
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
            pool.register (ActorName "test") (fun _ _ _ -> async.Return())
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
    finally
        FileAgent.dispose agent
}
