module WorkspaceOpsTests

open Gambol.Shared
open Xunit

// ─── helpers ───────────────────────────────────────────────────────────────

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private requireChanged (r: ApplyResult) =
    match r with
    | ApplyResult.Changed s -> s
    | ApplyResult.Unchanged _ -> failwith "expected Changed, got Unchanged"
    | ApplyResult.Invalid(_, msg) -> failwithf "expected Changed, got Invalid: %s" msg

let private requireInvalid (r: ApplyResult) : State * string =
    match r with
    | ApplyResult.Invalid(s, msg) -> s, msg
    | ApplyResult.Changed _ -> failwith "expected Invalid, got Changed"
    | ApplyResult.Unchanged _ -> failwith "expected Invalid, got Unchanged"

let private makeState (graph: Graph) : State =
    { graph = graph; history = History.empty; revision = Revision.Zero }

let private freshState () = makeState (Graph.create ())

let private owned id = { ref = Ownership.Owner; id = id }
let private asRef id = { ref = Ownership.Ref; id = id }

/// Add a node with Filename.Ok name directly into the graph's node map.
let private addNamedNode (name: string) (graph: Graph) : Graph * NodeId =
    let nodeId = NodeId.New()
    let node =
        { id = nodeId
          text = name
          name = Filename.Ok name
          children = []
          cssClasses = CssClass.empty
          owner = Graph.rootId
          kind = Normal
          updateTime = NodeUpdateTime.missing }
    Graph.fromNodes graph.root (graph.nodes |> Map.add nodeId node), nodeId

/// Insert a new owner child at the end of a node's child list.
let private appendOwned (parentId: NodeId) (childId: NodeId) (graph: Graph) : Graph =
    let childCount = graph.nodes.[parentId].children.Length
    Graph.replace parentId childCount [] [ owned childId ] graph
    |> requireOk "appendOwned"

// ─── Graph.setName ─────────────────────────────────────────────────────────

[<Fact>]
let ``SetName on special node updates name and text`` () =
    let nodeId = NodeId.New()
    let op = Op.NewSpecialNode(nodeId, Workspace, "my-ws")
    let state1 = Op.apply op (freshState ()) |> requireChanged
    let graph1 = state1.graph
    let graph2 = Graph.setName nodeId "my-ws" "renamed" graph1 |> requireOk "setName"
    let node = graph2.nodes.[nodeId]
    Assert.Equal(Filename.Ok "renamed", node.name)
    Assert.Equal("renamed", node.text)

[<Fact>]
let ``SetName on Normal updates name only not text`` () =
    let graph0 = Graph.create ()
    let graph1, nodeId = Graph.newNode "visible label" graph0
    let graph2 = Graph.setName nodeId "" "file-name" graph1 |> requireOk "setName"
    let node = graph2.nodes.[nodeId]
    Assert.Equal(Filename.Ok "file-name", node.name)
    Assert.Equal("visible label", node.text)

[<Fact>]
let ``SetName rejects canonical root id`` () =
    let graph = Graph.create ()
    Assert.True(Result.isError (Graph.setName Graph.rootId "ROOT" "other" graph))

[<Fact>]
let ``SetName rejects canonical trash id`` () =
    let graph = Graph.create ()
    Assert.True(Result.isError (Graph.setName Graph.trashId "Trash" "other" graph))

[<Fact>]
let ``SetName rejects canonical workspaces id`` () =
    let graph = Graph.create ()
    Assert.True(Result.isError (Graph.setName Graph.workspacesId "Workspaces" "other" graph))

[<Fact>]
let ``SetName rejects old name mismatch`` () =
    let graph0 = Graph.create ()
    let graph1, nodeId = addNamedNode "actual-name" graph0
    Assert.True(Result.isError (Graph.setName nodeId "wrong-name" "new-name" graph1))

[<Fact>]
let ``SetName rejects invalid new name`` () =
    let graph0 = Graph.create ()
    let graph1, nodeId = addNamedNode "old-name" graph0
    Assert.True(Result.isError (Graph.setName nodeId "old-name" "has spaces!" graph1))

[<Fact>]
let ``SetName rejects sibling name collision case-insensitive`` () =
    let graph0 = Graph.create ()
    let graph1, contId = Graph.newNode "container" graph0
    let graph2, nodeIdA = addNamedNode "alpha" graph1
    let graph3, nodeIdB = addNamedNode "beta" graph2
    let graph4 = appendOwned Graph.rootId contId graph3
    let graph5 = appendOwned contId nodeIdA graph4
    let graph6 = appendOwned contId nodeIdB graph5
    Assert.True(Result.isError (Graph.setName nodeIdA "alpha" "BETA" graph6))

[<Fact>]
let ``SetName same name as self is not a conflict`` () =
    let graph0 = Graph.create ()
    let graph1, nodeId = addNamedNode "alpha" graph0
    Assert.True(Result.isOk (Graph.setName nodeId "alpha" "alpha" graph1))

// ─── Graph.replace name uniqueness ─────────────────────────────────────────

[<Fact>]
let ``Replace rejects two new owner children with same name`` () =
    let graph0 = Graph.create ()
    let graph1, contId = Graph.newNode "container" graph0
    let graph2 = appendOwned Graph.rootId contId graph1
    let graph3, nodeIdA = addNamedNode "foo" graph2
    let graph4, nodeIdB = addNamedNode "foo" graph3
    let result = Graph.replace contId 0 [] [ owned nodeIdA; owned nodeIdB ] graph4
    Assert.True(Result.isError result)

[<Fact>]
let ``Replace rejects new child colliding with existing sibling name`` () =
    let graph0 = Graph.create ()
    let graph1, contId = Graph.newNode "container" graph0
    let graph2 = appendOwned Graph.rootId contId graph1
    let graph3, nodeIdA = addNamedNode "foo" graph2
    let graph4 = appendOwned contId nodeIdA graph3
    let graph5, nodeIdB = addNamedNode "FOO" graph4
    let result = Graph.replace contId 1 [] [ owned nodeIdB ] graph5
    Assert.True(Result.isError result)

[<Fact>]
let ``Replace allows ref child with same name as owner sibling`` () =
    let graph0 = Graph.create ()
    let graph1, contId = Graph.newNode "container" graph0
    let graph2 = appendOwned Graph.rootId contId graph1
    let graph3, nodeIdA = addNamedNode "foo" graph2
    let result = Graph.replace contId 0 [] [ owned nodeIdA; asRef nodeIdA ] graph3
    Assert.True(Result.isOk result)

[<Fact>]
let ``Replace allows owner children whose names are Empty`` () =
    let graph0 = Graph.create ()
    let graph1, contId = Graph.newNode "container" graph0
    let graph2, nodeIdA = Graph.newNode "a" graph1
    let graph3, nodeIdB = Graph.newNode "b" graph2
    let graph4 = appendOwned Graph.rootId contId graph3
    let result = Graph.replace contId 0 [] [ owned nodeIdA; owned nodeIdB ] graph4
    Assert.True(Result.isOk result)

// ─── Op apply / undo ───────────────────────────────────────────────────────

[<Fact>]
let ``NewSpecialNode Workspace apply creates node with correct kind name text`` () =
    let nodeId = NodeId.New()
    let op = Op.NewSpecialNode(nodeId, Workspace, "my-ws")
    let state1 = Op.apply op (freshState ()) |> requireChanged
    let node = state1.graph.nodes.[nodeId]
    Assert.Equal(Special Workspace, node.kind)
    Assert.Equal(Filename.Ok "my-ws", node.name)
    Assert.Equal("my-ws", node.text)

[<Fact>]
let ``NewSpecialNode Workspace undo removes the node`` () =
    let nodeId = NodeId.New()
    let op = Op.NewSpecialNode(nodeId, Workspace, "my-ws")
    let state1 = Op.apply op (freshState ()) |> requireChanged
    let state2 = Op.undo op state1 |> requireChanged
    Assert.False(state2.graph.nodes.ContainsKey(nodeId))

[<Fact>]
let ``NewSpecialNode Workspaces kind is rejected`` () =
    let op = Op.NewSpecialNode(NodeId.New(), Workspaces, "ws")
    let _, msg = Op.apply op (freshState ()) |> requireInvalid
    Assert.Contains("system-only", msg)

[<Fact>]
let ``NewSpecialNode Directory kind is allowed`` () =
    let op = Op.NewSpecialNode(NodeId.New(), Directory, "pkg")
    Op.apply op (freshState ()) |> requireChanged |> ignore

[<Fact>]
let ``NewSpecialNode invalid name is rejected`` () =
    let op = Op.NewSpecialNode(NodeId.New(), Workspace, "has spaces!")
    requireInvalid (Op.apply op (freshState ())) |> ignore

[<Fact>]
let ``SetName apply then undo round-trips name`` () =
    let graph0 = Graph.create ()
    let graph1, nodeId = addNamedNode "alpha" graph0
    let state0 = makeState graph1
    let op = Op.SetName(nodeId, "alpha", "beta")
    let state1 = Op.apply op state0 |> requireChanged
    Assert.Equal(Filename.Ok "beta", state1.graph.nodes.[nodeId].name)
    let state2 = Op.undo op state1 |> requireChanged
    Assert.Equal(Filename.Ok "alpha", state2.graph.nodes.[nodeId].name)

// ─── Change.invert ─────────────────────────────────────────────────────────

[<Fact>]
let ``Invert SetName swaps old and new`` () =
    let op = Op.SetName(NodeId.New(), "old", "new")
    let change = { id = 0; changeId = System.Guid.NewGuid(); ops = [ op ] }
    let inv = Change.invert change
    match inv.ops with
    | [ Op.SetName(_, invOld, invNew) ] ->
        Assert.Equal("new", invOld)
        Assert.Equal("old", invNew)
    | _ -> failwith "unexpected ops after invert"

[<Fact>]
let ``Invert NewSpecialNode is identity`` () =
    let id = NodeId.New()
    let op = Op.NewSpecialNode(id, Workspace, "ws-name")
    let change = { id = 0; changeId = System.Guid.NewGuid(); ops = [ op ] }
    let inv = Change.invert change
    match inv.ops with
    | [ Op.NewSpecialNode(invId, invKind, invName) ] ->
        Assert.Equal(id, invId)
        Assert.Equal(Workspace, invKind)
        Assert.Equal("ws-name", invName)
    | _ -> failwith "unexpected ops after invert"
