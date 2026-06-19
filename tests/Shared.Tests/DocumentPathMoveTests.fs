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

[<Fact>]
let ``planPathMoveForSetName returns new path for workspace rename`` () =
    let graph, wsId, _ = graphWithWorkspaceFile ()
    match DocumentPathMove.planPathMoveForSetName graph wsId "renamed" with
    | None -> Assert.Fail "expected Some"
    | Some move ->
        Assert.Equal("@home:", move.oldPath)
        Assert.Equal("@renamed:", move.newPath)
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
    Assert.Equal(Some "@:/TRASH/", NodeDesktopPath.pathForNodeId graph Graph.trashId)
