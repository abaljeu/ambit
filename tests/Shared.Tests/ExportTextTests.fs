module Gambol.Shared.Tests.ExportTextTests

open System
open Xunit
open Gambol.Shared

module Enc = Thoth.Json.Newtonsoft.Encode
module Dec = Thoth.Json.Newtonsoft.Decode

let private nl = Environment.NewLine

let private owned (ids: NodeId list) : ChildNode list =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

let private requireOk = function
    | Ok value -> value
    | Error err -> failwith err

let private requirePackage = function
    | Ok package -> package
    | Error err -> failwith $"Expected import package: {err}"

let private newNodeTexts (ops: Op list) : string list =
    ops
    |> List.choose (function
        | Op.NewNode(_, text) -> Some text
        | _ -> None)

/// Build graph: root → ids in order.
let private buildFlat (texts: string list) : Graph * NodeId list =
    let g0 = Graph.create ()
    let g1, ids = ModelBuilder.createNodes texts g0
    let g2 = Graph.replace g1.root 0 [] (owned ids) g1 |> requireOk
    g2, ids

/// Build graph: root → [parentText → childTexts].
let private buildNested (parentText: string) (childTexts: string list) : Graph * NodeId * NodeId list =
    let g0 = Graph.create ()
    let g1, parentIds = ModelBuilder.createNodes [ parentText ] g0
    let parentId = parentIds.[0]
    let g2, childIds = ModelBuilder.createNodes childTexts g1
    let g3 = Graph.replace g0.root 0 [] (owned [ parentId ]) g2 |> requireOk
    let g4 = Graph.replace parentId 0 [] (owned childIds) g3 |> requireOk
    g4, parentId, childIds

[<Fact>]
let ``serializeOwnedChildren single child`` () =
    let graph, parentId, childIds = buildNested "parent" [ "alpha" ]
    let text = ExportText.serializeOwnedChildren graph parentId

    Assert.Equal("alpha", text)

    let package = ImportText.buildPackage "note.txt" text |> requirePackage
    Assert.Equal<string list>([ "alpha" ], newNodeTexts package.ops)
    Assert.Single(package.topLevelIds)

[<Fact>]
let ``serializeOwnedChildren sibling children`` () =
    let graph, parentId, _ = buildNested "parent" [ "alpha"; "beta" ]
    let text = ExportText.serializeOwnedChildren graph parentId

    Assert.Equal("alpha" + nl + "beta", text)

    let package = ImportText.buildPackage "note.txt" text |> requirePackage
    Assert.Equal<string list>([ "alpha"; "beta" ], newNodeTexts package.ops)

[<Fact>]
let ``serializeOwnedChildren nested owned child`` () =
    let graph, parentId, childIds = buildNested "focus" [ "parent" ]
    let childId = childIds.[0]
    let graph, grandIds = ModelBuilder.createNodes [ "nested" ] graph
    let graph = Graph.replace childId 0 [] (owned grandIds) graph |> requireOk
    let text = ExportText.serializeOwnedChildren graph parentId
    let package = ImportText.buildPackage "note.txt" text |> requirePackage

    Assert.Equal("parent" + nl + "\tnested", text)
    Assert.Equal<string list>([ "parent"; "nested" ], newNodeTexts package.ops)

[<Fact>]
let ``serializeOwnedChildren omits ref children`` () =
    let graph, parentId, childIds = buildNested "parent" [ "owned" ]
    let graph, refIds = ModelBuilder.createNodes [ "ref-only" ] graph
    let refId = refIds.[0]
    let graph =
        Graph.replace parentId 0 (owned childIds) (owned childIds @ [ { ref = Ownership.Ref; id = refId } ]) graph
        |> requireOk

    let text = ExportText.serializeOwnedChildren graph parentId

    Assert.Equal("owned", text)
    Assert.DoesNotContain("ref-only", text)

[<Fact>]
let ``validateExportContent rejects blank subtree`` () =
    let graph, parentId, _ = buildNested "parent" []

    match ExportText.validateExportContent (ExportText.serializeOwnedChildren graph parentId) with
    | Ok _ -> failwith "Expected blank export to fail"
    | Error err -> Assert.Equal("export text is empty", err)

[<Fact>]
let ``trySerializeOwnedChildren returns error for empty subtree`` () =
    let graph, parentId, _ = buildNested "parent" []

    match ExportText.trySerializeOwnedChildren graph parentId with
    | Ok _ -> failwith "Expected empty export to fail"
    | Error err -> Assert.Equal("export text is empty", err)

[<Fact>]
let ``DesktopExportRequest serializes round-trip`` () =
    let request = { path = "note.txt"; content = "alpha" + nl }
    let json = Enc.toString 0 (Serialization.encodeDesktopExportRequest request)

    match Dec.fromString Serialization.decodeDesktopExportRequest json with
    | Error err -> failwith $"Decode failed: {err}"
    | Ok decoded ->
        Assert.Equal(request.path, decoded.path)
        Assert.Equal(request.content, decoded.content)

[<Fact>]
let ``DesktopExportResponse serializes round-trip`` () =
    let response = { path = "note.txt" }
    let json = Enc.toString 0 (Serialization.encodeDesktopExportResponse response)

    match Dec.fromString Serialization.decodeDesktopExportResponse json with
    | Error err -> failwith $"Decode failed: {err}"
    | Ok decoded -> Assert.Equal(response.path, decoded.path)
