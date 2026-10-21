module Gambol.Server.Tests.CoreActorPoolFunctionStartTests

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

let private carrierGraph (text: string) : Graph * NodeId =
    let graph, nodeId = Graph.newNode text (Graph.create ())
    Graph.withFocus (Some nodeId) graph, nodeId

let private functionStart
    (getGraph: GraphCarrier)
    (focusId: NodeId)
    (commandId: NodeId)
    : FunctionStart =
    { actor = TestActor.functionActor
      getGraph = getGraph
      focusId = focusId
      commandId = commandId }

let private postedTexts (events: Ev list) =
    events
    |> List.collect (fun event ->
        match event.body with
        | EventBody.Change ops ->
            ops
            |> List.choose (function
                | Op.NewNode(_, text) -> Some text
                | _ -> None)
        | _ -> [])

let private stopped (events: Ev list) =
    events
    |> List.exists (fun event ->
        match event.body with
        | EventBody.ActorStop(_, ActorSucceeded) -> true
        | EventBody.Notice (Notice.ActorStopped (_, ActorSucceeded)) ->
            true
        | _ -> false)

let private waitLog (host: MailboxHost) = task {
    let mutable found = None
    let start = DateTime.UtcNow
    while
        found.IsNone
        && (DateTime.UtcNow - start).TotalMilliseconds < 2000.0 do
        let! history = CoreMailboxApi.eventHistory host |> Async.StartAsTask
        if stopped history.events then
            found <- Some history
        else
            do! Task.Delay 10
    return found
}

[<Fact>]
let ``function start does not invoke Graph carrier`` () =
    let calls = ref 0
    let graph, nodeId = carrierGraph "hello"
    let getGraph () =
        calls := !calls + 1
        graph
    let pool = CoreActorPool.create ()
    let secret =
        pool.startFunction (functionStart getGraph nodeId nodeId)
        |> requireOk "startFunction"
    Assert.Equal(0, !calls)
    Assert.True(pool.isLive secret)
    Assert.True(Set.contains nodeId (pool.liveFocusIds ()))

[<Fact>]
let ``TestActor function start posts hello from Graph carrier`` () =
    task {
        let calls = ref 0
        let graph, nodeId = carrierGraph "hello"
        let getGraph () =
            calls := !calls + 1
            graph
        let pool = CoreActorPool.create ()
        let host = acceptingHost pool
        try
            let secret =
                pool.startFunction (functionStart getGraph nodeId nodeId)
                |> requireOk "startFunction"
            Assert.Equal(0, !calls)
            pool.schedule secret (CoreDoor.create true host testCaller)
            let! log = waitLog host
            match log with
            | None -> Assert.Fail("function Actor did not stop")
            | Some history ->
                Assert.True(!calls >= 1, "Actor did not call getGraph")
                Assert.Contains("hello", postedTexts history.events)
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``TestActor function start routes ping to pong`` () =
    task {
        let graph, nodeId = carrierGraph "ping"
        let pool = CoreActorPool.create ()
        let host = acceptingHost pool
        try
            let secret =
                pool.startFunction
                    (functionStart (fun () -> graph) nodeId nodeId)
                |> requireOk "startFunction"
            pool.schedule secret (CoreDoor.create true host testCaller)
            let! log = waitLog host
            match log with
            | None -> Assert.Fail("function Actor did not stop")
            | Some history ->
                let texts = postedTexts history.events
                Assert.Contains("pong", texts)
                Assert.DoesNotContain("hello", texts)
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``ID-bag startActor still starts TestActor hello`` () =
    task {
        let helloGraph, helloId = carrierGraph "?test hello"
        let request: ActorStart =
            { zoomId = helloGraph.root
              focusId = helloId
              commandId = helloId
              graphIds = [ helloGraph.root; helloId ]
              eventId = EventId.zero }
        let pool = CoreActorPool.create ()
        let host = acceptingHost pool
        try
            pool.register (ActorName "test") TestActor.actorFn
            let secret =
                pool.startActor request (fun () -> helloGraph)
                |> requireOk "startActor"
            pool.schedule secret (CoreDoor.create true host testCaller)
            let! log = waitLog host
            match log with
            | None -> Assert.Fail("ID-bag Actor did not stop")
            | Some history ->
                Assert.Contains("hello", postedTexts history.events)
        finally
            CoreMailboxApi.dispose host
    }
