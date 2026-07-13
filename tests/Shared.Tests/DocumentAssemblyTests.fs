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
    Node.Create(
        id,
        text = name,
        name = Filename.create name,
        owner = owner,
        kind = Special kind)

let private normalNode (id: NodeId) (text: string) (owner: NodeId) : Node =
    Node.Create(id, text = text, owner = owner)

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
    let fileNode =
        { specialNode fileId File "container.txt" Graph.rootId with
            children = owned [ dirId ] }
    let dirNode =
        { specialNode dirId Directory "inner" fileId with
            children = owned [ normalId ] }
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
        DocumentAssembly.classifyArtifactRelative "home/.amb"
        |> requireOk "workspace"
    Assert.Equal(DocumentAssembly.DocumentArtifactKind.Directory, ws.kind)

    let dir =
        DocumentAssembly.classifyArtifactRelative "home/docs/.amb"
        |> requireOk "directory"
    Assert.Equal(DocumentAssembly.DocumentArtifactKind.Directory, dir.kind)

    let file =
        DocumentAssembly.classifyArtifactRelative "home/docs/readme.txt"
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
let ``classifyArtifactRelative recognizes named amb files as File`` () =
    let rootFile =
        DocumentAssembly.classifyArtifactRelative "foo.amb"
        |> requireOk "root named amb file"
    Assert.Equal(DocumentAssembly.DocumentArtifactKind.File, rootFile.kind)

    let nested =
        DocumentAssembly.classifyArtifactRelative "d/bob/cea.amb"
        |> requireOk "nested named amb file"
    Assert.Equal(DocumentAssembly.DocumentArtifactKind.File, nested.kind)
    Assert.Equal("d/bob/cea.amb", nested.relativePath)

[<Fact>]
let ``classifyCodec uses Amb for marker and named amb paths`` () =
    let marker =
        DocumentFormat.classifyCodec ".amb" |> requireOk "root marker"
    Assert.Equal(DocumentCodec.Amb, marker)

    let nestedDirInfo =
        DocumentFormat.classifyCodec "d/bob/.amb" |> requireOk "nested marker"
    Assert.Equal(DocumentCodec.Amb, nestedDirInfo)

    let named =
        DocumentFormat.classifyCodec "d/bob/cea.amb" |> requireOk "named amb"
    Assert.Equal(DocumentCodec.Amb, named)

    let plain =
        DocumentFormat.classifyCodec "d/bob/readme.txt" |> requireOk "plain"
    Assert.Equal(DocumentCodec.Plain, plain)

[<Fact>]
let ``artifactRelativeForNodeReference maps workspace directory and file paths`` () =
    let ws =
        DocumentAssembly.artifactRelativeForNodeReference "//home"
        |> requireOk "workspace"
    Assert.Equal("home/.amb", ws)

    let dir =
        DocumentAssembly.artifactRelativeForNodeReference "//home/docs/"
        |> requireOk "directory"
    Assert.Equal("home/docs/.amb", dir)

    let file =
        DocumentAssembly.artifactRelativeForNodeReference "//home/docs/readme.txt"
        |> requireOk "file"
    Assert.Equal("home/docs/readme.txt", file)

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
let ``assembleFromArtifacts round trips nested named amb file`` () =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let fileId = NodeId.New()
    let normalId = NodeId.New()
    let wsNode = specialNode wsId Workspace "d" Graph.workspacesId
    let dirNode = specialNode dirId Directory "bob" wsId
    let fileNode = specialNode fileId File "cea.amb" dirId
    let body = normalNode normalId "ready" fileId

    let graph1 =
        graph0.nodes
        |> Map.add wsId wsNode
        |> Map.add dirId dirNode
        |> Map.add fileId fileNode
        |> Map.add normalId body
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    let expected =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph1
        |> requireOk "workspaces->ws"
        |> fun g -> Graph.replace wsId 0 [] (owned [ dirId ]) g
        |> requireOk "ws->dir"
        |> fun g -> Graph.replace dirId 0 [] (owned [ fileId ]) g
        |> requireOk "dir->file"
        |> fun g -> Graph.replace fileId 0 [] (owned [ normalId ]) g
        |> requireOk "file->normal"

    let artifacts = artifactMap expected
    Assert.True(Map.containsKey "d/bob/cea.amb" artifacts)
    Assert.Equal(
        DocumentCodec.Amb,
        DocumentFormat.classifyCodec "d/bob/cea.amb" |> requireOk "cea codec")
    let viaFormat =
        DocumentFormat.writeArtifact expected fileId "d/bob/cea.amb" None
        |> requireOk "format write"
    let viaAmb = AmbDocument.write expected fileId |> requireOk "amb write"
    Assert.Equal(viaAmb, viaFormat)
    let actual = DocumentAssembly.assembleFromArtifacts artifacts |> requireOk "assemble"
    let actualNormalId = actual.nodes.[fileId].children.Head.id
    Assert.Equal("ready", actual.nodes.[actualNormalId].text)
    Assert.Equal(fileId, actual.nodes.[dirId].children.Head.id)

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
    Assert.Equal(NodeKind.Special SpecialKind.Workspace, dirNode.kind)
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
let ``validateAssembledGraph stubs missing ref target with Broken link`` () =
    let graph0 = Graph.create ()
    let parentId = NodeId.New()
    let missingId = NodeId.New()
    let parent =
        Node.Create(
            parentId,
            text = "parent",
            children = [ { ref = Ownership.Ref; id = missingId } ])
    let graph =
        graph0.nodes
        |> Map.add parentId parent
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    let actual =
        DocumentAssembly.validateAssembledGraph graph |> requireOk "validate"
    Assert.True(Map.containsKey missingId actual.nodes)
    Assert.Equal("Broken link.", actual.nodes.[missingId].text)
    Assert.Equal(missingId, actual.nodes.[parentId].children.Head.id)
    Assert.Equal(Ownership.Ref, actual.nodes.[parentId].children.Head.ref)

[<Fact>]
let ``validateAssembledGraph preserves existing text on missing-target stub`` () =
    let graph0 = Graph.create ()
    let parentId = NodeId.New()
    let targetId = NodeId.New()
    let parent =
        Node.Create(
            parentId,
            text = "parent",
            children = [ { ref = Ownership.Ref; id = targetId } ])
    let stub =
        Node.Create(targetId, text = "kept annotation")
    let graph =
        graph0.nodes
        |> Map.add parentId parent
        |> Map.add targetId stub
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    let actual =
        DocumentAssembly.validateAssembledGraph graph |> requireOk "validate"
    Assert.Equal("kept annotation", actual.nodes.[targetId].text)

[<Fact>]
let ``assembleFromArtifacts stubs dangling same-doc ref with Broken link`` () =
    let missingId = NodeId.New()
    let sid = AmbDocument.formatStableId missingId
    let artifacts =
        Map.ofList
            [ ".amb", "-> ^" + sid + System.Environment.NewLine ]
    let actual =
        DocumentAssembly.assembleFromArtifacts artifacts |> requireOk "assemble"
    Assert.True(Map.containsKey missingId actual.nodes)
    Assert.Equal("Broken link.", actual.nodes.[missingId].text)
    Assert.True(
        actual.nodes.[Graph.rootId].children
        |> List.exists (fun c -> c.id = missingId && c.ref = Ownership.Ref))

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

[<Fact>]
let ``readArtifact warm Amb keeps stable id on text edit`` () =
    let graph0 = Graph.create ()
    let docId = NodeId.New()
    let aId = NodeId.New()
    let docNode =
        Node.Create(
            docId,
            text = "notes.amb",
            name = Filename.Ok "notes.amb",
            owner = graph0.root,
            kind = Special File,
            children = owned [ aId ])
    let aNode = Node.Create(aId, text = "alpha", owner = docId)
    let graph =
        graph0.nodes
        |> Map.add docId docNode
        |> Map.add aId aNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    let previous =
        "^" + AmbDocument.formatStableId aId + " alpha\n"
    let edited =
        "^" + AmbDocument.formatStableId aId + " ALPHA\n"
    let after =
        DocumentFormat.readArtifact "notes.amb" edited docId graph (Some previous)
        |> requireOk "warm amb read"
    Assert.Equal(aId, after.nodes.[docId].children.Head.id)
    Assert.Equal("ALPHA", after.nodes.[aId].text)

[<Fact>]
let ``readArtifact warm Plain keeps id on line text edit`` () =
    let graph0 = Graph.create ()
    let docId = NodeId.New()
    let aId = NodeId.New()
    let bId = NodeId.New()
    let docNode =
        Node.Create(
            docId,
            text = "readme.txt",
            name = Filename.Ok "readme.txt",
            owner = graph0.root,
            kind = Special File,
            children = owned [ aId; bId ])
    let aNode = Node.Create(aId, text = "alpha", owner = docId)
    let bNode = Node.Create(bId, text = "beta", owner = docId)
    let graph =
        graph0.nodes
        |> Map.add docId docNode
        |> Map.add aId aNode
        |> Map.add bId bNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    let previous = "alpha\nbeta\n"
    let edited = "ALPHA\nbeta\n"
    let after =
        DocumentFormat.readArtifact "readme.txt" edited docId graph (Some previous)
        |> requireOk "warm plain read"
    Assert.Equal(aId, after.nodes.[docId].children.Head.id)
    Assert.Equal("ALPHA", after.nodes.[aId].text)
    Assert.Equal(bId, after.nodes.[docId].children.[1].id)

[<Fact>]
let ``readArtifact cold Amb ignores previous when None`` () =
    let graph0 = Graph.create ()
    let docId = NodeId.New()
    let docNode =
        Node.Create(
            docId,
            text = "notes.amb",
            name = Filename.Ok "notes.amb",
            owner = graph0.root,
            kind = Special File)
    let graph =
        graph0.nodes
        |> Map.add docId docNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    let text = "hello\n"
    let after =
        DocumentFormat.readArtifact "notes.amb" text docId graph None
        |> requireOk "cold amb read"
    Assert.Equal(1, after.nodes.[docId].children.Length)
    Assert.Equal("hello", after.nodes.[after.nodes.[docId].children.Head.id].text)
