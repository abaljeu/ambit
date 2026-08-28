module ExprStructuralEvalTests

open Gambol.Shared
open Xunit

type private Fixture =
    { graph: Graph
      ws: NodeId
      x: NodeId
      y: NodeId
      aFs: NodeId
      abFs: NodeId
      outside: NodeId
      childA: NodeId
      childB: NodeId
      childC: NodeId }

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

let private addRef parentId targetId graph =
    let parent = graph.nodes.[parentId]
    let nodes =
        graph.nodes
        |> Map.add parentId
            { parent with
                children = parent.children @ [ ChildNode.reference targetId ] }
    Graph.fromNodes graph.root nodes

let private build () : Fixture =
    let wsId = NodeId.New()
    let xId = NodeId.New()
    let yId = NodeId.New()
    let aId = NodeId.New()
    let abId = NodeId.New()
    let outsideId = NodeId.New()
    let otherId = NodeId.New()
    let childA = NodeId.New()
    let childB = NodeId.New()
    let childC = NodeId.New()
    let graph =
        Graph.create ()
        |> addUnder Graph.workspacesId
            (specialNode wsId Workspace "ws" Graph.workspacesId)
        |> addUnder wsId (specialNode xId Directory "x" wsId)
        |> addUnder wsId (specialNode yId Directory "y" wsId)
        |> addUnder xId (specialNode aId File "a.fs" xId)
        |> addUnder xId (specialNode abId File "ab.fs" xId)
        |> addUnder Graph.workspacesId
            (specialNode otherId Workspace "other" Graph.workspacesId)
        |> addUnder otherId (specialNode outsideId File "outside.fs" otherId)
        |> addRef xId outsideId
        |> addUnder aId (Node.Create(childA, text = "A", owner = aId))
        |> addUnder aId (Node.Create(childB, text = "B", owner = aId))
        |> addUnder aId (Node.Create(childC, text = "C", owner = aId))
    { graph = graph
      ws = wsId
      x = xId
      y = yId
      aFs = aId
      abFs = abId
      outside = outsideId
      childA = childA
      childB = childB
      childC = childC }

let private evalOk graph input source =
    match ExprCompile.eval graph input source with
    | Ok answers -> answers
    | Error err -> failwith $"eval failed: {err}"

let private nodeIds (answers: ExprAnswer list) =
    answers
    |> List.map (function
        | ExprAnswer.Node n -> n.id
        | _ -> failwith "expected Node answer")

let private rootAnswer (graph: Graph) =
    ExprAnswer.Node graph.nodes.[graph.root]

[<Fact>]
let ``root slash ws slash x equals double-slash cluster`` () =
    let f = build ()
    let input = rootAnswer f.graph
    let spaced = evalOk f.graph input "root / \"ws\" / \"x\""
    let cluster = evalOk f.graph input "//ws/x"
    Assert.Equal<NodeId list>([ f.x ], nodeIds spaced)
    Assert.Equal<NodeId list>(nodeIds spaced, nodeIds cluster)

[<Fact>]
let ``structural slash does not enter directory or workspace children`` () =
    let f = build ()
    let input = rootAnswer f.graph
    let fromRoot = evalOk f.graph input "root / \"x\""
    Assert.Equal<NodeId list>([], nodeIds fromRoot)
    let chained = evalOk f.graph input "//ws/x"
    Assert.Equal<NodeId list>([ f.x ], nodeIds chained)

[<Fact>]
let ``star-star matches tree and does not follow Ref`` () =
    let f = build ()
    let fromWs = ExprAnswer.Node f.graph.nodes.[f.ws]
    let stars = evalOk f.graph fromWs "**"
    let tree = evalOk f.graph fromWs "tree"
    Assert.Equal<NodeId list>(nodeIds stars, nodeIds tree)
    Assert.DoesNotContain(f.outside, nodeIds stars)
    let desc = ExprWalk.descendantAnswers f.graph fromWs
    Assert.Contains(f.outside, nodeIds desc)
    Assert.NotEqual<NodeId list>(nodeIds stars, nodeIds desc)

[<Fact>]
let ``glob star matches and question mark is literal`` () =
    let f = build ()
    let fromX = ExprAnswer.Node f.graph.nodes.[f.x]
    let star = evalOk f.graph fromX "/ \"a*.fs\""
    Assert.Equal<NodeId list>([ f.aFs; f.abFs ], nodeIds star)
    let question = evalOk f.graph fromX "/ \"a?.fs\""
    Assert.Equal<NodeId list>([], nodeIds question)

[<Fact>]
let ``caret dot and wsroot walk the Owned chain`` () =
    let f = build ()
    let fromFile = ExprAnswer.Node f.graph.nodes.[f.aFs]
    Assert.Equal<NodeId list>([ f.aFs ], nodeIds (evalOk f.graph fromFile "^"))
    Assert.Equal<NodeId list>([ f.x ], nodeIds (evalOk f.graph fromFile "."))
    Assert.Equal<NodeId list>([ f.ws ], nodeIds (evalOk f.graph fromFile "wsroot"))

[<Fact>]
let ``colon indexes Children and bang indexes Owned siblings`` () =
    let f = build ()
    let fromFile = ExprAnswer.Node f.graph.nodes.[f.aFs]
    let fromB = ExprAnswer.Node f.graph.nodes.[f.childB]
    Assert.Equal<NodeId list>(
        [ f.childA; f.childB; f.childC ],
        nodeIds (evalOk f.graph fromFile ":*"))
    Assert.Equal<NodeId list>([ f.childB ], nodeIds (evalOk f.graph fromFile ":1"))
    Assert.Equal<NodeId list>([ f.childB ], nodeIds (evalOk f.graph fromB "!0"))
    Assert.Equal<NodeId list>([ f.childC ], nodeIds (evalOk f.graph fromB "!1"))
    Assert.Equal<NodeId list>(
        [ f.childA; f.childB; f.childC ],
        nodeIds (evalOk f.graph fromB "!*"))
