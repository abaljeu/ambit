module ExprChapter11Tests

open Gambol.Shared
open Xunit

/// Graph for spec chapter 11 structural, content, and filter rows.
type private Fixture =
    { graph: Graph
      ws: NodeId
      x: NodeId
      innerFile: NodeId
      current: NodeId
      todo: NodeId
      blueUnderTodo: NodeId
      sibA: NodeId
      wsTodo: NodeId
      dirD: NodeId
      eFile: NodeId
      eSec: NodeId
      fileD: NodeId
      namedFile: NodeId
      spacedFile: NodeId
      cdFile: NodeId
      cSec: NodeId
      theBlue: NodeId
      headed: NodeId
      focus: NodeId
      secX: NodeId
      secY: NodeId
      draft: NodeId }

let private specialNode id kind name owner =
    Node.Create(
        id,
        text = name,
        name = Filename.create name,
        owner = owner,
        kind = Special kind)

let private namedNormal id name owner =
    Node.Create(
        id,
        text = name,
        name = Filename.create name,
        owner = owner)

let private addUnder parentId child graph =
    let parent = graph.nodes.[parentId]
    let nodes =
        graph.nodes
        |> Map.add child.id child
        |> Map.add parentId
            { parent with children = parent.children @ [ ChildNode.owner child.id ] }
    Graph.fromNodes graph.root nodes

let private build () : Fixture =
    let wsId, xId, innerId = NodeId.New(), NodeId.New(), NodeId.New()
    let currentId, todoId = NodeId.New(), NodeId.New()
    let blueTodoId, sibAId = NodeId.New(), NodeId.New()
    let wsTodoId, dirId, eFileId = NodeId.New(), NodeId.New(), NodeId.New()
    let eSecId, fileDId, namedFileId = NodeId.New(), NodeId.New(), NodeId.New()
    let spacedId, abId, cdId = NodeId.New(), NodeId.New(), NodeId.New()
    let aId, bId, cId = NodeId.New(), NodeId.New(), NodeId.New()
    let theBlueId, headedId, focusId = NodeId.New(), NodeId.New(), NodeId.New()
    let secXId, secYId, draftId = NodeId.New(), NodeId.New(), NodeId.New()
    let g0 = Graph.create ()
    let graph =
        g0
        |> addUnder g0.root (Node.Create(focusId, text = "focus", owner = g0.root))
        |> addUnder Graph.workspacesId
            (specialNode wsId Workspace "ws" Graph.workspacesId)
        |> addUnder wsId (specialNode xId Directory "x" wsId)
        |> addUnder wsId (specialNode innerId File "inner.fs" wsId)
        |> addUnder innerId (Node.Create(currentId, text = "here", owner = innerId))
        |> addUnder innerId
            (Node.Create(
                todoId,
                text = "todo section",
                name = Filename.create "todo",
                owner = innerId))
        |> addUnder todoId (namedNormal blueTodoId "blue" todoId)
        |> addUnder innerId (Node.Create(sibAId, text = "A", owner = innerId))
        |> addUnder innerId (namedNormal secXId "x" innerId)
        |> addUnder innerId (namedNormal secYId "y" innerId)
        |> addUnder innerId
            (Node.Create(draftId, text = "draft notes", owner = innerId))
        |> addUnder wsId
            (Node.Create(
                wsTodoId,
                text = "ws todo",
                name = Filename.create "todo",
                owner = wsId))
        |> addUnder Graph.workspacesId
            (specialNode dirId Directory "d" Graph.workspacesId)
        |> addUnder dirId (specialNode eFileId File "e" dirId)
        |> addUnder dirId (namedNormal eSecId "e" dirId)
        |> addUnder Graph.workspacesId
            (specialNode fileDId File "d" Graph.workspacesId)
        |> addUnder Graph.workspacesId
            (specialNode namedFileId File "file" Graph.workspacesId)
        |> addUnder Graph.workspacesId
            (specialNode spacedId File "filename with spaces" Graph.workspacesId)
        |> addUnder Graph.workspacesId
            (specialNode abId Directory "a b" Graph.workspacesId)
        |> addUnder abId (specialNode cdId File "c d" abId)
        |> addUnder Graph.workspacesId (specialNode aId File "a" Graph.workspacesId)
        |> addUnder aId (namedNormal bId "b" aId)
        |> addUnder bId (namedNormal cId "c" bId)
        |> addUnder Graph.workspacesId
            (Node.Create(
                theBlueId,
                text = "the sky is blue",
                name = Filename.create "blue",
                owner = Graph.workspacesId))
        |> addUnder Graph.workspacesId
            (Node.Create(
                headedId,
                text = "the heading",
                name = Filename.create "red",
                cssClasses = CssClass.ofList [ "h1" ],
                owner = Graph.workspacesId))
    { graph = graph
      ws = wsId
      x = xId
      innerFile = innerId
      current = currentId
      todo = todoId
      blueUnderTodo = blueTodoId
      sibA = sibAId
      wsTodo = wsTodoId
      dirD = dirId
      eFile = eFileId
      eSec = eSecId
      fileD = fileDId
      namedFile = namedFileId
      spacedFile = spacedId
      cdFile = cdId
      cSec = cId
      theBlue = theBlueId
      headed = headedId
      focus = focusId
      secX = secXId
      secY = secYId
      draft = draftId }

let private nodeIds (answers: ExprAnswer list) =
    answers
    |> List.map (function
        | ExprAnswer.Node n -> n.id
        | _ -> failwith "expected Node answer")

let private answerOf (f: Fixture) id = ExprAnswer.Node f.graph.nodes.[id]

let private rootOf (f: Fixture) = ExprAnswer.Node f.graph.nodes.[f.graph.root]

let private nodeHits (f: Fixture) input source =
    match ExprCompile.evalOutcome f.graph input source with
    | ExprCompile.Hits(ExprAnswerType.Node, answers) -> nodeIds answers
    | ExprCompile.Hits(_, _) -> failwith "expected Node Answers"
    | ExprCompile.ParseFailed e -> failwith $"parse failed: {e}"
    | ExprCompile.TypeFailed e -> failwith $"type failed: {e}"

let private parseFailed (f: Fixture) source =
    match ExprCompile.evalOutcome f.graph (rootOf f) source with
    | ExprCompile.ParseFailed e -> e
    | other -> failwith $"expected parse error, got {other}"

[<Fact>]
let ``structural desugar and root rows`` () =
    let f = build ()
    let root = rootOf f
    Assert.Equal<NodeId list>([ f.ws ], nodeHits f root "//ws")
    Assert.Equal<NodeId list>(nodeHits f root "root / \"ws\"", nodeHits f root "//ws")
    Assert.Equal<NodeId list>(nodeHits f root "// \"ws\"", nodeHits f root "//ws")
    Assert.Equal<NodeId list>([ f.x ], nodeHits f root "//ws/x")
    Assert.Equal<NodeId list>([ f.namedFile ], nodeHits f root "//file")
    Assert.Equal<NodeId list>(
        [ f.spacedFile ],
        nodeHits f root "// \"filename with spaces\"")
    Assert.Equal<NodeId list>([ f.cdFile ], nodeHits f root "// \"a b\" / \"c d\"")
    Assert.Equal<NodeId list>([ f.eFile ], nodeHits f root "d/e")
    Assert.Equal<NodeId list>(nodeHits f root "root", nodeHits f root "root ws")
    Assert.Equal<NodeId list>(nodeHits f root "**", nodeHits f root "root tree")
    Assert.Contains(f.ws, nodeHits f root "root tree")
    Assert.DoesNotContain(f.graph.root, nodeHits f root "root tree")

[<Fact>]
let ``slash d dir is structural search then dir filter`` () =
    let f = build ()
    let root = rootOf f
    Assert.Equal<NodeId list>([ f.dirD; f.fileD ], nodeHits f root "/ \"d\"")
    Assert.Equal<NodeId list>([ f.dirD ], nodeHits f root "/ \"d\" dir")
    Assert.Equal<NodeId list>([ f.dirD ], nodeHits f (answerOf f f.dirD) "dir")
    Assert.Equal<NodeId list>([], nodeHits f (answerOf f f.fileD) "dir")

[<Fact>]
let ``content search rows d hash e, a hash b hash c, caret hash blue`` () =
    let f = build ()
    let root = rootOf f
    Assert.Equal<NodeId list>([ f.eSec ], nodeHits f root "d#e")
    Assert.Equal<NodeId list>(nodeHits f root "/ \"d\" # \"e\"", nodeHits f root "d#e")
    Assert.Equal<NodeId list>([ f.cSec ], nodeHits f root "a#b#c")
    let fromHere = answerOf f f.current
    Assert.DoesNotContain(f.blueUnderTodo, nodeHits f fromHere "^#blue")
    Assert.Equal<NodeId list>([], nodeHits f fromHere "^#blue")
    Assert.Equal<NodeId list>([ f.blueUnderTodo ], nodeHits f fromHere "^#todo#blue")
    Assert.Equal<NodeId list>([ f.wsTodo ], nodeHits f fromHere "wsroot #todo")

[<Fact>]
let ``pure filters class named containing; descendant composition`` () =
    let f = build ()
    let root = rootOf f
    Assert.Equal<NodeId list>(
        [ f.headed ],
        nodeHits f (answerOf f f.headed) "class \"h1\"")
    Assert.Equal<NodeId list>([], nodeHits f (answerOf f f.theBlue) "class \"h1\"")
    Assert.Equal<NodeId list>([ f.headed ], nodeHits f root "root descendant class \"h1\"")
    Assert.Equal<NodeId list>(
        [ f.theBlue ],
        nodeHits f (answerOf f f.theBlue) "named \"blue\"")
    Assert.Equal<NodeId list>([], nodeHits f (answerOf f f.headed) "named \"blue\"")
    Assert.Equal<NodeId list>(
        [ f.theBlue ],
        nodeHits f (answerOf f f.theBlue) "containing \"the\"")
    Assert.Equal<NodeId list>(
        [ f.theBlue ],
        nodeHits f root "root descendant containing \"the\" named \"blue\"")
    Assert.Contains(f.theBlue, nodeHits f root "root descendant containing \"the\"")
    Assert.Contains(f.headed, nodeHits f root "root descendant containing \"the\"")

[<Fact>]
let ``child equals colon-star; wsroot is containing Workspace`` () =
    let f = build ()
    let fromFile = answerOf f f.innerFile
    Assert.Equal<NodeId list>(nodeHits f fromFile ":*", nodeHits f fromFile "child")
    Assert.Contains(f.current, nodeHits f fromFile "child")
    Assert.Equal<NodeId list>([ f.ws ], nodeHits f (answerOf f f.current) "wsroot")

[<Theory>]
[<InlineData("// ws", "missing argument")>]
[<InlineData("/", "missing argument")>]
[<InlineData("// OR /", "missing argument")>]
[<InlineData("root descendant containing root", "missing argument")>]
[<InlineData("3", "a number is only valid as the slot of : ! left or right")>]
[<InlineData("\"d\" \"e\"", "cannot juxtapose two quoted strings")>]
let ``parse error rows assert spec messages`` (source: string) (needle: string) =
    let f = build ()
    Assert.Contains(needle, parseFailed f source)

[<Fact>]
let ``text hash todo is type error; hash todo text is Node to Text`` () =
    let f = build ()
    match ExprCompile.evalOutcome f.graph (rootOf f) "text #todo" with
    | ExprCompile.TypeFailed e -> Assert.Equal("type error", e)
    | other -> failwith $"expected type error, got {other}"
    match ExprCompile.evalOutcome f.graph (answerOf f f.innerFile) "#todo text" with
    | ExprCompile.Hits(ExprAnswerType.Text, [ ExprAnswer.Text t ]) ->
        Assert.Equal("todo section", t)
    | other -> failwith $"expected Text Answers, got {other}"

[<Fact>]
let ``out-of-range sibling offset is zero Answers not an error`` () =
    let f = build ()
    match ExprCompile.evalOutcome f.graph (answerOf f f.sibA) "!-249053534" with
    | ExprCompile.Hits(ExprAnswerType.Node, []) -> ()
    | other -> failwith $"expected empty Node Answers, got {other}"

[<Fact>]
let ``hash x comma hash y concatenates; a Node may appear twice`` () =
    let f = build ()
    let fromFile = answerOf f f.innerFile
    Assert.Equal<NodeId list>([ f.secX; f.secY ], nodeHits f fromFile "#x , #y")
    Assert.Equal<NodeId list>([ f.secX; f.secX ], nodeHits f fromFile "#x , #x")

[<Fact>]
let ``containing the AND named blue is same-input intersection`` () =
    let f = build ()
    let source = "containing \"the\" AND named \"blue\""
    Assert.Equal<NodeId list>(
        [ f.theBlue ],
        nodeHits f (answerOf f f.theBlue) source)
    Assert.Equal<NodeId list>([], nodeHits f (answerOf f f.headed) source)
    Assert.Equal<NodeId list>(
        [],
        nodeHits f (answerOf f f.blueUnderTodo) source)

[<Fact>]
let ``root descendant NOT containing draft is negation-as-failure`` () =
    let f = build ()
    let found = nodeHits f (rootOf f) "root descendant NOT containing \"draft\""
    Assert.Contains(f.theBlue, found)
    Assert.Contains(f.headed, found)
    Assert.Contains(f.secX, found)
    Assert.DoesNotContain(f.draft, found)

[<Fact>]
let ``root OUTER containing blue yields outermost Header matches`` () =
    let f = build ()
    let found = nodeHits f (rootOf f) "root OUTER containing \"blue\""
    Assert.Contains(f.theBlue, found)
    Assert.Contains(f.blueUnderTodo, found)
    Assert.DoesNotContain(f.graph.root, found)
    Assert.DoesNotContain(f.headed, found)
    let withRe = nodeHits f (rootOf f) "root OUTER re \".*blue.*\""
    Assert.Equal<NodeId list>(found, withRe)
    match ExprParse.parseExpr "OUTER containing \"blue\"" with
    | Ok(Expr.Outer _) -> ()
    | other -> failwith $"expected OUTER combinator, got {other}"

[<Fact>]
let ``IF containing blue keeps the input Node`` () =
    let f = build ()
    let fromBlue = answerOf f f.theBlue
    Assert.Equal<NodeId list>(
        [ f.theBlue ],
        nodeHits f fromBlue "IF containing \"blue\"")
    Assert.Equal<NodeId list>(
        [],
        nodeHits f (answerOf f f.headed) "IF containing \"blue\"")
    Assert.Equal<NodeId list>(
        nodeHits f fromBlue "IF containing \"blue\"",
        nodeHits f fromBlue "NOT (NOT containing \"blue\")")
    match ExprParse.parseExpr "IF containing \"blue\"" with
    | Ok(Expr.If _) -> ()
    | other -> failwith $"expected IF combinator, got {other}"
    let ifChild = nodeHits f (rootOf f) "root descendant IF child"
    Assert.Contains(f.todo, ifChild)
    Assert.DoesNotContain(f.sibA, ifChild)

[<Fact>]
let ``Run statement rows materialise named blue descendants`` () =
    let f = build ()
    match ExprRun.run f.focus f.graph "= root descendant named \"blue\"" with
    | ExprRun.Ignore -> failwith "expected Apply"
    | ExprRun.Apply plan ->
        let kids =
            plan.ops
            |> List.choose (function
                | Op.Replace(_, _, ks) -> Some(ks |> List.map (fun c -> c.id))
                | _ -> None)
            |> List.concat
        Assert.Contains(f.blueUnderTodo, kids)
        Assert.Contains(f.theBlue, kids)
        Assert.True(plan.unfold)
    match ExprRun.run f.focus f.graph "todo=root descendant named \"blue\"" with
    | ExprRun.Ignore -> failwith "expected Apply"
    | ExprRun.Apply plan ->
        match plan.ops |> List.tryFind (function Op.SetName _ -> true | _ -> false) with
        | Some(Op.SetName(id, _, "todo")) -> Assert.Equal(f.focus, id)
        | _ -> failwith "expected SetName todo"
