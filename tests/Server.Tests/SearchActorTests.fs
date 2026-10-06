module Gambol.Server.Tests.SearchActorTests

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http.HttpResults
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

module Enc = Thoth.Json.Newtonsoft.Encode
module Dec = Thoth.Json.Newtonsoft.Decode

let private clientIds (text: string) (zoom: NodeId) (graph: Graph) =
    match ViewModelSearch.startSearch text zoom graph with
    | None -> []
    | Some cursor ->
        ViewModelSearch.takeResults Int32.MaxValue cursor
        |> fst
        |> List.map (fun hit -> hit.nodeId)

let private ownedRoot (ids: NodeId list) (graph: Graph) : Graph =
    match Graph.replace graph.root 0 [] (ChildNode.owners ids) graph with
    | Ok next -> next
    | Error err -> failwith err

let private replaceChildren
    (parentId: NodeId)
    (ids: NodeId list)
    (graph: Graph)
    : Graph =
    match Graph.replace parentId 0 [] (ChildNode.owners ids) graph with
    | Ok next -> next
    | Error err -> failwith err

let private walk
    (text: string)
    (zoom: NodeId)
    (focusId: NodeId option)
    (graph: Graph)
    : SearchPicture.Reply =
    SearchActor.oneReply
        { text = text
          zoomRoot = zoom
          focusId = focusId
          graph = graph
          matchKey = SearchPicture.Text text }

[<Fact>]
let ``actor reply matches startSearch and takeResults`` () =
    let graph0 = Graph.create ()
    let graph1, ids =
        ModelBuilder.createNodes [ "hit alpha"; "other"; "hit beta" ] graph0
    let graph = ownedRoot ids graph1
    let got = walk "hit" graph.root None graph
    Assert.Equal<NodeId list>(
        clientIds "hit" graph.root graph,
        got.ids)
    Assert.Equal<NodeId list>([ ids.[0]; ids.[2] ], got.ids)
    Assert.Equal(SearchPicture.Text "hit", got.matchKey)

[<Fact>]
let ``actor walks the supplied graph from zoom then root`` () =
    let graph0 = Graph.create ()
    let graph1, ids =
        ModelBuilder.createNodes
            [ "zoom"; "hit under zoom"; "hit under root" ]
            graph0
    let zoom = ids.[0]
    let underZoom = ids.[1]
    let underRoot = ids.[2]
    let graph =
        graph1
        |> replaceChildren zoom [ underZoom ]
        |> ownedRoot [ zoom; underRoot ]
    let full = walk "hit" zoom (Some underRoot) graph
    Assert.Equal<NodeId list>([ underZoom; underRoot ], full.ids)
    let residence =
        graph1
        |> replaceChildren zoom [ underZoom ]
        |> ownedRoot [ zoom ]
    Assert.Equal<NodeId list>(
        [ underZoom ],
        (walk "hit" zoom None residence).ids)
    Assert.Equal<NodeId list>(
        clientIds "hit" zoom graph,
        full.ids)

[<Fact>]
let ``focus does not change the walk`` () =
    let graph0 = Graph.create ()
    let graph1, ids =
        ModelBuilder.createNodes [ "hit one"; "hit two" ] graph0
    let graph = ownedRoot ids graph1
    let left = walk "hit" graph.root (Some ids.[0]) graph
    let right =
        walk
            "hit"
            graph.root
            (Some ids.[1])
            (Graph.withFocus (Some ids.[1]) graph)
    Assert.Equal<NodeId list>(left.ids, right.ids)
    Assert.Equal<NodeId list>([ ids.[0]; ids.[1] ], left.ids)

[<Fact>]
let ``reply reads the carrier graph once and stops at 200`` () =
    let hitCount = 205
    let labels =
        [ 1 .. hitCount ] |> List.map (fun i -> $"CAPTOKEN {i}")
    let graph1, ids =
        ModelBuilder.createNodes labels (Graph.create ())
    let graph = ownedRoot ids graph1
    let calls = ResizeArray<int>()
    let getGraph () =
        calls.Add 1
        graph
    let request: SearchPicture.Request =
        { text = "CAPTOKEN"
          startId = graph.root
          generation = Some 4 }
    let got = SearchActor.reply getGraph request
    Assert.Equal(1, calls.Count)
    Assert.Equal(ViewModelSearch.searchHitCap, got.ids.Length)
    Assert.Equal<NodeId list>(List.take 200 ids, got.ids)
    Assert.Equal(SearchPicture.Generation 4, got.matchKey)
    Assert.Equal<NodeId list>(
        clientIds "CAPTOKEN" graph.root graph,
        got.ids)

[<Fact>]
let ``actorStart supplies root and focus`` () =
    let graph0 = Graph.create ()
    let graph1, ids =
        ModelBuilder.createNodes [ "hit beside root" ] graph0
    let graph =
        ownedRoot ids graph1
        |> Graph.withFocus (Some ids.[0])
    let start = SearchActor.actorStart graph EventId.zero
    Assert.Equal(graph.root, start.zoomId)
    Assert.Equal(ids.[0], start.focusId)
    Assert.Equal(graph.root, start.commandId)
    Assert.Equal<NodeId list>([ graph.root ], start.graphIds)
    Assert.Equal(EventId.zero, start.eventId)

[<Fact>]
let ``blank text returns no ids`` () =
    let graph = Graph.create ()
    let got = walk "   " graph.root (graph.focus) graph
    Assert.Empty(got.ids)

let private stateOf (graph: Graph) : State =
    { graph = graph
      eventId = EventId.zero }

let private acceptRecord _ = async.Return (Result.Ok ())

let private door (handle: CoreChanges) : Api.SearchActorDoor =
    { changes = handle
      recordStart = acceptRecord
      recordStop = acceptRecord }

let private handle (graph: Graph) : CoreChanges =
    { getState = fun () -> async.Return (Result.Ok (stateOf graph))
      getEventId = fun () -> async.Return EventId.zero
      getEventsSince = fun _ -> async.Return []
      isReady = fun () -> true
      postEvents = fun _ -> async.Return (Result.Error "no graph change")
      postGraphOnly = fun _ -> async.Return (Result.Error "no graph change")
      actorStop = fun _ -> async.Return (Result.Ok ())
      asCaller = fun _ -> Unchecked.defaultof<CoreChanges> }

[<Fact>]
let ``postSearch returns one reply from the server graph`` () = task {
    let graph1, ids =
        ModelBuilder.createNodes [ "quarterly report" ] (Graph.create ())
    let graph =
        ownedRoot ids graph1
        |> Graph.withFocus (Some ids.[0])
    let request: SearchPicture.Request =
        { text = "quarterly"
          startId = graph.root
          generation = None }
    let body = Enc.toString 0 (SearchPicture.encodeRequest request)
    let! result =
        Api.postSearch (door (handle graph)) body |> Async.StartAsTask
    match box result with
    | :? ContentHttpResult as content ->
        match Dec.fromString SearchPicture.decodeReply content.ResponseContent with
        | Error err -> failwith err
        | Ok reply ->
            Assert.Equal<NodeId list>([ ids.[0] ], reply.ids)
            Assert.Equal(SearchPicture.Text "quarterly", reply.matchKey)
            Assert.DoesNotContain("cursor", content.ResponseContent)
    | other ->
        failwith $"expected JSON content, got {other.GetType().Name}"
}

[<Fact>]
let ``postSearch records ActorStart with root focus and the server graph`` () = task {
    let dir = newTempDir ()
    let host, handle = createAdmittedFile dir
    try
        let! stateResult = handle.getState () |> Async.StartAsTask
        let state =
            match stateResult with
            | Ok state -> state
            | Error err -> failwith err
        let graph = state.graph
        let request: SearchPicture.Request =
            { text = "quarterly"
              startId = graph.root
              generation = None }
        let body = Enc.toString 0 (SearchPicture.encodeRequest request)
        let searchDoor: Api.SearchActorDoor =
            { changes = handle
              recordStart =
                fun start ->
                    CoreMailbox.recordSearchStart host testCaller start
              recordStop =
                fun focusId ->
                    CoreMailbox.recordSearchStop host testCaller focusId }
        let! _ = Api.postSearch searchDoor body |> Async.StartAsTask
        let! history =
            CoreMailbox.eventHistory host |> Async.StartAsTask
        let recorded =
            history.events
            |> List.tryPick (fun event ->
                match event.body with
                | EventBody.ActorStart start -> Some start
                | _ -> None)
        let start =
            match recorded with
            | Some start -> start
            | None ->
                failwith "ActorStart missing on the event source"
        let focusId =
            match graph.focus with
            | Some id -> id
            | None -> graph.root
        Assert.Equal(graph.root, start.zoomId)
        Assert.Equal(focusId, start.focusId)
        Assert.Equal(graph.root, start.commandId)
        Assert.Equal<NodeId list>([ graph.root ], start.graphIds)
        let stopped =
            history.events
            |> List.exists (fun event ->
                match event.body with
                | EventBody.ActorStop(stoppedId, ActorSucceeded) ->
                    stoppedId = start.focusId
                | _ -> false)
        Assert.True(stopped, "ActorStop missing after the one reply")
    finally
        CoreMailbox.dispose host
}

[<Fact>]
let ``postSearch rejects a body without search text`` () = task {
    let! result =
        Api.postSearch (door (handle (Graph.create ()))) "{}"
        |> Async.StartAsTask
    match box result with
    | :? BadRequest<obj> -> ()
    | other ->
        failwith $"expected BadRequest, got {other.GetType().Name}"
}
