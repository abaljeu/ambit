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

type private OuterFixture =
    { graph: Graph
      file: NodeId
      parentBlue: NodeId
      nestedBlue: NodeId
      clearParent: NodeId
      deepBlue: NodeId
      sibA: NodeId
      sibB: NodeId
      unloadedBlue: NodeId
      outsideBlue: NodeId }

let private addRef parentId targetId graph =
    let parent = graph.nodes.[parentId]
    let nodes =
        graph.nodes
        |> Map.add parentId
            { parent with
                children = parent.children @ [ ChildNode.reference targetId ] }
    Graph.fromNodes graph.root nodes

let private unnamed id text owner =
    Node.Create(id, text = text, owner = owner)

let private buildOuter () : OuterFixture =
    let fileId, parentBlueId, nestedBlueId = NodeId.New(), NodeId.New(), NodeId.New()
    let clearId, deepId, sibAId = NodeId.New(), NodeId.New(), NodeId.New()
    let sibBId, unloadedId, otherFileId = NodeId.New(), NodeId.New(), NodeId.New()
    let outsideId, wsId = NodeId.New(), NodeId.New()
    let graph =
        Graph.create ()
        |> addUnder Graph.workspacesId
            (specialNode wsId Workspace "ws" Graph.workspacesId)
        |> addUnder wsId (specialNode fileId File "f.fs" wsId)
        |> addUnder fileId (unnamed parentBlueId "blue parent" fileId)
        |> addUnder parentBlueId (unnamed nestedBlueId "blue nested" parentBlueId)
        |> addUnder fileId (unnamed clearId "clear" fileId)
        |> addUnder clearId (unnamed deepId "blue deep" clearId)
        |> addUnder fileId (unnamed sibAId "blue sib a" fileId)
        |> addUnder fileId (unnamed sibBId "blue sib b" fileId)
        |> addUnder fileId
            (Node.Create(
                unloadedId,
                text = "blue unloaded",
                owner = fileId,
                childrenStatus = Unloaded))
        |> addUnder wsId (specialNode otherFileId File "g.fs" wsId)
        |> addUnder otherFileId (unnamed outsideId "blue outside" otherFileId)
        |> addRef fileId outsideId
    { graph = graph
      file = fileId
      parentBlue = parentBlueId
      nestedBlue = nestedBlueId
      clearParent = clearId
      deepBlue = deepId
      sibA = sibAId
      sibB = sibBId
      unloadedBlue = unloadedId
      outsideBlue = outsideId }

let private containingBlue =
    Expr.Term(ExprTerm.Word("containing", Some "blue"))

[<Fact>]
let ``OUTER containing blue parses as combinator not bind`` () =
    Assert.Equal(Expr.Outer containingBlue, exprOk "OUTER containing \"blue\"")
    let root = Expr.Term(ExprTerm.Word("root", None))
    Assert.Equal(
        Expr.Pipe [ root; Expr.Outer containingBlue ],
        exprOk "root OUTER containing \"blue\"")
    match exprOk "outer containing \"blue\"" with
    | Expr.Term(ExprTerm.Word("outer", _))
    | Expr.Pipe(Expr.Term(ExprTerm.Word("outer", _)) :: _) -> ()
    | other -> failwith $"lowercase outer must be bind, got {other}"

[<Fact>]
let ``bare OUTER is a missing-operand parse error`` () =
    match ExprParse.parseExpr "OUTER" with
    | Error _ -> ()
    | Ok expr -> failwith $"expected parse error, got {expr}"
    match ExprParse.parseExpr "NOT" with
    | Error _ -> ()
    | Ok expr -> failwith $"expected parse error, got {expr}"
    let named = Expr.Term(ExprTerm.Word("named", Some "x"))
    let inner = Expr.And(containingBlue, named)
    Assert.Equal(
        Expr.Outer inner,
        exprOk "OUTER (containing \"blue\" AND named \"x\")")

[<Fact>]
let ``OUTER prunes descendants of a match and keeps match under non-match`` () =
    let f = buildOuter ()
    let fromFile = answerOf f.graph f.file
    let found = nodeIds (evalOk f.graph fromFile "OUTER containing \"blue\"")
    Assert.Contains(f.parentBlue, found)
    Assert.DoesNotContain(f.nestedBlue, found)
    Assert.DoesNotContain(f.clearParent, found)
    Assert.Contains(f.deepBlue, found)
    Assert.Contains(f.sibA, found)
    Assert.Contains(f.sibB, found)

[<Fact>]
let ``OUTER is Owned only, strictly below, Unloaded miss, not a tree prune`` () =
    let f = buildOuter ()
    let fromFile = answerOf f.graph f.file
    let outer = nodeIds (evalOk f.graph fromFile "OUTER containing \"blue\"")
    Assert.DoesNotContain(f.outsideBlue, outer)
    Assert.Contains(f.unloadedBlue, outer)
    Assert.Equal(Unloaded, f.graph.nodes.[f.unloadedBlue].childrenStatus)
    Assert.DoesNotContain(f.file, outer)
    let treeHits =
        nodeIds (evalOk f.graph fromFile "tree containing \"blue\"")
    Assert.Contains(f.nestedBlue, treeHits)
    Assert.Contains(f.parentBlue, treeHits)
    let stars = nodeIds (evalOk f.graph fromFile "**")
    let tree = nodeIds (evalOk f.graph fromFile "tree")
    Assert.Equal<NodeId list>(tree, stars)

[<Fact>]
let ``OUTER re and rei operands match Header like containing`` () =
    let f = buildOuter ()
    let fromFile = answerOf f.graph f.file
    let withContaining =
        nodeIds (evalOk f.graph fromFile "OUTER containing \"blue\"")
    Assert.Equal<NodeId list>(
        withContaining,
        nodeIds (evalOk f.graph fromFile "OUTER re \".*blue.*\""))
    Assert.Equal<NodeId list>(
        withContaining,
        nodeIds (evalOk f.graph fromFile "OUTER rei \".*BLUE.*\""))
