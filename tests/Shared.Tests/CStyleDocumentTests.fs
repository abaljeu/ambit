module Gambol.Shared.Tests.CStyleDocumentTests

open System
open Xunit
open Gambol.Shared

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private owned (ids: NodeId list) : ChildNode list =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

let private graphWithDocument (childNodes: Node list) : Graph * NodeId =
    let graph0 = Graph.create ()
    let docId = NodeId.New()

    let docNode =
        Node.Create(
            docId,
            text = "doc",
            name = Filename.Ok "Block",
            owner = graph0.root,
            kind = Special File)

    let graph1 =
        graph0.nodes
        |> Map.add docId docNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    let graph2 =
        childNodes
        |> List.fold
            (fun graph node ->
                graph.nodes
                |> Map.add node.id node
                |> fun nodes -> { graph with nodes = nodes })
            graph1

    let childIds = childNodes |> List.map (fun node -> node.id)

    Graph.replace docId 0 [] (owned childIds) graph2
    |> function
        | Ok graph -> graph, docId
        | Error msg -> failwith msg

let private childTexts (nodes: Map<NodeId, Node>) (parentId: NodeId) : string list =
    nodes.[parentId].children
    |> List.map (fun c -> nodes.[c.id].text)

let private hasClass (nodes: Map<NodeId, Node>) (nodeId: NodeId) (name: string) =
    nodes.[nodeId].cssClasses
    |> CssClass.toList
    |> List.contains name

let private sameLineCloseOpenFixture =
    "if (x) { y = 3;"
    + Environment.NewLine
    + "} else { y = 4; }"
    + Environment.NewLine

let private allmanSwitchFixture =
    "DefaultSees(ETile t)"
    + Environment.NewLine
    + "{"
    + Environment.NewLine
    + "\tswitch (t)"
    + Environment.NewLine
    + "\t{"
    + Environment.NewLine
    + "\t\tcase X:"
    + Environment.NewLine
    + "\t}"
    + Environment.NewLine
    + "}"
    + Environment.NewLine
    + "Sees(Tile t)"
    + Environment.NewLine

[<Fact>]
let ``same-line close-open brace split attaches braces to statements`` () =
    let graph, docId = graphWithDocument []
    let result =
        CStyleDocument.read sameLineCloseOpenFixture docId graph
        |> requireOk "read"

    Assert.Equal(2, result.nodes.[docId].children.Length)
    let ifId = result.nodes.[docId].children.Head.id
    let elseId = result.nodes.[docId].children.[1].id
    Assert.Equal("if (x) ", result.nodes.[ifId].text)
    Assert.True(hasClass result.nodes ifId "code-brace")
    Assert.Equal<string list>([ "y = 3;" ], childTexts result.nodes ifId)
    Assert.Equal("else", result.nodes.[elseId].text)
    Assert.True(hasClass result.nodes elseId "code-brace")
    Assert.Equal<string list>([ "y = 4;" ], childTexts result.nodes elseId)
    let y3 = result.nodes.[ifId].children.Head.id
    let y4 = result.nodes.[elseId].children.Head.id
    Assert.False(hasClass result.nodes y3 "code-brace")
    Assert.False(hasClass result.nodes y4 "code-brace")

[<Fact>]
let ``same-line close-open warm unchanged round-trip preserves layout`` () =
    let graph, docId = graphWithDocument []
    let input = sameLineCloseOpenFixture
    let readResult =
        CStyleDocument.read input docId graph |> requireOk "read"

    let output =
        CStyleDocument.writeWarm
            OutlineLcs.diffTexts
            { graph with nodes = readResult.nodes }
            docId
            readResult.complement
            input
        |> requireOk "write"

    Assert.Equal(input, output)

[<Fact>]
let ``Allman switch has no brace-only nodes and marks code-brace`` () =
    let graph, docId = graphWithDocument []
    let result =
        CStyleDocument.read allmanSwitchFixture docId graph
        |> requireOk "read"

    Assert.Equal(2, result.nodes.[docId].children.Length)
    let defaultId = result.nodes.[docId].children.Head.id
    let seesId = result.nodes.[docId].children.[1].id
    Assert.Equal("DefaultSees(ETile t)", result.nodes.[defaultId].text)
    Assert.True(hasClass result.nodes defaultId "code-brace")
    Assert.Equal("Sees(Tile t)", result.nodes.[seesId].text)
    Assert.False(hasClass result.nodes seesId "code-brace")
    let switchId = result.nodes.[defaultId].children.Head.id
    Assert.Equal("switch (t)", result.nodes.[switchId].text)
    Assert.True(hasClass result.nodes switchId "code-brace")
    Assert.Equal<string list>([ "case X:" ], childTexts result.nodes switchId)
    let texts =
        result.nodes
        |> Map.toList
        |> List.map (fun (_, n) -> n.text)

    Assert.False(List.contains "{" texts)
    Assert.False(List.contains "}" texts)

[<Fact>]
let ``warm Keep preserves surrounding brace layout when inner statement edits`` () =
    let graph, docId = graphWithDocument []
    let previous = sameLineCloseOpenFixture
    let readResult =
        CStyleDocument.read previous docId graph |> requireOk "read"

    let ifId = readResult.nodes.[docId].children.Head.id
    let y3Id = readResult.nodes.[ifId].children.Head.id
    let nodes =
        readResult.nodes
        |> Map.add y3Id { readResult.nodes.[y3Id] with text = "y = 9;" }

    let output =
        CStyleDocument.writeWarm
            OutlineLcs.diffTexts
            { graph with nodes = nodes }
            docId
            readResult.complement
            previous
        |> requireOk "write"

    Assert.Contains("if (x) {", output)
    Assert.Contains("} else {", output)
    Assert.Contains("y = 9;", output)
    Assert.DoesNotContain("y = 3;", output)

[<Fact>]
let ``classifyCodec maps cs files to CStyle`` () =
    match DocumentFormat.classifyCodec "src/foo.cs" with
    | Ok DocumentCodec.CStyle -> ()
    | other -> failwith $"expected CStyle, got {other}"
