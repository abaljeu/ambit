module Gambol.Shared.Tests.MdDocumentTests

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
        MdReconcile.handler.parse text (Graph.create ()) Graph.rootId
        |> requireOk "parse"
    let child = tree.children.Head
    Assert.Equal("a", child.text)
    Assert.True(
        child.span.end_ >= text.IndexOf("b"),
        "blank bytes should fold into preceding node span")

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
    Assert.Equal("# section" + Environment.NewLine + "- item" + Environment.NewLine, text)

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
let ``round trip preserves blank lines in previous text`` () =
    let graph, docId = graphWithDocument []
    let input = "# head" + Environment.NewLine + Environment.NewLine + "body" + Environment.NewLine
    let readResult = MdDocument.read input docId graph |> requireOk "read"
    let output =
        MdDocument.write
            { graph with nodes = readResult.nodes }
            docId
            readResult.complement
            (Some input)
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
        MdReconcile.reconcile previous graph' docId previous
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
        MdReconcile.reconcile previous graph' docId edited
        |> requireOk "reconcile"
    Assert.Equal("TITLE", result.nodes.[titleId].text)
    Assert.Equal(titleId, result.nodes.[docId].children.Head.id)
    Assert.Equal(bodyId, result.nodes.[titleId].children.Head.id)
