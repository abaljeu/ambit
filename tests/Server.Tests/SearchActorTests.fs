module Gambol.Server.Tests.SearchActorTests

open System
open System.Net
open System.Net.Http
open System.Text
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
    (graph: Graph)
    : SearchPicture.Reply =
    SearchActor.oneReply
        { text = text
          zoomRoot = zoom
          graph = graph
          matchKey = SearchPicture.Text text
          count = ViewModelSearch.searchHitCap }

[<Fact>]
let ``actor reply matches startSearch and takeResults`` () =
    let graph0 = Graph.create ()
    let graph1, ids =
        ModelBuilder.createNodes [ "hit alpha"; "other"; "hit beta" ] graph0
    let graph = ownedRoot ids graph1
    let got = walk "hit" graph.root graph
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
    let full = walk "hit" zoom graph
    Assert.Equal<NodeId list>([ underZoom; underRoot ], full.ids)
    let residence =
        graph1
        |> replaceChildren zoom [ underZoom ]
        |> ownedRoot [ zoom ]
    Assert.Equal<NodeId list>(
        [ underZoom ],
        (walk "hit" zoom residence).ids)
    Assert.Equal<NodeId list>(
        clientIds "hit" zoom graph,
        full.ids)

[<Fact>]
let ``reply walks from the request start`` () =
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
    let request: SearchPicture.Request =
        { text = "hit"
          startId = zoom
          generation = None
          count = ViewModelSearch.searchHitCap }
    let got = SearchActor.reply (fun () -> graph) request
    Assert.Equal<NodeId list>(
        clientIds "hit" zoom graph,
        got.ids)
    Assert.Equal<NodeId list>([ underZoom; underRoot ], got.ids)

[<Fact>]
let ``graph focus does not change the walk`` () =
    let graph0 = Graph.create ()
    let graph1, ids =
        ModelBuilder.createNodes [ "hit one"; "hit two" ] graph0
    let graph = ownedRoot ids graph1
    let left = walk "hit" graph.root graph
    let right =
        walk
            "hit"
            graph.root
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
          generation = Some 4
          count = ViewModelSearch.searchHitCap }
    let got = SearchActor.reply getGraph request
    Assert.Equal(1, calls.Count)
    Assert.Equal(ViewModelSearch.searchHitCap, got.ids.Length)
    Assert.Equal<NodeId list>(List.take 200 ids, got.ids)
    Assert.Equal(SearchPicture.Generation 4, got.matchKey)
    Assert.Equal<NodeId list>(
        clientIds "CAPTOKEN" graph.root graph,
        got.ids)

[<Fact>]
let ``reply stops at the request count`` () =
    let labels =
        [ 1 .. 5 ] |> List.map (fun i -> $"CAPTOKEN {i}")
    let graph1, ids =
        ModelBuilder.createNodes labels (Graph.create ())
    let graph = ownedRoot ids graph1
    let request: SearchPicture.Request =
        { text = "CAPTOKEN"
          startId = graph.root
          generation = None
          count = 2 }
    let got = SearchActor.reply (fun () -> graph) request
    Assert.Equal(2, got.ids.Length)
    Assert.Equal<NodeId list>(List.take 2 ids, got.ids)

[<Fact>]
let ``actorStart records the root`` () =
    let graph0 = Graph.create ()
    let graph1, ids =
        ModelBuilder.createNodes [ "hit beside root" ] graph0
    let graph =
        ownedRoot ids graph1
        |> Graph.withFocus (Some ids.[0])
    let start = SearchActor.actorStart graph EventId.zero
    Assert.Equal(graph.root, start.zoomId)
    Assert.Equal(graph.root, start.focusId)
    Assert.Equal(graph.root, start.runDefinitionId)
    Assert.Equal<NodeId list>([ graph.root ], start.graphIds)
    Assert.Equal(EventId.zero, start.eventId)

[<Fact>]
let ``blank text returns no ids`` () =
    let graph = Graph.create ()
    let got = walk "   " graph.root graph
    Assert.Empty(got.ids)

let private stateOf (graph: Graph) : GraphAtEvent =
    { graph = graph
      eventId = EventId.zero }

let private acceptRecord _ = async.Return (Result.Ok ())

let private acceptStop _ = async.Return ()

let private door (handle: CoreChangesDoor) : Api.SearchActorDoor =
    { changes = handle
      recordStart = acceptRecord
      recordStop = acceptStop }

let private handle (graph: Graph) : CoreChangesDoor =
    doorForReads (fun () -> Result.Ok (stateOf graph)) (fun _ -> [])

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
          generation = None
          count = ViewModelSearch.searchHitCap }
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
let ``postSearch records ActorStart at the root`` () = task {
    let dir = newTempDir ()
    let host, handle = createAdmittedFile dir
    try
        let! stateResult = CoreDoor.graphAtEvent handle |> Async.StartAsTask
        let state =
            match stateResult with
            | Ok state -> state
            | Error err -> failwith err
        let graph = state.graph
        let request: SearchPicture.Request =
            { text = "quarterly"
              startId = NodeId.New()
              generation = None
              count = ViewModelSearch.searchHitCap }
        let body = Enc.toString 0 (SearchPicture.encodeRequest request)
        let actorStopped = TaskCompletionSource<unit>()
        let searchActor run changes =
            async {
                try
                    return! SearchActor.searchActorAt run changes
                finally
                    actorStopped.TrySetResult() |> ignore
            }
        let searchDoor: Api.SearchActorDoor =
            { changes = handle
              recordStart =
                fun start ->
                    CoreMailboxApi.recordSearchStart
                        host
                        testCaller
                        searchActor
                        start
              recordStop =
                fun rootId ->
                    async {
                        let! _ =
                            CoreMailboxApi.recordSearchStop
                                host
                                testCaller
                                rootId
                        return ()
                    } }
        let! result = Api.postSearch searchDoor body |> Async.StartAsTask
        match box result with
        | :? ContentHttpResult -> ()
        | other ->
            failwith $"expected JSON content, got {other.GetType().Name}"
        do! actorStopped.Task
        let rootSucceeded (events: Ev list) =
            events
            |> List.exists (fun event ->
                match event.body with
                | EventBody.ActorStop (stoppedId, ActorSucceeded) ->
                    stoppedId = graph.root
                | _ -> false)
        let! events =
            waitForNotices
                (fun () ->
                    task {
                        let! history =
                            CoreMailboxApi.eventHistory host
                            |> Async.StartAsTask
                        return history.events
                    })
                rootSucceeded
        let started = hasStartActor "search" graph.root events
        Assert.True(started, "StartActor missing on the event source")
        Assert.True(rootSucceeded events, "ActorStop missing after the one reply")
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``server search returns the hits and their Want nodes`` () = task {
    let dir = newTempDir ()
    use client = createClientForDir dir
    let! _, before = StateEndpointTests.readFullState client
    let childId = NodeId.New()
    let term = "zebrasearch"
    let change =
        SpecialNodeTestHelpers.changeEventZero
            ""
            [ Op.NewNode(childId, term)
              Op.Replace(before.root, [], [ ChildNode.owner childId ]) ]
    let body =
        Enc.toString 0 (
            ApiResponseSerialization.encodeChangeRequest
                { events = [ change ]; want = [] })
    use posted =
        new StringContent(body, Encoding.UTF8, "application/json")
    let! created = client.PostAsync("/ambit/events", posted)
    Assert.Equal(HttpStatusCode.OK, created.StatusCode)
    let! _, after = StateEndpointTests.readFullState client
    let header = after.nodes.[childId]
    let request: SearchPicture.Request =
        { text = term
          startId = after.root
          generation = None
          count = ViewModelSearch.searchHitCap }
    let searchBody =
        Enc.toString 0 (SearchPicture.encodeRequest request)
    use content =
        new StringContent(searchBody, Encoding.UTF8, "application/json")
    let! response = client.PostAsync("/ambit/search", content)
    Assert.Equal(HttpStatusCode.OK, response.StatusCode)
    let! json = response.Content.ReadAsStringAsync()
    match Dec.fromString SearchPicture.decodeReply json with
    | Error err -> failwith err
    | Ok reply -> Assert.Contains(childId, reply.ids)
    let nodesDecoder =
        Thoth.Json.Core.Decode.object (fun get ->
            get.Required.Field
                "nodes"
                (Thoth.Json.Core.Decode.list Serialization.decodeNode))
    match Dec.fromString nodesDecoder json with
    | Error err -> failwith err
    | Ok nodes ->
        let carried = nodes |> List.filter (fun node -> node.id = childId)
        Assert.Equal(1, carried.Length)
        Assert.Equal(header.text, carried.[0].text)
        Assert.Equal(header.id, carried.[0].id)
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
