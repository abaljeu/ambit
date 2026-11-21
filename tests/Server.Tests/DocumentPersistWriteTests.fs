module DocumentPersistenceTests.Write

open System
open System.IO
open DocumentPersistenceTests.Fixtures
open Gambol.Server
open Gambol.Server.Tests.TestBackend
open Gambol.Shared
open Xunit

[<Fact>]
let ``writeAllDocuments bootstrap graph writes ROOT and TRASH artifacts`` () =
    let dataDir = newTempDir ()
    let graph = Graph.create ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "writeAllDocuments" |> ignore
    Assert.True(File.Exists(Path.Combine(dataDir, ".amb")))
    Assert.True(File.Exists(Path.Combine(dataDir, "TRASH", ".amb")))
    Assert.True(File.Exists(Path.Combine(dataDir, "SYSTEM", ".amb")))

[<Fact>]
let ``writeAllDocuments nested workspace tree writes expected paths`` () =
    let dataDir = newTempDir ()
    let graph, _, _, _, _ = graphWithNestedDocs ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "writeAllDocuments" |> ignore
    Assert.True(File.Exists(Path.Combine(dataDir, "home", ".amb")))
    Assert.True(File.Exists(Path.Combine(dataDir, "home", "docs", ".amb")))
    Assert.True(File.Exists(Path.Combine(dataDir, "home", "docs", "readme.txt")))

[<Fact>]
let ``writeAllDocuments stamps artifact updateTime from disk mtime`` () =
    let dataDir = newTempDir ()
    let graph, wsId, dirId, fileId, _ = graphWithNestedDocs ()
    let stamped =
        DocumentPersistWrite.writeAllDocuments dataDir graph
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
let ``planParseFile refuses oversized body before writing artifact`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, _ = graphWithNestedDocs ()
    let diskPath = artifactFullPath dataDir graph fileId
    let actualCodeUnits = DocumentParseLimits.maxInputCodeUnits + 1
    let text = String('x', actualCodeUnits)

    match DocumentPersistWrite.planParseFile dataDir graph fileId (Some text) with
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
    let diskPath = artifactFullPath dataDir graph fileId
    Directory.CreateDirectory(Path.GetDirectoryName diskPath) |> ignore
    File.WriteAllText(diskPath, "EXISTING")

    DocumentPersistWrite.planParseFile dataDir graph fileId (Some " \r\n\t")
    |> requireOk "blank body parse"
    |> ignore

    Assert.Equal(" \r\n\t", File.ReadAllText diskPath)

[<Fact>]
let ``planParseFile blank DataDir artifact without request text succeeds`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, _ = graphWithNestedDocs ()
    let diskPath = artifactFullPath dataDir graph fileId
    Directory.CreateDirectory(Path.GetDirectoryName diskPath) |> ignore
    File.WriteAllText(diskPath, " \r\n\t")

    DocumentPersistWrite.planParseFile dataDir graph fileId None
    |> requireOk "blank artifact parse"
    |> ignore

[<Fact>]
let ``planParseFile DataDir warm keeps line NodeId on text edit`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, normalId = graphWithNestedDocs ()

    DocumentPersistWrite.writeAllDocuments dataDir graph
    |> requireOk "writeAllDocuments"
    |> ignore

    File.WriteAllText(artifactFullPath dataDir graph fileId, "BODY\n")

    let ops =
        DocumentPersistWrite.planParseFile dataDir graph fileId None
        |> requireOk "planParseFile"

    Assert.False(List.isEmpty ops)

    let after = applyOps graph ops

    Assert.Equal(normalId, (Graph.children after fileId).Head.id)
    Assert.Equal("BODY", after.nodes.[normalId].text)
    Assert.Equal(Current, after.nodes.[fileId].documentState.AsEnum)

[<Fact>]
let ``planParseFile with body text writes artifact to DataDir`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, _ = graphWithNestedDocs ()
    let diskPath = artifactFullPath dataDir graph fileId

    DocumentPersistWrite.planParseFile
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
    let diskPath = artifactFullPath dataDir graph fileId
    Directory.CreateDirectory(Path.GetDirectoryName diskPath) |> ignore
    File.WriteAllText(diskPath, "STALE")
    let original = DateTime(2024, 2, 3, 4, 5, 6, DateTimeKind.Utc)
    File.SetLastWriteTimeUtc(diskPath, original)

    DocumentPersistWrite.planParseFile
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

    DocumentPersistWrite.writeAllDocuments dataDir graph
    |> requireOk "writeAllDocuments"
    |> ignore

    let ops =
        DocumentPersistWrite.planParseFile
            dataDir
            graph
            fileId
            (Some "FROMBODY\n")
        |> requireOk "planParseFile body"

    let after = applyOps graph ops

    Assert.Equal(normalId, (Graph.children after fileId).Head.id)
    Assert.Equal("FROMBODY", after.nodes.[normalId].text)

[<Fact>]
let ``planParseFile without text rejects File with no server occurrence`` () =
    let dataDir = newTempDir ()
    let graph, fileId = graphWithEmptyRootFile ()

    match DocumentPersistWrite.planParseFile dataDir graph fileId None with
    | Error msg ->
        Assert.Equal("selected File has no occurrence on the server", msg)
    | Ok _ -> Assert.Fail("expected no-occurrence error")

[<Fact>]
let ``writeAllDocuments ROOT file lands at dataDir root without amb suffix`` () =
    let dataDir = newTempDir ()
    let graph, fileId = graphWithRootFile ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "writeAllDocuments" |> ignore
    let path = artifactFullPath dataDir graph fileId
    Assert.Equal(Path.Combine(dataDir, "name.ext"), path)
    Assert.True(File.Exists path)

[<Fact>]
let ``planParseFile ROOT file reads artifact directly from DataDir`` () =
    let dataDir = newTempDir ()
    let graph, fileId = graphWithRootFile ()
    File.WriteAllText(Path.Combine(dataDir, "name.ext"), "FROM ROOT\n")

    let ops =
        DocumentPersistWrite.planParseFile dataDir graph fileId None
        |> requireOk "planParseFile ROOT"

    Assert.False(List.isEmpty ops)

[<Fact>]
let ``writeAllDocuments nested file directory boundary writes separate artifacts`` () =
    let dataDir = newTempDir ()
    let graph, fileId, dirId, normalId = graphFileOwnsDirectory ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "writeAllDocuments" |> ignore
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
let ``writeDocument round trip preserves member text`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, normalId = graphWithNestedDocs ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "writeAllDocuments" |> ignore
    let filePath = artifactFullPath dataDir graph fileId
    let text = File.ReadAllText filePath

    match PlainTextDocument.read text fileId graph with
    | Error msg -> failwith msg
    | Ok result ->
        match Map.tryFind normalId result.nodes with
        | None -> failwith "member node missing after read"
        | Some node -> Assert.Equal("body", node.text)

[<Fact>]
let ``writeAllDocuments plain file writes outline without stable id syntax`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, normalId = graphWithNestedDocs ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let filePath = artifactFullPath dataDir graph fileId
    let text = File.ReadAllText filePath
    let sid = AmbDocument.formatStableId normalId
    Assert.DoesNotContain("^" + sid, text)
    Assert.Contains("body", text)

[<Fact>]
let ``writeAllDocuments amb directory artifact keeps stable id syntax`` () =
    let dataDir = newTempDir ()
    let graph, _, dirId, fileId, _ = graphWithNestedDocs ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let dirPath = artifactFullPath dataDir graph dirId
    let text = File.ReadAllText dirPath
    let fileSid = AmbDocument.formatStableId fileId
    Assert.Contains("^" + fileSid, text)

[<Fact>]
let ``writeAllDocuments ref child in plain file writes target text only`` () =
    let dataDir = newTempDir ()
    let graph, _, fileId, sharedId = graphWithPlainFileRef ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let filePath = artifactFullPath dataDir graph fileId
    let text = File.ReadAllText filePath
    let sharedSid = AmbDocument.formatStableId sharedId
    Assert.Contains("shared text", text)
    Assert.DoesNotContain("->", text)
    Assert.DoesNotContain("^" + sharedSid, text)

[<Fact>]
let ``refuseDirectoryFileNamedDocument errors on illicit amb node name`` () =
    let node =
        Node.Create(
            NodeId.New(),
            text = ".amb",
            name = Filename.Ok ".amb",
            owner = Graph.rootId,
            kind = Special File)
    match DocumentPersistWrite.refuseDirectoryFileNamedDocument node with
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
    DocumentPersistWrite.refuseDirectoryFileNamedDocument node
    |> requireOk "refuseDirectoryFileNamedDocument"
    |> ignore

[<Fact>]
let ``writeDocument allows SYSTEM Directory File`` () =
    let dataDir = newTempDir ()
    let graph = Graph.create ()
    DocumentPersistWrite.writeDocument dataDir graph Graph.systemId
    |> requireOk "write SYSTEM/.amb"
    |> ignore
    Assert.True(File.Exists(Path.Combine(dataDir, "SYSTEM", ".amb")))

[<Fact>]
let ``writeDocument allows SYSTEM user css`` () =
    let dataDir = newTempDir ()
    let graph, fileId = graphWithSystemFile "user.css"
    DocumentPersistWrite.writeDocument dataDir graph fileId
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
    match DocumentPersistWrite.writeDocument dataDir graph fileId with
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
    match DocumentPersistWrite.planParseFile dataDir graph fileId (Some "new") with
    | Ok _ -> failwith "expected planParseFile to refuse SYSTEM write"
    | Error msg ->
        Assert.Contains(
            "system directory write refused",
            msg,
            StringComparison.OrdinalIgnoreCase)
    Assert.Equal("SECRET", File.ReadAllText path)

[<Fact>]
let ``writeDocument refuses illicit amb-named file and leaves DataDir untouched`` () =
    let dataDir = newTempDir ()
    let graph, _, fileId = graphWithIllicitAmbFile ()
    let homeDir = Path.Combine(dataDir, "home")
    Directory.CreateDirectory homeDir |> ignore
    let directoryFilePath = Path.Combine(homeDir, ".amb")
    File.WriteAllText(directoryFilePath, "MARKER")
    match DocumentPersistWrite.writeDocument dataDir graph fileId with
    | Ok _ -> failwith "expected writeDocument to refuse illicit .amb"
    | Error msg ->
        Assert.Contains("directory file", msg, StringComparison.OrdinalIgnoreCase)
    Assert.Equal("MARKER", File.ReadAllText directoryFilePath)
