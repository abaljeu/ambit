module Gambol.Server.Tests.QueryActorTests

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.HttpResults
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

let private insertAtRoot (ids: NodeId list) (graph: Graph) : Graph =
    match Graph.replace graph.root 0 [] (ChildNode.owners ids) graph with
    | Ok next -> next
    | Error err -> failwith err

let private withoutRootChild (id: NodeId) (graph: Graph) : Graph =
    let old = Graph.children graph graph.root
    let keep = old |> List.filter (fun child -> child.id <> id)
    match Graph.replace graph.root 0 old keep graph with
    | Ok next -> next
    | Error err -> failwith err

let private withName (id: NodeId) (name: string) (graph: Graph) =
    match Filename.create name with
    | Filename.Ok valid ->
        let node = graph.nodes.[id]
        { graph with
            nodes =
                Map.add id { node with name = Filename.Ok valid } graph.nodes }
    | _ -> failwith "name"

let private lineText = "= root descendant named \"Bob\""

let private bobGraph () =
    let graph1, ids =
        ModelBuilder.createNodes
            [ lineText; "other"; "Ann" ]
            (Graph.create ())
    let lineId, bobId = ids.[0], ids.[1]
    let graph =
        graph1
        |> withName bobId "Bob"
        |> insertAtRoot ids
    lineId, bobId, graph

let private recordingStops (stops: ResizeArray<ActorResult>) : CoreChanges =
    let accepted =
        CoreChanges.accepted EventId.zero true [] false None
    let rec make () : CoreChanges =
        { getState = fun () -> async.Return (Result.Error "unused")
          getEventId = fun () -> async.Return EventId.zero
          getEventsSince = fun _ -> async.Return []
          isReady = fun () -> true
          postEvents = fun _ -> async.Return (Result.Ok accepted)
          postGraphOnly = fun _ -> async.Return (Result.Ok accepted)
          actorStop =
            fun result ->
                stops.Add result
                async.Return (Result.Ok ())
          asCaller = fun _ -> make () }
    make ()

[<Fact>]
let ``query reply reads the carrier graph once`` () =
    let lineId, bobId, full = bobGraph ()
    let calls = ResizeArray<int>()
    let getGraph () =
        calls.Add 1
        full
    let got = SearchActor.queryReply getGraph lineId
    Assert.Equal(1, calls.Count)
    Assert.Equal<NodeId list>([ bobId ], got.ids)
    let extract =
        full
        |> withoutRootChild bobId
    let missed = SearchActor.queryReply (fun () -> extract) lineId
    Assert.DoesNotContain(bobId, missed.ids)

[<Fact>]
let ``query reply is not the find walk`` () =
    let lineId, bobId, graph = bobGraph ()
    let query = SearchActor.queryReply (fun () -> graph) lineId
    let request: SearchPicture.Request =
        { text = "Bob"
          startId = graph.root
          generation = None }
    let found = SearchActor.reply (fun () -> graph) request
    Assert.Equal<NodeId list>([ bobId ], query.ids)
    Assert.Contains(lineId, found.ids)
    Assert.DoesNotContain(lineId, query.ids)
    Assert.NotEqual<NodeId list>(query.ids, found.ids)

[<Fact>]
let ``question text is not a query request`` () =
    let graph1, ids =
        ModelBuilder.createNodes [ "?test a=b" ] (Graph.create ())
    let commandId = ids.[0]
    let graph = insertAtRoot [ commandId ] graph1
    let request: ActorStart =
        { zoomId = graph.root
          focusId = commandId
          commandId = commandId
          graphIds = [ graph.root; commandId ]
          eventId = EventId.zero }
    Assert.False(SearchActor.isQueryRequest graph request)

[<Fact>]
let ``runQuery evals once then stops`` () =
    let lineId, bobId, graph = bobGraph ()
    let calls = ResizeArray<int>()
    let getGraph () =
        calls.Add 1
        graph
    let stops = ResizeArray<ActorResult>()
    let run: FunctionRun =
        { getGraph = getGraph
          secret = Credential "query-test" }
    let reply =
        SearchActor.runQuery lineId run (recordingStops stops)
        |> Async.RunSynchronously
    Assert.Equal(1, calls.Count)
    Assert.Equal<NodeId list>([ bobId ], reply.ids)
    Assert.Equal<ActorResult list>([ ActorSucceeded ], List.ofSeq stops)

[<Fact>]
let ``empty equals line returns no node ids`` () =
    let graph1, ids =
        ModelBuilder.createNodes [ "=" ] (Graph.create ())
    let lineId = ids.[0]
    let graph = insertAtRoot [ lineId ] graph1
    let got = SearchActor.queryReply (fun () -> graph) lineId
    Assert.Empty(got.ids)

let private decodeUniversal json =
    Decode.fromString
        ApiResponseSerialization.decodeUniversalResponseDecoder
        json

let private requireUniversal (result: IResult) =
    match box result with
    | :? ContentHttpResult as content ->
        match decodeUniversal content.ResponseContent with
        | Error err -> failwith err
        | Ok response -> response
    | other ->
        failwith $"expected command JSON, got {other.GetType().Name}"

let private waitStop (host: MailboxHost) (lineId: NodeId) = task {
    let mutable found = false
    let start = DateTime.UtcNow
    while
        not found
        && (DateTime.UtcNow - start).TotalMilliseconds < 2000.0 do
        let! history =
            CoreMailbox.eventHistory host |> Async.StartAsTask
        found <-
            history.events
            |> List.exists (fun event ->
                match event.body with
                | EventBody.ActorStop(id, ActorSucceeded) -> id = lineId
                | _ -> false)
        if not found then do! Task.Delay 10
    return found
}

[<Fact>]
let ``Run on an equals line evals the server graph once then stops`` () =
    task {
        let dataDir = newTempDir ()
        let agent = FileAgent.create dataDir
        let filling = FileAgent.persist agent
        let calls = ref 0
        let handlers = filling.handlers
        let wrapped =
            { filling with
                handlers =
                    { handlers with
                        getState =
                            fun () ->
                                Interlocked.Increment calls |> ignore
                                handlers.getState () } }
        let host =
            CoreMailbox.host
                (CoreActorPool.create ())
                wrapped
                admittedCredentials
        let handle = CoreMailbox.coreChanges host testCaller
        try
            let! stateResult = handle.getState () |> Async.StartAsTask
            let state =
                match stateResult with
                | Ok value -> value
                | Error err -> failwith err
            let root = state.graph.root
            let lineId = NodeId.New()
            let bobId = NodeId.New()
            let oldKids = Graph.children state.graph root
            let seed =
                changeEvent
                    ""
                    EventId.zero
                    (Guid.NewGuid())
                    [ Op.NewNode(lineId, lineText)
                      Op.NewNode(bobId, "other")
                      Op.SetName(bobId, "", "Bob")
                      Op.Replace(
                          root,
                          oldKids,
                          oldKids
                          @ [ ChildNode.owner lineId
                              ChildNode.owner bobId ]) ]
            let! seeded =
                handle.postGraphOnly seed |> Async.StartAsTask
            match seeded with
            | Error err -> failwith err
            | Ok _ -> ()
            let! eventId =
                CoreMailbox.getEventId host |> Async.StartAsTask
            calls := 0
            let request: ActorStart =
                { zoomId = root
                  focusId = lineId
                  commandId = lineId
                  graphIds = [ root ]
                  eventId = eventId }
            let body =
                Encode.toString 0 (EventJson.encodeStartRequest request)
            let! result =
                Api.postCommand
                    (fun start ->
                        CoreMailbox.startActor host testCaller start)
                    handle
                    body
                |> Async.StartAsTask
            let response = requireUniversal result
            let started =
                response.events
                |> List.exists (fun event ->
                    match event.body with
                    | EventBody.ActorStart start ->
                        start.focusId = lineId
                        && start.commandId = lineId
                        && start.graphIds = [ root ]
                    | _ -> false)
            Assert.True(started, "ActorStart missing from the command response")
            let! stopped = waitStop host lineId
            Assert.True(stopped, "ActorStop missing after the one eval")
            Assert.Equal(3, !calls)
            let! after = handle.getState () |> Async.StartAsTask
            match after with
            | Error err -> failwith err
            | Ok afterState ->
                let kids = Graph.children afterState.graph lineId
                Assert.Empty(kids)
                let again =
                    SearchActor.queryReply
                        (fun () -> afterState.graph)
                        lineId
                Assert.Equal<NodeId list>([ bobId ], again.ids)
        finally
            CoreMailbox.dispose host
    }
