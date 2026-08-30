module ExprSectionTests

open Gambol.Shared
open Xunit

type private Fixture =
    { graph: Graph
      ws: NodeId
      dirD: NodeId
      fileD: NodeId
      todo: NodeId
      childTodo: NodeId
      unnamed: NodeId
      throughTodo: NodeId
      blue: NodeId }

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
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let fileId = NodeId.New()
    let todoId = NodeId.New()
    let childTodoId = NodeId.New()
    let unnamedId = NodeId.New()
    let throughId = NodeId.New()
    let blueId = NodeId.New()
    let graph =
        Graph.create ()
        |> addUnder Graph.workspacesId
            (specialNode wsId Workspace "ws" Graph.workspacesId)
        |> addUnder wsId (specialNode dirId Directory "d" wsId)
        |> addUnder wsId (specialNode fileId File "d" wsId)
        |> addUnder fileId (namedNormal todoId "todo" fileId)
        |> addUnder todoId (namedNormal childTodoId "todo" todoId)
        |> addUnder fileId
            (Node.Create(unnamedId, text = "unnamed", owner = fileId))
        |> addUnder unnamedId (namedNormal throughId "todo" unnamedId)
        |> addUnder fileId (namedNormal blueId "blue" fileId)
    { graph = graph
      ws = wsId
      dirD = dirId
      fileD = fileId
      todo = todoId
      childTodo = childTodoId
      unnamed = unnamedId
      throughTodo = throughId
      blue = blueId }

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

let private parseFailed graph source =
    match ExprCompile.evalOutcome graph (answerOf graph graph.root) source with
    | ExprCompile.ParseFailed e -> e
    | other -> failwith $"expected parse fail, got {other}"

[<Fact>]
let ``section keeps a named Normal and yields nothing otherwise`` () =
    let f = build ()
    Assert.Equal<NodeId list>(
        [ f.todo ],
        nodeIds (evalOk f.graph (answerOf f.graph f.todo) "section"))
    Assert.Equal<NodeId list>(
        [],
        nodeIds (evalOk f.graph (answerOf f.graph f.unnamed) "section"))
    Assert.Equal<NodeId list>(
        [],
        nodeIds (evalOk f.graph (answerOf f.graph f.fileD) "section"))
    Assert.Equal<NodeId list>(
        [],
        nodeIds (evalOk f.graph (answerOf f.graph f.dirD) "section"))
    Assert.Equal<NodeId list>(
        [],
        nodeIds (evalOk f.graph (answerOf f.graph f.ws) "section"))
    Assert.DoesNotContain(
        f.childTodo,
        nodeIds (evalOk f.graph (answerOf f.graph f.todo) "section"))

[<Fact>]
let ``subsection quoted todo equals hash todo`` () =
    let f = build ()
    let fromFile = answerOf f.graph f.fileD
    Assert.Equal<NodeId list>(
        nodeIds (evalOk f.graph fromFile "#todo"),
        nodeIds (evalOk f.graph fromFile "subsection \"todo\""))

[<Fact>]
let ``bare subsection is missing argument like bare hash`` () =
    let f = build ()
    Assert.Contains("missing argument", parseFailed f.graph "#")
    Assert.Contains("missing argument", parseFailed f.graph "subsection")

[<Fact>]
let ``named glob stays distinct from section`` () =
    let f = build ()
    let fromBlue = answerOf f.graph f.blue
    Assert.Equal<NodeId list>(
        [ f.blue ],
        nodeIds (evalOk f.graph fromBlue "named \"blue\""))
    Assert.Equal<NodeId list>(
        [ f.blue ],
        nodeIds (evalOk f.graph fromBlue "section"))
    Assert.Equal<NodeId list>(
        [],
        nodeIds (evalOk f.graph (answerOf f.graph f.todo) "named \"blue\""))
    Assert.Equal<NodeId list>(
        [ f.todo ],
        nodeIds (evalOk f.graph (answerOf f.graph f.todo) "section"))
