module DocumentAssemblyTests

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
      fileState = FileState.defaultValue
      updateTime = NodeUpdateTime.missing }

let private normalNode (id: NodeId) (text: string) (owner: NodeId) : Node =
    { id = id
      text = text
      name = Filename.Empty
      children = []
      cssClasses = CssClass.empty
      owner = owner
      kind = Normal
      fileState = FileState.defaultValue
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
    let dirNode =
        { specialNode dirId Directory "inner" fileId with
            children = [ { ref = Ownership.Owner; id = normalId } ] }
    let normalNode = normalNode normalId "nested" dirId
    let fileNode =
        { specialNode fileId File "container.txt" Graph.rootId with
            children = [ { ref = Ownership.Owner; id = dirId } ] }

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

    graph2, fileId, dirId, normalId

let private artifactMap (graph: Graph) : Map<string, string> =
    graph.nodes
    |> Map.toSeq
    |> Seq.choose (fun (id, _) ->
        if DocumentPartition.isDocumentRootNode graph id then
            DocumentPartition.artifactFileRelative graph id
            |> Option.bind (fun rel ->
                match DocumentFormat.writeArtifact graph id rel None with
                | Ok text -> Some (rel, text)
                | Error _ -> None)
        else
            None)
    |> Map.ofSeq

[<Fact>]
let ``classifyArtifactRelative recognizes canonical and nested paths`` () =
    let ws =
        DocumentAssembly.classifyArtifactRelative "@home/.amb"
        |> requireOk "workspace"
    Assert.Equal(DocumentAssembly.DocumentArtifactKind.Directory, ws.kind)

    let dir =
        DocumentAssembly.classifyArtifactRelative "@home/docs/.amb"
        |> requireOk "directory"
    Assert.Equal(DocumentAssembly.DocumentArtifactKind.Directory, dir.kind)

    let file =
        DocumentAssembly.classifyArtifactRelative "@home/docs/readme.txt"
        |> requireOk "file"
    Assert.Equal(DocumentAssembly.DocumentArtifactKind.File, file.kind)

    let root =
        DocumentAssembly.classifyArtifactRelative ".amb"
        |> requireOk "root"
    Assert.Equal(DocumentAssembly.DocumentArtifactKind.Directory, root.kind)

    let trash =
        DocumentAssembly.classifyArtifactRelative "TRASH/.amb"
        |> requireOk "trash"
    Assert.Equal(DocumentAssembly.DocumentArtifactKind.Directory, trash.kind)

    let rootFile =
        DocumentAssembly.classifyArtifactRelative "name.ext"
        |> requireOk "root file"
    Assert.Equal(DocumentAssembly.DocumentArtifactKind.File, rootFile.kind)

    let rootDir =
        DocumentAssembly.classifyArtifactRelative "inner/.amb"
        |> requireOk "root dir"
    Assert.Equal(DocumentAssembly.DocumentArtifactKind.Directory, rootDir.kind)

[<Fact>]
let ``classifyArtifactRelative rejects stray amb files`` () =
    match DocumentAssembly.classifyArtifactRelative "foo.amb" with
    | Ok _ -> failwith "expected error"
    | Error _ -> ()

[<Fact>]
let ``artifactRelativeForNodeReference maps workspace directory and file paths`` () =
    let ws =
        DocumentAssembly.artifactRelativeForNodeReference "//home"
        |> requireOk "workspace"
    Assert.Equal("@home/.amb", ws)

    let dir =
        DocumentAssembly.artifactRelativeForNodeReference "//home/docs/"
        |> requireOk "directory"
    Assert.Equal("@home/docs/.amb", dir)

    let file =
        DocumentAssembly.artifactRelativeForNodeReference "//home/docs/readme.txt"
        |> requireOk "file"
    Assert.Equal("@home/docs/readme.txt", file)

[<Fact>]
let ``artifactRelativeForNodeReference maps root file and directory paths`` () =
    let rootFile =
        DocumentAssembly.artifactRelativeForNodeReference "//name.ext"
        |> requireOk "root file"
    Assert.Equal("name.ext", rootFile)

    let rootDir =
        DocumentAssembly.artifactRelativeForNodeReference "//docs/"
        |> requireOk "root dir"
    Assert.Equal("docs/.amb", rootDir)

[<Fact>]
let ``artifactRelativeForNodeReference maps file owns directory to sibling amb`` () =
    let inner =
        DocumentAssembly.artifactRelativeForNodeReference "//container.txt/inner/"
        |> requireOk "non-canonical file owns directory"
    Assert.Equal("inner/.amb", inner)

    let canonical =
        DocumentAssembly.artifactRelativeForNodeReference "//inner/"
        |> requireOk "canonical root directory"
    Assert.Equal("inner/.amb", canonical)

[<Fact>]
let ``assembleFromArtifacts ignores stray amb in artifact map`` () =
    let expected, _, _, _, _ = graphWithNestedDocs ()
    let artifacts =
        artifactMap expected
        |> Map.add "foo.amb" "stray"
    DocumentAssembly.assembleFromArtifacts artifacts |> requireOk "assemble" |> ignore

[<Fact>]
let ``assembleFromArtifacts stubs missing referenced artifact`` () =
    let expected, fileId, dirId, _ = graphFileOwnsDirectory ()
    let artifacts = artifactMap expected |> Map.remove "inner/.amb"
    let actual = DocumentAssembly.assembleFromArtifacts artifacts |> requireOk "assemble"
    let dirNode = actual.nodes.[dirId]
    Assert.Equal(NodeKind.Special SpecialKind.Directory, dirNode.kind)
    Assert.Equal("inner", Filename.tryValue dirNode.name |> Option.get)
    Assert.Empty(dirNode.children)
    Assert.Equal(dirId, actual.nodes.[fileId].children.Head.id)

[<Fact>]
let ``scanRefIndex extracts workspace ref from ROOT text`` () =
    let wsId = NodeId.New()
    let rootText = "-> //home^" + AmbDocument.formatStableId wsId + System.Environment.NewLine
    let index = DocumentAssembly.scanRefIndex [ rootText ] |> requireOk "scan"
    Assert.Equal(wsId, index.["//home"])

[<Fact>]
let ``assembleFromArtifacts round trips nested workspace tree`` () =
    let expected, wsId, dirId, fileId, normalId = graphWithNestedDocs ()
    let artifacts = artifactMap expected
    let actual = DocumentAssembly.assembleFromArtifacts artifacts |> requireOk "assemble"
    let actualNormalId = actual.nodes.[fileId].children.Head.id
    Assert.Equal("body", actual.nodes.[actualNormalId].text)
    Assert.Equal(wsId, actual.nodes.[Graph.workspacesId].children.Head.id)
    Assert.Equal(dirId, actual.nodes.[wsId].children.Head.id)
    Assert.Equal(fileId, actual.nodes.[dirId].children.Head.id)

[<Fact>]
let ``assembleFromArtifacts round trips file owns directory boundary`` () =
    let expected, fileId, dirId, normalId = graphFileOwnsDirectory ()
    let artifacts = artifactMap expected
    let actual = DocumentAssembly.assembleFromArtifacts artifacts |> requireOk "assemble"
    let actualNormalId = actual.nodes.[dirId].children.Head.id
    Assert.Equal("nested", actual.nodes.[actualNormalId].text)
    Assert.Equal(dirId, actual.nodes.[fileId].children.Head.id)
    Assert.True(actual.nodes.[dirId].children |> List.exists (fun c -> c.id = actualNormalId))

[<Fact>]
let ``validateAssembledGraph catches missing ref target`` () =
    let graph0 = Graph.create ()
    let parentId = NodeId.New()
    let missingId = NodeId.New()
    let parent =
        { id = parentId
          text = "parent"
          name = Filename.Empty
          children = [ { ref = Ownership.Ref; id = missingId } ]
          cssClasses = CssClass.empty
          owner = Graph.rootId
          kind = Normal
          fileState = FileState.defaultValue
          updateTime = NodeUpdateTime.missing }
    let graph =
        graph0.nodes
        |> Map.add parentId parent
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    match DocumentAssembly.validateAssembledGraph graph with
    | Ok _ -> failwith "expected error"
    | Error msg -> Assert.Contains("ref", msg)

[<Fact>]
let ``validateAssembledGraph catches overlapping document membership`` () =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let sharedId = NodeId.New()
    let wsNode = specialNode wsId Workspace "home" Graph.workspacesId
    let sharedNode = normalNode sharedId "shared" wsId
    let graph1 =
        graph0.nodes
        |> Map.add wsId wsNode
        |> Map.add sharedId sharedNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    let graph2 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph1
        |> requireOk "workspaces->ws"
    let graph3 =
        Graph.replace wsId 0 [] (owned [ sharedId ]) graph2
        |> requireOk "ws->shared"
    let graph4 =
        Graph.replace Graph.rootId 0 [] (owned [ sharedId ]) graph3
        |> requireOk "root->shared"
    match DocumentAssembly.validateAssembledGraph graph4 with
    | Ok _ -> failwith "expected error"
    | Error msg -> Assert.Contains("member", msg)
