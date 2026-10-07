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

let private stopFor (getGraph: unit -> Graph) (lineId: NodeId) =
    let stops = ResizeArray<ActorResult>()
    let request: ActorStart =
        { zoomId = lineId
          focusId = lineId
          commandId = lineId
          graphIds = [ lineId ]
          eventId = EventId.zero }
    let start = SearchActor.functionStart request getGraph
    let run: FunctionRun =
        { getGraph = getGraph
          secret = Credential "query-test" }
    start.actor run (recordingStops stops) |> Async.RunSynchronously
    List.ofSeq stops

[<Fact>]
let ``ActorStop reads the carrier graph once`` () =
    let lineId, bobId, full = bobGraph ()
    let calls = ResizeArray<int>()
    let getGraph () =
        calls.Add 1
        full
    let stops = stopFor getGraph lineId
    Assert.Equal(1, calls.Count)
    Assert.Equal<ActorResult list>([ ActorQuery [ bobId ] ], stops)
    let extract = full |> withoutRootChild bobId
    let missed = stopFor (fun () -> extract) lineId
    match missed with
    | [ ActorQuery ids ] -> Assert.DoesNotContain(bobId, ids)
    | other -> Assert.Fail($"expected ActorQuery, got {other}")

[<Fact>]
let ``ActorStop ids are not the find walk`` () =
    let lineId, bobId, graph = bobGraph ()
    let stops = stopFor (fun () -> graph) lineId
    let request: SearchPicture.Request =
        { text = "Bob"
          startId = graph.root
          generation = None }
    let found = SearchActor.reply (fun () -> graph) request
    match stops with
    | [ ActorQuery ids ] ->
        Assert.Equal<NodeId list>([ bobId ], ids)
        Assert.DoesNotContain(lineId, ids)
        Assert.NotEqual<NodeId list>(ids, found.ids)
    | other -> Assert.Fail($"expected ActorQuery, got {other}")
    Assert.Contains(lineId, found.ids)

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
let ``empty equals line stops with no node ids`` () =
    let graph1, ids =
        ModelBuilder.createNodes [ "=" ] (Graph.create ())
    let lineId = ids.[0]
    let graph = insertAtRoot [ lineId ] graph1
    let stops = stopFor (fun () -> graph) lineId
    Assert.Equal<ActorResult list>([ ActorQuery [] ], stops)

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

let private waitStopEvent (host: MailboxHost) (lineId: NodeId) = task {
    let mutable found: Ev option = None
    let start = DateTime.UtcNow
    while
        found.IsNone
        && (DateTime.UtcNow - start).TotalMilliseconds < 2000.0 do
        let! history =
            CoreMailbox.eventHistory host |> Async.StartAsTask
        found <-
            history.events
            |> List.tryFind (fun event ->
                match event.body with
                | EventBody.ActorStop(id, ActorQuery _) -> id = lineId
                | _ -> false)
        if found.IsNone then do! Task.Delay 10
    return found
}

let private idsOfStop (event: Ev) =
    match event.body with
    | EventBody.ActorStop(_, ActorQuery ids) -> ids
    | other -> failwith $"expected ActorQuery, got {other}"

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
            let! stopped = waitStopEvent host lineId
            let event =
                match stopped with
                | Some value -> value
                | None -> failwith "ActorStop missing after the one eval"
            Assert.Equal<NodeId list>([ bobId ], idsOfStop event)
            let json = Encode.toString 0 (EventJson.encode event)
            match Decode.fromString EventJson.decode json with
            | Error err -> failwith err
            | Ok decoded ->
                Assert.Equal<NodeId list>([ bobId ], idsOfStop decoded)
            Assert.Equal(3, !calls)
            let! after = handle.getState () |> Async.StartAsTask
            match after with
            | Error err -> failwith err
            | Ok afterState ->
                let kids = Graph.children afterState.graph lineId
                Assert.Empty(kids)
        finally
            CoreMailbox.dispose host
    }
