module Gambol.Shared.Tests.DocumentColdParseTests

open System
open Xunit
open Gambol.Shared

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err -> failwith $"{label}: {err}"

let private stubRoot (name: string) =
    let documentRootId = NodeId.New()
    let graph0 = Graph.create ()

    let file =
        Node.Create(
            documentRootId,
            text = name,
            name = Filename.create name,
            owner = graph0.root,
            kind = Special File,
            documentState = Unparsed)

    let graph =
        graph0.nodes
        |> Map.add documentRootId file
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    documentRootId, graph

[<Fact>]
let ``planApplyCold paste path uses Plain indent nesting`` () =
    let text = "parent" + Environment.NewLine + "\tchild" + Environment.NewLine
    let rootId, graph = stubRoot "paste.txt"

    let ops =
        DocumentColdParse.planApplyCold
            graph
            rootId
            DocumentColdParse.PasteRelativePath
            text
        |> requireOk "planApplyCold"

    let topLevelIds, nested =
        DocumentColdParse.peelDocumentRootOps rootId ops

    Assert.Equal(1, topLevelIds.Length)
    let parentId = topLevelIds.Head

    let childIds =
        nested
        |> List.tryPick (function
            | Op.Replace(id, _, _, children) when id = parentId ->
                Some(children |> List.map (fun c -> c.id))
            | _ -> None)

    match childIds with
    | None -> failwith "expected child under parent"
    | Some ids -> Assert.Equal(1, ids.Length)

[<Fact>]
let ``PasteRelativePath classifies as Plain`` () =
    match DocumentFormat.classifyCodec DocumentColdParse.PasteRelativePath with
    | Ok DocumentCodec.Plain -> ()
    | Ok other -> failwith $"expected Plain, got {other}"
    | Error err -> failwith err

[<Fact>]
let ``planApplyCold on empty string yields no top-level peel`` () =
    let rootId, graph = stubRoot "empty.txt"

    let ops =
        DocumentColdParse.planApplyCold
            graph
            rootId
            DocumentColdParse.PasteRelativePath
            ""
        |> requireOk "planApplyCold empty"

    let topLevelIds, _ =
        DocumentColdParse.peelDocumentRootOps rootId ops

    Assert.True(List.isEmpty topLevelIds)

[<Fact>]
let ``planApplyCold md heading emits SetClasses md-head`` () =
    let text = "# Title" + Environment.NewLine + "body" + Environment.NewLine
    let rootId, graph = stubRoot "notes.md"

    let after =
        DocumentColdParse.readArtifactCold "notes.md" text rootId graph
        |> requireOk "readArtifactCold"

    let headId = after.nodes.[rootId].children.Head.id
    Assert.True(
        CssClass.toList after.nodes.[headId].cssClasses
        |> List.contains "md-head")

    let ops =
        DocumentColdParse.planOpsFromGraphs graph rootId after

    let topLevelIds, nested =
        DocumentColdParse.peelDocumentRootOps rootId ops

    Assert.Equal(1, topLevelIds.Length)
    Assert.Equal(headId, topLevelIds.Head)

    let headClasses =
        nested
        |> List.tryPick (function
            | Op.SetClasses(id, _, classes) when id = headId ->
                Some(CssClass.toList classes)
            | _ -> None)

    match headClasses with
    | None -> failwith "expected SetClasses for heading node"
    | Some classes -> Assert.Contains("md-head", classes)
