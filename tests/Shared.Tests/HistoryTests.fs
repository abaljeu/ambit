module HistoryTests

open Gambol.Shared
open Xunit

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
let ``Apply NewNode adds node to graph`` () =
    let state = ModelBuilder.createState12 ()
    let nodeId = NodeId.New()
    let op = Op.NewNode(nodeId, "hello")
    let state2 = Op.apply op state
    Assert.True(Graph.contains nodeId state2.graph)
    let node = state2.graph.nodes |> Map.find nodeId
    Assert.Equal("hello", node.text)

[<Fact>]
let ``Apply SetText updates node text`` () =
    let state = ModelBuilder.createState12 ()
    let nodeId = state.graph.root
    let op = Op.SetText(nodeId, "root", "new root text")
    let state2 = Op.apply op state
    let node = state2.graph.nodes |> Map.find nodeId
    Assert.Equal("new root text", node.text)

[<Fact>]
let ``Apply Replace updates parent children`` () =
    let state = ModelBuilder.createState12 ()
    let parentId = state.graph.root
    let parent = state.graph.nodes |> Map.find parentId
    let originalChildren = parent.children
    let newNodeId = NodeId.New()
    let state1 = Op.apply (Op.NewNode(newNodeId, "new")) state
    let op = Op.Replace(parentId, 0, [ originalChildren.[0] ], [ newNodeId ])
    let state2 = Op.apply op state1
    let updatedParent = state2.graph.nodes |> Map.find parentId
    Assert.Equal(newNodeId, updatedParent.children.[0])
