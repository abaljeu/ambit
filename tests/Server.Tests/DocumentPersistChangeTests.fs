module DocumentPersistenceTests.Change

open System
open System.IO
open DocumentPersistenceTests.Fixtures
open Gambol.Server
open Gambol.Server.Tests.TestBackend
open Gambol.Shared
open Xunit

let private assertNestedWorkspaceLoad (expected: Graph) (actual: Graph) =
    let wsId =
        expected.nodes
        |> Map.toSeq
        |> Seq.pick (fun (id, node) ->
            match node.kind with
            | NodeKind.Special SpecialKind.Workspace when id <> Graph.rootId -> Some id
            | _ -> None)
    let dirId = (Graph.children expected wsId).Head.id
    let fileId = (Graph.children expected dirId).Head.id
    Assert.Equal(wsId, (Graph.children actual Graph.workspacesId).Head.id)
    Assert.Equal(dirId, (Graph.children actual wsId).Head.id)
    Assert.Equal(fileId, (Graph.children actual dirId).Head.id)
    // Cold load: on-disk plain body → Unparsed File stub (no children), not Normal outline.
    Assert.Equal(NodeKind.Special SpecialKind.File, actual.nodes.[fileId].kind)
    Assert.Equal("readme.txt", Filename.tryValue actual.nodes.[fileId].name |> Option.get)
    Assert.Empty(Graph.children actual fileId)

[<Fact(Skip = "128: upload structure persistence preserves existing file mtime pending")>]
let ``upload structure persistence preserves existing file mtime`` () =
    let dataDir = newTempDir ()
    let graph0, _, _, fileId, _ = graphWithNestedDocs ()
    let graph =
        DocumentPersistWrite.writeAllDocuments dataDir graph0
        |> requireOk "initial write"
    let uploadOps =
        WorkspaceUploadStructure.planStubOps
            graph
            "home"
            [ { relative = "docs/uploaded.txt"; isDirectory = false } ]
        |> requireOk "upload structure"
    let afterUpload = applyOps graph uploadOps
    let filePath = artifactFullPath dataDir graph fileId
    let original = DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc)
    File.SetLastWriteTimeUtc(filePath, original)

    let stamped =
        DocumentPersistChange.persistGraphChange dataDir graph afterUpload
        |> requireOk "persist upload structure"

    Assert.Equal(original, File.GetLastWriteTimeUtc filePath)
    Assert.Equal(
        NodeUpdateTime.toDbPrecision original,
        stamped.graph.nodes.[fileId].updateTime)
    let uploadedId =
        WorkspaceUploadStructure.tryResolveFileNode
            afterUpload
            "home"
            "docs/uploaded.txt"
        |> Option.get
    let uploadedPath = artifactFullPath dataDir afterUpload uploadedId
    Assert.Equal(PullingFile, stamped.graph.nodes.[uploadedId].documentState.AsEnum)
    Assert.False(File.Exists uploadedPath)

[<Fact>]
let ``persistGraphChange does not rewrite untouched sibling document artifact`` () =
    let dataDir = newTempDir ()
    let graph, fileAId, fileBId, bodyAId, _ = graphWithTwoSiblingFiles ()
    DocumentPersistWrite.writeAllDocuments dataDir graph
    |> requireOk "initial write"
    |> ignore
    let pathA = artifactFullPath dataDir graph fileAId
    let pathB = artifactFullPath dataDir graph fileBId
    let planted = "PLANTED-UNTOUCHED"
    File.WriteAllText(pathB, planted)
    let mtimeB = File.GetLastWriteTimeUtc pathB
    let post =
        Graph.setText bodyAId "alpha" "ALPHA" graph
        |> requireOk "edit bodyA"

    DocumentPersistChange.persistGraphChange dataDir graph post
    |> requireOk "persistGraphChange"
    |> ignore

    Assert.Equal(planted, File.ReadAllText pathB)
    Assert.Equal(mtimeB, File.GetLastWriteTimeUtc pathB)
    Assert.Contains("ALPHA", File.ReadAllText pathA)

[<Fact>]
let ``persistGraphChange leaves an untouched sibling root Unpersisted`` () =
    let dataDir = newTempDir ()
    let built, fileAId, fileBId, bodyAId, _ = graphWithTwoSiblingFiles ()
    let wsId = built.nodes.[fileAId].owner
    let markUnpersisted id graph =
        Graph.setPersistState id PersistState.Unpersisted graph
        |> requireOk "unpersisted"
    let prior =
        built
        |> markUnpersisted fileBId
        |> markUnpersisted wsId
    DocumentPersistWrite.writeAllDocuments dataDir prior
    |> requireOk "initial write"
    |> ignore
    let post =
        Graph.setText bodyAId "alpha" "ALPHA" prior
        |> requireOk "edit a"
    let result =
        DocumentPersistChange.persistGraphChange dataDir prior post
        |> requireOk "persistGraphChange"
    Assert.Equal(PersistState.Persisted, result.graph.nodes.[fileAId].documentState.persistState)
    Assert.Equal(PersistState.Unpersisted, result.graph.nodes.[fileBId].documentState.persistState)
    Assert.Equal(PersistState.Unpersisted, result.graph.nodes.[wsId].documentState.persistState)

[<Fact>]
let ``persistGraphChange leaves a failed write Unpersisted when the old file remains`` () =
    let dataDir = newTempDir ()
    let graph, fileId, bodyId = graphWithSystemSecretBody ()
    let full = artifactFullPath dataDir graph fileId
    let parent = Path.GetDirectoryName full
    Directory.CreateDirectory parent |> ignore
    File.WriteAllText(full, "OLD")
    let post =
        Graph.setText bodyId "old" "NEW" graph
        |> requireOk "edit"
    Assert.Equal(PersistState.Unpersisted, post.nodes.[fileId].documentState.persistState)
    let result =
        DocumentPersistChange.persistGraphChange dataDir graph post
        |> requireOk "persistGraphChange"
    Assert.Equal(
        Some(DocumentPersistWrite.fileCouldNotSave "SYSTEM/secret.txt"),
        result.message)
    Assert.Equal("OLD", File.ReadAllText full)
    Assert.Equal(PersistState.Unpersisted, result.graph.nodes.[fileId].documentState.persistState)
    Assert.Equal("NEW", result.graph.nodes.[bodyId].text)

[<Fact>]
let ``readAllDocuments cold load stamps Directory File roots from disk mtime`` () =
    let dataDir = newTempDir ()
    let graph, wsId, dirId, fileId, _ = graphWithNestedDocs ()
    DocumentPersistWrite.writeAllDocuments dataDir graph
    |> requireOk "writeAllDocuments"
    |> ignore
    let dirPath = artifactFullPath dataDir graph dirId
    let wsPath = artifactFullPath dataDir graph wsId
    let dirMtime =
        File.GetLastWriteTimeUtc dirPath |> NodeUpdateTime.toDbPrecision
    let wsMtime =
        File.GetLastWriteTimeUtc wsPath |> NodeUpdateTime.toDbPrecision
    let loaded =
        DocumentPersistChange.readAllDocuments dataDir |> requireOk "read"
    Assert.Equal(dirMtime, loaded.nodes.[dirId].updateTime)
    Assert.Equal(wsMtime, loaded.nodes.[wsId].updateTime)
    Assert.True(Map.containsKey fileId loaded.nodes)

[<Fact>]
let ``persistNode writes one document node's file`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, _ = graphWithNestedDocs ()
    DocumentPersistChange.persistNode dataDir graph fileId
    |> requireOk "persistNode"
    |> ignore
    let filePath = artifactFullPath dataDir graph fileId
    Assert.Contains("body", File.ReadAllText filePath)

[<Fact>]
let ``readAllDocuments round trip matches normalized snapshot outline`` () =
    let dataDir = newTempDir ()
    let graph = Graph.create ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let actual = DocumentPersistChange.readAllDocuments dataDir |> requireOk "read"
    let expectedOutline = Snapshot.normalizeOutlineForCompare (Snapshot.write graph)
    let actualOutline = Snapshot.normalizeOutlineForCompare (Snapshot.write actual)
    Assert.Equal(expectedOutline, actualOutline)

[<Fact>]
let ``readAllDocuments round trips nested workspace tree`` () =
    let dataDir = newTempDir ()
    let expected, _, _, _, _ = graphWithNestedDocs ()
    DocumentPersistWrite.writeAllDocuments dataDir expected |> requireOk "write" |> ignore
    let actual = DocumentPersistChange.readAllDocuments dataDir |> requireOk "read"
    assertNestedWorkspaceLoad expected actual
    Assert.DoesNotContain(
        actual.nodes |> Map.toSeq,
        fun (_, node) -> Filename.tryValue node.name = Some ".amb")

[<Fact>]
let ``readAllDocuments cold load file-owns-directory without plain body`` () =
    let dataDir = newTempDir ()
    let expected, fileId, dirId, _ = graphFileOwnsDirectory ()
    DocumentPersistWrite.writeAllDocuments dataDir expected |> requireOk "write" |> ignore
    let actual = DocumentPersistChange.readAllDocuments dataDir |> requireOk "read"
    Assert.True(Map.containsKey fileId actual.nodes)
    Assert.Equal("container.txt", Filename.tryValue actual.nodes.[fileId].name |> Option.get)
    Assert.Empty(Graph.children actual fileId)
    Assert.False(Map.containsKey dirId actual.nodes)

[<Fact>]
let ``readAllDocuments preserves owner handle when artifact is missing`` () =
    let dataDir = newTempDir ()
    let graph, _, dirId, fileId, _ = graphWithNestedDocs ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let filePath = artifactFullPath dataDir graph fileId
    File.Delete filePath
    let actual = DocumentPersistChange.readAllDocuments dataDir |> requireOk "read"
    let fileNode = actual.nodes.[fileId]
    Assert.Equal(NodeKind.Normal, fileNode.kind)
    Assert.Equal("readme.txt", Filename.tryValue fileNode.name |> Option.get)
    Assert.Empty(Graph.children actual fileId)
    Assert.Equal(fileId, (Graph.children actual dirId).Head.id)
    Assert.Equal(Ownership.Owner, (Graph.children actual dirId).Head.ref)

[<Fact>]
let ``readAllDocuments ignores stray amb file`` () =
    let dataDir = newTempDir ()
    let graph, _, _, _, _ = graphWithNestedDocs ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    File.WriteAllText(Path.Combine(dataDir, "foo.amb"), "stray")
    DocumentPersistChange.readAllDocuments dataDir |> requireOk "read" |> ignore

[<Fact>]
let ``readAllDocuments ignores large non-Directory-File under dataDir`` () =
    let dataDir = newTempDir ()
    File.WriteAllText(Path.Combine(dataDir, ".amb"), "")
    File.WriteAllText(
        Path.Combine(dataDir, "blob.bin"),
        String.replicate (DocumentParseLimits.maxInputCodeUnits + 1) "x")
    File.WriteAllText(
        Path.Combine(dataDir, "notes.amb"),
        String.replicate (DocumentParseLimits.maxInputCodeUnits + 1) "y")
    DocumentPersistChange.readAllDocuments dataDir |> requireOk "read" |> ignore

[<Fact>]
let ``readAllDocuments skips oversized Directory File`` () =
    let dataDir = newTempDir ()
    let graph, wsId, _, _, _ = graphWithNestedDocs ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let oversized =
        String.replicate (DocumentParseLimits.maxInputCodeUnits + 1) "a"
    File.WriteAllText(Path.Combine(dataDir, "home", ".amb"), oversized)
    let actual =
        DocumentPersistChange.readAllDocuments dataDir |> requireOk "read"
    Assert.True(Map.containsKey wsId actual.nodes)
    Assert.Equal(0, (Graph.children actual wsId).Length)

[<Fact>]
let ``readAllDocuments duplicate id corruption returns error`` () =
    let dataDir = newTempDir ()
    let graph, wsId, dirId, _, normalId = graphWithNestedDocs ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let wsPath = artifactFullPath dataDir graph wsId
    let dirPath = artifactFullPath dataDir graph dirId
    let sid = AmbDocument.formatStableId normalId
    let corrupt = "^" + sid + " corrupt" + System.Environment.NewLine
    File.WriteAllText(wsPath, File.ReadAllText wsPath + corrupt)
    File.WriteAllText(dirPath, File.ReadAllText dirPath + corrupt)
    match DocumentPersistChange.readAllDocuments dataDir with
    | Ok _ -> failwith "expected error"
    | Error msg ->
        Assert.True(msg.Contains("conflicting") || msg.Contains("member"))

[<Fact>]
let ``readAllDocuments cold load keeps file outline without plain body`` () =
    let dataDir = newTempDir ()
    let expected, _, dirId, fileId, _ = graphWithNestedDocs ()
    DocumentPersistWrite.writeAllDocuments dataDir expected |> requireOk "write" |> ignore
    let actual = DocumentPersistChange.readAllDocuments dataDir |> requireOk "read"
    Assert.Equal(fileId, (Graph.children actual dirId).Head.id)
    // Body is on disk but unloaded: Special File + Unparsed, empty children.
    Assert.Equal(NodeKind.Special SpecialKind.File, actual.nodes.[fileId].kind)
    Assert.Equal("readme.txt", Filename.tryValue actual.nodes.[fileId].name |> Option.get)
    Assert.Empty(Graph.children actual fileId)

[<Fact>]
let ``readAllDocuments cold load skips plain file body artifact`` () =
    let dataDir = newTempDir ()
    let graph, _, fileId, sharedId = graphWithPlainFileRef ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let actual = DocumentPersistChange.readAllDocuments dataDir |> requireOk "read"
    Assert.True(Map.containsKey fileId actual.nodes)
    Assert.Empty(Graph.children actual fileId)
    Assert.False(Map.containsKey sharedId actual.nodes)

[<Fact>]
let ``validatePathMoves refuses rename of non-allowlisted SYSTEM file`` () =
    let dataDir = newTempDir ()
    let pre, fileId = graphWithSystemFile "secret.txt"
    let systemDir = Path.Combine(dataDir, "SYSTEM")
    Directory.CreateDirectory systemDir |> ignore
    File.WriteAllText(Path.Combine(systemDir, "secret.txt"), "SECRET")
    let post =
        let node = pre.nodes.[fileId]
        Graph.fromNodes
            pre.root
            (Map.add
                fileId
                { node with
                    name = Filename.Ok "other.txt"
                    text = "other.txt" }
                pre.nodes)
            pre.childMap
    match DocumentPersistChange.validatePathMoves dataDir pre post with
    | Ok () -> failwith "expected validatePathMoves to refuse SYSTEM path"
    | Error msg ->
        Assert.Contains(
            "system directory write refused",
            msg,
            StringComparison.OrdinalIgnoreCase)
