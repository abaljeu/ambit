module DocumentPersistenceTests

open System
open System.IO
open Gambol.Server
open Gambol.Server.Tests.TestBackend
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

let private graphWithRootFile () : Graph * NodeId =
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

    graph2, fileId

let private artifactFullPath (dataDir: string) (graph: Graph) (documentRootId: NodeId) =
    DocumentPersistence.resolveArtifactPath dataDir graph documentRootId
    |> requireOk "resolveArtifactPath"

let private assertNestedWorkspaceLoad (expected: Graph) (actual: Graph) =
    let wsId =
        expected.nodes
        |> Map.toSeq
        |> Seq.pick (fun (id, node) ->
            match node.kind with
            | NodeKind.Special SpecialKind.Workspace when id <> Graph.rootId -> Some id
            | _ -> None)
    let dirId = expected.nodes.[wsId].children.Head.id
    let fileId = expected.nodes.[dirId].children.Head.id
    let actualNormalId = actual.nodes.[fileId].children.Head.id
    Assert.Equal("body", actual.nodes.[actualNormalId].text)
    Assert.Equal(wsId, actual.nodes.[Graph.workspacesId].children.Head.id)
    Assert.Equal(dirId, actual.nodes.[wsId].children.Head.id)
    Assert.Equal(fileId, actual.nodes.[dirId].children.Head.id)

let private assertFileOwnsDirectoryLoad (expected: Graph) (actual: Graph) =
    let fileId =
        expected.nodes
        |> Map.toSeq
        |> Seq.pick (fun (id, node) ->
            match node.kind with
            | NodeKind.Special SpecialKind.File -> Some id
            | _ -> None)
    let dirId = expected.nodes.[fileId].children.Head.id
    let actualNormalId = actual.nodes.[dirId].children.Head.id
    Assert.Equal("nested", actual.nodes.[actualNormalId].text)
    Assert.Equal(dirId, actual.nodes.[fileId].children.Head.id)
    Assert.True(actual.nodes.[dirId].children |> List.exists (fun c -> c.id = actualNormalId))

let private graphWithPlainFileRef () : Graph * NodeId * NodeId * NodeId =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let fileId = NodeId.New()
    let holderId = NodeId.New()
    let sharedId = NodeId.New()
    let wsNode = specialNode wsId Workspace "home" Graph.workspacesId
    let fileNode = specialNode fileId File "readme.txt" wsId
    let holderNode =
        { normalNode holderId "holder" fileId with
            children = [ { ref = Ownership.Ref; id = sharedId } ] }
    let sharedNode = normalNode sharedId "shared text" Graph.rootId

    let graph1 =
        graph0.nodes
        |> Map.add wsId wsNode
        |> Map.add fileId fileNode
        |> Map.add holderId holderNode
        |> Map.add sharedId sharedNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    let graph2 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph1
        |> requireOk "workspaces->ws"

    let graph3 =
        Graph.replace wsId 0 [] (owned [ fileId ]) graph2
        |> requireOk "ws->file"

    let graph4 =
        Graph.replace fileId 0 [] (owned [ holderId ]) graph3
        |> requireOk "file->holder"

    graph4, wsId, fileId, sharedId

[<Fact>]
let ``writeAllDocuments bootstrap graph writes ROOT and TRASH artifacts`` () =
    let dataDir = newTempDir ()
    let graph = Graph.create ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "writeAllDocuments" |> ignore
    Assert.True(File.Exists(Path.Combine(dataDir, ".amb")))
    Assert.True(File.Exists(Path.Combine(dataDir, "TRASH", ".amb")))

[<Fact>]
let ``writeAllDocuments nested workspace tree writes expected paths`` () =
    let dataDir = newTempDir ()
    let graph, _, dirId, fileId, _ = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "writeAllDocuments" |> ignore
    Assert.True(File.Exists(Path.Combine(dataDir, "@home", ".amb")))
    Assert.True(File.Exists(Path.Combine(dataDir, "@home", "docs", ".amb")))
    Assert.True(File.Exists(Path.Combine(dataDir, "@home", "docs", "readme.txt")))

[<Fact>]
let ``writeAllDocuments ROOT file lands at dataDir root without amb suffix`` () =
    let dataDir = newTempDir ()
    let graph, fileId = graphWithRootFile ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "writeAllDocuments" |> ignore
    let path = artifactFullPath dataDir graph fileId
    Assert.Equal(Path.Combine(dataDir, "name.ext"), path)
    Assert.True(File.Exists path)

[<Fact>]
let ``writeAllDocuments nested file directory boundary writes separate artifacts`` () =
    let dataDir = newTempDir ()
    let graph, fileId, dirId, normalId = graphFileOwnsDirectory ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "writeAllDocuments" |> ignore
    let filePath = artifactFullPath dataDir graph fileId
    let dirPath = artifactFullPath dataDir graph dirId
    Assert.Equal(Path.Combine(dataDir, "container.txt"), filePath)
    Assert.Equal(Path.Combine(dataDir, "inner", ".amb"), dirPath)
    Assert.True(File.Exists filePath)
    Assert.True(File.Exists dirPath)
    let dirText = File.ReadAllText dirPath
    let normalSid = AmbDocument.formatStableId normalId
    Assert.DoesNotContain("^" + normalSid, dirText)
    Assert.Contains("nested", dirText)

[<Fact>]
let ``resolveArtifactPath unknown document root returns error`` () =
    let dataDir = newTempDir ()
    let graph = Graph.create ()
    let unknownId = NodeId.New()

    match DocumentPersistence.resolveArtifactPath dataDir graph unknownId with
    | Ok _ -> failwith "expected error"
    | Error _ -> ()

    Assert.False(Directory.EnumerateFiles(dataDir, "*", SearchOption.AllDirectories) |> Seq.exists (fun _ -> true))

[<Fact>]
let ``writeDocument round trip preserves member text`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, normalId = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "writeAllDocuments" |> ignore
    let filePath = artifactFullPath dataDir graph fileId
    let text = File.ReadAllText filePath

    match PlainTextDocument.read text fileId graph with
    | Error msg -> failwith msg
    | Ok result ->
        match Map.tryFind normalId result.nodes with
        | None -> failwith "member node missing after read"
        | Some node -> Assert.Equal("body", node.text)

[<Fact>]
let ``discoverArtifactRelatives finds all written artifacts`` () =
    let dataDir = newTempDir ()
    let graph, wsId, dirId, fileId, _ = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let relatives =
        DocumentPersistence.discoverArtifactRelatives dataDir
        |> requireOk "discover"
        |> Set.ofList
    let expected =
        [ Graph.rootId; Graph.trashId; wsId; dirId; fileId ]
        |> List.choose (DocumentPartition.artifactFileRelative graph)
        |> Set.ofList
    Assert.Equal<Set<string>>(expected, relatives)

[<Fact>]
let ``readAllDocuments round trip matches normalized snapshot outline`` () =
    let dataDir = newTempDir ()
    let graph = Graph.create ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let actual = DocumentPersistence.readAllDocuments dataDir |> requireOk "read"
    let expectedOutline = Snapshot.normalizeOutlineForCompare (Snapshot.write graph)
    let actualOutline = Snapshot.normalizeOutlineForCompare (Snapshot.write actual)
    Assert.Equal(expectedOutline, actualOutline)

[<Fact>]
let ``readAllDocuments round trips nested workspace tree`` () =
    let dataDir = newTempDir ()
    let expected, _, _, _, _ = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir expected |> requireOk "write" |> ignore
    let actual = DocumentPersistence.readAllDocuments dataDir |> requireOk "read"
    assertNestedWorkspaceLoad expected actual

[<Fact>]
let ``readAllDocuments round trips file owns directory boundary`` () =
    let dataDir = newTempDir ()
    let expected, _, _, _ = graphFileOwnsDirectory ()
    DocumentPersistence.writeAllDocuments dataDir expected |> requireOk "write" |> ignore
    let actual = DocumentPersistence.readAllDocuments dataDir |> requireOk "read"
    assertFileOwnsDirectoryLoad expected actual

[<Fact>]
let ``readAllDocuments stubs missing referenced artifact`` () =
    let dataDir = newTempDir ()
    let graph, _, dirId, fileId, _ = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let filePath = artifactFullPath dataDir graph fileId
    File.Delete filePath
    let actual = DocumentPersistence.readAllDocuments dataDir |> requireOk "read"
    let fileNode = actual.nodes.[fileId]
    Assert.Equal(NodeKind.Special SpecialKind.File, fileNode.kind)
    Assert.Equal("readme.txt", Filename.tryValue fileNode.name |> Option.get)
    Assert.Empty(fileNode.children)
    Assert.Equal(fileId, actual.nodes.[dirId].children.Head.id)

[<Fact>]
let ``discoverArtifactRelatives lists stray amb file`` () =
    let dataDir = newTempDir ()
    File.WriteAllText(Path.Combine(dataDir, "foo.amb"), "")
    let relatives =
        DocumentPersistence.discoverArtifactRelatives dataDir
        |> requireOk "discover"
    Assert.Contains("foo.amb", relatives)

[<Fact>]
let ``readAllDocuments ignores stray amb file`` () =
    let dataDir = newTempDir ()
    let graph, _, _, _, _ = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    File.WriteAllText(Path.Combine(dataDir, "foo.amb"), "stray")
    DocumentPersistence.readAllDocuments dataDir |> requireOk "read" |> ignore

[<Fact>]
let ``readAllDocuments duplicate id corruption returns error`` () =
    let dataDir = newTempDir ()
    let graph, _, dirId, fileId, normalId = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let filePath = artifactFullPath dataDir graph fileId
    let dirPath = artifactFullPath dataDir graph dirId
    let sid = AmbDocument.formatStableId normalId
    let corrupt = "^" + sid + " corrupt" + System.Environment.NewLine
    File.WriteAllText(filePath, corrupt)
    File.WriteAllText(dirPath, File.ReadAllText dirPath + corrupt)
    match DocumentPersistence.readAllDocuments dataDir with
    | Ok _ -> failwith "expected error"
    | Error msg ->
        Assert.True(msg.Contains("conflicting") || msg.Contains("member"))

[<Fact>]
let ``hasArtifactSet false on empty dir true after write`` () =
    let dataDir = newTempDir ()
    Assert.False(DocumentPersistence.hasArtifactSet dataDir)
    let graph, _, _, _, _ = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    Assert.True(DocumentPersistence.hasArtifactSet dataDir)

[<Fact>]
let ``writeAllDocuments plain file writes outline without stable id syntax`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, normalId = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let filePath = artifactFullPath dataDir graph fileId
    let text = File.ReadAllText filePath
    let sid = AmbDocument.formatStableId normalId
    Assert.DoesNotContain("^" + sid, text)
    Assert.Contains("body", text)

[<Fact>]
let ``writeAllDocuments amb directory artifact keeps stable id syntax`` () =
    let dataDir = newTempDir ()
    let graph, _, dirId, fileId, _ = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let dirPath = artifactFullPath dataDir graph dirId
    let text = File.ReadAllText dirPath
    let fileSid = AmbDocument.formatStableId fileId
    Assert.Contains("^" + fileSid, text)

[<Fact>]
let ``readAllDocuments round trips plain file member text`` () =
    let dataDir = newTempDir ()
    let expected, _, _, fileId, _ = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir expected |> requireOk "write" |> ignore
    let actual = DocumentPersistence.readAllDocuments dataDir |> requireOk "read"
    let actualNormalId = actual.nodes.[fileId].children.Head.id
    Assert.Equal("body", actual.nodes.[actualNormalId].text)

[<Fact>]
let ``discoverArtifactRelatives lists plain file alongside amb artifacts`` () =
    let dataDir = newTempDir ()
    let graph, wsId, dirId, fileId, _ = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let relatives =
        DocumentPersistence.discoverArtifactRelatives dataDir
        |> requireOk "discover"
        |> Set.ofList
    let fileRel = DocumentPartition.artifactFileRelative graph fileId |> Option.get
    let dirRel = DocumentPartition.artifactFileRelative graph dirId |> Option.get
    Assert.True(relatives.Contains fileRel)
    Assert.True(relatives.Contains dirRel)
    Assert.EndsWith(".txt", fileRel, StringComparison.OrdinalIgnoreCase)
    Assert.EndsWith(".amb", dirRel, StringComparison.OrdinalIgnoreCase)

[<Fact>]
let ``resolveArtifactPath plain file resolves to named extension path`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, _ = graphWithNestedDocs ()
    let path = artifactFullPath dataDir graph fileId
    Assert.Equal(Path.Combine(dataDir, "@home", "docs", "readme.txt"), path)

[<Fact>]
let ``writeAllDocuments ref child in plain file writes target text only`` () =
    let dataDir = newTempDir ()
    let graph, _, fileId, sharedId = graphWithPlainFileRef ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let filePath = artifactFullPath dataDir graph fileId
    let text = File.ReadAllText filePath
    let sharedSid = AmbDocument.formatStableId sharedId
    Assert.Contains("shared text", text)
    Assert.DoesNotContain("->", text)
    Assert.DoesNotContain("^" + sharedSid, text)

[<Fact>]
let ``readAllDocuments cold load of plain file loses ref edge`` () =
    let dataDir = newTempDir ()
    let graph, _, fileId, sharedId = graphWithPlainFileRef ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let actual = DocumentPersistence.readAllDocuments dataDir |> requireOk "read"
    let holderId = actual.nodes.[fileId].children.Head.id
    let child = actual.nodes.[holderId].children.Head
    Assert.Equal("shared text", actual.nodes.[child.id].text)
    Assert.Equal(Ownership.Owner, child.ref)
    Assert.NotEqual(sharedId, child.id)
