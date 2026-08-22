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
let ``planParseFile rejects file with no server body`` () =
    let rootId, graph0 = stubRoot "absent.txt"
    let graph =
        Graph.fromNodes
            graph0.root
            (Map.add
                rootId
                { graph0.nodes.[rootId] with
                    documentState = NoServerFile }
                graph0.nodes)
    match ImportDocument.planParseFile graph rootId "body" with
    | Ok _ -> Assert.Fail("expected absent server file to be rejected")
    | Error error -> Assert.Equal("no file on server", error)

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
            | Op.Replace(id, _, children) when id = parentId ->
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
let ``planApplyCold rejects oversized text before graph materialization`` () =
    let rootId, graph = stubRoot "large.csv"
    let actualCodeUnits = DocumentParseLimits.maxInputCodeUnits + 1
    let text = String('x', actualCodeUnits)

    match
        DocumentColdParse.planApplyCold
            graph
            rootId
            "large.csv"
            text
    with
    | Ok _ -> Assert.Fail("expected oversized parse to fail")
    | Error err ->
        Assert.Equal(
            DocumentParseLimits.errorForCodeUnits actualCodeUnits,
            err)

/// Mirrors Client UpdatePaste.pasteNodesSelecting after planPasteOps.
let private applySelectModeExternalPaste
    (graph: Graph)
    (parentId: NodeId)
    (selStart: int)
    (selected: ChildNode list)
    (pastedText: string)
    : Result<Graph, string> =
    match DocumentColdParse.planPasteOps pastedText with
    | Error e -> Error e
    | Ok(topLevelIds, nested) ->
        let insertChildren =
            ChildNode.owners topLevelIds

        let parentChildren = graph.nodes.[parentId].children
        let replaceOp =
            ChildListWire.edit
                parentId
                parentChildren
                selStart
                selected.Length
                selStart
                insertChildren

        let change =
            { id = 0
              changeId = Guid.NewGuid()
              ops = nested @ [ replaceOp ] }

        let state =
            { graph = graph
              history = History.empty
              revision = Revision.Zero }

        match History.applyChange change state with
        | ApplyResult.Changed s -> Ok s.graph
        | ApplyResult.Unchanged _ -> Error "paste applied as Unchanged"
        | ApplyResult.Invalid(_, msg) -> Error msg

[<Fact>]
let ``live-parent planApplyCold peel is empty when siblings exist`` () =
    let g0 = Graph.create ()
    let parentId = NodeId.New()
    let keepId = NodeId.New()
    let selectedId = NodeId.New()

    let parent =
        Node.Create(
            parentId,
            text = "parent",
            owner = g0.root,
            children =
                [ ChildNode.owner keepId
                  ChildNode.owner selectedId ])

    let keep = Node.Create(keepId, text = "keep", owner = parentId)
    let selected = Node.Create(selectedId, text = "selected", owner = parentId)

    let graph =
        g0.nodes
        |> Map.add parentId parent
        |> Map.add keepId keep
        |> Map.add selectedId selected
        |> fun nodes ->
            let rootChildren =
                g0.nodes.[g0.root].children
                @ [ ChildNode.owner parentId ]

            Map.add g0.root { g0.nodes.[g0.root] with children = rootChildren } nodes
        |> fun nodes -> Graph.fromNodes g0.root nodes

    let pasted = "alpha" + Environment.NewLine + "beta" + Environment.NewLine

    let planned =
        DocumentColdParse.planApplyCold
            graph
            parentId
            DocumentColdParse.PasteRelativePath
            pasted
        |> requireOk "planApplyCold"

    let topLevelIds, _ =
        DocumentColdParse.peelDocumentRootOps parentId planned

    Assert.True(
        topLevelIds.IsEmpty,
        "documents why paste must not cold-plan against the live parent")

[<Fact>]
let ``select-mode external multiline paste keeps non-selected siblings`` () =
    let g0 = Graph.create ()
    let parentId = NodeId.New()
    let keepId = NodeId.New()
    let selectedId = NodeId.New()

    let parent =
        Node.Create(
            parentId,
            text = "parent",
            owner = g0.root,
            children =
                [ ChildNode.owner keepId
                  ChildNode.owner selectedId ])

    let keep = Node.Create(keepId, text = "keep", owner = parentId)
    let selected = Node.Create(selectedId, text = "selected", owner = parentId)

    let graph =
        g0.nodes
        |> Map.add parentId parent
        |> Map.add keepId keep
        |> Map.add selectedId selected
        |> fun nodes ->
            let rootChildren =
                g0.nodes.[g0.root].children
                @ [ ChildNode.owner parentId ]

            Map.add g0.root { g0.nodes.[g0.root] with children = rootChildren } nodes
        |> fun nodes -> Graph.fromNodes g0.root nodes

    let pasted = "alpha" + Environment.NewLine + "beta" + Environment.NewLine
    let selectedChild = ChildNode.owner selectedId

    match
        applySelectModeExternalPaste
            graph
            parentId
            1
            [ selectedChild ]
            pasted
    with
    | Error err -> Assert.Fail($"expected paste apply to succeed: {err}")
    | Ok after ->
        let children = after.nodes.[parentId].children
        let texts =
            children
            |> List.map (fun c -> after.nodes.[c.id].text)

        Assert.Contains("keep", texts)
        Assert.Contains("alpha", texts)
        Assert.Contains("beta", texts)
        Assert.DoesNotContain("selected", texts)
        Assert.Equal(keepId, children.Head.id)

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
