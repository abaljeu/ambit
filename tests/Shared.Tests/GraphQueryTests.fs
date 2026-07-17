module GraphQueryTests

open Gambol.Shared
open Xunit

let private owned (ids: NodeId list) : ChildNode list =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private specialNode (id: NodeId) (kind: SpecialKind) (name: string) (owner: NodeId) : Node =
    Node.Create(
        id,
        text = name,
        name = Filename.create name,
        owner = owner,
        kind = Special kind)

let private graphWithWorkspaceTree () : Graph * NodeId * NodeId * NodeId =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let fileId = NodeId.New()
    let wsNode = specialNode wsId Workspace "home" Graph.workspacesId
    let dirNode = specialNode dirId Directory "docs" wsId
    let fileNode = specialNode fileId File "readme.txt" dirId

    let graph1 =
        graph0.nodes
        |> Map.add wsId wsNode
        |> Map.add dirId dirNode
        |> Map.add fileId fileNode
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

    graph4, wsId, dirId, fileId

[<Fact>]
let ``enclosing finds first owner-chain match including start`` () =
    let graph, wsId, dirId, fileId = graphWithWorkspaceTree ()
    let isDirectory node =
        match node.kind with
        | Special Directory -> true
        | _ -> false

    Assert.Equal(Some dirId, GraphQuery.enclosing graph isDirectory dirId)
    Assert.Equal(Some dirId, GraphQuery.enclosing graph isDirectory fileId)
    Assert.Equal(None, GraphQuery.enclosing graph isDirectory wsId)

[<Fact>]
let ``enclosingWorkspace resolves named workspace ROOT and TRASH`` () =
    let graph, wsId, dirId, fileId = graphWithWorkspaceTree ()
    Assert.Equal(Some wsId, GraphQuery.enclosingWorkspace graph wsId)
    Assert.Equal(Some wsId, GraphQuery.enclosingWorkspace graph dirId)
    Assert.Equal(Some wsId, GraphQuery.enclosingWorkspace graph fileId)
    Assert.Equal(Some Graph.rootId, GraphQuery.enclosingWorkspace graph Graph.rootId)
    Assert.Equal(Some Graph.trashId, GraphQuery.enclosingWorkspace graph Graph.trashId)
    Assert.Equal(
        Some Graph.rootId,
        GraphQuery.enclosingWorkspace graph Graph.workspacesId)

[<Fact>]
let ``resolveOwnedFileDirectoryInsert returns Some focus for nested normal under Directory`` () =
    let graph0 = Graph.create ()
    let dirId = NodeId.New()
    let dirNode = specialNode dirId Directory "docs" Graph.rootId
    let graph1 =
        graph0.nodes
        |> Map.add dirId dirNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    let idx = Graph.fileTreeInsertIndex graph1 Graph.rootId
    let graph2 =
        Graph.replace Graph.rootId idx [] (owned [ dirId ]) graph1
        |> requireOk "root->dir"
    let graph3, focusId = Graph.newNode "nested" graph2
    let graph4 =
        Graph.replace dirId 0 [] (owned [ focusId ]) graph3
        |> requireOk "dir->nested"
    match GraphQuery.resolveOwnedFileDirectoryInsert graph4 focusId with
    | Some(parentId, _) -> Assert.Equal(focusId, parentId)
    | None -> Assert.True(false, "expected Some(focus)")

[<Fact>]
let ``resolveOwnedFileDirectoryInsert returns None for nested normal under File`` () =
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let fileNode = specialNode fileId File "note.txt" Graph.rootId
    let graph1 =
        graph0.nodes
        |> Map.add fileId fileNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    let idx = Graph.fileTreeInsertIndex graph1 Graph.rootId
    let graph2 =
        Graph.replace Graph.rootId idx [] (owned [ fileId ]) graph1
        |> requireOk "root->file"
    let graph3, focusId = Graph.newNode "nested" graph2
    let graph4 =
        Graph.replace fileId 0 [] (owned [ focusId ]) graph3
        |> requireOk "file->nested"
    Assert.Equal(
        None,
        GraphQuery.resolveOwnedFileDirectoryInsert graph4 focusId)

[<Fact>]
let ``ownedNameTaken is true across Normal branches in same Directory`` () =
    let graph0 = Graph.create ()
    let dirId = NodeId.New()
    let dirNode = specialNode dirId Directory "docs" Graph.rootId
    let graph1 =
        graph0.nodes
        |> Map.add dirId dirNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    let idx = Graph.fileTreeInsertIndex graph1 Graph.rootId
    let graph2 =
        Graph.replace Graph.rootId idx [] (owned [ dirId ]) graph1
        |> requireOk "root->dir"
    let graph3, n1 = Graph.newNode "n1" graph2
    let graph4, n2 = Graph.newNode "n2" graph3
    let graph5 =
        Graph.replace dirId 0 [] (owned [ n1; n2 ]) graph4
        |> requireOk "dir->normals"
    let fileId = NodeId.New()
    let fileNode = specialNode fileId File "a.txt" n1
    let graph6 =
        graph5.nodes
        |> Map.add fileId fileNode
        |> fun nodes -> Graph.fromNodes graph5.root nodes
    let graph7 =
        Graph.replace n1 0 [] (owned [ fileId ]) graph6
        |> requireOk "n1->file"
    Assert.True(GraphQuery.ownedNameTaken graph7 n2 None "a.txt")
    Assert.False(GraphQuery.ownedNameTaken graph7 n2 None "other.txt")
