module SplitOriginTraceTests

open Gambol.Shared
open Gambol.Shared.ViewModel
open GraphChildMapHelpers
open Xunit

let private owned = ChildNode.owners

let private expectChanged (result: ApplyResult) : GraphAtEvent =
    match result with
    | ApplyResult.Changed state -> state
    | ApplyResult.Unchanged _ -> failwith "expected Changed, got Unchanged"
    | ApplyResult.Invalid(_, msg) ->
        failwithf "expected Changed, got Invalid: %s" msg

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private zoomHelloSib () : Graph * SiteMap * NodeId * NodeId * NodeId =
    let g0 = Graph.create ()
    let g1, zoomIds = ModelBuilder.createNodes [ "zoom" ] g0
    let zoomId = zoomIds.[0]
    let g2, childIds = ModelBuilder.createNodes [ "hello"; "sib" ] g1
    let helloId, sibId = childIds.[0], childIds.[1]
    let g3 =
        Graph.replace g2.root 0 [] (owned [ zoomId ]) g2
        |> requireOk "root"
    let graph =
        Graph.replace zoomId 0 [] (owned [ helloId; sibId ]) g3
        |> requireOk "zoom"
    let siteMap, _ = buildSiteMapFrom graph zoomId (Sid 0)
    graph, siteMap, zoomId, helloId, sibId

let private middleSplitOps helloId sibId parentId (graph: Graph) suffixId =
    let before = Graph.children graph parentId
    [ Op.NewNode(suffixId, "lo")
      ChildListWire.insertAt parentId before 1 [ ChildNode.owner suffixId ]
      Op.SetText(helloId, "hello", "hel") ]

let private applyMiddleSplit helloId sibId parentId graph =
    let suffixId = NodeId.New()
    let state0 = { graph = graph; eventId = EventId.zero }
    let after =
        ChangeValidation.applyOps
            (middleSplitOps helloId sibId parentId graph suffixId)
            state0
        |> expectChanged
    after, suffixId

let private pendingSplit localId ops : Ev =
    { id = EventId.zero
      submissionId = localId
      authority = Authority "Browser"
      commandName = "Split at cursor"
      body = EventBody.Change ops }

type private SplitLoadFixture =
    { zoomId: NodeId
      helloId: NodeId
      sibId: NodeId
      suffixId: NodeId
      eventId: EventId
      pending: Ev list
      state: ClientSyncState
      nodes: Node list
      childMap: Map<NodeId, ChildNode list> }

let private splitLoadFixture () =
    let graph, _, zoomId, helloId, sibId = zoomHelloSib ()
    let split, suffixId = applyMiddleSplit helloId sibId zoomId graph
    let pending =
        [ pendingSplit
            (System.Guid.NewGuid())
            (middleSplitOps helloId sibId zoomId graph suffixId) ]
    let state =
        { ClientSyncState.create
            split.graph
            split.eventId
            (ClientHistory.clear ())
          with
            pending = pending }
    let staleHello =
        { split.graph.nodes.[helloId] with text = "hello" }
    { zoomId = zoomId
      helloId = helloId
      sibId = sibId
      suffixId = suffixId
      eventId = split.eventId
      pending = pending
      state = state
      nodes = [ staleHello; split.graph.nodes.[sibId] ]
      childMap = Map.ofList [ zoomId, owned [ helloId; sibId ] ] }

let private assertVisibleSplit
    (fix: SplitLoadFixture)
    (next: ClientSyncState)
    =
    Assert.Equal<NodeId list>(
        [ fix.helloId; fix.suffixId; fix.sibId ],
        Graph.children next.graph fix.zoomId |> List.map _.id)
    Assert.Equal("hel", next.graph.nodes.[fix.helloId].text)
    Assert.Equal("lo", next.graph.nodes.[fix.suffixId].text)
    Assert.Equal<Ev list>(fix.pending, next.pending)

let private applySplitLoad (events: Ev list) (fix: SplitLoadFixture) =
    SyncLogic.applyLoadResponse
        fix.eventId
        true
        { events = events
          nodes = fix.nodes
          childMap = fix.childMap }
        fix.state

[<Fact>]
let ``stale installWantAnswer after split drops suffix and restores hello text`` () =
    let graph, _, zoomId, helloId, sibId = zoomHelloSib ()
    let split, suffixId = applyMiddleSplit helloId sibId zoomId graph
    let staleHello =
        { split.graph.nodes.[helloId] with text = "hello" }
    let staleSib = split.graph.nodes.[sibId]
    let installed =
        ResidentProjection.installWantAnswer
            (Map.ofList [ zoomId, owned [ helloId; sibId ] ])
            [ staleHello; staleSib ]
            split.graph
        |> requireOk "stale-want"
    Assert.Equal<NodeId list>(
        [ helloId; sibId ],
        Graph.children installed zoomId |> List.map _.id)
    Assert.DoesNotContain(suffixId, Graph.children installed zoomId |> List.map _.id)
    Assert.Equal("hello", installed.nodes.[helloId].text)
    Assert.True(Map.containsKey suffixId installed.nodes)

[<Fact>]
let ``compose after Loaded split does not want the parent`` () =
    let graph, siteMap, zoomId, helloId, sibId = zoomHelloSib ()
    let split, _ = applyMiddleSplit helloId sibId zoomId graph
    let want = Want.compose split.graph siteMap zoomId
    Assert.DoesNotContain(zoomId, want)

[<Fact>]
let ``applyLoadResponse answer-only with pending applies the Load reply`` () =
    let fix = splitLoadFixture ()
    match applySplitLoad [] fix with
    | Error msg -> failwith $"expected apply, got {msg}"
    | Ok next -> assertVisibleSplit fix next

[<Fact>]
let ``applyLoadResponse applies a Notice with a nodes-only Load answer`` () =
    let fix = splitLoadFixture ()
    let notice =
        { pendingSplit (System.Guid.NewGuid()) [] with
            id = EventIdFixtures.storedId 9
            authority = Authority "Browser"
            commandName = "Edit node"
            body =
                EventBody.Notice(
                    Notice.NodePersisted(
                        fix.helloId,
                        EventId.zero,
                        System.DateTime.UtcNow)) }
    match applySplitLoad [ notice ] fix with
    | Error msg -> failwith $"expected apply, got {msg}"
    | Ok next ->
        Assert.Equal(EventIdFixtures.storedId 9, next.eventId)
        assertVisibleSplit fix next

[<Fact>]
let ``applySyncResponse stale Want re-applies the pending split for visibility`` () =
    let graph, _, zoomId, helloId, sibId = zoomHelloSib ()
    let suffixId = NodeId.New()
    let splitOps = middleSplitOps helloId sibId zoomId graph suffixId
    let split =
        ChangeValidation.applyOps
            splitOps
            { graph = graph; eventId = EventId.zero }
        |> expectChanged
    let pending = [ pendingSplit (System.Guid.NewGuid()) splitOps ]
    let st =
        { ClientSyncState.create split.graph split.eventId (ClientHistory.clear ())
          with
            pending = pending }
    let staleHello = { split.graph.nodes.[helloId] with text = "hello" }
    let response: SyncResponse =
        { events = []
          nodes = [ staleHello; split.graph.nodes.[sibId] ]
          childMap = Map.ofList [ zoomId, owned [ helloId; sibId ] ] }
    match SyncLogic.applySyncResponse response st with
    | Error msg -> failwith $"expected Ok, got {msg}"
    | Ok next ->
        Assert.Equal<NodeId list>(
            [ helloId; suffixId; sibId ],
            Graph.children next.graph zoomId |> List.map _.id)
        Assert.Equal("hel", next.graph.nodes.[helloId].text)
        Assert.Equal("lo", next.graph.nodes.[suffixId].text)
        Assert.Equal<Ev list>(pending, next.pending)

[<Fact>]
let ``applyOpsForSync Replace mismatch undoes all pending then applies the Server merge`` () =
    let graph, _, zoomId, helloId, sibId = zoomHelloSib ()
    let suffixId = NodeId.New()
    let splitOps = middleSplitOps helloId sibId zoomId graph suffixId
    let state0 = { graph = graph; eventId = EventId.zero }
    let split = ChangeValidation.applyOps splitOps state0 |> expectChanged
    let localId = System.Guid.NewGuid()
    let incoming =
        [ ChildListWire.replace
              zoomId
              (owned [ helloId; sibId ])
              (owned [ helloId; suffixId; sibId ])
          Op.SetText(helloId, "hello", "hel") ]
    let sync =
        { ResidentProjection.emptyPending with
            pending = [ pendingSplit localId splitOps ]
            submissionId = System.Guid.NewGuid() }
    match ResidentProjection.applyOpsForSync incoming split sync with
    | ApplyResult.Changed next, _ ->
        Assert.Equal<NodeId list>(
            [ helloId; suffixId; sibId ],
            Graph.children next.graph zoomId |> List.map _.id)
        Assert.Equal("hel", next.graph.nodes.[helloId].text)
        Assert.Equal("lo", next.graph.nodes.[suffixId].text)
    | other -> failwith $"expected Server merge apply, got {other}"
