module DocumentPathMoveTests

open Gambol.Shared
open Xunit

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private applyOps (graph: Graph) (ops: Op list) : Graph =
    let state = { graph = graph; history = History.empty; revision = Revision.Zero }
    ops
    |> List.fold (fun s op ->
        match Op.apply op s with
        | ApplyResult.Changed s' -> s'
        | ApplyResult.Unchanged s' -> s'
        | ApplyResult.Invalid(_, msg) -> failwith msg) state
    |> fun s -> s.graph

let private owned ids =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

let private graphWithWorkspaceFile () : Graph * NodeId * NodeId =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let fileId = NodeId.New()
    let wsNode =
        { id = wsId
          text = "home"
          name = Filename.Ok "home"
          children = []
          cssClasses = CssClass.empty
          owner = Graph.workspacesId
          kind = Special Workspace
          updateTime = NodeUpdateTime.missing }
    let fileNode =
        { id = fileId
          text = "readme.txt"
          name = Filename.Ok "readme.txt"
          children = []
          cssClasses = CssClass.empty
          owner = wsId
          kind = Special File
          updateTime = NodeUpdateTime.missing }

    let graph1 =
        graph0.nodes
        |> Map.add wsId wsNode
        |> Map.add fileId fileNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    let graph2 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph1
        |> requireOk "workspaces->ws"

    let graph3 =
        Graph.replace wsId 0 [] (owned [ fileId ]) graph2
        |> requireOk "ws->file"

    graph3, wsId, fileId

let private specialNode (id: NodeId) (kind: SpecialKind) (name: string) (owner: NodeId) : Node =
    { id = id
      text = name
      name = Filename.create name
      children = []
      cssClasses = CssClass.empty
      owner = owner
      kind = Special kind
      updateTime = NodeUpdateTime.missing }

let private normalNode (id: NodeId) (text: string) (owner: NodeId) : Node =
    { id = id
      text = text
      name = Filename.Empty
      children = []
      cssClasses = CssClass.empty
      owner = owner
      kind = Normal
      updateTime = NodeUpdateTime.missing }

let private graphWithNestedDocs () : Graph * NodeId * NodeId * NodeId * NodeId =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let fileId = NodeId.New()
    let normalId = NodeId.New()

    let graph1 =
        graph0.nodes
        |> Map.add wsId (specialNode wsId Workspace "home" Graph.workspacesId)
        |> Map.add dirId (specialNode dirId Directory "docs" wsId)
        |> Map.add fileId (specialNode fileId File "readme.txt" dirId)
        |> Map.add normalId (normalNode normalId "body" fileId)
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    let graph2 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph1
        |> requireOk "workspaces->ws"

    let graph3 =
        Graph.replace wsId 0 [] (owned [ dirId ]) graph2
        |> requireOk "ws->dir"

    let graph4 =
        Graph.replace dirId 0 [] (owned [ fileId ]) graph3
        |> requireOk "dir->file"

    let graph5 =
        Graph.replace fileId 0 [] (owned [ normalId ]) graph4
        |> requireOk "file->normal"

    graph5, wsId, dirId, fileId, normalId

let private graphFileOwnsDirectory () : Graph * NodeId * NodeId * NodeId =
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let dirId = NodeId.New()
    let normalId = NodeId.New()

    let graph1 =
        graph0.nodes
        |> Map.add fileId (specialNode fileId File "container.txt" Graph.rootId)
        |> Map.add dirId (specialNode dirId Directory "inner" fileId)
        |> Map.add normalId (normalNode normalId "nested" dirId)
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    let idx = Graph.fileTreeInsertIndex graph1 Graph.rootId
    let graph2 =
        Graph.replace Graph.rootId idx [] (owned [ fileId ]) graph1
        |> requireOk "root->file"

    let graph3 =
        Graph.replace fileId 0 [] (owned [ dirId ]) graph2
        |> requireOk "file->dir"

    let graph4 =
        Graph.replace dirId 0 [] (owned [ normalId ]) graph3
        |> requireOk "dir->normal"

    graph4, fileId, dirId, normalId

[<Fact>]
let ``planPathMoveForSetName returns new path for workspace rename`` () =
    let graph, wsId, _ = graphWithWorkspaceFile ()
    match DocumentPathMove.planPathMoveForSetName graph wsId "renamed" with
    | None -> Assert.Fail "expected Some"
    | Some move ->
        Assert.Equal("//home", move.oldPath)
        Assert.Equal("//renamed", move.newPath)
        Assert.Equal(wsId, move.nodeId)

[<Fact>]
let ``planPathMoveForReparent returns none for workspace move to trash`` () =
    let graph, wsId, _ = graphWithWorkspaceFile ()
    Assert.Equal(None, DocumentPathMove.planPathMoveForReparent graph wsId Graph.trashId)

[<Fact>]
let ``planRenameNode rejects canonical trash id`` () =
    let graph = Graph.create ()
    Assert.True(Result.isError (NodeRenameOps.planRenameNode graph Graph.trashId "other"))

[<Fact>]
let ``planRenameNode on Normal updates name not text`` () =
    let graph0 = Graph.create ()
    let graph1, nodeId = Graph.newNode "visible label" graph0
    match NodeRenameOps.planRenameNode graph1 nodeId "file-name" with
    | Error e -> Assert.Fail e
    | Ok (ops, pathMove) ->
        Assert.Equal(None, pathMove)
        let graph2 = applyOps graph1 ops
        let node = graph2.nodes.[nodeId]
        Assert.Equal(Filename.Ok "file-name", node.name)
        Assert.Equal("visible label", node.text)

[<Fact>]
let ``planRenameNode returns empty ops when name is unchanged`` () =
    let graph0 = Graph.create ()
    let graph1, nodeId = Graph.newNode "visible label" graph0
    let graph2 =
        Graph.setName nodeId "" "file-name" graph1
        |> requireOk "set initial name"
    match NodeRenameOps.planRenameNode graph2 nodeId "file-name" with
    | Error e -> Assert.Fail e
    | Ok (ops, pathMove) ->
        Assert.Empty(ops)
        Assert.Equal(None, pathMove)

[<Fact>]
let ``pathForNodeId trash returns TRASH directory path`` () =
    let graph = Graph.create ()
    Assert.Equal(Some "//TRASH/", NodeDesktopPath.pathForNodeId graph Graph.trashId)

[<Fact>]
let ``planPathMovesBetweenGraphs includes nested roots after directory rename`` () =
    let graph, _, dirId, fileId, _ = graphWithNestedDocs ()
    let postGraph = Graph.setName dirId "docs" "archive" graph |> requireOk "rename dir"
    let moves = DocumentPathMove.planPathMovesBetweenGraphs graph postGraph

    Assert.Contains(
        { nodeId = dirId; oldPath = "//home/docs/"; newPath = "//home/archive/" },
        moves)
    Assert.Contains(
        { nodeId = fileId
          oldPath = "//home/docs/readme.txt"
          newPath = "//home/archive/readme.txt" },
        moves)

[<Fact>]
let ``planPathMovesBetweenGraphs includes file reparent across workspaces`` () =
    let graph, wsId, fileId = graphWithWorkspaceFile ()
    let ws2Id = NodeId.New()
    let ws2Node = specialNode ws2Id Workspace "other" Graph.workspacesId
    let graph1 =
        graph.nodes
        |> Map.add ws2Id ws2Node
        |> fun nodes -> Graph.fromNodes graph.root nodes
    let graph2 =
        Graph.replace Graph.workspacesId 1 [] (owned [ ws2Id ]) graph1
        |> requireOk "insert second ws"
    let graph3 =
        Graph.replace wsId 0 (owned [ fileId ]) [] graph2
        |> requireOk "remove file"
    let postGraph =
        Graph.replace ws2Id 0 [] (owned [ fileId ]) graph3
        |> requireOk "insert file"

    let moves = DocumentPathMove.planPathMovesBetweenGraphs graph postGraph
    Assert.Equal<DocumentPathMove list>(
        [ { nodeId = fileId
            oldPath = "//home/readme.txt"
            newPath = "//other/readme.txt" } ],
        moves)

[<Fact>]
let ``planPathMovesBetweenGraphs includes file move to trash`` () =
    let graph, wsId, fileId = graphWithWorkspaceFile ()
    let graph1 =
        Graph.replace wsId 0 (owned [ fileId ]) [] graph
        |> requireOk "remove file"
    let postGraph =
        Graph.replace Graph.trashId 0 [] (owned [ fileId ]) graph1
        |> requireOk "trash file"

    let moves = DocumentPathMove.planPathMovesBetweenGraphs graph postGraph
    Assert.Equal<DocumentPathMove list>(
        [ { nodeId = fileId
            oldPath = "//home/readme.txt"
            newPath = "//TRASH/readme.txt" } ],
        moves)

[<Fact>]
let ``coalescePathMoves drops nested roots covered by directory move`` () =
    let graph, _, dirId, fileId, _ = graphWithNestedDocs ()
    let postGraph = Graph.setName dirId "docs" "archive" graph |> requireOk "rename dir"
    let moves = DocumentPathMove.planPathMovesBetweenGraphs graph postGraph
    let coalesced = DocumentPathMove.coalescePathMoves graph moves

    Assert.Equal<DocumentPathMove list>(
        [ { nodeId = dirId; oldPath = "//home/docs/"; newPath = "//home/archive/" } ],
        coalesced)
    Assert.DoesNotContain(coalesced, fun move -> move.nodeId = fileId)

[<Fact>]
let ``coalescePathMoves keeps file-owned directory separate from file move`` () =
    let graph, fileId, dirId, _ = graphFileOwnsDirectory ()
    let postGraph = Graph.setName fileId "container.txt" "box.txt" graph |> requireOk "rename file"
    let moves = DocumentPathMove.planPathMovesBetweenGraphs graph postGraph
    let coalesced = DocumentPathMove.coalescePathMoves graph moves

    Assert.Contains(coalesced, fun move -> move.nodeId = fileId)
    Assert.Contains(coalesced, fun move -> move.nodeId = dirId)
