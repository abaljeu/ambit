module HistoryTests

open Gambol.Shared
open Xunit

let private expectChanged (result: ApplyResult) : State =
    match result with
    | ApplyResult.Changed state -> state
    | ApplyResult.Unchanged _ -> failwith "expected Changed, got Unchanged"
    | ApplyResult.Invalid(_, msg) -> failwithf "expected Changed, got Invalid: %s" msg

let private expectUnchanged (result: ApplyResult) : State =
    match result with
    | ApplyResult.Unchanged state -> state
    | ApplyResult.Changed _ -> failwith "expected Unchanged, got Changed"
    | ApplyResult.Invalid(_, msg) -> failwithf "expected Unchanged, got Invalid: %s" msg

let private findNodeByText (text: string) (state: State) : Node =
    state.graph.nodes
    |> Map.toSeq
    |> Seq.map snd
    |> Seq.find (fun n -> n.text = text)

[<Fact>]
let ``CreateState12 has empty history`` () =
    let state = ModelBuilder.createState12 ()
    Assert.Empty(state.history.past)
    Assert.Empty(state.history.future)

[<Fact>]
let ``NewChange uses next id and has no ops`` () =
    let history = History.empty
    let change: Change = History.newChange history
    Assert.Equal(0, change.id)
    Assert.Empty(change.ops)

[<Fact>]
let ``AddOp appends to change`` () =
    let history = History.empty
    let change0: Change = History.newChange history
    let op1 = Op.SetText(NodeId.New(), "", "x")
    let op2 = Op.SetText(NodeId.New(), "", "y")
    let change1 = Change.addOp op1 change0
    let change2 = Change.addOp op2 change1
    Assert.Equal<Op>([ op1; op2 ], change2.ops)

[<Fact>]
let ``AddChange pushes to past and clears future`` () =
    let history0 =
        { History.empty with
            future = [ { id = 99; ops = [] } ] }

    let change: Change = History.newChange history0
    let history1 = History.addChange change history0
    Assert.Equal(1, history1.past.Length)
    Assert.Empty(history1.future)

[<Fact>]
let ``Undo does nothing when past is empty`` () =
    let state = ModelBuilder.createState12 ()
    let state1 = History.undo state |> expectUnchanged
    Assert.Same(state, state1)

[<Fact>]
let ``Redo does nothing when future is empty`` () =
    let state = ModelBuilder.createState12 ()
    let state1 = History.redo state |> expectUnchanged
    Assert.Same(state, state1)

[<Fact>]
let ``Apply change that updates f g h text`` () =
    let state0 = ModelBuilder.createState12 ()
    let nodeF = findNodeByText "f" state0
    let nodeG = findNodeByText "g" state0
    let nodeH = findNodeByText "h" state0

    let change =
        History.newChange History.empty
        |> Change.addOp (Op.SetText(nodeF.id, nodeF.text, "newf"))
        |> Change.addOp (Op.SetText(nodeG.id, nodeG.text, "newg"))
        |> Change.addOp (Op.SetText(nodeH.id, nodeH.text, "newh"))

    let state1 = History.applyChange change state0 |> expectChanged

    let nodeF' = state1.graph.nodes |> Map.find nodeF.id
    let nodeG' = state1.graph.nodes |> Map.find nodeG.id
    let nodeH' = state1.graph.nodes |> Map.find nodeH.id

    Assert.Equal("newf", nodeF'.text)
    Assert.Equal("newg", nodeG'.text)
    Assert.Equal("newh", nodeH'.text)

    let state2 = History.undo state1 |> expectChanged

    let nodeF'' = state2.graph.nodes |> Map.find nodeF.id
    let nodeG'' = state2.graph.nodes |> Map.find nodeG.id
    let nodeH'' = state2.graph.nodes |> Map.find nodeH.id

    Assert.Equal(nodeF.text, nodeF''.text)
    Assert.Equal(nodeG.text, nodeG''.text)
    Assert.Equal(nodeH.text, nodeH''.text)

[<Fact>]
let ``Apply NewNode adds node to graph`` () =
    let state = ModelBuilder.createState12 ()
    let nodeId = NodeId.New()
    let op = Op.NewNode(nodeId, "hello")
    let state2 = Op.apply op state |> expectChanged
    Assert.True(Graph.contains nodeId state2.graph)
    let node = state2.graph.nodes |> Map.find nodeId
    Assert.Equal("hello", node.text)

[<Fact>]
let ``Apply SetText updates node text`` () =
    let state = ModelBuilder.createState12 ()
    let nodeId = state.graph.root
    let op = Op.SetText(nodeId, "root", "new root text")
    let state2 = Op.apply op state |> expectChanged
    let node = state2.graph.nodes |> Map.find nodeId
    Assert.Equal("new root text", node.text)

[<Fact>]
let ``Apply Replace updates parent children`` () =
    let state = ModelBuilder.createState12 ()
    let parentId = state.graph.root
    let parent = state.graph.nodes |> Map.find parentId
    let originalChildren = parent.children
    let oldChild0 = originalChildren |> List.head
    let newNodeId = NodeId.New()
    let state1 = Op.apply (Op.NewNode(newNodeId, "new")) state |> expectChanged
    let op = Op.Replace(parentId, 0, [ oldChild0 ], [ newNodeId ])
    let state2 = Op.apply op state1 |> expectChanged
    let updatedParent = state2.graph.nodes |> Map.find parentId
    Assert.Equal(newNodeId, updatedParent.children.[0])
