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

[<Fact>]
let ``warm write sibling reorder follows graph not previous disk order`` () =
    let graph0, docId = graphWithDocument []
    let nl = Environment.NewLine
    let previous =
        "using System.Drawing.Drawing2D;"
        + nl
        + "using System.Media;"
        + nl
        + nl
        + "namespace mask"
        + nl
        + "{"
        + nl
        + "    class Form1"
        + nl
        + "    {"
        + nl
        + "    }"
        + nl
        + "}"
        + nl
    let readResult =
        CStyleDocument.read previous docId graph0 |> requireOk "read"
    let graph = { graph0 with nodes = readResult.nodes }
    let children = graph.nodes.[docId].children
    let texts = childTexts readResult.nodes docId
    Assert.True(texts.Length >= 3, sprintf "children=%A" texts)
    let drawingId = children.[0].id
    let mediaId = children.[1].id
    Assert.Equal("using System.Drawing.Drawing2D;", graph.nodes.[drawingId].text)
    Assert.Equal("using System.Media;", graph.nodes.[mediaId].text)
    let rest = children |> List.skip 2
    let graph =
        Graph.replace
            docId
            0
            children
            (owned ([ mediaId; drawingId ] @ (rest |> List.map (fun c -> c.id))))
            graph
        |> requireOk "reorder usings"
    Assert.Equal<string list>(
        [ "using System.Media;"; "using System.Drawing.Drawing2D;" ]
        @ (rest |> List.map (fun c -> graph.nodes.[c.id].text)),
        childTexts graph.nodes docId)
    let output =
        DocumentWarm.writeArtifact
            OutlineLcs.diffTexts
            graph
            docId
            "Form1.cs"
            (Some previous)
        |> requireOk "write"
    let mediaAt = output.IndexOf("using System.Media;")
    let drawingAt = output.IndexOf("using System.Drawing.Drawing2D;")
    Assert.True(mediaAt >= 0, "media using missing")
    Assert.True(drawingAt >= 0, "drawing using missing")
    Assert.True(
        mediaAt < drawingAt,
        $"graph order Media then Drawing2D; got:{nl}{output}")
    Assert.False(
        output.Contains("}}}"),
        $"nested closes must not concatenate:{nl}{output}")
    Assert.Contains("namespace mask", output)
    Assert.Contains("class Form1", output)

[<Fact>]
let ``warm write first graph node replaces mismatched leading file line`` () =
    let graph0, docId = graphWithDocument []
    let nl = Environment.NewLine
    let previous =
        nl
        + "using System.Drawing.Drawing2D;"
        + nl
        + "using System.Media;"
        + nl
    let media =
        Node.Create(NodeId.New(), text = "using System.Media;", owner = docId)
    let drawing =
        Node.Create(
            NodeId.New(),
            text = "using System.Drawing.Drawing2D;",
            owner = docId)
    let graph1 =
        graph0.nodes
        |> Map.add docId
            { graph0.nodes.[docId] with
                name = Filename.Ok "Form1.cs"
                children = [] }
        |> Map.add media.id media
        |> Map.add drawing.id drawing
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    let graph =
        Graph.replace docId 0 [] (owned [ media.id; drawing.id ]) graph1
        |> requireOk "attach"
    let output =
        DocumentWarm.writeArtifact
            OutlineLcs.diffTexts
            graph
            docId
            "Form1.cs"
            (Some previous)
        |> requireOk "write"
    Assert.True(
        output.StartsWith("using System.Media;"),
        sprintf "first graph node must win; got:%s%s" nl output)
    let mediaAt = output.IndexOf("using System.Media;")
    let drawingAt = output.IndexOf("using System.Drawing.Drawing2D;")
    Assert.True(mediaAt < drawingAt, sprintf "order; got:%s%s" nl output)

[<Fact>]
let ``warm write nested Allman edit preserves close layout`` () =
    let graph0, docId = graphWithDocument []
    let nl = Environment.NewLine
    let previous =
        "namespace mask"
        + nl
        + "{"
        + nl
        + "    class Form1"
        + nl
        + "    {"
        + nl
        + "        void M()"
        + nl
        + "        {"
        + nl
        + "            int x = 1;"
        + nl
        + "        }"
        + nl
        + "    }"
        + nl
        + "}"
        + nl
    let readResult =
        CStyleDocument.read previous docId graph0 |> requireOk "read"
    let graph = { graph0 with nodes = readResult.nodes }
    let mId =
        graph.nodes.[docId].children.Head.id
        |> fun ns -> graph.nodes.[ns].children.Head.id
        |> fun cls -> graph.nodes.[cls].children.Head.id
        |> fun m -> graph.nodes.[m].children.Head.id
    let graph =
        graph.nodes
        |> Map.add mId { graph.nodes.[mId] with text = "int x = 2;" }
        |> fun nodes -> { graph with nodes = nodes }
    let output =
        CStyleDocument.writeWarm
            OutlineLcs.diffTexts
            graph
            docId
            readResult.complement
            previous
        |> requireOk "write"
    Assert.Contains("int x = 2;", output)
    Assert.DoesNotContain("}}}", output)
    Assert.False(
        output.Replace("\r\n", "\n").Contains("}}}"),
        sprintf "closes concatenated:%s%s" nl output)
    let normalized = output.Replace("\r\n", "\n")
    Assert.True(
        normalized.Contains("}\n    }\n}\n")
        || normalized.Contains("}\n    }\n}"),
        sprintf "expected Allman nested closes; got:%s%s" nl output)

let private form1LikeFixture =
    let nl = Environment.NewLine
    "using System.Drawing.Drawing2D;"
    + nl
    + "using System.Media;"
    + nl
    + nl
    + "namespace mask"
    + nl
    + "{"
    + nl
    + "    public partial class Form1 : Form"
    + nl
    + "    {"
    + nl
    + "        public Form1()"
    + nl
    + "        {"
    + nl
    + "            InitializeComponent();"
    + nl
    + "            WindowState = FormWindowState.Maximized;"
    + nl
    + "        }"
    + nl
    + "    }"
    + nl
    + "}"
    + nl

[<Fact>]
let ``Form1-like warm round-trip does not duplicate usings or mash braces`` () =
    let graph0, docId = graphWithDocument []
    let previous = form1LikeFixture
    let readResult =
        CStyleDocument.read previous docId graph0 |> requireOk "read"
    let graph = { graph0 with nodes = readResult.nodes }
    let output =
        CStyleDocument.writeWarm
            OutlineLcs.diffTexts
            graph
            docId
            readResult.complement
            previous
        |> requireOk "write"
    let drawingCount =
        let needle = "using System.Drawing.Drawing2D;"
        let rec loop (i: int) acc =
            match output.IndexOf(needle, i) with
            | -1 -> acc
            | j -> loop (j + needle.Length) (acc + 1)
        loop 0 0
    Assert.Equal(1, drawingCount)
    Assert.DoesNotContain("}}}", output)
    Assert.DoesNotContain("{            InitializeComponent", output)
    Assert.False(
        output.Contains("}using "),
        sprintf "trailing using after close:%s%s" Environment.NewLine output)
    Assert.Equal(previous, output)

[<Fact>]
let ``Form1-like warm edit replaces mismatched body line without duplicating`` () =
    let graph0, docId = graphWithDocument []
    let previous = form1LikeFixture
    let readResult =
        CStyleDocument.read previous docId graph0 |> requireOk "read"
    let graph = { graph0 with nodes = readResult.nodes }
    let initId =
        graph.nodes.[docId].children
        |> List.find (fun c -> graph.nodes.[c.id].text = "namespace mask")
        |> fun ns -> graph.nodes.[ns.id].children.Head.id
        |> fun cls -> graph.nodes.[cls].children.Head.id
        |> fun ctor -> graph.nodes.[ctor].children.Head.id
    let graph =
        graph.nodes
        |> Map.add initId {
            graph.nodes.[initId] with
                text = "InitializeComponent(); // warm"
        }
        |> fun nodes -> { graph with nodes = nodes }
    let output =
        CStyleDocument.writeWarm
            OutlineLcs.diffTexts
            graph
            docId
            readResult.complement
            previous
        |> requireOk "write"
    Assert.Contains("InitializeComponent(); // warm", output)
    Assert.DoesNotContain("}}}", output)
    Assert.DoesNotContain("{            InitializeComponent", output)
    let drawingCount =
        let needle = "using System.Drawing.Drawing2D;"
        let rec loop (i: int) acc =
            match output.IndexOf(needle, i) with
            | -1 -> acc
            | j -> loop (j + needle.Length) (acc + 1)
        loop 0 0
    Assert.Equal(1, drawingCount)
    Assert.False(output.Contains("}using "), sprintf "got:%s" output)
