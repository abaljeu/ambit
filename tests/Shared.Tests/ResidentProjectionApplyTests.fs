module ResidentProjectionApplyTests

open Gambol.Shared
open Xunit

let private stateWithNode text : State * NodeId =
    let graph, nodeId = Graph.newNode text (Graph.create ())
    { graph = graph; eventId = EventId.zero }, nodeId

[<Fact>]
let ``applyOps SetName CAS stays Invalid`` () =
    let state, nodeId = stateWithNode "n"
    match
        ResidentProjection.applyOps
            [ Op.SetName(nodeId, "stale", "new.md") ]
            state
    with
    | ApplyResult.Invalid (_, msg) ->
        Assert.Equal("old name does not match", msg)
    | other -> failwith $"expected Invalid, got {other}"

[<Fact>]
let ``applyOpsForSync soft-skips SetName CAS like Unchanged`` () =
    let state, nodeId = stateWithNode "n"
    match
        ResidentProjection.applyOpsForSync
            [ Op.SetName(nodeId, "stale", "new.md") ]
            state
            ResidentProjection.emptyPending
    with
    | ApplyResult.Unchanged next, Some msg ->
        Assert.Equal("can't change the name", msg)
        Assert.Equal(Filename.Empty, next.graph.nodes.[nodeId].name)
        Assert.Equal("n", next.graph.nodes.[nodeId].text)
    | other -> failwith $"expected Unchanged with note, got {other}"

[<Fact>]
let ``applyOpsForSync SetName CAS then SetText applies the text`` () =
    let state, nodeId = stateWithNode "n"
    match
        ResidentProjection.applyOpsForSync
            [ Op.SetName(nodeId, "stale", "new.md")
              Op.SetText(nodeId, "n", "later") ]
            state
            ResidentProjection.emptyPending
    with
    | ApplyResult.Changed next, Some msg ->
        Assert.Equal("can't change the name", msg)
        Assert.Equal(Filename.Empty, next.graph.nodes.[nodeId].name)
        Assert.Equal("later", next.graph.nodes.[nodeId].text)
    | other -> failwith $"expected Changed with note, got {other}"

[<Fact>]
let ``applyOpsForSync keeps the first CAS user message`` () =
    let state, nodeId = stateWithNode "n"
    match
        ResidentProjection.applyOpsForSync
            [ Op.SetName(nodeId, "stale", "new.md")
              Op.SetText(nodeId, "stale", "later") ]
            state
            ResidentProjection.emptyPending
    with
    | ApplyResult.Unchanged _, Some msg ->
        Assert.Equal("can't change the name", msg)
    | other -> failwith $"expected first note, got {other}"

[<Fact>]
let ``applyOpsForSync soft-skips SetText SetClasses and Replace CAS`` () =
    let state, nodeId = stateWithNode "n"
    let childId = NodeId.New()
    let ghost = NodeId.New()
    let state =
        match
            ChangeValidation.applyOps
                [ Op.NewNode(childId, "c")
                  ChildListWire.replace
                      nodeId
                      []
                      [ ChildNode.owner childId ] ]
                state
        with
        | ApplyResult.Changed next -> next
        | other -> failwith $"seed child: {other}"
    let cases =
        [ [ Op.SetText(nodeId, "stale", "x") ], "can't change the text"
          [ Op.SetClasses(
                nodeId,
                CssClass.ofList [ "stale" ],
                CssClass.ofList [ "next" ]) ],
            "can't change the classes"
          [ ChildListWire.replace nodeId [ ChildNode.owner ghost ] [] ],
            "can't change the structure" ]
    for ops, expected in cases do
        match
            ResidentProjection.applyOpsForSync
                ops
                state
                ResidentProjection.emptyPending
        with
        | ApplyResult.Unchanged next, Some msg ->
            Assert.Equal(expected, msg)
            Assert.Equal("n", next.graph.nodes.[nodeId].text)
        | other -> failwith $"expected Unchanged {expected}, got {other}"

[<Fact>]
let ``applyLocalEvent SetName CAS stays Error`` () =
    let state, nodeId = stateWithNode "n"
    let event =
        { id = EventId.zero
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = "Rename"
          body = EventBody.Change [ Op.SetName(nodeId, "stale", "new.md") ] }
    match
        SyncLogic.applyLocalEvent
            event
            (ClientSyncState.create state.graph state.eventId (ClientHistory.clear ()))
    with
    | Error msg -> Assert.Equal("old name does not match", msg)
    | Ok _ -> failwith "expected local apply to stay Error"

let private namedState () : State * NodeId =
    let graph0, nodeId = Graph.newNode "n" (Graph.create ())
    match Graph.setName nodeId "" "keep.md" graph0 with
    | Error msg -> failwith msg
    | Ok graph -> { graph = graph; eventId = EventId.zero }, nodeId

let private pendingChange submissionId ops : Ev =
    { id = EventId.zero
      submissionId = submissionId
      authority = Authority "Browser"
      commandName = "Rename"
      body = EventBody.Change ops }

[<Fact>]
let ``applyOpsForSync SetName CAS undoes a conflicting pending rename`` () =
    let state, nodeId = namedState ()
    let localId = System.Guid.NewGuid()
    let localOps = [ Op.SetName(nodeId, "keep.md", "local.md") ]
    let state =
        match ResidentProjection.applyOps localOps state with
        | ApplyResult.Changed next -> next
        | other -> failwith $"local rename: {other}"
    let incoming = [ Op.SetName(nodeId, "keep.md", "server.md") ]
    let sync =
        { ResidentProjection.emptyPending with
            pending = [ pendingChange localId localOps ]
            submissionId = System.Guid.NewGuid() }
    match ResidentProjection.applyOpsForSync incoming state sync with
    | ApplyResult.Changed next, Some msg ->
        Assert.Equal("can't change the name", msg)
        Assert.Equal(Filename.Ok "server.md", next.graph.nodes.[nodeId].name)
    | other -> failwith $"expected Server merge server.md, got {other}"

[<Fact>]
let ``applyOpsForSync SetName CAS does not undo an echo of the same Change`` () =
    let state, nodeId = namedState ()
    let localId = System.Guid.NewGuid()
    let localOps = [ Op.SetName(nodeId, "keep.md", "local.md") ]
    let state =
        match ResidentProjection.applyOps localOps state with
        | ApplyResult.Changed next -> next
        | other -> failwith $"local rename: {other}"
    let incoming = localOps
    let sync =
        { ResidentProjection.emptyPending with
            pending = [ pendingChange localId localOps ]
            submissionId = localId }
    match ResidentProjection.applyOpsForSync incoming state sync with
    | ApplyResult.Unchanged next, Some msg ->
        Assert.Equal("can't change the name", msg)
        Assert.Equal(Filename.Ok "local.md", next.graph.nodes.[nodeId].name)
    | other -> failwith $"expected echo to keep local.md, got {other}"

[<Fact>]
let ``applyOpsForSync SetText CAS undoes a conflicting pending edit`` () =
    let state, nodeId = stateWithNode "orig"
    let localId = System.Guid.NewGuid()
    let localOps = [ Op.SetText(nodeId, "orig", "local") ]
    let state =
        match ResidentProjection.applyOps localOps state with
        | ApplyResult.Changed next -> next
        | other -> failwith $"local text: {other}"
    let incoming = [ Op.SetText(nodeId, "orig", "server") ]
    let sync =
        { ResidentProjection.emptyPending with
            pending = [ pendingChange localId localOps ]
            submissionId = System.Guid.NewGuid() }
    match ResidentProjection.applyOpsForSync incoming state sync with
    | ApplyResult.Changed next, Some msg ->
        Assert.Equal("can't change the text", msg)
        Assert.Equal("server", next.graph.nodes.[nodeId].text)
    | other -> failwith $"expected Server merge server, got {other}"
