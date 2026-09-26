module Gambol.Shared.Tests.CStyleDocumentTests

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
            name = Filename.Ok "Block",
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

let private childTexts childMap (nodes: Map<NodeId, Node>) parentId =
    kids childMap parentId
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

let private nestedWarmFixture =
    let nl = Environment.NewLine

    "outer" + nl
    + "{" + nl
    + "    inner" + nl
    + "    {" + nl
    + "        leaf" + nl
    + "    }" + nl
    + "    tail" + nl
    + "}" + nl
    + "after" + nl

[<Fact>]
let ``same-line close-open brace split attaches braces to statements`` () =
    let graph, docId = graphWithDocument []
    let result =
        CStyleDocument.read sameLineCloseOpenFixture docId graph
        |> requireOk "read"

    Assert.Equal(2, (kids result.childMap docId).Length)
    let ifId = (kids result.childMap docId).Head.id
    let elseId = (kids result.childMap docId).[1].id
    Assert.Equal("if (x) ", result.nodes.[ifId].text)
    Assert.True(hasClass result.nodes ifId "code-brace")
    Assert.Equal<string list>(
        [ "y = 3;" ],
        childTexts result.childMap result.nodes ifId)
    Assert.Equal("else", result.nodes.[elseId].text)
    Assert.True(hasClass result.nodes elseId "code-brace")
    Assert.Equal<string list>(
        [ "y = 4;" ],
        childTexts result.childMap result.nodes elseId)
    let y3 = (kids result.childMap ifId).Head.id
    let y4 = (kids result.childMap elseId).Head.id
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
            (withRead graph readResult.nodes readResult.childMap)
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

    Assert.Equal(2, (kids result.childMap docId).Length)
    let defaultId = (kids result.childMap docId).Head.id
    let seesId = (kids result.childMap docId).[1].id
    Assert.Equal("DefaultSees(ETile t)", result.nodes.[defaultId].text)
    Assert.True(hasClass result.nodes defaultId "code-brace")
    Assert.Equal("Sees(Tile t)", result.nodes.[seesId].text)
    Assert.False(hasClass result.nodes seesId "code-brace")
    let switchId = (kids result.childMap defaultId).Head.id
    Assert.Equal("switch (t)", result.nodes.[switchId].text)
    Assert.True(hasClass result.nodes switchId "code-brace")
    Assert.Equal<string list>(
        [ "case X:" ],
        childTexts result.childMap result.nodes switchId)
    let texts =
        result.nodes
        |> Map.toList
        |> List.map (fun (_, n) -> n.text)

    Assert.False(List.contains "{" texts)
    Assert.False(List.contains "}" texts)

[<Fact>]
let ``warm units preserve nested preorder`` () =
    let units = CStyleBrace.toWarmUnits nestedWarmFixture

    Assert.Equal<string list>(
        [ "outer"; "inner"; "leaf"; "tail"; "after" ],
        units |> List.map (fun unit -> unit.text))
    Assert.Equal<bool list>(
        [ true; true; false; false; false ],
        units |> List.map (fun unit -> unit.braced))

[<Fact>]
let ``warm units omit empty brace-only structure`` () =
    let units = CStyleBrace.toWarmUnits ("{}" + Environment.NewLine)
    Assert.Empty(units)

[<Fact>]
let ``warm write preserves nested input byte for byte`` () =
    let graph, docId = graphWithDocument []
    let readResult =
        CStyleDocument.read nestedWarmFixture docId graph
        |> requireOk "read"

    let output =
        CStyleDocument.writeWarm
            OutlineLcs.diffTexts
            (withRead graph readResult.nodes readResult.childMap)
            docId
            readResult.complement
            nestedWarmFixture
        |> requireOk "write"

    Assert.Equal(nestedWarmFixture, output)

[<Fact>]
let ``warm write empty brace-only structure remains empty output`` () =
    let graph, docId = graphWithDocument []
    let previous = "{}" + Environment.NewLine
    let readResult =
        CStyleDocument.read previous docId graph |> requireOk "read"

    let output =
        CStyleDocument.writeWarm
            OutlineLcs.diffTexts
            (withRead graph readResult.nodes readResult.childMap)
            docId
            readResult.complement
            previous
        |> requireOk "write"

    Assert.Equal("", output)

[<Fact>]
let ``warm Keep preserves surrounding brace layout when inner statement edits`` () =
    let graph, docId = graphWithDocument []
    let previous = sameLineCloseOpenFixture
    let readResult =
        CStyleDocument.read previous docId graph |> requireOk "read"

    let ifId = (kids readResult.childMap docId).Head.id
    let y3Id = (kids readResult.childMap ifId).Head.id
    let nodes =
        readResult.nodes
        |> Map.add y3Id { readResult.nodes.[y3Id] with text = "y = 9;" }

    let output =
        CStyleDocument.writeWarm
            OutlineLcs.diffTexts
            (withRead graph nodes readResult.childMap)
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
    let graph = withRead graph0 readResult.nodes readResult.childMap
    let children = Graph.children graph docId
    let texts = childTexts graph.childMap graph.nodes docId
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
        childTexts graph.childMap graph.nodes docId)
    let output =
        DocumentWarm.writeArtifact
            OutlineLcs.diffTexts
            graph
            docId
            "Form1.cs"
            (Some previous)
        |> requireOk "write"
        |> fun w -> w.text
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
        fromExisting
            graph0
            (graph0.nodes
             |> Map.add docId { graph0.nodes.[docId] with name = Filename.Ok "Form1.cs" })
        |> addDetachedMany [ media; drawing ]
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
        |> fun w -> w.text
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
    let graph = withRead graph0 readResult.nodes readResult.childMap
    let mId =
        (Graph.children graph docId).Head.id
        |> fun ns -> (Graph.children graph ns).Head.id
        |> fun cls -> (Graph.children graph cls).Head.id
        |> fun m -> (Graph.children graph m).Head.id
    let graph =
        fromExisting
            graph
            (Map.add mId { graph.nodes.[mId] with text = "int x = 2;" } graph.nodes)
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
    let graph = withRead graph0 readResult.nodes readResult.childMap
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
    let graph = withRead graph0 readResult.nodes readResult.childMap
    let initId =
        Graph.children graph docId
        |> List.find (fun c -> graph.nodes.[c.id].text = "namespace mask")
        |> fun ns -> (Graph.children graph ns.id).Head.id
        |> fun cls -> (Graph.children graph cls).Head.id
        |> fun ctor -> (Graph.children graph ctor).Head.id
    let graph =
        fromExisting
            graph
            (Map.add
                initId
                { graph.nodes.[initId] with
                    text = "InitializeComponent(); // warm" }
                graph.nodes)
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

[<Fact>]
let ``writeArtifact falls back to cold when warm throws`` () =
    let line =
        Node.Create(NodeId.New(), text = "using System;", owner = NodeId.New())
    let graph0, docId = graphWithDocument []
    let line = { line with owner = docId }
    let graph =
        Graph.addDetachedNode line graph0
        |> Graph.replace docId 0 [] (owned [ line.id ])
        |> requireOk "attach"
    let previous = "using System;" + Environment.NewLine
    let throwingDiff _ _ : OutlineDiffOp list =
        failwith "injected warm failure"
    let written =
        DocumentWarm.writeArtifact
            throwingDiff
            graph
            docId
            "Form1.cs"
            (Some previous)
        |> requireOk "writeArtifact"
    Assert.True(written.stableUpdateFailed)
    Assert.Contains("using System;", written.text)

[<Fact>]
let ``cold write emits Allman braces for code-brace parent`` () =
    let nl = Environment.NewLine
    let blockId = NodeId.New()
    let yId = NodeId.New()
    let xId = NodeId.New()
    let y = Node.Create(yId, text = "y", owner = blockId)
    let x = Node.Create(xId, text = "x", owner = blockId)
    let block =
        Node.Create(
            blockId,
            text = "block",
            owner = NodeId.New(),
            cssClasses = CssClass.ofList [ "code-brace" ])
    let graph0, docId = graphWithDocument []
    let block = { block with owner = docId }
    let graph1 =
        fromExisting
            graph0
            (Map.add
                docId
                { graph0.nodes.[docId] with name = Filename.Ok "user.css" }
                graph0.nodes)
        |> addDetachedMany [ block; y; x ]
        |> setChildren blockId (owned [ yId; xId ])
    let graph =
        Graph.replace docId 0 [] (owned [ blockId ]) graph1
        |> requireOk "attach"
    let output =
        CStyleDocument.writeArtifact graph docId None
        |> requireOk "cold write"
    let expected =
        "block" + nl
        + "{" + nl
        + "\ty" + nl
        + "\tx" + nl
        + "}" + nl
    Assert.Equal(expected, output)

[<Fact>]
let ``warm write overrides brace-less file when graph has code-brace`` () =
    let nl = Environment.NewLine
    let previous =
        "block" + nl
        + " y" + nl
        + " x" + nl
    let graph0, docId = graphWithDocument []
    let readResult =
        CStyleDocument.read previous docId graph0 |> requireOk "read"
    let blockId = (kids readResult.childMap docId).Head.id
    let nodes =
        readResult.nodes
        |> Map.add
            blockId
            { readResult.nodes.[blockId] with
                cssClasses = CssClass.ofList [ "code-brace" ] }
        |> Map.add
            docId
            { readResult.nodes.[docId] with name = Filename.Ok "user.css" }
    let graph = withRead graph0 nodes readResult.childMap
    let output =
        DocumentWarm.writeArtifact
            OutlineLcs.diffTexts
            graph
            docId
            "SYSTEM/user.css"
            (Some previous)
        |> requireOk "write"
        |> fun w -> w.text
    Assert.Contains("block" + nl + "{" + nl, output)
    Assert.Contains("}" + nl, output)
    Assert.Contains("y", output)
    Assert.Contains("x", output)
    let openAt = output.IndexOf("block" + nl + "{")
    let yAt = output.IndexOf("y")
    let xAt = output.IndexOf("x")
    let closeAt = output.LastIndexOf("}")
    Assert.True(openAt >= 0 && openAt < yAt && yAt < xAt && xAt < closeAt,
        sprintf "Allman braces around children; got:%s%s" nl output)
