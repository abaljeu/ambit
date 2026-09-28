module Gambol.Server.Tests.CoreActorPoolFunctionStartTests

open System
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private waitUntil pred timeoutMs =
    task {
        let start = DateTime.UtcNow
        while
            not (pred ())
            && (DateTime.UtcNow - start).TotalMilliseconds
               < float timeoutMs do
            do! Task.Delay 10
        return pred ()
    }

let private carrierGraph (text: string) : Graph * NodeId =
    let graph, nodeId = Graph.newNode text (Graph.create ())
    Graph.withFocus (Some nodeId) graph, nodeId

let private recordingChanges
    (posted: ResizeArray<Ev>)
    (stops: ResizeArray<ActorResult>)
    : CoreChanges =
    let accepted =
        CoreChanges.accepted EventId.zero true [] false None
    let rec make () : CoreChanges =
        { getState = fun () -> async.Return (Error "unused")
          getEventId = fun () -> async.Return EventId.zero
          getEventsSince = fun _ -> async.Return []
          isReady = fun () -> true
          postEvents =
            fun events ->
                posted.AddRange events
                async.Return (Ok accepted)
          postGraphOnly =
            fun event ->
                posted.Add event
                async.Return (Ok accepted)
          actorStop =
            fun result ->
                stops.Add result
                async.Return (Ok ())
          asCaller = fun _ -> make () }
    make ()

let private functionStart
    (getGraph: GraphCarrier)
    (focusId: NodeId)
    (commandId: NodeId)
    : FunctionStart =
    { actor = TestActor.functionActor
      getGraph = getGraph
      focusId = focusId
      commandId = commandId }

let private postedTexts (posted: ResizeArray<Ev>) =
    posted
    |> Seq.collect (fun event ->
        match event.body with
        | EventBody.Change ops ->
            ops
            |> List.choose (function
                | Op.NewNode(_, text) -> Some text
                | _ -> None)
        | _ -> [])
    |> Seq.toList

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
        let posted = ResizeArray<Ev>()
        let stops = ResizeArray<ActorResult>()
        let pool = CoreActorPool.create ()
        let secret =
            pool.startFunction (functionStart getGraph nodeId nodeId)
            |> requireOk "startFunction"
        Assert.Equal(0, !calls)
        pool.schedule secret (recordingChanges posted stops)
        let! finished = waitUntil (fun () -> stops.Count > 0) 1000
        Assert.True(finished, "function Actor did not stop")
        Assert.True(!calls >= 1, "Actor did not call getGraph")
        Assert.Contains("hello", postedTexts posted)
        Assert.Equal(ActorSucceeded, stops.[0])
    }

[<Fact>]
let ``TestActor function start routes ping to pong`` () =
    task {
        let graph, nodeId = carrierGraph "ping"
        let posted = ResizeArray<Ev>()
        let stops = ResizeArray<ActorResult>()
        let pool = CoreActorPool.create ()
        let secret =
            pool.startFunction
                (functionStart (fun () -> graph) nodeId nodeId)
            |> requireOk "startFunction"
        pool.schedule secret (recordingChanges posted stops)
        let! finished = waitUntil (fun () -> stops.Count > 0) 1000
        Assert.True(finished, "function Actor did not stop")
        Assert.Contains("pong", postedTexts posted)
        Assert.DoesNotContain("hello", postedTexts posted)
        Assert.Equal(ActorSucceeded, stops.[0])
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
        let posted = ResizeArray<Ev>()
        let stops = ResizeArray<ActorResult>()
        let pool = CoreActorPool.create ()
        pool.register (ActorName "test") TestActor.actorFn
        let secret =
            pool.startActor request (fun () -> helloGraph)
            |> requireOk "startActor"
        pool.schedule secret (recordingChanges posted stops)
        let! finished = waitUntil (fun () -> stops.Count > 0) 1000
        Assert.True(finished, "ID-bag Actor did not stop")
        Assert.Contains("hello", postedTexts posted)
        Assert.Equal(ActorSucceeded, stops.[0])
    }
