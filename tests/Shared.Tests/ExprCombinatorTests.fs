module ExprCombinatorTests

open Gambol.Shared
open Xunit

type private Fixture =
    { graph: Graph
      file: NodeId
      x: NodeId
      y: NodeId
      both: NodeId
      draft: NodeId
      keep: NodeId }

let private specialNode id kind name owner =
    Node.Create(
        id,
        text = name,
        name = Filename.create name,
        owner = owner,
        kind = Special kind)

let private namedNormal id name text owner =
    Node.Create(
        id,
        text = text,
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
    let wsId = NodeId.New()
    let fileId = NodeId.New()
    let xId = NodeId.New()
    let yId = NodeId.New()
    let bothId = NodeId.New()
    let draftId = NodeId.New()
    let keepId = NodeId.New()
    let graph =
        Graph.create ()
        |> addUnder Graph.workspacesId
            (specialNode wsId Workspace "ws" Graph.workspacesId)
        |> addUnder wsId (specialNode fileId File "f.fs" wsId)
        |> addUnder fileId (namedNormal xId "x" "alpha" fileId)
        |> addUnder fileId (namedNormal yId "y" "other" fileId)
        |> addUnder fileId (namedNormal bothId "blue" "the heading" fileId)
        |> addUnder fileId (namedNormal draftId "d" "draft notes" fileId)
        |> addUnder fileId (namedNormal keepId "k" "ok" fileId)
    { graph = graph
      file = fileId
      x = xId
      y = yId
      both = bothId
      draft = draftId
      keep = keepId }

let private evalOk graph input source =
    match ExprCompile.eval graph input source with
    | Ok answers -> answers
    | Error err -> failwith $"eval failed: {err}"

let private nodeIds (answers: ExprAnswer list) =
    answers
    |> List.map (function
        | ExprAnswer.Node n -> n.id
        | _ -> failwith "expected Node answer")

let private answerOf graph id =
    ExprAnswer.Node graph.nodes.[id]

let private rootAnswer (graph: Graph) =
    ExprAnswer.Node graph.nodes.[graph.root]

let private exprOk input =
    match ExprParse.parseExpr input with
    | Ok expr -> expr
    | Error err -> failwith $"parse failed: {err}"

[<Fact>]
let ``hash x comma hash y concatenates and may repeat a Node`` () =
    let f = build ()
    let fromFile = answerOf f.graph f.file
    Assert.Equal<NodeId list>(
        [ f.x; f.y ],
        nodeIds (evalOk f.graph fromFile "#x , #y"))
    Assert.Equal<NodeId list>(
        [ f.x; f.y ],
        nodeIds (evalOk f.graph fromFile "#x,#y"))
    Assert.Equal<NodeId list>(
        [ f.x; f.y ],
        nodeIds (evalOk f.graph fromFile "#x OR #y"))
    Assert.Equal<NodeId list>(
        [ f.x; f.x ],
        nodeIds (evalOk f.graph fromFile "#x , #x"))

[<Fact>]
let ``containing AND named keeps the input only when both succeed`` () =
    let f = build ()
    let source = "containing \"the\" AND named \"blue\""
    Assert.Equal<NodeId list>(
        [ f.both ],
        nodeIds (evalOk f.graph (answerOf f.graph f.both) source))
    Assert.Equal<NodeId list>(
        [],
        nodeIds (evalOk f.graph (answerOf f.graph f.x) source))
    Assert.Equal<NodeId list>(
        [],
        nodeIds (evalOk f.graph (answerOf f.graph f.keep) source))

[<Fact>]
let ``root descendant NOT containing draft keeps Nodes with empty inner`` () =
    let f = build ()
    let found =
        nodeIds (evalOk f.graph (rootAnswer f.graph) "root descendant NOT containing \"draft\"")
    Assert.Contains(f.keep, found)
    Assert.Contains(f.x, found)
    Assert.DoesNotContain(f.draft, found)

[<Fact>]
let ``d AND b OR c parses as AND then OR by precedence`` () =
    let mixed = exprOk "d AND b OR c"
    let grouped = exprOk "(d AND b) OR c"
    let andOr = exprOk "d AND (b OR c)"
    Assert.Equal(mixed, grouped)
    Assert.NotEqual(mixed, andOr)
    let d = Expr.Term(ExprTerm.Word("d", None))
    let b = Expr.Term(ExprTerm.Word("b", None))
    let c = Expr.Term(ExprTerm.Word("c", None))
    Assert.Equal(Expr.Or(Expr.And(d, b), c), mixed)

[<Fact>]
let ``child AND descendant OR root evals as AND then OR`` () =
    let f = build ()
    let fromFile = answerOf f.graph f.file
    let mixed =
        nodeIds (evalOk f.graph fromFile "child AND descendant OR root")
    let grouped =
        nodeIds (evalOk f.graph fromFile "(child AND descendant) OR root")
    let andOr =
        nodeIds (evalOk f.graph fromFile "child AND (descendant OR root)")
    Assert.Equal<NodeId list>(mixed, grouped)
    Assert.Contains(f.graph.root, mixed)
    Assert.DoesNotContain(f.graph.root, andOr)

[<Fact>]
let ``mixed operand types across combinators is a type error`` () =
    let catalog = ExprPrimitive.catalog (Graph.create ())
    let err =
        match ExprCompile.inferType catalog "text OR root" with
        | Error e -> e
        | Ok _ -> failwith "expected type error"
    Assert.Equal("type error", err)
    match ExprCompile.inferType catalog "containing \"the\" AND text" with
    | Error e -> Assert.Equal("type error", e)
    | Ok _ -> failwith "expected type error"
