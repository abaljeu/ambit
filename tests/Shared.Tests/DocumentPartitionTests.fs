module DocumentPartitionTests

open Gambol.Shared
open Xunit

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private owned (ids: NodeId list) : ChildNode list =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

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
    let wsNode = specialNode wsId Workspace "home" Graph.workspacesId
    let dirNode = specialNode dirId Directory "docs" wsId
    let fileNode = specialNode fileId File "readme.txt" dirId
    let normalNode = normalNode normalId "body" fileId

    let graph1 =
        graph0.nodes
        |> Map.add wsId wsNode
        |> Map.add dirId dirNode
        |> Map.add fileId fileNode
        |> Map.add normalId normalNode
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
    let fileNode = specialNode fileId File "container.txt" Graph.rootId
    let dirNode = specialNode dirId Directory "inner" fileId
    let normalNode = normalNode normalId "nested" dirId

    let graph1 =
        graph0.nodes
        |> Map.add fileId fileNode
        |> Map.add dirId dirNode
        |> Map.add normalId normalNode
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
let ``documentRootForNode on normal resolves to file document root`` () =
    let graph, _, _, fileId, normalId = graphWithNestedDocs ()
    Assert.Equal(Some fileId, DocumentPartition.documentRootForNode graph normalId)

[<Fact>]
let ``documentRootForNode on workspace resolves to workspace id`` () =
    let graph, wsId, _, _, _ = graphWithNestedDocs ()
    Assert.Equal(Some wsId, DocumentPartition.documentRootForNode graph wsId)

[<Fact>]
let ``memberNodeIds for workspace excludes nested directory subtree`` () =
    let graph, wsId, dirId, fileId, normalId = graphWithNestedDocs ()
    let members = DocumentPartition.memberNodeIds graph wsId
    Assert.True(Set.contains wsId members)
    Assert.True(Set.contains dirId members)
    Assert.False(Set.contains fileId members)
    Assert.False(Set.contains normalId members)

[<Fact>]
let ``memberNodeIds nested file directory normal belongs to directory doc`` () =
    let graph, fileId, dirId, normalId = graphFileOwnsDirectory ()
    let fileMembers = DocumentPartition.memberNodeIds graph fileId
    let dirMembers = DocumentPartition.memberNodeIds graph dirId
    Assert.True(Set.contains fileId fileMembers)
    Assert.True(Set.contains dirId fileMembers)
    Assert.False(Set.contains normalId fileMembers)
    Assert.True(Set.contains dirId dirMembers)
    Assert.True(Set.contains normalId dirMembers)

[<Fact>]
let ``artifact paths for ROOT workspace`` () =
    let graph = Graph.create ()
    Assert.Equal(None, DocumentPartition.artifactDirectoryRelative graph Graph.rootId)
    Assert.Equal(Some ".amb", DocumentPartition.artifactFileRelative graph Graph.rootId)

[<Fact>]
let ``artifact paths for named workspace blue`` () =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let wsNode = specialNode wsId Workspace "blue" Graph.workspacesId
    let graph1 =
        graph0.nodes
        |> Map.add wsId wsNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    let graph2 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph1
        |> requireOk "workspaces->blue"
    Assert.Equal(Some "blue/", DocumentPartition.artifactDirectoryRelative graph2 wsId)
    Assert.Equal(Some "blue/.amb", DocumentPartition.artifactFileRelative graph2 wsId)

[<Fact>]
let ``artifact paths for TRASH`` () =
    let graph = Graph.create ()
    Assert.Equal(Some "TRASH/", DocumentPartition.artifactDirectoryRelative graph Graph.trashId)
    Assert.Equal(Some "TRASH/.amb", DocumentPartition.artifactFileRelative graph Graph.trashId)

[<Fact>]
let ``artifact paths for directory under workspace`` () =
    let graph, _, dirId, _, _ = graphWithNestedDocs ()
    Assert.Equal(Some "home/docs/", DocumentPartition.artifactDirectoryRelative graph dirId)
    Assert.Equal(Some "home/docs/.amb", DocumentPartition.artifactFileRelative graph dirId)

[<Fact>]
let ``artifact paths for file under workspace`` () =
    let graph, _, _, fileId, _ = graphWithNestedDocs ()
    Assert.Equal(None, DocumentPartition.artifactDirectoryRelative graph fileId)
    Assert.Equal(Some "home/docs/readme.txt", DocumentPartition.artifactFileRelative graph fileId)

[<Fact>]
let ``artifact paths for file under ROOT`` () =
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let fileNode = specialNode fileId File "name.ext" Graph.rootId
    let graph1 =
        graph0.nodes
        |> Map.add fileId fileNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    let idx = Graph.fileTreeInsertIndex graph1 Graph.rootId
    let graph2 =
        Graph.replace Graph.rootId idx [] (owned [ fileId ]) graph1
        |> requireOk "root->file"
    Assert.Equal(None, DocumentPartition.artifactDirectoryRelative graph2 fileId)
    Assert.Equal(Some "name.ext", DocumentPartition.artifactFileRelative graph2 fileId)

[<Fact>]
let ``artifact paths for directory owned by file use nearest directory ancestor`` () =
    let graph, fileId, dirId, _ = graphFileOwnsDirectory ()
    Assert.Equal(None, DocumentPartition.artifactDirectoryRelative graph fileId)
    Assert.Equal(Some "container.txt", DocumentPartition.artifactFileRelative graph fileId)
    Assert.Equal(Some "inner/", DocumentPartition.artifactDirectoryRelative graph dirId)
    Assert.Equal(Some "inner/.amb", DocumentPartition.artifactFileRelative graph dirId)

[<Fact>]
let ``write nested file directory emits ref not directory children`` () =
    let graph, fileId, dirId, normalId = graphFileOwnsDirectory ()
    let text =
        AmbDocument.write graph fileId
        |> function
            | Ok s -> s
            | Error msg -> failwith msg
    let dirSid = AmbDocument.formatStableId dirId
    let normalSid = AmbDocument.formatStableId normalId
    let dirPath = NodeDesktopPath.pathForNodeId graph dirId |> Option.get
    Assert.Contains("-> " + dirPath + "^" + dirSid, text)
    Assert.DoesNotContain("^" + normalSid, text)
