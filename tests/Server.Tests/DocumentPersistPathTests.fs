module DocumentPersistenceTests.Path

open System
open System.IO
open DocumentPersistenceTests.Fixtures
open Gambol.Server
open Gambol.Server.Tests.TestBackend
open Gambol.Shared
open Xunit

[<Fact>]
let ``fileStatusForReference reports existing and missing server artifacts`` () =
    let dataDir = newTempDir ()
    let graph, _, _, _, _ = graphWithNestedDocs ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "writeAllDocuments" |> ignore

    let existing =
        DocumentPersistPath.fileStatusForReference dataDir "//home/docs/readme.txt"
        |> requireOk "existing status"

    let missing =
        DocumentPersistPath.fileStatusForReference dataDir "//home/docs/missing.txt"
        |> requireOk "missing status"

    Assert.Equal(ExistingFile, existing.status)
    Assert.True(existing.sourceModifiedUtc.IsSome)
    Assert.Equal(MissingArtifact, missing.status)
    Assert.Equal(None, missing.sourceModifiedUtc)

[<Fact>]
let ``fileStatusForReference reports workspace and directory as folder not file`` () =
    let dataDir = newTempDir ()
    let graph, _, _, _, _ = graphWithNestedDocs ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "writeAllDocuments" |> ignore

    let ws =
        DocumentPersistPath.fileStatusForReference dataDir "//home"
        |> requireOk "workspace status"
    let dir =
        DocumentPersistPath.fileStatusForReference dataDir "//home/docs/"
        |> requireOk "directory status"

    Assert.Equal(ExistingFolder, ws.status)
    Assert.Equal(ExistingFolder, dir.status)

[<Fact>]
let ``fileStatusForReference directory is folder when dir exists without amb`` () =
    let dataDir = newTempDir ()
    let dirPath = Path.Combine(dataDir, "fambit", "elm")
    Directory.CreateDirectory dirPath |> ignore

    let status =
        DocumentPersistPath.fileStatusForReference dataDir "//fambit/elm/"
        |> requireOk "mkcol-only directory"

    Assert.Equal(ExistingFolder, status.status)

[<Fact>]
let ``fileStatusForReference directory missing when neither dir nor amb exists`` () =
    let dataDir = newTempDir ()
    Directory.CreateDirectory(Path.Combine(dataDir, "fambit")) |> ignore

    let status =
        DocumentPersistPath.fileStatusForReference dataDir "//fambit/doc/"
        |> requireOk "absent directory"

    Assert.Equal(MissingArtifact, status.status)

[<Fact>]
let ``fileStatusForReference missing when neither amb nor folder exist`` () =
    let dataDir = newTempDir ()

    let status =
        DocumentPersistPath.fileStatusForReference dataDir "//fambit/doc/"
        |> requireOk "missing folder"

    Assert.Equal(MissingArtifact, status.status)

[<Fact>]
let ``resolveArtifactPath malformed document root identifies node`` () =
    let dataDir = newTempDir ()
    let graph0 = Graph.create ()
    let malformedId = NodeId.New()
    let malformedNode =
        Node.Create(
            malformedId,
            text = "orphan",
            name = Filename.create "orphan",
            owner = Graph.rootId,
            kind = Special Directory)
    let graph = Graph.addDetachedNode malformedNode graph0

    let expected =
        $"no artifact path for document root: id={malformedId.Value}; "
        + $"kind=Special Directory; name=orphan; owner={Graph.rootId.Value}"

    match DocumentPersistPath.resolveArtifactPath dataDir graph malformedId with
    | Ok _ -> failwith "expected error"
    | Error error -> Assert.Equal(expected, error)

[<Fact>]
let ``resolveArtifactPath unknown document root identifies missing node`` () =
    let dataDir = newTempDir ()
    let graph = Graph.create ()
    let unknownId = NodeId.New()
    let expected =
        $"no artifact path for document root: id={unknownId.Value}; node=missing"

    match DocumentPersistPath.resolveArtifactPath dataDir graph unknownId with
    | Ok _ -> failwith "expected error"
    | Error error -> Assert.Equal(expected, error)

    Assert.False(
        Directory.EnumerateFiles(dataDir, "*", SearchOption.AllDirectories)
        |> Seq.exists (fun _ -> true))

[<Fact>]
let ``discoverArtifactRelatives finds all written artifacts`` () =
    let dataDir = newTempDir ()
    let graph, wsId, dirId, fileId, _ = graphWithNestedDocs ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let relatives =
        DocumentPersistPath.discoverArtifactRelatives dataDir
        |> requireOk "discover"
        |> Set.ofList
    let expected =
        [ Graph.rootId; Graph.systemId; Graph.trashId; wsId; dirId; fileId ]
        |> List.choose (DocumentPartition.artifactFileRelative graph)
        |> Set.ofList
    Assert.Equal<Set<string>>(expected, relatives)

[<Fact>]
let ``discoverArtifactRelatives lists stray amb file`` () =
    let dataDir = newTempDir ()
    File.WriteAllText(Path.Combine(dataDir, "foo.amb"), "")
    let relatives =
        DocumentPersistPath.discoverArtifactRelatives dataDir
        |> requireOk "discover"
    Assert.Contains("foo.amb", relatives)

[<Fact>]
let ``discoverArtifactRelatives excludes reserved gambol dot files`` () =
    let dataDir = newTempDir ()
    let nested = Path.Combine(dataDir, "nested")
    Directory.CreateDirectory(nested) |> ignore
    File.WriteAllText(Path.Combine(dataDir, "gambol.events"), "bookkeeping")
    File.WriteAllText(Path.Combine(dataDir, "fetch.log"), "ordinary artifact")
    File.WriteAllText(Path.Combine(nested, "GAMBOL.meta"), "bookkeeping")
    File.WriteAllText(Path.Combine(dataDir, "gambolish"), "ordinary artifact")
    let relatives =
        DocumentPersistPath.discoverArtifactRelatives dataDir
        |> requireOk "discover"
    Assert.DoesNotContain("gambol.events", relatives)
    Assert.DoesNotContain("nested/GAMBOL.meta", relatives)
    Assert.Contains("fetch.log", relatives)
    Assert.Contains("gambolish", relatives)

[<Fact>]
let ``discoverArtifactRelatives lists plain file alongside amb artifacts`` () =
    let dataDir = newTempDir ()
    let graph, _, dirId, fileId, _ = graphWithNestedDocs ()
    DocumentPersistWrite.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let relatives =
        DocumentPersistPath.discoverArtifactRelatives dataDir
        |> requireOk "discover"
        |> Set.ofList
    let fileRel = DocumentPartition.artifactFileRelative graph fileId |> Option.get
    let dirRel = DocumentPartition.artifactFileRelative graph dirId |> Option.get
    Assert.True(relatives.Contains fileRel)
    Assert.True(relatives.Contains dirRel)
    Assert.EndsWith(".txt", fileRel, StringComparison.OrdinalIgnoreCase)
    Assert.EndsWith(".amb", dirRel, StringComparison.OrdinalIgnoreCase)

let private writeImmediateMemberTree (dataDir: string) =
    Directory.CreateDirectory(Path.Combine(dataDir, "docs", "inner")) |> ignore
    Directory.CreateDirectory(Path.Combine(dataDir, "empty")) |> ignore
    File.WriteAllText(Path.Combine(dataDir, "readme.txt"), "top")
    File.WriteAllText(Path.Combine(dataDir, "notes.amb"), "")
    File.WriteAllText(Path.Combine(dataDir, "docs", "nested.txt"), "nested")
    File.WriteAllText(
        Path.Combine(dataDir, "docs", "inner", "deep.txt"),
        "deep")

[<Fact>]
let ``discoverImmediateMembers returns direct files and child directories`` () =
    let dataDir = newTempDir ()
    writeImmediateMemberTree dataDir
    let members =
        DocumentPersistPath.discoverImmediateMembers dataDir
        |> requireOk "discover"
        |> Set.ofList
    let expected =
        Set.ofList [ "readme.txt"; "notes.amb"; "docs"; "empty" ]
    Assert.Equal<Set<string>>(expected, members)

[<Fact>]
let ``discoverArtifactRelatives returns nested files`` () =
    let dataDir = newTempDir ()
    writeImmediateMemberTree dataDir
    let relatives =
        DocumentPersistPath.discoverArtifactRelatives dataDir
        |> requireOk "discover"
        |> Set.ofList
    let expected =
        Set.ofList [
            "readme.txt"
            "notes.amb"
            "docs/nested.txt"
            "docs/inner/deep.txt"
        ]
    Assert.Equal<Set<string>>(expected, relatives)

[<Fact>]
let ``resolveArtifactPath plain file resolves to named extension path`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, _ = graphWithNestedDocs ()
    let path = artifactFullPath dataDir graph fileId
    Assert.Equal(Path.Combine(dataDir, "home", "docs", "readme.txt"), path)
