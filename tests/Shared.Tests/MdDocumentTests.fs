module Gambol.Shared.Tests.MdDocumentTests

open System
open Xunit
open Gambol.Shared

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private owned = ChildNode.owners

let private graphWithDocument (childNodes: Node list) : Graph * NodeId =
    let graph0 = Graph.create ()
    let docId = NodeId.New()
    let docNode =
        Node.Create(
            docId,
            text = "doc",
            name = Filename.Ok "notes",
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
                graph.nodes |> Map.add node.id node |> fun nodes -> { graph with nodes = nodes })
            graph1
    let childIds = childNodes |> List.map (fun node -> node.id)
    Graph.replace docId 0 [] (owned childIds) graph2
    |> function
        | Ok graph -> graph, docId
        | Error msg -> failwith msg

let private normalNode (id: NodeId) (text: string) (owner: NodeId) : Node =
    Node.Create(id, text = text, owner = owner)

let private childTexts (nodes: Map<NodeId, Node>) (parentId: NodeId) : string list =
    nodes.[parentId].children
    |> List.map (fun c -> nodes.[c.id].text)

let private emptyComplement : MdComplement = {
    cssClassesByNodeId = Map.empty
}

let private hasClass (nodes: Map<NodeId, Node>) (nodeId: NodeId) (name: string) =
    nodes.[nodeId].cssClasses
    |> CssClass.toList
    |> List.contains name

[<Fact>]
let ``read heading nesting creates hierarchy`` () =
    let graph, docId = graphWithDocument []
    let text = "# one" + Environment.NewLine + "plain" + Environment.NewLine + "## two" + Environment.NewLine
    let result = MdDocument.read text docId graph |> requireOk "read"
    let h1 = result.nodes.[docId].children.Head.id
    Assert.Equal("one", result.nodes.[h1].text)
    Assert.True(hasClass result.nodes h1 "md-head")
    let plainId = result.nodes.[h1].children.Head.id
    Assert.Equal("plain", result.nodes.[plainId].text)
    let h2 = result.nodes.[h1].children.[1].id
    Assert.Equal("two", result.nodes.[h2].text)
    Assert.True(hasClass result.nodes h2 "md-head")

[<Fact>]
let ``read tag line stays plain not heading`` () =
    let graph, docId = graphWithDocument []
    let text = "#tag" + Environment.NewLine + "# real" + Environment.NewLine
    let result = MdDocument.read text docId graph |> requireOk "read"
    let tagId = result.nodes.[docId].children.Head.id
    Assert.Equal("#tag", result.nodes.[tagId].text)
    Assert.False(hasClass result.nodes tagId "md-head")
    let headId = result.nodes.[docId].children.[1].id
    Assert.Equal("real", result.nodes.[headId].text)
    Assert.True(hasClass result.nodes headId "md-head")

[<Fact>]
let ``read list and nested list`` () =
    let graph, docId = graphWithDocument []
    let text =
        "# section"
        + Environment.NewLine
        + "- item"
        + Environment.NewLine
        + "  - nested"
        + Environment.NewLine
    let result = MdDocument.read text docId graph |> requireOk "read"
    let headId = result.nodes.[docId].children.Head.id
    let listId = result.nodes.[headId].children.Head.id
    Assert.Equal("item", result.nodes.[listId].text)
    Assert.True(hasClass result.nodes listId "md-list")
    let nestedId = result.nodes.[listId].children.Head.id
    Assert.Equal("nested", result.nodes.[nestedId].text)
    Assert.True(hasClass result.nodes nestedId "md-list")

[<Fact>]
let ``read plain line under heading is sibling depth`` () =
    let graph, docId = graphWithDocument []
    let text = "# head" + Environment.NewLine + "body one" + Environment.NewLine + "body two" + Environment.NewLine
    let result = MdDocument.read text docId graph |> requireOk "read"
    let headId = result.nodes.[docId].children.Head.id
    Assert.Equal<string list>([ "body one"; "body two" ], childTexts result.nodes headId)

[<Fact>]
let ``read blank lines do not create nodes`` () =
    let graph, docId = graphWithDocument []
    let text = "alpha" + Environment.NewLine + Environment.NewLine + "beta" + Environment.NewLine
    let result = MdDocument.read text docId graph |> requireOk "read"
    Assert.Equal(2, result.nodes.[docId].children.Length)
    Assert.Equal<string list>([ "alpha"; "beta" ], childTexts result.nodes docId)

[<Fact>]
let ``parse span tree absorbs blank into preceding node`` () =
    let text = "a\n\nb\n"
    let tree =
        (MdReconcile.handler OutlineLcs.diffTexts).parse text (Graph.create ()) Graph.rootId
        |> requireOk "parse"
    let child = tree.children.Head
    Assert.Equal("a", child.text)
    Assert.True(
        child.span.end_ >= text.IndexOf("b"),
        "blank bytes should fold into preceding node span")

[<Fact>]
let ``parse span tree preserves order and trailing blank bounds`` () =
    let text = "a\n\n\nb\n\n"
    let tree =
        (MdReconcile.handler OutlineLcs.diffTexts).parse text (Graph.create ()) Graph.rootId
        |> requireOk "parse"

    Assert.Equal<string list>(
        [ "a"; "b" ],
        tree.children |> List.map (fun child -> child.text))
    Assert.Equal(text.IndexOf("b"), tree.children.[0].span.end_)
    Assert.Equal(text.Length, tree.children.[1].span.end_)

[<Fact>]
let ``parse empty markdown produces empty bounded tree`` () =
    let tree =
        (MdReconcile.handler OutlineLcs.diffTexts).parse "" (Graph.create ()) Graph.rootId
        |> requireOk "parse"

    Assert.Empty(tree.children)
    Assert.Equal(0, tree.span.start)
    Assert.Equal(0, tree.span.end_)

[<Fact>]
let ``write cold emits headings and lists`` () =
    let headId = NodeId.New()
    let listId = NodeId.New()
    let head =
        { normalNode headId "section" Graph.rootId with
            cssClasses = CssClass.ofList [ "md-head" ]
            children = owned [ listId ] }
    let listItem =
        { normalNode listId "item" headId with
            cssClasses = CssClass.ofList [ "md-list" ] }
    let graph, docId = graphWithDocument [ head ]
    let graph' =
        graph.nodes
        |> Map.add listId listItem
        |> Map.add headId { graph.nodes.[headId] with children = owned [ listId ] }
        |> fun nodes -> { graph with nodes = nodes }
    let text =
        MdDocument.write graph' docId emptyComplement None
        |> requireOk "write"
    Assert.Equal(
        "# section" + Environment.NewLine + Environment.NewLine + "- item" + Environment.NewLine,
        text)

[<Fact>]
let ``write with md-head emits hash line`` () =
    let headId = NodeId.New()
    let head =
        { normalNode headId "Title" Graph.rootId with
            cssClasses = CssClass.ofList [ "md-head" ] }
    let graph, docId = graphWithDocument [ head ]
    let text =
        MdDocument.write graph docId emptyComplement None
        |> requireOk "write"
    Assert.Equal("# Title" + Environment.NewLine, text)

[<Fact>]
let ``write without class at same depth emits plain line`` () =
    let plainId = NodeId.New()
    let plain = normalNode plainId "Title" Graph.rootId
    let graph, docId = graphWithDocument [ plain ]
    let text =
        MdDocument.write graph docId emptyComplement None
        |> requireOk "write"
    Assert.Equal("Title" + Environment.NewLine, text)
    Assert.False(text.StartsWith("#"))

[<Fact>]
let ``write cold keeps plain siblings tight and pre-blanks heading`` () =
    let aId = NodeId.New()
    let bId = NodeId.New()
    let headId = NodeId.New()
    let a = normalNode aId "Read this." Graph.rootId
    let b = normalNode bId "hello" Graph.rootId
    let head =
        { normalNode headId "Next" Graph.rootId with
            cssClasses = CssClass.ofList [ "md-head" ] }
    let graph, docId = graphWithDocument [ a; b; head ]
    let text =
        MdDocument.write graph docId emptyComplement None
        |> requireOk "write"
    let nl = Environment.NewLine
    Assert.Equal(
        "Read this." + nl + "hello" + nl + nl + "# Next" + nl,
        text)

[<Fact>]
let ``write cold pre-blanks first list item of a run`` () =
    let plainId = NodeId.New()
    let aId = NodeId.New()
    let bId = NodeId.New()
    let plain = normalNode plainId "intro" Graph.rootId
    let a =
        { normalNode aId "one" Graph.rootId with
            cssClasses = CssClass.ofList [ "md-list" ] }
    let b =
        { normalNode bId "two" Graph.rootId with
            cssClasses = CssClass.ofList [ "md-list" ] }
    let graph, docId = graphWithDocument [ plain; a; b ]
    let text =
        MdDocument.write graph docId emptyComplement None
        |> requireOk "write"
    let nl = Environment.NewLine
    Assert.Equal("intro" + nl + nl + "- one" + nl + "- two" + nl, text)

[<Fact>]
let ``write cold skips empty nodes and does not multiply blanks`` () =
    let aId = NodeId.New()
    let emptyId = NodeId.New()
    let bId = NodeId.New()
    let a = normalNode aId "alpha" Graph.rootId
    let empty = normalNode emptyId "" Graph.rootId
    let b = normalNode bId "beta" Graph.rootId
    let graph, docId = graphWithDocument [ a; empty; b ]
    let text =
        MdDocument.write graph docId emptyComplement None
        |> requireOk "write"
    let nl = Environment.NewLine
    Assert.Equal("alpha" + nl + "beta" + nl, text)

[<Fact>]
let ``write warm does not append blank for empty new node`` () =
    let graph, docId = graphWithDocument []
    let previous = "alpha" + Environment.NewLine + Environment.NewLine + "beta" + Environment.NewLine
    let readResult = MdDocument.read previous docId graph |> requireOk "read"
    let emptyId = NodeId.New()
    let alphaId = readResult.nodes.[docId].children.Head.id
    let betaId = readResult.nodes.[docId].children.[1].id
    let empty = normalNode emptyId "" docId
    let graph' =
        { graph with nodes = readResult.nodes }
        |> fun g ->
            g.nodes
            |> Map.add emptyId empty
            |> Map.add docId { g.nodes.[docId] with children = owned [ alphaId; emptyId; betaId ] }
            |> fun nodes -> { g with nodes = nodes }
    let text =
        MdDocument.writeWarm
            OutlineLcs.diffTexts
            graph'
            docId
            readResult.complement
            previous
        |> requireOk "write"
    Assert.Equal(previous, text)

[<Fact>]
let ``write warm delete drops absorbed trailing blanks`` () =
    let graph0, docId = graphWithDocument []
    let nl = Environment.NewLine
    let previous = "alpha" + nl + nl + nl + "beta" + nl
    let readResult = MdDocument.read previous docId graph0 |> requireOk "read"
    let alphaId = readResult.nodes.[docId].children.Head.id
    let betaId = readResult.nodes.[docId].children.[1].id
    let graph =
        { graph0 with nodes = readResult.nodes }
        |> fun g ->
            g.nodes
            |> Map.remove alphaId
            |> Map.add docId { g.nodes.[docId] with children = owned [ betaId ] }
            |> fun nodes -> { g with nodes = nodes }
    let text =
        MdDocument.writeWarm
            OutlineLcs.diffTexts
            graph
            docId
            readResult.complement
            previous
        |> requireOk "write"
    Assert.Equal("beta" + nl, text)

[<Fact>]
let ``round trip preserves blank lines in previous text`` () =
    let graph, docId = graphWithDocument []
    let input = "# head" + Environment.NewLine + Environment.NewLine + "body" + Environment.NewLine
    let readResult = MdDocument.read input docId graph |> requireOk "read"
    let output =
        MdDocument.writeWarm
            OutlineLcs.diffTexts
            { graph with nodes = readResult.nodes }
            docId
            readResult.complement
            input
        |> requireOk "write"
    Assert.Equal(input, output)

[<Fact>]
let ``unchanged export reconciles with same node ids`` () =
    let graph, docId = graphWithDocument []
    let previous = "# title" + Environment.NewLine + "body" + Environment.NewLine
    let readResult = MdDocument.read previous docId graph |> requireOk "read"
    let graph' = { graph with nodes = readResult.nodes }
    let titleId = readResult.nodes.[docId].children.Head.id
    let bodyId = readResult.nodes.[titleId].children.Head.id
    let result =
        MdReconcile.reconcile OutlineLcs.diffTexts previous graph' docId previous
        |> requireOk "reconcile"
    Assert.Equal(titleId, result.nodes.[docId].children.Head.id)
    Assert.Equal(bodyId, result.nodes.[titleId].children.Head.id)

[<Fact>]
let ``reconcile line text edit keeps ids`` () =
    let graph, docId = graphWithDocument []
    let previous = "# title" + Environment.NewLine + "body" + Environment.NewLine
    let readResult = MdDocument.read previous docId graph |> requireOk "read"
    let graph' = { graph with nodes = readResult.nodes }
    let titleId = readResult.nodes.[docId].children.Head.id
    let bodyId = readResult.nodes.[titleId].children.Head.id
    let edited = "# TITLE" + Environment.NewLine + "body" + Environment.NewLine
    let result =
        MdReconcile.reconcile OutlineLcs.diffTexts previous graph' docId edited
        |> requireOk "reconcile"
    Assert.Equal("TITLE", result.nodes.[titleId].text)
    Assert.Equal(titleId, result.nodes.[docId].children.Head.id)
    Assert.Equal(bodyId, result.nodes.[titleId].children.Head.id)

[<Fact>]
let ``reconcile sibling reorder updates child order`` () =
    let graph, docId = graphWithDocument []
    let nl = Environment.NewLine
    let orderA = "# Title" + nl + "alpha" + nl + "beta" + nl
    let orderB = "# Title" + nl + "beta" + nl + "alpha" + nl
    let readResult = MdDocument.read orderA docId graph |> requireOk "read"
    let graph' = { graph with nodes = readResult.nodes }
    let titleId = readResult.nodes.[docId].children.Head.id
    let alphaId = readResult.nodes.[titleId].children.[0].id
    let betaId = readResult.nodes.[titleId].children.[1].id
    let previous =
        MdDocument.write graph' docId readResult.complement None
        |> requireOk "write"
    let result =
        MdReconcile.reconcile OutlineLcs.diffTexts previous graph' docId orderB
        |> requireOk "reconcile"
    Assert.Equal<string list>([ "beta"; "alpha" ], childTexts result.nodes titleId)
    Assert.Equal(betaId, result.nodes.[titleId].children.[0].id)
    Assert.Equal(alphaId, result.nodes.[titleId].children.[1].id)

[<Fact>]
let ``reconcile section reorder updates outline under h1`` () =
    let graph, docId = graphWithDocument []
    let nl = Environment.NewLine
    let orderA =
        "# Session Start" + nl
        + "## First" + nl
        + "Read this after AGENTS.md." + nl
        + "## Second" + nl
        + "body" + nl
    let orderB =
        "# Session Start" + nl
        + "## Second" + nl
        + "body" + nl
        + "## First" + nl
        + "Read this after AGENTS.md." + nl
    let readResult = MdDocument.read orderA docId graph |> requireOk "read"
    let graph' = { graph with nodes = readResult.nodes }
    let h1 = readResult.nodes.[docId].children.Head.id
    Assert.Equal<string list>([ "First"; "Second" ], childTexts readResult.nodes h1)
    let previous =
        MdDocument.write graph' docId readResult.complement None
        |> requireOk "write"
    let result =
        MdReconcile.reconcile OutlineLcs.diffTexts previous graph' docId orderB
        |> requireOk "reconcile"
    Assert.Equal<string list>([ "Second"; "First" ], childTexts result.nodes h1)

[<Fact>]
let ``unchanged bytes still rebuild outline from file not graph copy`` () =
    let graph, docId = graphWithDocument []
    let nl = Environment.NewLine
    let text = "# Title" + nl + "alpha" + nl + "beta" + nl
    let readResult = MdDocument.read text docId graph |> requireOk "read"
    let graph' = { graph with nodes = readResult.nodes }
    let titleId = readResult.nodes.[docId].children.Head.id
    let previous =
        MdDocument.write graph' docId readResult.complement None
        |> requireOk "write"
    let result =
        MdReconcile.reconcile OutlineLcs.diffTexts previous graph' docId previous
        |> requireOk "reconcile"
    Assert.Equal(titleId, result.nodes.[docId].children.Head.id)
    Assert.Equal<string list>([ "alpha"; "beta" ], childTexts result.nodes titleId)
