module Gambol.Shared.Tests.AmbExtractWalkTests

open System
open Xunit
open Gambol.Shared
open GraphChildMapHelpers

let private owned = ChildNode.owners
let private nl = Environment.NewLine

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private writeDefault graph rootId =
    AmbDocument.write graph rootId |> requireOk "default write"

let private writeExtract graph rootId =
    AmbDocument.writeWith AmbWriteWalk.SuppliedExtract graph rootId
    |> requireOk "extract write"

let private placeOwned parentId childIds graph =
    Graph.replace parentId 0 [] (owned childIds) graph
    |> requireOk "place owned"

/// Directory document that owns a File (nested document root) with a child.
let private directoryWithNestedFile () =
    let graph0 = Graph.create ()
    let dirId = NodeId.New()
    let fileId = NodeId.New()
    let insideId = NodeId.New()
    let dir =
        Node.Create(
            dirId,
            text = "docs",
            name = Filename.Ok "docs",
            owner = graph0.root,
            kind = Special Directory)
    let file =
        Node.Create(
            fileId,
            text = "file body",
            name = Filename.Ok "note.txt",
            owner = dirId,
            kind = Special File)
    let inside =
        Node.Create(insideId, text = "inside-file", owner = fileId)
    let graph1 = addDetachedMany [ dir; file; inside ] graph0
    let graph2 = placeOwned Graph.rootId [ dirId ] graph1
    let graph3 = placeOwned dirId [ fileId ] graph2
    let graph = placeOwned fileId [ insideId ] graph3
    graph, dirId, fileId, insideId

/// Document whose only mention of `shared` is a Ref; shared owns `leaf`.
let private documentWithRefOnlyTarget () =
    let graph0 = Graph.create ()
    let docId = NodeId.New()
    let holderId = NodeId.New()
    let sharedId = NodeId.New()
    let leafId = NodeId.New()
    let doc =
        Node.Create(
            docId,
            text = "doc",
            name = Filename.Ok "notes",
            owner = graph0.root,
            kind = Special File)
    let holder = Node.Create(holderId, text = "holder", owner = docId)
    let shared =
        Node.Create(
            sharedId,
            text = "shared-body",
            owner = Graph.rootId)
    let leaf = Node.Create(leafId, text = "ref-leaf", owner = sharedId)
    let graph1 = addDetachedMany [ doc; holder; shared; leaf ] graph0
    let graph2 = placeOwned Graph.rootId [ docId ] graph1
    let graph3 = placeOwned docId [ holderId ] graph2
    let graph4 =
        setChildren holderId [ ChildNode.reference sharedId ] graph3
    let graph = placeOwned sharedId [ leafId ] graph4
    graph, docId, sharedId, leafId

[<Fact>]
let ``extract walk recurses nested File children; default write stops`` () =
    let graph, dirId, _, insideId = directoryWithNestedFile ()
    let defaultText = writeDefault graph dirId
    let extractText = writeExtract graph dirId
    Assert.DoesNotContain("inside-file", defaultText)
    Assert.Contains("inside-file", extractText)
    Assert.Contains("file body", extractText)
    Assert.DoesNotContain(AmbDocument.formatStableId insideId + " unused", extractText)

[<Fact>]
let ``extract walk recurses present Ref; default write emits link only`` () =
    let graph, docId, sharedId, _ = documentWithRefOnlyTarget ()
    let defaultText = writeDefault graph docId
    let extractText = writeExtract graph docId
    let sid = AmbDocument.formatStableId sharedId
    Assert.Contains("-> ", defaultText)
    Assert.Contains(sid, defaultText)
    Assert.DoesNotContain("ref-leaf", defaultText)
    Assert.DoesNotContain("shared-body", defaultText)
    Assert.Contains("shared-body", extractText)
    Assert.Contains("ref-leaf", extractText)
    Assert.DoesNotContain("-> ", extractText)

[<Fact>]
let ``extract walk omits child ids missing from the extract`` () =
    let rootId = NodeId.New()
    let presentId = NodeId.New()
    let missingId = NodeId.New()
    let present = Node.Create(presentId, text = "kept-child")
    let root = Node.Create(rootId, text = "zoom")
    let graph =
        Graph.fromExtracted
            rootId
            (Map.ofList [ rootId, root; presentId, present ])
            (Map.ofList
                [ (rootId,
                   [ ChildNode.owner presentId
                     ChildNode.owner missingId
                     ChildNode.reference missingId ])
                  (presentId, []) ])
    let text = writeExtract graph rootId
    Assert.Contains("kept-child", text)
    Assert.DoesNotContain(AmbDocument.formatStableId missingId, text)
    Assert.DoesNotContain("-> ", text)

[<Fact>]
let ``extract walk Amb text has no Focus sentinel`` () =
    let rootId = NodeId.New()
    let childId = NodeId.New()
    let child = Node.Create(childId, text = "visible")
    let root = Node.Create(rootId, text = "zoom")
    let graph =
        Graph.fromExtracted
            rootId
            (Map.ofList [ rootId, root; childId, child ])
            (Map.ofList [ rootId, owned [ childId ]; childId, [] ])
        |> Graph.withFocus (Some childId)
    let text = writeExtract graph rootId
    Assert.Contains("visible", text)
    Assert.DoesNotContain("<focus>", text)
    Assert.DoesNotContain("<FOCUS>", text)
    Assert.DoesNotContain("FOCUS ", text)
    Assert.DoesNotContain("Focus ", text)

[<Fact>]
let ``extract walk returns Amb text and does not persist a file`` () =
    let graph, dirId, fileId, _ = directoryWithNestedFile ()
    let focused = Graph.withFocus (Some fileId) graph
    match
        AmbDocument.writeWith AmbWriteWalk.SuppliedExtract focused dirId
    with
    | Error msg -> failwith $"extract write: {msg}"
    | Ok text ->
        Assert.False(String.IsNullOrWhiteSpace text)
        Assert.Contains(nl, text)
        Assert.DoesNotContain("\\", text.Replace("\t", ""))
        match DocumentPartition.artifactFileRelative focused dirId with
        | None -> failwith "expected a directory artifact path"
        | Some path -> Assert.DoesNotContain(path, text)
