module Gambol.Shared.Tests.MdDocumentTests

open System
open Xunit
open Gambol.Shared
open GraphChildMapHelpers

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private owned = ChildNode.owners

let private kids
    (childMap: Map<NodeId, ChildNode list>)
    parentId
    : ChildNode list =
    Map.tryFind parentId childMap |> Option.defaultValue []

let private withRead (graph: Graph) nodes childMap =
    let pairs =
        childMap
        |> Map.toList
        |> List.filter (fun (id, _) ->
            id <> graph.root && not (Graph.isSystemFolderNode id))
    fromExisting graph nodes |> setChildMap pairs

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
        Graph.addDetachedNode docNode graph0
        |> appendKids graph0.root [ ChildNode.owner docId ]
    let graph2 = addDetachedMany childNodes graph1
    let childIds = childNodes |> List.map (fun node -> node.id)
    Graph.replace docId 0 [] (owned childIds) graph2
    |> function
        | Ok graph -> graph, docId
        | Error msg -> failwith msg

let private normalNode (id: NodeId) (text: string) (owner: NodeId) : Node =
    Node.Create(id, text = text, owner = owner)

let private childTexts childMap (nodes: Map<NodeId, Node>) parentId =
    kids childMap parentId
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
    let text =
        "# one"
        + Environment.NewLine
        + "plain"
        + Environment.NewLine
        + "## two"
        + Environment.NewLine
    let result = MdDocument.read text docId graph |> requireOk "read"
    let h1 = (kids result.childMap docId).Head.id
    Assert.Equal("one", result.nodes.[h1].text)
    Assert.True(hasClass result.nodes h1 "md-head")
    let plainId = (kids result.childMap h1).Head.id
    Assert.Equal("plain", result.nodes.[plainId].text)
    let h2 = (kids result.childMap h1).[1].id
    Assert.Equal("two", result.nodes.[h2].text)
    Assert.True(hasClass result.nodes h2 "md-head")

[<Fact>]
let ``read tag line stays plain not heading`` () =
    let graph, docId = graphWithDocument []
    let text = "#tag" + Environment.NewLine + "# real" + Environment.NewLine
    let result = MdDocument.read text docId graph |> requireOk "read"
    let tagId = (kids result.childMap docId).Head.id
    Assert.Equal("#tag", result.nodes.[tagId].text)
    Assert.False(hasClass result.nodes tagId "md-head")
    let headId = (kids result.childMap docId).[1].id
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
    let headId = (kids result.childMap docId).Head.id
    let listId = (kids result.childMap headId).Head.id
    Assert.Equal("item", result.nodes.[listId].text)
    Assert.True(hasClass result.nodes listId "md-list")
    let nestedId = (kids result.childMap listId).Head.id
    Assert.Equal("nested", result.nodes.[nestedId].text)
    Assert.True(hasClass result.nodes nestedId "md-list")

[<Fact>]
let ``read plain line under heading is sibling depth`` () =
    let graph, docId = graphWithDocument []
    let text =
        "# head"
        + Environment.NewLine
        + "body one"
        + Environment.NewLine
        + "body two"
        + Environment.NewLine
    let result = MdDocument.read text docId graph |> requireOk "read"
    let headId = (kids result.childMap docId).Head.id
    Assert.Equal<string list>(
        [ "body one"; "body two" ],
        childTexts result.childMap result.nodes headId)

[<Fact>]
let ``read blank lines do not create nodes`` () =
    let graph, docId = graphWithDocument []
    let text =
        "alpha"
        + Environment.NewLine
        + Environment.NewLine
        + "beta"
        + Environment.NewLine
    let result = MdDocument.read text docId graph |> requireOk "read"
    Assert.Equal(2, (kids result.childMap docId).Length)
    Assert.Equal<string list>(
        [ "alpha"; "beta" ],
        childTexts result.childMap result.nodes docId)

[<Fact>]
let ``parse span tree absorbs blank into preceding node`` () =
    let text = "a\n\nb\n"
    let tree =
        (MdReconcile.handler OutlineLcs.diffTexts).parse
            text
            (Graph.create ())
            Graph.rootId
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
        (MdReconcile.handler OutlineLcs.diffTexts).parse
            text
            (Graph.create ())
            Graph.rootId
        |> requireOk "parse"

    Assert.Equal<string list>(
        [ "a"; "b" ],
        tree.children |> List.map (fun child -> child.text))
    Assert.Equal(text.IndexOf("b"), tree.children.[0].span.end_)
    Assert.Equal(text.Length, tree.children.[1].span.end_)

[<Fact>]
let ``parse empty markdown produces empty bounded tree`` () =
    let tree =
        (MdReconcile.handler OutlineLcs.diffTexts).parse
            ""
            (Graph.create ())
            Graph.rootId
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
            cssClasses = CssClass.ofList [ "md-head" ] }
    let listItem =
        { normalNode listId "item" headId with
            cssClasses = CssClass.ofList [ "md-list" ] }
    let graph0, docId = graphWithDocument [ head ]
    let graph' =
        Graph.addDetachedNode listItem graph0
        |> setChildren headId (owned [ listId ])
    let text =
        MdDocument.write graph' docId emptyComplement None
        |> requireOk "write"
    Assert.Equal(
        "# section"
        + Environment.NewLine
        + Environment.NewLine
        + "- item"
        + Environment.NewLine,
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
    let previous =
        "alpha"
        + Environment.NewLine
        + Environment.NewLine
        + "beta"
        + Environment.NewLine
    let readResult = MdDocument.read previous docId graph |> requireOk "read"
    let emptyId = NodeId.New()
    let alphaId = (kids readResult.childMap docId).Head.id
    let betaId = (kids readResult.childMap docId).[1].id
    let empty = normalNode emptyId "" docId
    let graph' =
        withRead graph readResult.nodes readResult.childMap
        |> Graph.addDetachedNode empty
        |> setChildren docId (owned [ alphaId; emptyId; betaId ])
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
    let alphaId = (kids readResult.childMap docId).Head.id
    let betaId = (kids readResult.childMap docId).[1].id
    let graph =
        withRead graph0 readResult.nodes readResult.childMap
        |> fun g ->
            fromExisting g (Map.remove alphaId g.nodes)
            |> setChildren docId (owned [ betaId ])
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
    let input =
        "# head"
        + Environment.NewLine
        + Environment.NewLine
        + "body"
        + Environment.NewLine
    let readResult = MdDocument.read input docId graph |> requireOk "read"
    let output =
        MdDocument.writeWarm
            OutlineLcs.diffTexts
            (withRead graph readResult.nodes readResult.childMap)
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
    let graph' = withRead graph readResult.nodes readResult.childMap
    let titleId = (kids readResult.childMap docId).Head.id
    let bodyId = (kids readResult.childMap titleId).Head.id
    let result =
        MdReconcile.reconcile OutlineLcs.diffTexts previous graph' docId previous
        |> requireOk "reconcile"
    Assert.Equal(titleId, (kids result.childMap docId).Head.id)
    Assert.Equal(bodyId, (kids result.childMap titleId).Head.id)

[<Fact>]
let ``reconcile line text edit keeps ids`` () =
    let graph, docId = graphWithDocument []
    let previous = "# title" + Environment.NewLine + "body" + Environment.NewLine
    let readResult = MdDocument.read previous docId graph |> requireOk "read"
    let graph' = withRead graph readResult.nodes readResult.childMap
    let titleId = (kids readResult.childMap docId).Head.id
    let bodyId = (kids readResult.childMap titleId).Head.id
    let edited = "# TITLE" + Environment.NewLine + "body" + Environment.NewLine
    let result =
        MdReconcile.reconcile OutlineLcs.diffTexts previous graph' docId edited
        |> requireOk "reconcile"
    Assert.Equal("TITLE", result.nodes.[titleId].text)
    Assert.Equal(titleId, (kids result.childMap docId).Head.id)
    Assert.Equal(bodyId, (kids result.childMap titleId).Head.id)

[<Fact>]
let ``reconcile sibling reorder updates child order`` () =
    let graph, docId = graphWithDocument []
    let nl = Environment.NewLine
    let orderA = "# Title" + nl + "alpha" + nl + "beta" + nl
    let orderB = "# Title" + nl + "beta" + nl + "alpha" + nl
    let readResult = MdDocument.read orderA docId graph |> requireOk "read"
    let graph' = withRead graph readResult.nodes readResult.childMap
    let titleId = (kids readResult.childMap docId).Head.id
    let alphaId = (kids readResult.childMap titleId).[0].id
    let betaId = (kids readResult.childMap titleId).[1].id
    let previous =
        MdDocument.write graph' docId readResult.complement None
        |> requireOk "write"
    let result =
        MdReconcile.reconcile OutlineLcs.diffTexts previous graph' docId orderB
        |> requireOk "reconcile"
    Assert.Equal<string list>(
        [ "beta"; "alpha" ],
        childTexts result.childMap result.nodes titleId)
    Assert.Equal(betaId, (kids result.childMap titleId).[0].id)
    Assert.Equal(alphaId, (kids result.childMap titleId).[1].id)

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
    let graph' = withRead graph readResult.nodes readResult.childMap
    let h1 = (kids readResult.childMap docId).Head.id
    Assert.Equal<string list>(
        [ "First"; "Second" ],
        childTexts readResult.childMap readResult.nodes h1)
    let previous =
        MdDocument.write graph' docId readResult.complement None
        |> requireOk "write"
    let result =
        MdReconcile.reconcile OutlineLcs.diffTexts previous graph' docId orderB
        |> requireOk "reconcile"
    Assert.Equal<string list>(
        [ "Second"; "First" ],
        childTexts result.childMap result.nodes h1)

[<Fact>]
let ``unchanged bytes still rebuild outline from file not graph copy`` () =
    let graph, docId = graphWithDocument []
    let nl = Environment.NewLine
    let text = "# Title" + nl + "alpha" + nl + "beta" + nl
    let readResult = MdDocument.read text docId graph |> requireOk "read"
    let graph' = withRead graph readResult.nodes readResult.childMap
    let titleId = (kids readResult.childMap docId).Head.id
    let previous =
        MdDocument.write graph' docId readResult.complement None
        |> requireOk "write"
    let result =
        MdReconcile.reconcile OutlineLcs.diffTexts previous graph' docId previous
        |> requireOk "reconcile"
    Assert.Equal(titleId, (kids result.childMap docId).Head.id)
    Assert.Equal<string list>(
        [ "alpha"; "beta" ],
        childTexts result.childMap result.nodes titleId)

let private nl = Environment.NewLine

let private readDoc text =
    let graph, docId = graphWithDocument []
    let result = MdDocument.read text docId graph |> requireOk "read"
    graph, docId, result

let private coldText (graph: Graph) docId (result: MdReadResult) =
    MdDocument.write
        (withRead graph result.nodes result.childMap)
        docId
        result.complement
        None
    |> requireOk "write"

let private structuralNames = [
    "md-head"
    "md-list"
    "md-list-star"
    "md-number"
    "md-table"
]

let private structuralOf (nodes: Map<NodeId, Node>) id =
    nodes.[id].cssClasses
    |> CssClass.toList
    |> List.filter (fun name -> List.contains name structuralNames)

let rec private shape (result: MdReadResult) (parentId: NodeId) : string list =
    kids result.childMap parentId
    |> List.map (fun child ->
        let classes = structuralOf result.nodes child.id |> String.concat ","
        let nested = shape result child.id |> String.concat "/"
        result.nodes.[child.id].text + "[" + classes + "](" + nested + ")")

[<Fact>]
let ``digits period space become md-number nodes`` () =
    let _, docId, result = readDoc ("1. item" + nl + "12. item" + nl)
    let ids = kids result.childMap docId |> List.map (fun c -> c.id)
    Assert.Equal<string list>([ "item"; "item" ], childTexts result.childMap result.nodes docId)
    Assert.Equal<string list>([ "md-number" ], structuralOf result.nodes ids.[0])
    Assert.Equal<string list>([ "md-number" ], structuralOf result.nodes ids.[1])

[<Fact>]
let ``close paren and missing space stay plain`` () =
    let _, docId, result = readDoc ("1)" + nl + "1.item" + nl)
    Assert.Equal<string list>(
        [ "1)"; "1.item" ],
        childTexts result.childMap result.nodes docId)
    for child in kids result.childMap docId do
        Assert.Empty(structuralOf result.nodes child.id)

[<Fact>]
let ``numbered lists share dash depth and indent`` () =
    let text =
        "# H" + nl
        + "- dash" + nl
        + "  - nested dash" + nl
        + "1. num" + nl
        + "  1. nested num" + nl
    let _, docId, result = readDoc text
    let head = (kids result.childMap docId).Head.id
    let top = kids result.childMap head |> List.map (fun c -> c.id)
    Assert.Equal<string list>([ "dash"; "num" ], childTexts result.childMap result.nodes head)
    Assert.Equal("nested dash", result.nodes.[(kids result.childMap top.[0]).Head.id].text)
    Assert.Equal("nested num", result.nodes.[(kids result.childMap top.[1]).Head.id].text)

[<Fact>]
let ``star marker is md-list-star and dash stays md-list`` () =
    let _, docId, result = readDoc ("* item" + nl + "- item" + nl)
    let ids = kids result.childMap docId |> List.map (fun c -> c.id)
    Assert.Equal("item", result.nodes.[ids.[0]].text)
    Assert.Equal<string list>([ "md-list-star" ], structuralOf result.nodes ids.[0])
    Assert.Equal("item", result.nodes.[ids.[1]].text)
    Assert.Equal<string list>([ "md-list" ], structuralOf result.nodes ids.[1])

[<Fact>]
let ``list markers require a following space`` () =
    let _, docId, result =
        readDoc ("*item" + nl + "-item" + nl + "***" + nl + "---" + nl)
    Assert.Equal<string list>(
        [ "*item"; "-item"; "***"; "---" ],
        childTexts result.childMap result.nodes docId)
    for child in kids result.childMap docId do
        Assert.Empty(structuralOf result.nodes child.id)

[<Fact>]
let ``plain line splits on sentence marks`` () =
    let _, docId, result = readDoc ("One. Two. Three" + nl)
    let parent = (kids result.childMap docId).Head.id
    Assert.Equal("One.", result.nodes.[parent].text)
    Assert.Equal<string list>(
        [ "Two."; "Three" ],
        childTexts result.childMap result.nodes parent)
    for child in kids result.childMap parent do
        Assert.Empty(structuralOf result.nodes child.id)

[<Fact>]
let ``list and number bodies keep the marker on the first sentence`` () =
    let _, docId, result = readDoc ("- One. Two." + nl + "1. One. Two." + nl)
    let ids = kids result.childMap docId |> List.map (fun c -> c.id)
    Assert.Equal("One.", result.nodes.[ids.[0]].text)
    Assert.Equal<string list>([ "md-list" ], structuralOf result.nodes ids.[0])
    Assert.Equal<string list>(
        [ "Two." ],
        childTexts result.childMap result.nodes ids.[0])
    Assert.Empty(structuralOf result.nodes (kids result.childMap ids.[0]).Head.id)
    Assert.Equal("One.", result.nodes.[ids.[1]].text)
    Assert.Equal<string list>([ "md-number" ], structuralOf result.nodes ids.[1])
    Assert.Equal<string list>(
        [ "Two." ],
        childTexts result.childMap result.nodes ids.[1])

[<Fact>]
let ``heading body splits and keeps md-head on the first sentence`` () =
    let _, docId, result = readDoc ("# One. Two." + nl)
    let head = (kids result.childMap docId).Head.id
    Assert.Equal("One.", result.nodes.[head].text)
    Assert.Equal<string list>([ "md-head" ], structuralOf result.nodes head)
    Assert.Equal<string list>([ "Two." ], childTexts result.childMap result.nodes head)
    Assert.Empty(structuralOf result.nodes (kids result.childMap head).Head.id)

[<Fact>]
let ``heading list number does not end the sentence`` () =
    let _, docId, result = readDoc ("## 1. Story paths" + nl)
    let head = (kids result.childMap docId).Head.id
    Assert.Equal("1. Story paths", result.nodes.[head].text)
    Assert.Equal<string list>([ "md-head" ], structuralOf result.nodes head)
    Assert.Empty(kids result.childMap head)
    let _, docId2, deeper = readDoc ("### 2. Foo" + nl)
    let h2 = (kids deeper.childMap docId2).Head.id
    Assert.Equal("2. Foo", deeper.nodes.[h2].text)
    Assert.Equal<string list>([ "md-head" ], structuralOf deeper.nodes h2)
    Assert.Empty(kids deeper.childMap h2)
    let _, docId3, continued = readDoc ("## 1. Story paths. More." + nl)
    let h3 = (kids continued.childMap docId3).Head.id
    Assert.Equal("1. Story paths.", continued.nodes.[h3].text)
    Assert.Equal<string list>(
        [ "md-head" ],
        structuralOf continued.nodes h3)
    Assert.Equal<string list>(
        [ "More." ],
        childTexts continued.childMap continued.nodes h3)

[<Fact>]
let ``atx heading whose title starts with a number stays md-head`` () =
    let text =
        "# CoLab Senior 3D — recruiter prep (Kristen, 30 min)" + nl
        + nl
        + "For Alan. Plain notes..." + nl
        + nl
        + "## 1. What the role is + your Oct 3 yes/no" + nl
        + nl
        + "**Role in one line:** Backend for **3D AutoReview** — ..." + nl
    let graph, docId, result = readDoc text
    let h1 = (kids result.childMap docId).Head.id
    Assert.Equal<string list>([ "md-head" ], structuralOf result.nodes h1)
    let h2 =
        kids result.childMap h1
        |> List.find (fun child ->
            result.nodes.[child.id].text.StartsWith "1. What")
    Assert.Equal(
        "1. What the role is + your Oct 3 yes/no",
        result.nodes.[h2.id].text)
    Assert.Equal<string list>([ "md-head" ], structuralOf result.nodes h2.id)
    Assert.Equal<string list>(
        [ "**Role in one line:** Backend for **3D AutoReview** — ..." ],
        childTexts result.childMap result.nodes h2.id)
    let roleId = (kids result.childMap h2.id).Head.id
    Assert.Empty(structuralOf result.nodes roleId)
    let h1Texts = childTexts result.childMap result.nodes h1
    Assert.DoesNotContain(result.nodes.[roleId].text, h1Texts)
    let written = coldText graph docId result
    Assert.Contains("## 1. What the role is + your Oct 3 yes/no" + nl, written)
    let _, docAgain, again = readDoc written
    Assert.Equal<string list>(shape result docId, shape again docAgain)

    let _, docId2, hash1 = readDoc ("# 1. Opening item" + nl + "body" + nl)
    let head = (kids hash1.childMap docId2).Head.id
    Assert.Equal("1. Opening item", hash1.nodes.[head].text)
    Assert.Equal<string list>([ "md-head" ], structuralOf hash1.nodes head)
    Assert.Equal<string list>(
        [ "body" ],
        childTexts hash1.childMap hash1.nodes head)

[<Fact>]
let ``pipe line does not sentence-split`` () =
    let _, docId, result = readDoc ("| a | Hello. World. |" + nl)
    let carrier = (kids result.childMap docId).Head.id
    let header = (kids result.childMap carrier).Head.id
    Assert.Equal("| a | Hello. World. |", result.nodes.[header].text)
    Assert.Equal<string list>([ "md-table" ], structuralOf result.nodes header)
    Assert.Empty(kids result.childMap header)

[<Fact>]
let ``period inside AGENTS.md does not split the line`` () =
    let _, docId, result = readDoc ("Read this after AGENTS.md." + nl)
    Assert.Equal<string list>(
        [ "Read this after AGENTS.md." ],
        childTexts result.childMap result.nodes docId)
    Assert.Empty(kids result.childMap (kids result.childMap docId).Head.id)

[<Fact>]
let ``write joins childless plain tails onto one file line`` () =
    let parentId = NodeId.New()
    let childId = NodeId.New()
    let parent = normalNode parentId "One." Graph.rootId
    let child = normalNode childId "Two." parentId
    let graph, docId = graphWithDocument [ parent ]
    let graph' =
        Graph.addDetachedNode child graph
        |> setChildren parentId (owned [ childId ])
    let text =
        MdDocument.write graph' docId emptyComplement None |> requireOk "write"
    Assert.Equal("One. Two." + nl, text)

[<Fact>]
let ``three-line table hangs under an empty carrier`` () =
    let text =
        "| first | second | third |" + nl
        + "|-----|------|-----|" + nl
        + "|first | second | third |" + nl
    let _, docId, result = readDoc text
    let carrier = (kids result.childMap docId).Head.id
    Assert.Equal("", result.nodes.[carrier].text)
    Assert.Empty(structuralOf result.nodes carrier)
    let header = (kids result.childMap carrier).Head.id
    Assert.Equal("| first | second | third |", result.nodes.[header].text)
    Assert.Equal<string list>([ "md-table" ], structuralOf result.nodes header)
    Assert.Equal<string list>(
        [ "|-----|------|-----|"; "|first | second | third |" ],
        childTexts result.childMap result.nodes header)
    for child in kids result.childMap header do
        Assert.Equal<string list>([ "md-table" ], structuralOf result.nodes child.id)

[<Fact>]
let ``heading with no paragraph inserts a table carrier`` () =
    let _, docId, result = readDoc ("# H" + nl + "| a |" + nl + "|---|" + nl)
    let head = (kids result.childMap docId).Head.id
    let carrier = (kids result.childMap head).Head.id
    Assert.Equal("", result.nodes.[carrier].text)
    Assert.Empty(structuralOf result.nodes carrier)
    let header = (kids result.childMap carrier).Head.id
    Assert.Equal("| a |", result.nodes.[header].text)
    Assert.Equal("|---|", result.nodes.[(kids result.childMap header).Head.id].text)

[<Fact>]
let ``paragraph is the table parent and no carrier is invented`` () =
    let _, docId, result = readDoc ("# H" + nl + "Para." + nl + "| a |" + nl)
    let head = (kids result.childMap docId).Head.id
    Assert.Equal<string list>([ "Para." ], childTexts result.childMap result.nodes head)
    let para = (kids result.childMap head).Head.id
    Assert.Equal<string list>([ "| a |" ], childTexts result.childMap result.nodes para)
    Assert.Equal<string list>(
        [ "md-table" ],
        structuralOf result.nodes (kids result.childMap para).Head.id)

[<Fact>]
let ``sentence tail and table header are siblings`` () =
    let _, docId, result = readDoc ("One. Two." + nl + "| a |" + nl)
    let parent = (kids result.childMap docId).Head.id
    Assert.Equal("One.", result.nodes.[parent].text)
    Assert.Equal<string list>(
        [ "Two."; "| a |" ],
        childTexts result.childMap result.nodes parent)
    Assert.Empty(structuralOf result.nodes (kids result.childMap parent).[0].id)
    Assert.Equal<string list>(
        [ "md-table" ],
        structuralOf result.nodes (kids result.childMap parent).[1].id)

[<Fact>]
let ``blank line splits pipe groups into two tables`` () =
    let _, docId, result = readDoc ("| a |" + nl + nl + "| b |" + nl)
    let carrier = (kids result.childMap docId).Head.id
    Assert.Equal("", result.nodes.[carrier].text)
    Assert.Equal<string list>(
        [ "| a |"; "| b |" ],
        childTexts result.childMap result.nodes carrier)

[<Fact>]
let ``table text drops trailing spaces`` () =
    let _, docId, result = readDoc ("| a |   " + nl)
    let header = (kids result.childMap (kids result.childMap docId).Head.id).Head.id
    Assert.Equal("| a |", result.nodes.[header].text)

[<Fact>]
let ``marker round-trip renumbers sibling md-number nodes`` () =
    let source = "- a" + nl + "* b" + nl + "9. c" + nl + "8. d" + nl
    let graph, docId, result = readDoc source
    let written = coldText graph docId result
    Assert.Equal("- a" + nl + "* b" + nl + "1. c" + nl + "2. d" + nl, written)
    let _, docId2, again = readDoc written
    Assert.Equal<string list>(shape result docId, shape again docId2)

[<Fact>]
let ``table round-trip omits the carrier line`` () =
    let source =
        "| first | second | third |" + nl
        + "|-----|------|-----|" + nl
        + "|first | second | third |" + nl
    let graph, docId, result = readDoc source
    let written = coldText graph docId result
    Assert.Equal(source, written)
    let _, docId2, again = readDoc written
    Assert.Equal<string list>(shape result docId, shape again docId2)

[<Fact>]
let ``two tables gain one blank line on cold write`` () =
    let source = "| a |" + nl + nl + "| b |" + nl
    let graph, docId, result = readDoc source
    Assert.Equal(source, coldText graph docId result)
    let _, docId2, again = readDoc (coldText graph docId result)
    Assert.Equal<string list>(shape result docId, shape again docId2)

[<Fact>]
let ``sentence round-trip writes one file line`` () =
    let source = "One. Two." + nl
    let graph, docId, result = readDoc source
    Assert.Equal(source, coldText graph docId result)
    let _, docId2, again = readDoc source
    Assert.Equal<string list>(shape result docId, shape again docId2)

[<Fact>]
let ``sentence tail that owns a table writes on its own line`` () =
    let source = "# One. Two." + nl + "| a |" + nl
    let writtenExpect = "# One." + nl + "Two." + nl + "| a |" + nl
    let graph, docId, result = readDoc source
    Assert.Equal(writtenExpect, coldText graph docId result)
    let _, docId2, again = readDoc writtenExpect
    Assert.Equal<string list>(shape result docId, shape again docId2)

[<Fact>]
let ``warm write keeps a joined sentence line`` () =
    let graph, docId = graphWithDocument []
    let previous = "One. Two." + nl
    let result = MdDocument.read previous docId graph |> requireOk "read"
    let text =
        MdDocument.writeWarm
            OutlineLcs.diffTexts
            (withRead graph result.nodes result.childMap)
            docId
            result.complement
            previous
        |> requireOk "write"
    Assert.Equal(previous, text)

[<Fact>]
let ``ordinary text stays plain lines`` () =
    let samples = [
        "Heading" + nl + "=======" + nl, [ "Heading"; "=======" ]
        "####### deep" + nl, [ "####### deep" ]
        "[^1]: note" + nl, [ "[^1]: note" ]
        "<div>hi</div>" + nl, [ "<div>hi</div>" ]
        "---" + nl + "title: note" + nl + "---" + nl, [ "---"; "title: note"; "---" ]
    ]
    for text, expected in samples do
        let _, docId, result = readDoc text
        Assert.Equal<string list>(expected, childTexts result.childMap result.nodes docId)
        for child in kids result.childMap docId do
            Assert.False(hasClass result.nodes child.id "md-head")

[<Fact>]
let ``parse span of two sentences yields two span nodes`` () =
    let text = "One. Two."
    let tree =
        (MdReconcile.handler OutlineLcs.diffTexts).parse
            text
            (Graph.create ())
            Graph.rootId
        |> requireOk "parse"
    let rec count (node: SpanNode) =
        1 + List.sumBy count node.children
    Assert.Equal(2, count tree - 1)
    Assert.Equal("One.", tree.children.Head.text)
    Assert.Equal("Two.", tree.children.Head.children.Head.text)
