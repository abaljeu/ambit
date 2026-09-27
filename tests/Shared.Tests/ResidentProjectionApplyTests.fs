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
        match ResidentProjection.applyOpsForSync ops state with
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
