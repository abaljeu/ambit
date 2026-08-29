module ExprTextOpsTests

open Gambol.Shared
open Xunit

/// Graph for the spec chapter 7 text rows: `text`, `name`, `left`, `right`, `IS`.
type private Fixture =
    { graph: Graph
      file: NodeId
      rapid: NodeId
      slow: NodeId
      blank: NodeId
      unnamed: NodeId }

let private specialNode id kind name owner =
    Node.Create(
        id,
        text = name,
        name = Filename.create name,
        owner = owner,
        kind = Special kind)

let private addUnder parentId child graph =
    let parent = graph.nodes.[parentId]
    let nodes =
        graph.nodes
        |> Map.add child.id child
        |> Map.add parentId
            { parent with children = parent.children @ [ ChildNode.owner child.id ] }
    Graph.fromNodes graph.root nodes

let private build () : Fixture =
    let wsId, fileId, rapidId = NodeId.New(), NodeId.New(), NodeId.New()
    let slowId, blankId, unnamedId = NodeId.New(), NodeId.New(), NodeId.New()
    let graph =
        Graph.create ()
        |> addUnder Graph.workspacesId
            (specialNode wsId Workspace "ws" Graph.workspacesId)
        |> addUnder wsId (specialNode fileId File "notes.txt" wsId)
        |> addUnder fileId
            (Node.Create(
                rapidId,
                text = "rapid transit",
                name = Filename.create "rapid.txt",
                owner = fileId))
        |> addUnder fileId
            (Node.Create(
                slowId,
                text = "slow going",
                name = Filename.create "slow.md",
                owner = fileId))
        |> addUnder fileId (Node.Create(blankId, text = "", owner = fileId))
        |> addUnder fileId (Node.Create(unnamedId, text = "no name here", owner = fileId))
    { graph = graph
      file = fileId
      rapid = rapidId
      slow = slowId
      blank = blankId
      unnamed = unnamedId }

let private answerOf (f: Fixture) id = ExprAnswer.Node f.graph.nodes.[id]

let private evalOk (f: Fixture) input source =
    match ExprCompile.eval f.graph input source with
    | Ok answers -> answers
    | Error err -> failwith $"eval failed: {err}"

let private texts (answers: ExprAnswer list) =
    answers
    |> List.map (function
        | ExprAnswer.Text t -> t
        | other -> failwith $"expected Text answer, got {other}")

let private nodeIds (answers: ExprAnswer list) =
    answers
    |> List.map (function
        | ExprAnswer.Node n -> n.id
        | other -> failwith $"expected Node answer, got {other}")

let private textHits (f: Fixture) input source =
    match ExprCompile.evalOutcome f.graph input source with
    | ExprCompile.Hits(ExprAnswerType.Text, answers) -> texts answers
    | other -> failwith $"expected Text Answers, got {other}"

let private typeFailed (f: Fixture) source =
    match ExprCompile.evalOutcome f.graph (answerOf f f.rapid) source with
    | ExprCompile.TypeFailed e -> e
    | other -> failwith $"expected type error, got {other}"

[<Fact>]
let ``text is node text; the empty Header is one Answer`` () =
    let f = build ()
    Assert.Equal<string list>(
        [ "rapid transit" ],
        textHits f (answerOf f f.rapid) "text")
    Assert.Equal<string list>([ "" ], textHits f (answerOf f f.blank) "text")

[<Fact>]
let ``name is Filename Ok only; Empty and Invalid are a miss`` () =
    let f = build ()
    Assert.Equal<string list>([ "rapid.txt" ], textHits f (answerOf f f.rapid) "name")
    Assert.Equal<string list>([], textHits f (answerOf f f.unnamed) "name")
    Assert.Equal(Filename.Empty, f.graph.nodes.[f.unnamed].name)
    Assert.Equal<string list>([], textHits f (answerOf f f.unnamed) "name right 4")

[<Fact>]
let ``left and right always yield one string; length never misses`` () =
    let f = build ()
    let fromRapid = answerOf f f.rapid
    Assert.Equal<string list>([ "rapid" ], textHits f fromRapid "text left 5")
    Assert.Equal<string list>([ "ansit" ], textHits f fromRapid "text right 5")
    Assert.Equal<string list>(
        [ "rapid transit" ],
        textHits f fromRapid "text left 400")
    Assert.Equal<string list>(
        [ "rapid transit" ],
        textHits f fromRapid "text right 400")
    Assert.Equal<string list>([ "" ], textHits f fromRapid "text right 0")
    Assert.Equal<string list>([ "" ], textHits f fromRapid "text left -1")
    Assert.Equal<string list>([ "" ], textHits f (answerOf f f.blank) "text left 5")

[<Fact>]
let ``no implicit Node to Text coerce`` () =
    let f = build ()
    Assert.Equal("type error", typeFailed f "left 5")
    Assert.Equal("type error", typeFailed f "right 4 IS \".txt\"")
    Assert.Equal("type error", typeFailed f "text text")
    Assert.Equal("type error", typeFailed f "name text")
    Assert.Equal<string list>(
        [],
        ExprEval.toList (ExprWalk.leftText 5 (answerOf f f.rapid)) |> texts)

[<Fact>]
let ``bare left and right are missing-argument parse errors`` () =
    match ExprParse.parseExpr "text left" with
    | Error e -> Assert.Contains(ExprParse.missingArgument, e)
    | Ok expr -> failwith $"expected parse error, got {expr}"
    match ExprParse.parseExpr "text right" with
    | Error e -> Assert.Contains(ExprParse.missingArgument, e)
    | Ok expr -> failwith $"expected parse error, got {expr}"
    match ExprParse.parseExpr "5" with
    | Error e -> Assert.Equal(ExprParse.numberOnlyOperand, e)
    | Ok expr -> failwith $"expected parse error, got {expr}"

[<Fact>]
let ``a quoted string in Expression position yields that Text`` () =
    let f = build ()
    Assert.Equal<string list>(
        [ "rapid" ],
        textHits f (answerOf f f.rapid) "\"rapid\"")
    Assert.Equal(
        Expr.Term(ExprTerm.Text "rapid"),
        match ExprParse.parseExpr "\"rapid\"" with
        | Ok expr -> expr
        | Error e -> failwith $"parse failed: {e}")

[<Fact>]
let ``adjacent quoted strings in juxtaposition are a parse error`` () =
    match ExprParse.parseExpr "\"d\" \"e\"" with
    | Error e -> Assert.Equal(ExprParse.adjacentQuotedStrings, e)
    | Ok expr -> failwith $"expected parse error, got {expr}"

[<Fact>]
let ``quoted strings stay legal as combinator operands and slots`` () =
    let parseOk source =
        match ExprParse.parseExpr source with
        | Ok expr -> expr
        | Error e -> failwith $"parse failed: {e}"
    parseOk "left 5 IS \"rapid\"" |> ignore
    parseOk "containing \"blue\"" |> ignore
    parseOk "text containing \"x\"" |> ignore
    parseOk "IF (name right 4 IS \".txt\")" |> ignore
    match parseOk "(text IF (\"b\" IS left 1)) OR \"isn't a b word\"" with
    | Expr.Or(_, Expr.Term(ExprTerm.Text _)) -> ()
    | other -> failwith $"expected OR with a quoted right operand, got {other}"

[<Fact>]
let ``IS runs both sides on the same input and yields matching LHS`` () =
    let f = build ()
    Assert.Equal<string list>(
        [ "rapid" ],
        textHits f (answerOf f f.rapid) "text left 5 IS \"rapid\"")
    Assert.Equal<string list>(
        [],
        textHits f (answerOf f f.slow) "text left 5 IS \"rapid\"")
    Assert.Equal<string list>(
        [ ".txt" ],
        textHits f (answerOf f f.rapid) "name right 4 IS \".txt\"")
    Assert.Equal<string list>(
        [],
        textHits f (answerOf f f.slow) "name right 4 IS \".txt\"")
    Assert.Equal<string list>(
        [ "" ],
        textHits f (answerOf f f.rapid) "text right 0 IS \"\"")

[<Fact>]
let ``IS is empty when either side has no equal Answer`` () =
    let f = build ()
    Assert.Equal<string list>(
        [],
        textHits f (answerOf f f.unnamed) "name IS \"x\"")
    Assert.Equal<string list>(
        [],
        textHits f (answerOf f f.rapid) "text IS \"absent\"")

[<Fact>]
let ``IS attaches like AND and is capitals only`` () =
    let parsed =
        match ExprParse.parseExpr "text left 5 IS \"rapid\"" with
        | Ok expr -> expr
        | Error e -> failwith $"parse failed: {e}"
    let lhs =
        Expr.Pipe
            [ Expr.Term(ExprTerm.Word("text", None))
              Expr.Term(ExprTerm.Word("left", Some "5")) ]
    Assert.Equal(Expr.Is(lhs, Expr.Term(ExprTerm.Text "rapid")), parsed)
    match ExprParse.parseExpr "text is \"rapid\"" with
    | Ok(Expr.Pipe items) ->
        Assert.Contains(Expr.Term(ExprTerm.Word("is", None)), items)
    | other -> failwith $"lowercase is must be a word, got {other}"
    match ExprParse.parseExpr "IS" with
    | Error _ -> ()
    | Ok expr -> failwith $"expected parse error, got {expr}"

[<Fact>]
let ``IF pulls a text predicate back to the Node`` () =
    let f = build ()
    let fromFile = answerOf f f.file
    Assert.Equal<NodeId list>(
        [ f.rapid ],
        nodeIds (evalOk f fromFile "child IF (text left 5 IS \"rapid\")"))
    Assert.Equal<NodeId list>(
        [ f.rapid ],
        nodeIds (evalOk f fromFile "child IF (name right 4 IS \".txt\")"))
    Assert.Equal<NodeId list>(
        [ f.rapid ],
        nodeIds (evalOk f fromFile "OUTER (text left 5 IS \"rapid\")"))
