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

let private owned = ChildNode.owners

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
    Assert.Equal(wsId, actual.nodes.[Graph.workspacesId].children.Head.id)
    Assert.Equal(dirId, actual.nodes.[wsId].children.Head.id)
    Assert.Equal(fileId, actual.nodes.[dirId].children.Head.id)
    // Cold load: on-disk plain body → Unparsed File stub (no children), not Normal outline.
    Assert.Equal(NodeKind.Special SpecialKind.File, actual.nodes.[fileId].kind)
    Assert.Equal(Unparsed, actual.nodes.[fileId].documentState)
    Assert.Equal("readme.txt", Filename.tryValue actual.nodes.[fileId].name |> Option.get)
    Assert.Empty(actual.nodes.[fileId].children)

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
            children = [ ChildNode.reference sharedId ] }
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

let private graphWithTwoSiblingFiles () : Graph * NodeId * NodeId * NodeId * NodeId =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let fileAId = NodeId.New()
    let fileBId = NodeId.New()
    let bodyAId = NodeId.New()
    let bodyBId = NodeId.New()
    let graph1 =
        graph0.nodes
        |> Map.add wsId (specialNode wsId Workspace "home" Graph.workspacesId)
        |> Map.add fileAId (specialNode fileAId File "a.txt" wsId)
        |> Map.add fileBId (specialNode fileBId File "b.txt" wsId)
        |> Map.add bodyAId (normalNode bodyAId "alpha" fileAId)
        |> Map.add bodyBId (normalNode bodyBId "beta" fileBId)
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    let graph2 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph1
        |> requireOk "workspaces->ws"
    let graph3 =
        Graph.replace wsId 0 [] (owned [ fileAId; fileBId ]) graph2
        |> requireOk "ws->files"
    let graph4 =
        Graph.replace fileAId 0 [] (owned [ bodyAId ]) graph3
        |> requireOk "fileA->body"
    let graph5 =
        Graph.replace fileBId 0 [] (owned [ bodyBId ]) graph4
        |> requireOk "fileB->body"
    graph5, fileAId, fileBId, bodyAId, bodyBId

[<Fact>]
let ``writeAllDocuments bootstrap graph writes ROOT and TRASH artifacts`` () =
    let dataDir = newTempDir ()
    let graph = Graph.create ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "writeAllDocuments" |> ignore
    Assert.True(File.Exists(Path.Combine(dataDir, ".amb")))
    Assert.True(File.Exists(Path.Combine(dataDir, "TRASH", ".amb")))
    Assert.True(File.Exists(Path.Combine(dataDir, "SYSTEM", ".amb")))

[<Fact>]
let ``writeAllDocuments nested workspace tree writes expected paths`` () =
    let dataDir = newTempDir ()
    let graph, _, dirId, fileId, _ = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "writeAllDocuments" |> ignore
    Assert.True(File.Exists(Path.Combine(dataDir, "home", ".amb")))
    Assert.True(File.Exists(Path.Combine(dataDir, "home", "docs", ".amb")))
    Assert.True(File.Exists(Path.Combine(dataDir, "home", "docs", "readme.txt")))

[<Fact>]
let ``writeAllDocuments stamps artifact updateTime from disk mtime`` () =
    let dataDir = newTempDir ()
    let graph, wsId, dirId, fileId, _ = graphWithNestedDocs ()
    let stamped =
        DocumentPersistence.writeAllDocuments dataDir graph
        |> requireOk "writeAllDocuments"
    let filePath = artifactFullPath dataDir graph fileId
    let dirPath = artifactFullPath dataDir graph dirId
    let wsPath = artifactFullPath dataDir graph wsId
    let fileMtime =
        File.GetLastWriteTimeUtc filePath |> NodeUpdateTime.toDbPrecision
    let dirMtime =
        File.GetLastWriteTimeUtc dirPath |> NodeUpdateTime.toDbPrecision
    let wsMtime =
        File.GetLastWriteTimeUtc wsPath |> NodeUpdateTime.toDbPrecision
    Assert.Equal(fileMtime, stamped.nodes.[fileId].updateTime)
    Assert.Equal(dirMtime, stamped.nodes.[dirId].updateTime)
    Assert.Equal(wsMtime, stamped.nodes.[wsId].updateTime)
    Assert.NotEqual(
        NodeUpdateTime.missing,
        stamped.nodes.[fileId].updateTime)

[<Fact>]
let ``upload structure persistence preserves existing file mtime`` () =
    let dataDir = newTempDir ()
    let graph0, _, _, fileId, _ = graphWithNestedDocs ()
    let graph =
        DocumentPersistence.writeAllDocuments dataDir graph0
        |> requireOk "initial write"
    let uploadOps =
        WorkspaceUploadStructure.planStubOps
            graph
            "home"
            [ { relative = "docs/uploaded.txt"; isDirectory = false } ]
        |> requireOk "upload structure"
    let afterUpload =
        uploadOps
        |> List.fold
            (fun state op ->
                match Op.apply op state with
                | ApplyResult.Changed next
                | ApplyResult.Unchanged next -> next
                | ApplyResult.Invalid(_, error) -> failwith error)
            { graph = graph; history = History.empty; revision = Revision.Zero }
        |> fun state -> state.graph
    let filePath = artifactFullPath dataDir graph fileId
    let original = DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc)
    File.SetLastWriteTimeUtc(filePath, original)

    let stamped =
        DocumentPersistence.persistGraphChange dataDir graph afterUpload
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
    Assert.Equal(NoServerFile, stamped.graph.nodes.[uploadedId].documentState)
    Assert.False(File.Exists uploadedPath)

[<Fact>]
let ``persistGraphChange does not rewrite untouched sibling document artifact`` () =
    let dataDir = newTempDir ()
    let graph, fileAId, fileBId, bodyAId, _ = graphWithTwoSiblingFiles ()
    DocumentPersistence.writeAllDocuments dataDir graph
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

    DocumentPersistence.persistGraphChange dataDir graph post
    |> requireOk "persistGraphChange"
    |> ignore

    Assert.Equal(planted, File.ReadAllText pathB)
    Assert.Equal(mtimeB, File.GetLastWriteTimeUtc pathB)
    Assert.Contains("ALPHA", File.ReadAllText pathA)

[<Fact>]
let ``readAllDocuments cold load stamps Directory File roots from disk mtime`` () =
    let dataDir = newTempDir ()
    let graph, wsId, dirId, fileId, _ = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir graph
    |> requireOk "writeAllDocuments"
    |> ignore
    let dirPath = artifactFullPath dataDir graph dirId
    let wsPath = artifactFullPath dataDir graph wsId
    let dirMtime =
        File.GetLastWriteTimeUtc dirPath |> NodeUpdateTime.toDbPrecision
    let wsMtime =
        File.GetLastWriteTimeUtc wsPath |> NodeUpdateTime.toDbPrecision
    let loaded =
        DocumentPersistence.readAllDocuments dataDir |> requireOk "read"
    Assert.Equal(dirMtime, loaded.nodes.[dirId].updateTime)
    Assert.Equal(wsMtime, loaded.nodes.[wsId].updateTime)
    Assert.True(Map.containsKey fileId loaded.nodes)

[<Fact>]
let ``fileStatusForReference reports existing and missing server artifacts`` () =
    let dataDir = newTempDir ()
    let graph, _, _, _, _ = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "writeAllDocuments" |> ignore

    let existing =
        DocumentPersistence.fileStatusForReference dataDir "//home/docs/readme.txt"
        |> requireOk "existing status"

    let missing =
        DocumentPersistence.fileStatusForReference dataDir "//home/docs/missing.txt"
        |> requireOk "missing status"

    Assert.Equal(ExistingFile, existing.status)
    Assert.True(existing.sourceModifiedUtc.IsSome)
    Assert.Equal(MissingArtifact, missing.status)
    Assert.Equal(None, missing.sourceModifiedUtc)

[<Fact>]
let ``fileStatusForReference reports workspace and directory as folder not file`` () =
    let dataDir = newTempDir ()
    let graph, _, _, _, _ = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "writeAllDocuments" |> ignore

    let ws =
        DocumentPersistence.fileStatusForReference dataDir "//home"
        |> requireOk "workspace status"
    let dir =
        DocumentPersistence.fileStatusForReference dataDir "//home/docs/"
        |> requireOk "directory status"

    Assert.Equal(ExistingFolder, ws.status)
    Assert.Equal(ExistingFolder, dir.status)

[<Fact>]
let ``fileStatusForReference directory is folder when dir exists without amb`` () =
    let dataDir = newTempDir ()
    let dirPath = Path.Combine(dataDir, "fambit", "elm")
    Directory.CreateDirectory dirPath |> ignore

    let status =
        DocumentPersistence.fileStatusForReference dataDir "//fambit/elm/"
        |> requireOk "mkcol-only directory"

    Assert.Equal(ExistingFolder, status.status)

[<Fact>]
let ``fileStatusForReference directory missing when neither dir nor amb exists`` () =
    let dataDir = newTempDir ()
    Directory.CreateDirectory(Path.Combine(dataDir, "fambit")) |> ignore

    let status =
        DocumentPersistence.fileStatusForReference dataDir "//fambit/doc/"
        |> requireOk "absent directory"

    Assert.Equal(MissingArtifact, status.status)

[<Fact>]
let ``fileStatusForReference missing when neither amb nor folder exist`` () =
    let dataDir = newTempDir ()

    let status =
        DocumentPersistence.fileStatusForReference dataDir "//fambit/doc/"
        |> requireOk "missing folder"

    Assert.Equal(MissingArtifact, status.status)

[<Fact>]
let ``importPackageForReference builds package from DataDir file`` () =
    let dataDir = newTempDir ()
    let relDir = Path.Combine(dataDir, "life", "memory")
    Directory.CreateDirectory relDir |> ignore
    File.WriteAllText(Path.Combine(relDir, "goal.md"), "hello goal")

    let package =
        DocumentPersistence.importPackageForReference
            dataDir
            "//life/memory/goal.md"
        |> requireOk "import package"

    Assert.Equal("//life/memory/goal.md", package.sourcePath)
    Assert.False(package.isDirectory)
    Assert.False(List.isEmpty package.topLevelIds)
    Assert.False(List.isEmpty package.ops)

[<Fact>]
let ``importPackageForReference reports missing DataDir file`` () =
    let dataDir = newTempDir ()

    match
        DocumentPersistence.importPackageForReference
            dataDir
            "//life/memory/goal.md"
    with
    | Error msg -> Assert.Equal("file not found", msg)
    | Ok _ -> Assert.Fail("expected file not found")

[<Fact>]
let ``planParseFile refuses oversized body before writing artifact`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, _ = graphWithNestedDocs ()
    let diskPath = Path.Combine(dataDir, "home", "docs", "readme.txt")
    let actualCodeUnits = DocumentParseLimits.maxInputCodeUnits + 1
    let text = String('x', actualCodeUnits)

    match DocumentPersistence.planParseFile dataDir graph fileId (Some text) with
    | Error msg ->
        Assert.Equal(
            DocumentParseLimits.errorForCodeUnits actualCodeUnits,
            msg)
        Assert.False(File.Exists diskPath)
    | Ok _ -> Assert.Fail("expected oversized parse to fail")

[<Fact>]
let ``planParseFile blank body overwrites artifact and returns ops`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, _ = graphWithNestedDocs ()
    let diskPath = Path.Combine(dataDir, "home", "docs", "readme.txt")
    Directory.CreateDirectory(Path.GetDirectoryName diskPath) |> ignore
    File.WriteAllText(diskPath, "EXISTING")

    DocumentPersistence.planParseFile dataDir graph fileId (Some " \r\n\t")
    |> requireOk "blank body parse"
    |> ignore

    Assert.Equal(" \r\n\t", File.ReadAllText diskPath)

[<Fact>]
let ``planParseFile blank DataDir artifact without request text succeeds`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, _ = graphWithNestedDocs ()
    let diskPath = Path.Combine(dataDir, "home", "docs", "readme.txt")
    Directory.CreateDirectory(Path.GetDirectoryName diskPath) |> ignore
    File.WriteAllText(diskPath, " \r\n\t")

    DocumentPersistence.planParseFile dataDir graph fileId None
    |> requireOk "blank artifact parse"
    |> ignore

[<Fact>]
let ``planParseFile DataDir warm keeps line NodeId on text edit`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, normalId = graphWithNestedDocs ()

    DocumentPersistence.writeAllDocuments dataDir graph
    |> requireOk "writeAllDocuments"
    |> ignore

    File.WriteAllText(
        Path.Combine(dataDir, "home", "docs", "readme.txt"),
        "BODY\n")

    let ops =
        DocumentPersistence.planParseFile
            dataDir
            graph
            fileId
            None
        |> requireOk "planParseFile"

    Assert.False(List.isEmpty ops)

    let state0 =
        { graph = graph; history = History.empty; revision = Revision.Zero }
    let after =
        ops
        |> List.fold
            (fun state op ->
                match Op.apply op state with
                | ApplyResult.Changed next
                | ApplyResult.Unchanged next -> next
                | ApplyResult.Invalid(_, error) -> failwith error)
            state0

    Assert.Equal(normalId, after.graph.nodes.[fileId].children.Head.id)
    Assert.Equal("BODY", after.graph.nodes.[normalId].text)
    Assert.Equal(Current, after.graph.nodes.[fileId].documentState)

[<Fact>]
let ``planParseFile with body text writes artifact to DataDir`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, _ = graphWithNestedDocs ()
    let diskPath = Path.Combine(dataDir, "home", "docs", "readme.txt")

    DocumentPersistence.planParseFile
        dataDir
        graph
        fileId
        (Some "UPLOADED\n")
    |> requireOk "planParseFile"
    |> ignore

    Assert.True(File.Exists diskPath)
    Assert.Equal("UPLOADED\n", File.ReadAllText diskPath)

[<Fact>]
let ``planParseFile with body text overwrites stale DataDir content`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, _ = graphWithNestedDocs ()
    let diskPath = Path.Combine(dataDir, "home", "docs", "readme.txt")
    Directory.CreateDirectory(Path.GetDirectoryName diskPath) |> ignore
    File.WriteAllText(diskPath, "STALE")
    let original = DateTime(2024, 2, 3, 4, 5, 6, DateTimeKind.Utc)
    File.SetLastWriteTimeUtc(diskPath, original)

    DocumentPersistence.planParseFile
        dataDir
        graph
        fileId
        (Some "FRESH\n")
    |> requireOk "planParseFile"
    |> ignore

    Assert.Equal("FRESH\n", File.ReadAllText diskPath)
    Assert.Equal(original, File.GetLastWriteTimeUtc diskPath)

[<Fact>]
let ``planParseFile uses body text over DataDir`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, normalId = graphWithNestedDocs ()

    DocumentPersistence.writeAllDocuments dataDir graph
    |> requireOk "writeAllDocuments"
    |> ignore

    let ops =
        DocumentPersistence.planParseFile
            dataDir
            graph
            fileId
            (Some "FROMBODY\n")
        |> requireOk "planParseFile body"

    let state0 =
        { graph = graph; history = History.empty; revision = Revision.Zero }
    let after =
        ops
        |> List.fold
            (fun state op ->
                match Op.apply op state with
                | ApplyResult.Changed next
                | ApplyResult.Unchanged next -> next
                | ApplyResult.Invalid(_, error) -> failwith error)
            state0

    Assert.Equal(normalId, after.graph.nodes.[fileId].children.Head.id)
    Assert.Equal("FROMBODY", after.graph.nodes.[normalId].text)

[<Fact>]
let ``planParseFile without text rejects File with no server occurrence`` () =
    let dataDir = newTempDir ()
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let fileNode =
        Node.Create(
            fileId,
            text = "",
            name = Filename.Empty,
            owner = Graph.rootId,
            kind = Special File,
            documentState = Unparsed)
    let graph1 =
        graph0.nodes
        |> Map.add fileId fileNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    let graph =
        Graph.replace Graph.rootId 0 [] (owned [ fileId ]) graph1
        |> requireOk "root->file"

    match DocumentPersistence.planParseFile dataDir graph fileId None with
    | Error msg ->
        Assert.Equal("selected File has no occurrence on the server", msg)
    | Ok _ -> Assert.Fail("expected no-occurrence error")

[<Fact>]
let ``GET /ambit/file returns import package for DataDir file`` () = task {
    let dataDir = newTempDir ()
    let relDir = Path.Combine(dataDir, "life", "memory")
    Directory.CreateDirectory relDir |> ignore
    File.WriteAllText(Path.Combine(relDir, "goal.md"), "alpha")
    use client = createClientForDir dataDir
    let path = Uri.EscapeDataString("//life/memory/goal.md")
    let! resp = client.GetAsync("/ambit/file?path=" + path)
    Assert.Equal(System.Net.HttpStatusCode.OK, resp.StatusCode)
    let! body = resp.Content.ReadAsStringAsync()
    Assert.Contains("\"sourcePath\"", body)
    Assert.Contains("goal.md", body)
    Assert.Contains("\"ops\"", body)
}

[<Fact>]
let ``writeAllDocuments ROOT file lands at dataDir root without amb suffix`` () =
    let dataDir = newTempDir ()
    let graph, fileId = graphWithRootFile ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "writeAllDocuments" |> ignore
    let path = artifactFullPath dataDir graph fileId
    Assert.Equal(Path.Combine(dataDir, "name.ext"), path)
    Assert.True(File.Exists path)

[<Fact>]
let ``planParseFile ROOT file reads artifact directly from DataDir`` () =
    let dataDir = newTempDir ()
    let graph, fileId = graphWithRootFile ()
    File.WriteAllText(Path.Combine(dataDir, "name.ext"), "FROM ROOT\n")

    let ops =
        DocumentPersistence.planParseFile dataDir graph fileId None
        |> requireOk "planParseFile ROOT"

    Assert.False(List.isEmpty ops)

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
let ``resolveArtifactPath malformed document root identifies node`` () =
    let dataDir = newTempDir ()
    let graph0 = Graph.create ()
    let malformedId = NodeId.New()
    let malformedNode = specialNode malformedId Directory "orphan" Graph.rootId
    let graph =
        graph0.nodes
        |> Map.add malformedId malformedNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    let expected =
        $"no artifact path for document root: id={malformedId.Value}; "
        + $"kind=Special Directory; name=orphan; owner={Graph.rootId.Value}"

    match DocumentPersistence.resolveArtifactPath dataDir graph malformedId with
    | Ok _ -> failwith "expected error"
    | Error error -> Assert.Equal(expected, error)

[<Fact>]
let ``resolveArtifactPath unknown document root identifies missing node`` () =
    let dataDir = newTempDir ()
    let graph = Graph.create ()
    let unknownId = NodeId.New()
    let expected =
        $"no artifact path for document root: id={unknownId.Value}; node=missing"

    match DocumentPersistence.resolveArtifactPath dataDir graph unknownId with
    | Ok _ -> failwith "expected error"
    | Error error -> Assert.Equal(expected, error)

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
        [ Graph.rootId; Graph.systemId; Graph.trashId; wsId; dirId; fileId ]
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
    Assert.DoesNotContain(
        actual.nodes |> Map.toSeq,
        fun (_, node) -> Filename.tryValue node.name = Some ".amb")

[<Fact>]
let ``readAllDocuments cold load file-owns-directory without plain body`` () =
    let dataDir = newTempDir ()
    let expected, fileId, dirId, _ = graphFileOwnsDirectory ()
    DocumentPersistence.writeAllDocuments dataDir expected |> requireOk "write" |> ignore
    let actual = DocumentPersistence.readAllDocuments dataDir |> requireOk "read"
    Assert.True(Map.containsKey fileId actual.nodes)
    Assert.Equal("container.txt", Filename.tryValue actual.nodes.[fileId].name |> Option.get)
    Assert.Empty(actual.nodes.[fileId].children)
    Assert.False(Map.containsKey dirId actual.nodes)

[<Fact>]
let ``readAllDocuments preserves owner handle when artifact is missing`` () =
    let dataDir = newTempDir ()
    let graph, _, dirId, fileId, _ = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let filePath = artifactFullPath dataDir graph fileId
    File.Delete filePath
    let actual = DocumentPersistence.readAllDocuments dataDir |> requireOk "read"
    let fileNode = actual.nodes.[fileId]
    Assert.Equal(NodeKind.Normal, fileNode.kind)
    Assert.Equal("readme.txt", Filename.tryValue fileNode.name |> Option.get)
    Assert.Empty(fileNode.children)
    Assert.Equal(fileId, actual.nodes.[dirId].children.Head.id)
    Assert.Equal(Ownership.Owner, actual.nodes.[dirId].children.Head.ref)

[<Fact>]
let ``discoverArtifactRelatives lists stray amb file`` () =
    let dataDir = newTempDir ()
    File.WriteAllText(Path.Combine(dataDir, "foo.amb"), "")
    let relatives =
        DocumentPersistence.discoverArtifactRelatives dataDir
        |> requireOk "discover"
    Assert.Contains("foo.amb", relatives)

[<Fact>]
let ``discoverArtifactRelatives excludes reserved gambol dot files`` () =
    let dataDir = newTempDir ()
    let nested = Path.Combine(dataDir, "nested")
    Directory.CreateDirectory(nested) |> ignore
    File.WriteAllText(Path.Combine(dataDir, "gambol.log"), "bookkeeping")
    File.WriteAllText(Path.Combine(dataDir, "fetch.log"), "ordinary artifact")
    File.WriteAllText(Path.Combine(nested, "GAMBOL.meta"), "bookkeeping")
    File.WriteAllText(Path.Combine(dataDir, "gambolish"), "ordinary artifact")
    let relatives =
        DocumentPersistence.discoverArtifactRelatives dataDir
        |> requireOk "discover"
    Assert.DoesNotContain("gambol.log", relatives)
    Assert.DoesNotContain("nested/GAMBOL.meta", relatives)
    Assert.Contains("fetch.log", relatives)
    Assert.Contains("gambolish", relatives)

[<Fact>]
let ``readAllDocuments ignores stray amb file`` () =
    let dataDir = newTempDir ()
    let graph, _, _, _, _ = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    File.WriteAllText(Path.Combine(dataDir, "foo.amb"), "stray")
    DocumentPersistence.readAllDocuments dataDir |> requireOk "read" |> ignore

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
    DocumentPersistence.readAllDocuments dataDir |> requireOk "read" |> ignore

[<Fact>]
let ``readAllDocuments skips oversized Directory File`` () =
    let dataDir = newTempDir ()
    let graph, wsId, _, _, _ = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let oversized =
        String.replicate (DocumentParseLimits.maxInputCodeUnits + 1) "a"
    File.WriteAllText(Path.Combine(dataDir, "home", ".amb"), oversized)
    let actual =
        DocumentPersistence.readAllDocuments dataDir |> requireOk "read"
    Assert.True(Map.containsKey wsId actual.nodes)
    Assert.Equal(0, actual.nodes.[wsId].children.Length)

[<Fact>]
let ``readAllDocuments duplicate id corruption returns error`` () =
    let dataDir = newTempDir ()
    let graph, wsId, dirId, _, normalId = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let wsPath = artifactFullPath dataDir graph wsId
    let dirPath = artifactFullPath dataDir graph dirId
    let sid = AmbDocument.formatStableId normalId
    let corrupt = "^" + sid + " corrupt" + System.Environment.NewLine
    File.WriteAllText(wsPath, File.ReadAllText wsPath + corrupt)
    File.WriteAllText(dirPath, File.ReadAllText dirPath + corrupt)
    match DocumentPersistence.readAllDocuments dataDir with
    | Ok _ -> failwith "expected error"
    | Error msg ->
        Assert.True(msg.Contains("conflicting") || msg.Contains("member"))

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
let ``readAllDocuments cold load keeps file outline without plain body`` () =
    let dataDir = newTempDir ()
    let expected, _, dirId, fileId, _ = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir expected |> requireOk "write" |> ignore
    let actual = DocumentPersistence.readAllDocuments dataDir |> requireOk "read"
    Assert.Equal(fileId, actual.nodes.[dirId].children.Head.id)
    // Body is on disk but unloaded: Special File + Unparsed, empty children.
    Assert.Equal(NodeKind.Special SpecialKind.File, actual.nodes.[fileId].kind)
    Assert.Equal(Unparsed, actual.nodes.[fileId].documentState)
    Assert.Equal("readme.txt", Filename.tryValue actual.nodes.[fileId].name |> Option.get)
    Assert.Empty(actual.nodes.[fileId].children)

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
    Assert.Equal(Path.Combine(dataDir, "home", "docs", "readme.txt"), path)

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
let ``readAllDocuments cold load skips plain file body artifact`` () =
    let dataDir = newTempDir ()
    let graph, _, fileId, sharedId = graphWithPlainFileRef ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let actual = DocumentPersistence.readAllDocuments dataDir |> requireOk "read"
    Assert.True(Map.containsKey fileId actual.nodes)
    Assert.Empty(actual.nodes.[fileId].children)
    Assert.False(Map.containsKey sharedId actual.nodes)

let private graphWithIllicitAmbFile () : Graph * NodeId * NodeId =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let fileId = NodeId.New()
    let wsNode = specialNode wsId Workspace "home" Graph.workspacesId
    // Bypass Filename.create: illicit Ok ".amb" that somehow exists.
    let fileNode =
        Node.Create(
            fileId,
            text = ".amb",
            name = Filename.Ok ".amb",
            owner = wsId,
            kind = Special File)
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
let ``refuseDirectoryFileNamedDocument errors on illicit amb node name`` () =
    let node =
        Node.Create(
            NodeId.New(),
            text = ".amb",
            name = Filename.Ok ".amb",
            owner = Graph.rootId,
            kind = Special File)
    match DocumentPersistence.refuseDirectoryFileNamedDocument node with
    | Ok () -> failwith "expected refuse"
    | Error msg ->
        Assert.Contains("directory file", msg, StringComparison.OrdinalIgnoreCase)

[<Fact>]
let ``refuseDirectoryFileNamedDocument allows legitimate workspace name`` () =
    let node =
        Node.Create(
            NodeId.New(),
            text = "home",
            name = Filename.create "home",
            owner = Graph.workspacesId,
            kind = Special Workspace)
    DocumentPersistence.refuseDirectoryFileNamedDocument node
    |> requireOk "refuseDirectoryFileNamedDocument"
    |> ignore

let private graphWithSystemFile (fileName: string) : Graph * NodeId =
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let fileNode = specialNode fileId File fileName Graph.systemId
    let graph1 =
        graph0.nodes
        |> Map.add fileId fileNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    let graph2 =
        Graph.replace Graph.systemId 0 [] (owned [ fileId ]) graph1
        |> requireOk "system->file"
    graph2, fileId

[<Fact>]
let ``writeDocument allows SYSTEM Directory File`` () =
    let dataDir = newTempDir ()
    let graph = Graph.create ()
    DocumentPersistence.writeDocument dataDir graph Graph.systemId
    |> requireOk "write SYSTEM/.amb"
    |> ignore
    Assert.True(File.Exists(Path.Combine(dataDir, "SYSTEM", ".amb")))

[<Fact>]
let ``writeDocument allows SYSTEM user css`` () =
    let dataDir = newTempDir ()
    let graph, fileId = graphWithSystemFile "user.css"
    DocumentPersistence.writeDocument dataDir graph fileId
    |> requireOk "write SYSTEM/user.css"
    |> ignore
    Assert.True(File.Exists(Path.Combine(dataDir, "SYSTEM", "user.css")))

[<Fact>]
let ``writeDocument refuses illicit SYSTEM file and leaves DataDir untouched`` () =
    let dataDir = newTempDir ()
    let graph, fileId = graphWithSystemFile "secret.txt"
    let systemDir = Path.Combine(dataDir, "SYSTEM")
    Directory.CreateDirectory systemDir |> ignore
    let path = Path.Combine(systemDir, "secret.txt")
    File.WriteAllText(path, "SECRET")
    match DocumentPersistence.writeDocument dataDir graph fileId with
    | Ok _ -> failwith "expected writeDocument to refuse SYSTEM file"
    | Error msg ->
        Assert.Contains(
            "system directory write refused",
            msg,
            StringComparison.OrdinalIgnoreCase)
    Assert.Equal("SECRET", File.ReadAllText path)

[<Fact>]
let ``planParseFile refuses illicit SYSTEM body write`` () =
    let dataDir = newTempDir ()
    let graph, fileId = graphWithSystemFile "secret.txt"
    let systemDir = Path.Combine(dataDir, "SYSTEM")
    Directory.CreateDirectory systemDir |> ignore
    let path = Path.Combine(systemDir, "secret.txt")
    File.WriteAllText(path, "SECRET")
    match DocumentPersistence.planParseFile dataDir graph fileId (Some "new") with
    | Ok _ -> failwith "expected planParseFile to refuse SYSTEM write"
    | Error msg ->
        Assert.Contains(
            "system directory write refused",
            msg,
            StringComparison.OrdinalIgnoreCase)
    Assert.Equal("SECRET", File.ReadAllText path)

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
    match DocumentPersistence.validatePathMoves dataDir pre post with
    | Ok () -> failwith "expected validatePathMoves to refuse SYSTEM path"
    | Error msg ->
        Assert.Contains(
            "system directory write refused",
            msg,
            StringComparison.OrdinalIgnoreCase)

[<Fact>]
let ``writeDocument refuses illicit amb-named file and leaves DataDir untouched`` () =
    let dataDir = newTempDir ()
    let graph, _, fileId = graphWithIllicitAmbFile ()
    let homeDir = Path.Combine(dataDir, "home")
    Directory.CreateDirectory homeDir |> ignore
    let directoryFilePath = Path.Combine(homeDir, ".amb")
    File.WriteAllText(directoryFilePath, "MARKER")
    match DocumentPersistence.writeDocument dataDir graph fileId with
    | Ok _ -> failwith "expected writeDocument to refuse illicit .amb"
    | Error msg ->
        Assert.Contains("directory file", msg, StringComparison.OrdinalIgnoreCase)
    Assert.Equal("MARKER", File.ReadAllText directoryFilePath)
