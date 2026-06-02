module NodeDesktopPathTests

open Gambol.Shared
open Gambol.Shared.ViewModel
open Xunit

let private owned (ids: NodeId list) : ChildNode list =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private specialNode (id: NodeId) (kind: SpecialKind) (name: string) (owner: NodeId) : Node =
    { id = id
      text = name
      name = Filename.create name
      children = []
      cssClasses = CssClass.empty
      owner = owner
      kind = Special kind }

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

let private selectionOn (graph: Graph) (parentId: NodeId) (focusIdx: int) : Selection =
    let parent = graph.nodes.[parentId]
    let focusChild = parent.children.[focusIdx]

    { range =
        { parent =
            { instanceId = Sid 0
              nodeId = parentId
              parentInstanceId = None
              expanded = true
              childrenStale = false
              children = [] }
          start = focusIdx
          endd = focusIdx + 1 }
      focus = focusIdx }

[<Fact>]
let ``pathForNodeId Normal returns first file reference`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "[[note.txt]] body" ] graph0
    let path = NodeDesktopPath.pathForNodeId graph1 ids.[0]
    Assert.Equal(Some "note.txt", path)

[<Fact>]
let ``pathForNodeId Workspaces and Trash return None`` () =
    let graph = Graph.create ()
    Assert.Equal(None, NodeDesktopPath.pathForNodeId graph Graph.workspacesId)
    Assert.Equal(None, NodeDesktopPath.pathForNodeId graph Graph.trashId)

[<Fact>]
let ``pathForNodeId Workspace returns at-name colon`` () =
    let graph, wsId, _, _ = graphWithWorkspaceTree ()
    Assert.Equal(Some "@home:", NodeDesktopPath.pathForNodeId graph wsId)

[<Fact>]
let ``pathForNodeId Directory and File append owner path and name`` () =
    let graph, _, dirId, fileId = graphWithWorkspaceTree ()
    Assert.Equal(Some "@home:/docs", NodeDesktopPath.pathForNodeId graph dirId)
    Assert.Equal(Some "@home:/docs/readme.txt", NodeDesktopPath.pathForNodeId graph fileId)

[<Fact>]
let ``pathForNodeId Directory returns None when owner path is None`` () =
    let graph = Graph.create ()
    let dirId = NodeId.New()
    let dirNode = specialNode dirId Directory "orphan" Graph.workspacesId

    let graph1 =
        graph.nodes
        |> Map.add dirId dirNode
        |> fun nodes -> Graph.fromNodes graph.root nodes

    Assert.Equal(None, NodeDesktopPath.pathForNodeId graph1 dirId)

[<Fact>]
let ``fileReferenceForNodeId Normal preserves invalid file reference`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "broken [[reference" ] graph0

    match NodeDesktopPath.fileReferenceForNodeId graph1 ids.[0] with
    | Some InvalidFileReference -> ()
    | other -> Assert.True(false, $"expected InvalidFileReference, got {other}")

[<Fact>]
let ``tryFindFocusedPath returns focus id and path`` () =
    let graph, _, _, fileId = graphWithWorkspaceTree ()
    let parentId = graph.nodes.[fileId].owner
    let sel = selectionOn graph parentId 0

    match tryFindFocusedPath graph sel with
    | None -> Assert.True(false, "expected Some")
    | Some (focusId, path) ->
        Assert.Equal(fileId, focusId)
        Assert.Equal("@home:/docs/readme.txt", path)
