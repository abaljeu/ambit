module ExprFilterTests

open Gambol.Shared
open Xunit

type private Fixture =
    { graph: Graph
      ws: NodeId
      dirD: NodeId
      fileD: NodeId
      blue: NodeId
      childBlue: NodeId
      headed: NodeId }

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
    let blueId = NodeId.New()
    let childBlueId = NodeId.New()
    let headedId = NodeId.New()
    let graph =
        Graph.create ()
        |> addUnder Graph.workspacesId
            (specialNode wsId Workspace "ws" Graph.workspacesId)
        |> addUnder wsId (specialNode dirId Directory "d" wsId)
        |> addUnder wsId (specialNode fileId File "d" wsId)
        |> addUnder fileId (namedNormal blueId "blue" fileId)
        |> addUnder blueId (namedNormal childBlueId "blue" blueId)
        |> addUnder fileId
            (Node.Create(
                headedId,
                text = "the heading",
                name = Filename.create "red",
                cssClasses = CssClass.ofList [ "h1"; "blue" ],
                owner = fileId))
    { graph = graph
      ws = wsId
      dirD = dirId
      fileD = fileId
      blue = blueId
      childBlue = childBlueId
      headed = headedId }

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

[<Fact>]
let ``named keeps a matching Normal and does not walk Children`` () =
    let f = build ()
    let fromBlue = answerOf f.graph f.blue
    Assert.Equal<NodeId list>(
        [ f.blue ],
        nodeIds (evalOk f.graph fromBlue "named \"blue\""))
    Assert.Equal<NodeId list>([], nodeIds (evalOk f.graph fromBlue "named \"red\""))
    Assert.Equal<NodeId list>(
        [],
        nodeIds (evalOk f.graph (answerOf f.graph f.fileD) "named \"blue\""))
    Assert.DoesNotContain(
        f.childBlue,
        nodeIds (evalOk f.graph fromBlue "named \"blue\""))

[<Fact>]
let ``named is not content search`` () =
    let f = build ()
    let fromFile = answerOf f.graph f.fileD
    Assert.Equal<NodeId list>([], nodeIds (evalOk f.graph fromFile "named \"blue\""))
    Assert.Contains(f.blue, nodeIds (evalOk f.graph fromFile "#blue"))

[<Fact>]
let ``root ws equals root; slash d dir keeps Directory only`` () =
    let f = build ()
    let input = rootAnswer f.graph
    Assert.Equal<NodeId list>(
        nodeIds (evalOk f.graph input "root"),
        nodeIds (evalOk f.graph input "root ws"))
    let fromWs = answerOf f.graph f.ws
    Assert.Equal<NodeId list>(
        [ f.dirD ],
        nodeIds (evalOk f.graph fromWs "/ \"d\" dir"))
    Assert.Equal<NodeId list>(
        [ f.fileD ],
        nodeIds (evalOk f.graph fromWs "/ \"d\" file"))
    Assert.Equal<NodeId list>(
        [ f.blue ],
        nodeIds (evalOk f.graph (answerOf f.graph f.blue) "normal"))

[<Fact>]
let ``class keeps exact cssClasses membership`` () =
    let f = build ()
    let fromHeaded = answerOf f.graph f.headed
    Assert.Equal<NodeId list>(
        [ f.headed ],
        nodeIds (evalOk f.graph fromHeaded "class \"h1\""))
    Assert.Equal<NodeId list>([], nodeIds (evalOk f.graph fromHeaded "class \"H1\""))
    Assert.Equal<NodeId list>([], nodeIds (evalOk f.graph fromHeaded "class \"h\""))
    Assert.Equal<NodeId list>(
        [],
        nodeIds (evalOk f.graph (answerOf f.graph f.blue) "class \"h1\""))

[<Fact>]
let ``re matches Header like containing for same-case substring`` () =
    let f = build ()
    let fromHeaded = answerOf f.graph f.headed
    let fromBlue = answerOf f.graph f.blue
    Assert.Equal<NodeId list>(
        nodeIds (evalOk f.graph fromHeaded "containing \"the\""),
        nodeIds (evalOk f.graph fromHeaded "re \".*the.*\""))
    Assert.Equal<NodeId list>(
        [ f.blue ],
        nodeIds (evalOk f.graph fromBlue "re \".*blue.*\""))
    Assert.Equal<NodeId list>(
        [],
        nodeIds (evalOk f.graph fromHeaded "re \".*red.*\""))

[<Fact>]
let ``re is case-sensitive; rei uses engine ignore-case`` () =
    let f = build ()
    let fromHeaded = answerOf f.graph f.headed
    Assert.Equal<NodeId list>(
        [],
        nodeIds (evalOk f.graph fromHeaded "re \".*THE.*\""))
    Assert.Equal<NodeId list>(
        [ f.headed ],
        nodeIds (evalOk f.graph fromHeaded "rei \".*THE.*\""))
    Assert.Equal<NodeId list>(
        [ f.headed ],
        nodeIds (evalOk f.graph fromHeaded "containing \"THE\""))

[<Fact>]
let ``invalid re pattern is a miss`` () =
    let f = build ()
    let fromHeaded = answerOf f.graph f.headed
    Assert.Equal<NodeId list>(
        [],
        nodeIds (evalOk f.graph fromHeaded "re \"(\""))
    Assert.Equal<NodeId list>(
        [],
        nodeIds (evalOk f.graph fromHeaded "rei \"(\""))
