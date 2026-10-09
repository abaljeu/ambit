module Gambol.Server.Tests.QueryActorTests

open System
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

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

let private posted (commandName: string) (body: EventBody) : Ev =
    { id = EventId.zero
      submissionId = Guid.NewGuid()
      authority = Authority "Browser"
      commandName = commandName
      body = body }

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err -> failwith $"{label}: {err}"

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

let private hostNow () =
    CoreMailbox.host
        (CoreActorPool.create ())
        (FileAgent.persist (FileAgent.create (newTempDir ())))
        admittedCredentials

let private seedPlain host = task {
    let! state = CoreMailbox.getState host |> Async.StartAsTask
    let graph =
        match state with
        | Ok value -> value.graph
        | Error err -> failwith err
    let root = graph.root
    let lineId = NodeId.New()
    let bobId = NodeId.New()
    let oldKids = Graph.children graph root
    let seed =
        posted
            "seed"
            (EventBody.Change
                [ Op.NewNode(lineId, "plain")
                  Op.NewNode(bobId, "other")
                  Op.SetName(bobId, "", "Bob")
                  Op.Replace(
                      root,
                      oldKids,
                      oldKids
                      @ [ ChildNode.owner lineId
                          ChildNode.owner bobId ]) ])
    let! seeded =
        CoreMailbox.postEvents host testCaller [ seed ]
        |> Async.StartAsTask
    requireOk "seed" seeded |> ignore
    return root, lineId, bobId
}

let private queryStart root lineId eventId : Ev =
    let request: ActorStart =
        { zoomId = root
          focusId = lineId
          commandId = lineId
          graphIds = [ root; lineId ]
          eventId = eventId }
    posted "Exec" (EventBody.ActorStart request)

[<Fact>]
let ``edit then ActorStart classifies the equals line after the edit`` () =
    task {
        let host = hostNow ()
        try
            let! root, lineId, bobId = seedPlain host
            let! eventId =
                CoreMailbox.getEventId host |> Async.StartAsTask
            let edit =
                posted
                    "edit"
                    (EventBody.Change
                        [ Op.SetText(lineId, "plain", lineText) ])
            let! result =
                CoreMailbox.postEvents
                    host testCaller
                    [ edit; queryStart root lineId eventId ]
                |> Async.StartAsTask
            let accepted = requireOk "list" result
            Assert.Contains(
                accepted.events,
                fun event ->
                    match event.body with
                    | EventBody.Change _ -> true
                    | _ -> false)
            let! stopped = waitStopEvent host lineId
            let event =
                match stopped with
                | Some value -> value
                | None -> failwith "ActorStop missing after the one eval"
            Assert.Equal<NodeId list>([ bobId ], idsOfStop event)
            let! after = CoreMailbox.getState host |> Async.StartAsTask
            match after with
            | Error err -> failwith err
            | Ok afterState ->
                Assert.Empty(Graph.children afterState.graph lineId)
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``query start error stores ActorStart and ActorFailed and keeps the edit`` () =
    task {
        let hold = TaskCompletionSource<unit>()
        let actor (input: ActorInput) (changes: CoreChanges) =
            async {
                do! Async.AwaitTask hold.Task
                let caller =
                    { authority = Authority "Actor"
                      name = ""
                      secret = input.secret }
                let! _ =
                    changes.asCaller(caller).actorStop ActorSucceeded
                return ()
            }
        let pool = CoreActorPool.create ()
        pool.register (ActorName "gate") actor
        let host =
            CoreMailbox.host
                pool
                (FileAgent.persist (FileAgent.create (newTempDir ())))
                admittedCredentials
        try
            let! root, lineId, _ = seedPlain host
            let! eventId =
                CoreMailbox.getEventId host |> Async.StartAsTask
            let gate =
                posted
                    "edit"
                    (EventBody.Change
                        [ Op.SetText(lineId, "plain", "?gate") ])
            let! started =
                CoreMailbox.postEvents
                    host testCaller
                    [ gate; queryStart root lineId eventId ]
                |> Async.StartAsTask
            requireOk "gate" started |> ignore
            let! again =
                CoreMailbox.getEventId host |> Async.StartAsTask
            let edit =
                posted
                    "edit"
                    (EventBody.Change
                        [ Op.SetText(lineId, "?gate", lineText) ])
            let! result =
                CoreMailbox.postEvents
                    host testCaller
                    [ edit; queryStart root lineId again ]
                |> Async.StartAsTask
            requireOk "list" result |> ignore
            let! history =
                CoreMailbox.eventHistory host |> Async.StartAsTask
            let failed =
                history.events
                |> List.exists (fun event ->
                    match event.body with
                    | EventBody.ActorStop(id, ActorFailed message) ->
                        id = lineId
                        && message = "focus already has a live Actor"
                    | _ -> false)
            Assert.True(failed, "ActorFailed missing")
            let! after = CoreMailbox.getState host |> Async.StartAsTask
            match after with
            | Error err -> failwith err
            | Ok afterState ->
                Assert.Equal(
                    lineText,
                    afterState.graph.nodes.[lineId].text)
        finally
            hold.TrySetResult() |> ignore
            CoreMailbox.dispose host
    }
