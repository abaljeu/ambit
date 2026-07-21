module Gambol.Shared.Tests.PlainTextDocumentTests

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

let private emptyComplement : PlainTextComplement = {
    indentStyle = PlainTextIndentStyle.Tabs
    cssClassesByNodeId = Map.empty
}

[<Fact>]
let ``read lf text imports sibling nodes`` () =
    let graph, docId = graphWithDocument []
    let text = "alpha" + Environment.NewLine + "beta" + Environment.NewLine
    let result = PlainTextDocument.read text docId graph |> requireOk "read"
    Assert.Equal<string list>([ "alpha"; "beta" ], childTexts result.nodes docId)

[<Fact>]
let ``read crlf text imports sibling nodes`` () =
    let graph, docId = graphWithDocument []
    let text = "alpha\r\nbeta\r\n"
    let result = PlainTextDocument.read text docId graph |> requireOk "read"
    Assert.Equal<string list>([ "alpha"; "beta" ], childTexts result.nodes docId)

[<Fact>]
let ``read tab indent creates child node`` () =
    let graph, docId = graphWithDocument []
    let text = "parent" + Environment.NewLine + "\tchild" + Environment.NewLine
    let result = PlainTextDocument.read text docId graph |> requireOk "read"
    let parentId = result.nodes.[docId].children.Head.id
    Assert.Equal("parent", result.nodes.[parentId].text)
    Assert.Equal<string list>([ "child" ], childTexts result.nodes parentId)
    Assert.Equal(PlainTextIndentStyle.Tabs, result.complement.indentStyle)

[<Fact>]
let ``read space indent infers spaces per level`` () =
    let graph, docId = graphWithDocument []
    let text = "root" + Environment.NewLine + "  child" + Environment.NewLine + "    grand" + Environment.NewLine
    let result = PlainTextDocument.read text docId graph |> requireOk "read"
    let rootId = result.nodes.[docId].children.Head.id
    let middleId = result.nodes.[rootId].children.Head.id
    Assert.Equal("grand", result.nodes.[middleId].children.Head |> fun c -> result.nodes.[c.id].text)
    match result.complement.indentStyle with
    | PlainTextIndentStyle.Spaces 2 -> ()
    | other -> failwith $"expected Spaces 2, got {other}"

[<Fact>]
let ``read blank lines create empty nodes`` () =
    let graph, docId = graphWithDocument []
    let text = "only" + Environment.NewLine + Environment.NewLine + "also" + Environment.NewLine
    let result = PlainTextDocument.read text docId graph |> requireOk "read"
    Assert.Equal(3, result.nodes.[docId].children.Length)
    Assert.Equal<string list>([ "only"; ""; "also" ], childTexts result.nodes docId)

[<Fact>]
let ``read uses line body literally as node text`` () =
    let graph, docId = graphWithDocument []
    let text = "hello #anchor" + Environment.NewLine
    let result = PlainTextDocument.read text docId graph |> requireOk "read"
    let nodeId = result.nodes.[docId].children.Head.id
    Assert.Equal("hello #anchor", result.nodes.[nodeId].text)
    Assert.Equal(Filename.Empty, result.nodes.[nodeId].name)

[<Fact>]
let ``read treats ref-like line as ordinary content`` () =
    let graph, docId = graphWithDocument []
    let text = "holder" + Environment.NewLine + "\t-> #peer" + Environment.NewLine
    let result = PlainTextDocument.read text docId graph |> requireOk "read"
    let holderId = result.nodes.[docId].children.Head.id
    Assert.Equal("holder", result.nodes.[holderId].text)
    Assert.Equal<string list>([ "-> #peer" ], childTexts result.nodes holderId)

[<Fact>]
let ``write emits visible text only`` () =
    let nodeId = NodeId.New()
    let node = normalNode nodeId "hello" Graph.rootId
    let graph, docId = graphWithDocument [ node ]
    let text =
        PlainTextDocument.write graph docId emptyComplement None
        |> requireOk "write"
    Assert.Equal("hello" + Environment.NewLine, text)

[<Fact>]
let ``write ref occurrence exports target visible text`` () =
    let sharedId = NodeId.New()
    let parentId = NodeId.New()
    let parent =
        { normalNode parentId "holder" Graph.rootId with
            children = [ { ref = Ownership.Ref; id = sharedId } ] }
    let shared = normalNode sharedId "shared text" Graph.rootId
    let graph0, docId = graphWithDocument [ parent ]
    let graph =
        graph0.nodes |> Map.add sharedId shared |> fun nodes -> { graph0 with nodes = nodes }
    let text =
        PlainTextDocument.write graph docId emptyComplement None
        |> requireOk "write"
    Assert.Equal("holder" + Environment.NewLine + "\tshared text" + Environment.NewLine, text)

[<Fact>]
let ``write preserves tab indent style from complement`` () =
    let childId = NodeId.New()
    let parentId = NodeId.New()
    let parent = normalNode parentId "parent" Graph.rootId
    let child = normalNode childId "child" parentId
    let graph, docId =
        graphWithDocument [ { parent with children = owned [ childId ] }; child ]
    let complement = { emptyComplement with indentStyle = PlainTextIndentStyle.Tabs }
    let text =
        PlainTextDocument.write graph docId complement None
        |> requireOk "write"
    Assert.StartsWith("parent" + Environment.NewLine + "\tchild", text)

[<Fact>]
let ``write preserves space indent style from complement`` () =
    let childId = NodeId.New()
    let parentId = NodeId.New()
    let parent = normalNode parentId "parent" Graph.rootId
    let child = normalNode childId "child" parentId
    let graph, docId =
        graphWithDocument [ { parent with children = owned [ childId ] }; child ]
    let complement = { emptyComplement with indentStyle = PlainTextIndentStyle.Spaces 2 }
    let text =
        PlainTextDocument.write graph docId complement None
        |> requireOk "write"
    Assert.StartsWith("parent" + Environment.NewLine + "  child", text)

[<Fact>]
let ``unchanged import writes byte identically`` () =
    let graph, docId = graphWithDocument []
    let input = "alpha\r\n" + Environment.NewLine + "\tchild\r\n"
    let readResult = PlainTextDocument.read input docId graph |> requireOk "read"
    let output =
        PlainTextDocument.write
            { graph with nodes = readResult.nodes }
            docId
            readResult.complement
            (Some input)
        |> requireOk "write"
    Assert.Equal(input, output)

[<Fact>]
let ``unchanged export reconciles with same node ids`` () =
    let aId = NodeId.New()
    let bId = NodeId.New()
    let a = normalNode aId "alpha" Graph.rootId
    let b = normalNode bId "beta" Graph.rootId
    let graph, docId = graphWithDocument [ a; b ]
    let written =
        PlainTextDocument.write graph docId emptyComplement None
        |> requireOk "write"
    let result =
        PlainTextReconcile.reconcile OutlineLcs.diffTexts written graph docId written
        |> requireOk "reconcile"
    Assert.Equal(aId, result.nodes.[docId].children.Head.id)
    Assert.Equal(bId, result.nodes.[docId].children.[1].id)

[<Fact>]
let ``round trip preserves blank lines in previous text`` () =
    let graph, docId = graphWithDocument []
    let input = "line" + Environment.NewLine + Environment.NewLine + "next" + Environment.NewLine
    let readResult = PlainTextDocument.read input docId graph |> requireOk "read"
    let output =
        PlainTextDocument.write
            { graph with nodes = readResult.nodes }
            docId
            readResult.complement
            (Some input)
        |> requireOk "write"
    Assert.Equal(input, output)

[<Fact>]
let ``reconcile line text edit updates node and preserves other ids`` () =
    let aId = NodeId.New()
    let bId = NodeId.New()
    let a = normalNode aId "alpha" Graph.rootId
    let b = normalNode bId "beta" Graph.rootId
    let graph, docId = graphWithDocument [ a; b ]
    let previous =
        PlainTextDocument.write graph docId emptyComplement None
        |> requireOk "write previous"
    let edited = previous.Replace("alpha", "ALPHA")
    let result =
        PlainTextReconcile.reconcile OutlineLcs.diffTexts previous graph docId edited
        |> requireOk "reconcile"
    Assert.Equal("ALPHA", result.nodes.[aId].text)
    Assert.Equal("beta", result.nodes.[bId].text)
    Assert.Equal(bId, result.nodes.[docId].children.[1].id)

[<Fact>]
let ``reconcile line add mints new node id`` () =
    let aId = NodeId.New()
    let a = normalNode aId "alpha" Graph.rootId
    let graph, docId = graphWithDocument [ a ]
    let previous =
        PlainTextDocument.write graph docId emptyComplement None
        |> requireOk "write previous"
    let edited = previous + ("gamma" + Environment.NewLine)
    let result =
        PlainTextReconcile.reconcile OutlineLcs.diffTexts previous graph docId edited
        |> requireOk "reconcile"
    Assert.Equal(2, result.nodes.[docId].children.Length)
    let newId =
        result.nodes.[docId].children
        |> List.map (fun c -> c.id)
        |> List.find (fun id -> id <> aId)
    Assert.Equal("gamma", result.nodes.[newId].text)

[<Fact>]
let ``reconcile line delete removes node from document`` () =
    let aId = NodeId.New()
    let bId = NodeId.New()
    let a = normalNode aId "alpha" Graph.rootId
    let b = normalNode bId "beta" Graph.rootId
    let graph, docId = graphWithDocument [ a; b ]
    let previous =
        PlainTextDocument.write graph docId emptyComplement None
        |> requireOk "write previous"
    let edited = previous.Replace("beta" + Environment.NewLine, "")
    let result =
        PlainTextReconcile.reconcile OutlineLcs.diffTexts previous graph docId edited
        |> requireOk "reconcile"
    Assert.Equal(1, result.nodes.[docId].children.Length)
    Assert.Equal(aId, result.nodes.[docId].children.Head.id)

[<Fact>]
let ``reconcile external swap of unique lines keeps ids`` () =
    let aId = NodeId.New()
    let bId = NodeId.New()
    let a = normalNode aId "alpha" Graph.rootId
    let b = normalNode bId "beta" Graph.rootId
    let graph, docId = graphWithDocument [ a; b ]
    let previous = "alpha" + Environment.NewLine + "beta" + Environment.NewLine
    let edited = "beta" + Environment.NewLine + "alpha" + Environment.NewLine
    let result =
        PlainTextReconcile.reconcile OutlineLcs.diffTexts previous graph docId edited
        |> requireOk "reconcile"
    Assert.Equal(bId, result.nodes.[docId].children.[0].id)
    Assert.Equal(aId, result.nodes.[docId].children.[1].id)
    Assert.Equal("beta", result.nodes.[bId].text)
    Assert.Equal("alpha", result.nodes.[aId].text)

[<Fact>]
let ``reconcile mid insert keeps neighbor ids`` () =
    let aId = NodeId.New()
    let bId = NodeId.New()
    let a = normalNode aId "alpha" Graph.rootId
    let b = normalNode bId "beta" Graph.rootId
    let graph, docId = graphWithDocument [ a; b ]
    let previous = "alpha" + Environment.NewLine + "beta" + Environment.NewLine
    let edited =
        "alpha"
        + Environment.NewLine
        + "blat"
        + Environment.NewLine
        + "beta"
        + Environment.NewLine
    let result =
        PlainTextReconcile.reconcile OutlineLcs.diffTexts previous graph docId edited
        |> requireOk "reconcile"
    let children = result.nodes.[docId].children
    Assert.Equal(3, children.Length)
    Assert.Equal(aId, children.[0].id)
    Assert.Equal(bId, children.[2].id)
    Assert.Equal("blat", result.nodes.[children.[1].id].text)
    Assert.True(children.[1].id <> aId && children.[1].id <> bId)

[<Fact>]
let ``reconcile block reindent keeps ids with new depths`` () =
    let aId = NodeId.New()
    let bId = NodeId.New()
    let a = normalNode aId "parent" Graph.rootId
    let b = normalNode bId "child" Graph.rootId
    let graph, docId = graphWithDocument [ a; b ]
    let previous = "parent" + Environment.NewLine + "child" + Environment.NewLine
    let edited = "parent" + Environment.NewLine + "\tchild" + Environment.NewLine
    let result =
        PlainTextReconcile.reconcile OutlineLcs.diffTexts previous graph docId edited
        |> requireOk "reconcile"
    Assert.Equal(aId, result.nodes.[docId].children.Head.id)
    Assert.Equal(bId, result.nodes.[aId].children.Head.id)
    Assert.Equal("child", result.nodes.[bId].text)

[<Fact>]
let ``write after reparent preserves node id at new depth`` () =
    let aId = NodeId.New()
    let bId = NodeId.New()
    let a = normalNode aId "parent" Graph.rootId
    let b = normalNode bId "child" Graph.rootId
    let graph, docId =
        graphWithDocument [ { a with children = owned [ bId ] }; b ]
    let nested =
        "parent" + Environment.NewLine + "\tchild" + Environment.NewLine
    let nestedRead = PlainTextDocument.read nested docId graph |> requireOk "read nested"
    let graph =
        Graph.replace aId 0 (owned [ bId ]) [] graph
        |> requireOk "remove child from parent"
    let graph =
        Graph.replace docId 0 (owned [ aId ]) (owned [ aId; bId ]) graph
        |> requireOk "reparent child to doc root"
    let text =
        PlainTextDocument.write graph docId nestedRead.complement (Some nested)
        |> requireOk "write"
    Assert.Contains("child" + Environment.NewLine, text)
    Assert.DoesNotContain("\tchild", text)
    let result =
        PlainTextReconcile.reconcile OutlineLcs.diffTexts nested graph docId text
        |> requireOk "reconcile"
    Assert.True(
        result.nodes.[docId].children
        |> List.exists (fun c -> c.id = bId && c.ref = Ownership.Owner)
    )

[<Fact>]
let ``reconcile preserves cssClasses from complement`` () =
    let nodeId = NodeId.New()
    let classes = CssClass.ofList [ "tag" ]
    let node =
        { normalNode nodeId "item" Graph.rootId with
            cssClasses = classes }
    let graph, docId = graphWithDocument [ node ]
    let complement = {
        emptyComplement with
            cssClassesByNodeId = Map.ofList [ nodeId, classes ]
    }
    let previous =
        PlainTextDocument.write graph docId complement None
        |> requireOk "write previous"
    let result =
        PlainTextReconcile.reconcile OutlineLcs.diffTexts previous graph docId previous
        |> requireOk "reconcile"
    Assert.Equal(classes, result.nodes.[nodeId].cssClasses)

[<Fact>]
let ``cold read of exported ref line creates ordinary content`` () =
    let sharedId = NodeId.New()
    let parentId = NodeId.New()
    let parent =
        { normalNode parentId "holder" Graph.rootId with
            children = [ { ref = Ownership.Ref; id = sharedId } ] }
    let shared = normalNode sharedId "shared text" Graph.rootId
    let graph0, docId = graphWithDocument [ parent ]
    let graph =
        graph0.nodes |> Map.add sharedId shared |> fun nodes -> { graph0 with nodes = nodes }
    let exported =
        PlainTextDocument.write graph docId emptyComplement None
        |> requireOk "write"
    let coldGraph, coldDocId = graphWithDocument []
    let result = PlainTextDocument.read exported coldDocId coldGraph |> requireOk "read"
    let holderId = result.nodes.[coldDocId].children.Head.id
    Assert.Equal("holder", result.nodes.[holderId].text)
    let child = result.nodes.[holderId].children.Head
    Assert.Equal("shared text", result.nodes.[child.id].text)
    Assert.Equal(Ownership.Owner, child.ref)

[<Fact>]
let ``reconcile preserves ref edge from graph context`` () =
    let sharedId = NodeId.New()
    let parentId = NodeId.New()
    let parent =
        { normalNode parentId "holder" Graph.rootId with
            children = [ { ref = Ownership.Ref; id = sharedId } ] }
    let shared = normalNode sharedId "shared text" Graph.rootId
    let graph0, docId = graphWithDocument [ parent ]
    let graph =
        graph0.nodes |> Map.add sharedId shared |> fun nodes -> { graph0 with nodes = nodes }
    let previous =
        PlainTextDocument.write graph docId emptyComplement None
        |> requireOk "write"
    let result =
        PlainTextReconcile.reconcile OutlineLcs.diffTexts previous graph docId previous
        |> requireOk "reconcile"
    let holderId = result.nodes.[docId].children.Head.id
    Assert.Equal(1, result.nodes.[holderId].children.Length)
    Assert.Equal(sharedId, result.nodes.[holderId].children.Head.id)
    Assert.Equal(Ownership.Ref, result.nodes.[holderId].children.Head.ref)

[<Fact>]
let ``write incremental preserves blank run when two content lines edit`` () =
    let graph0, docId = graphWithDocument []
    let previous =
        "alpha"
        + Environment.NewLine
        + Environment.NewLine
        + Environment.NewLine
        + "beta"
        + Environment.NewLine
        + "gamma"
        + Environment.NewLine
    let readResult = PlainTextDocument.read previous docId graph0 |> requireOk "read"
    let graph = { graph0 with nodes = readResult.nodes }
    let children = graph.nodes.[docId].children
    let bId = children.[3].id
    let cId = children.[4].id
    let graph =
        graph.nodes
        |> Map.add bId { graph.nodes.[bId] with text = "BETA" }
        |> Map.add cId { graph.nodes.[cId] with text = "GAMMA" }
        |> fun nodes -> { graph with nodes = nodes }
    let output =
        PlainTextDocument.write graph docId readResult.complement (Some previous)
        |> requireOk "write"
    let expected =
        "alpha"
        + Environment.NewLine
        + Environment.NewLine
        + Environment.NewLine
        + "BETA"
        + Environment.NewLine
        + "GAMMA"
        + Environment.NewLine
    Assert.Equal(expected, output)

[<Fact>]
let ``write projects empty nodes as blank lines`` () =
    let aId = NodeId.New()
    let blankId = NodeId.New()
    let bId = NodeId.New()
    let a = normalNode aId "alpha" Graph.rootId
    let blank = normalNode blankId "" Graph.rootId
    let b = normalNode bId "beta" Graph.rootId
    let graph, docId = graphWithDocument [ a; blank; b ]
    let text =
        PlainTextDocument.write graph docId emptyComplement None
        |> requireOk "write"
    Assert.Equal(
        "alpha" + Environment.NewLine + Environment.NewLine + "beta" + Environment.NewLine,
        text)
