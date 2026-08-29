module ExprPipelineTests

open Gambol.Shared
open Xunit

type private Fixture =
    { graph: Graph
      file: NodeId
      theNode: NodeId
      otherNode: NodeId
      namedThe: NodeId
      outside: NodeId }

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
    let fileId = NodeId.New()
    let theId = NodeId.New()
    let otherId = NodeId.New()
    let namedTheId = NodeId.New()
    let otherWs = NodeId.New()
    let outsideId = NodeId.New()
    let graph =
        Graph.create ()
        |> addUnder Graph.workspacesId
            (specialNode wsId Workspace "ws" Graph.workspacesId)
        |> addUnder wsId (specialNode fileId File "f.fs" wsId)
        |> addUnder fileId
            (Node.Create(theId, text = "the cat sat", owner = fileId))
        |> addUnder fileId
            (Node.Create(otherId, text = "dog", owner = fileId))
        |> addUnder fileId
            (Node.Create(
                namedTheId,
                text = "hello",
                name = Filename.create "the",
                owner = fileId))
        |> addUnder Graph.workspacesId
            (specialNode otherWs Workspace "other" Graph.workspacesId)
        |> addUnder otherWs (specialNode outsideId File "out.fs" otherWs)
        |> addRef fileId outsideId
    { graph = graph
      file = fileId
      theNode = theId
      otherNode = otherId
      namedThe = namedTheId
      outside = outsideId }

let private evalOk graph input source =
    match ExprCompile.eval graph input source with
    | Ok answers -> answers
    | Error err -> failwith $"eval failed: {err}"

let private evalErr graph input source =
    match ExprCompile.eval graph input source with
    | Error err -> err
    | Ok _ -> failwith "expected eval error"

let private nodeIds (answers: ExprAnswer list) =
    answers
    |> List.map (function
        | ExprAnswer.Node n -> n.id
        | _ -> failwith "expected Node answer")

let private rootAnswer (graph: Graph) =
    ExprAnswer.Node graph.nodes.[graph.root]

[<Fact>]
let ``root descendant containing the is bind over Header text`` () =
    let f = build ()
    let found =
        nodeIds (evalOk f.graph (rootAnswer f.graph) "root descendant containing \"the\"")
    Assert.Contains(f.theNode, found)
    Assert.DoesNotContain(f.otherNode, found)
    Assert.DoesNotContain(f.namedThe, found)

[<Fact>]
let ``child equals colon-star; descendant follows Ref; tree matches star-star`` () =
    let f = build ()
    let fromFile = ExprAnswer.Node f.graph.nodes.[f.file]
    Assert.Equal<NodeId list>(
        nodeIds (evalOk f.graph fromFile "child"),
        nodeIds (evalOk f.graph fromFile ":*"))
    let desc = nodeIds (evalOk f.graph fromFile "descendant")
    Assert.Contains(f.outside, desc)
    let tree = nodeIds (evalOk f.graph fromFile "tree")
    let stars = nodeIds (evalOk f.graph fromFile "**")
    Assert.Equal<NodeId list>(tree, stars)
    Assert.DoesNotContain(f.outside, tree)

[<Fact>]
let ``spaced ws after double slash is parse error; slash-slash cluster is valid`` () =
    let f = build ()
    let input = rootAnswer f.graph
    Assert.Contains("missing argument", evalErr f.graph input "// ws")
    Assert.Equal<NodeId list>(
        nodeIds (evalOk f.graph input "root / \"ws\""),
        nodeIds (evalOk f.graph input "//ws"))

[<Fact>]
let ``unknown standalone word is a parse error`` () =
    let f = build ()
    let err = evalErr f.graph (rootAnswer f.graph) "nope"
    Assert.Contains("unknown word", err)

[<Fact>]
let ``owned is Owned Children; ref is Ref; child is those four in order`` () =
    let f = build ()
    let fromFile = ExprAnswer.Node f.graph.nodes.[f.file]
    Assert.Equal<NodeId list>(
        [ f.theNode; f.otherNode; f.namedThe ],
        nodeIds (evalOk f.graph fromFile "owned"))
    Assert.Equal<NodeId list>(
        [ f.outside ],
        nodeIds (evalOk f.graph fromFile "ref"))
    Assert.Equal<NodeId list>(
        [ f.theNode; f.otherNode; f.namedThe; f.outside ],
        nodeIds (evalOk f.graph fromFile "child"))
    Assert.Equal<NodeId list>(
        nodeIds (evalOk f.graph fromFile "child"),
        nodeIds (evalOk f.graph fromFile ":*"))

[<Fact>]
let ``owned is immediate only; owned child is not the partition`` () =
    let f = build ()
    let fromFile = ExprAnswer.Node f.graph.nodes.[f.file]
    let treeHits = nodeIds (evalOk f.graph fromFile "tree")
    Assert.Contains(f.theNode, treeHits)
    Assert.Equal<NodeId list>(
        [ f.theNode; f.otherNode; f.namedThe ],
        nodeIds (evalOk f.graph fromFile "owned"))
    Assert.DoesNotContain(f.outside, nodeIds (evalOk f.graph fromFile "owned"))
    Assert.Equal<NodeId list>([], nodeIds (evalOk f.graph fromFile "owned child"))

[<Fact>]
let ``owned OR ref concatenates and is not child when roles interleave`` () =
    let f = build ()
    let parentId = NodeId.New()
    let aId, bId, gId = NodeId.New(), NodeId.New(), NodeId.New()
    let graph =
        f.graph
        |> addUnder f.file (Node.Create(parentId, text = "mixed", owner = f.file))
        |> addUnder parentId (Node.Create(aId, text = "A", owner = parentId))
        |> addRef parentId f.outside
        |> addUnder parentId (Node.Create(bId, text = "B", owner = parentId))
        |> addUnder aId (Node.Create(gId, text = "G", owner = aId))
    let fromParent = ExprAnswer.Node graph.nodes.[parentId]
    Assert.Equal<NodeId list>(
        [ aId; f.outside; bId ],
        nodeIds (evalOk graph fromParent "child"))
    Assert.Equal<NodeId list>([ aId; bId ], nodeIds (evalOk graph fromParent "owned"))
    Assert.Equal<NodeId list>([ f.outside ], nodeIds (evalOk graph fromParent "ref"))
    Assert.Equal<NodeId list>(
        [ aId; bId; f.outside ],
        nodeIds (evalOk graph fromParent "owned OR ref"))
    Assert.DoesNotContain(gId, nodeIds (evalOk graph fromParent "owned"))
    Assert.Contains(gId, nodeIds (evalOk graph fromParent "tree"))
    Assert.Contains(f.outside, nodeIds (evalOk graph fromParent "descendant"))
    Assert.DoesNotContain(f.outside, nodeIds (evalOk graph fromParent "tree"))

[<Fact>]
let ``Unloaded input: child, owned, and ref miss and do not Load`` () =
    let f = build ()
    let unloadedId = NodeId.New()
    let unloaded =
        Node.Create(
            unloadedId,
            text = "hollow",
            owner = f.file,
            childrenStatus = Unloaded)
    let graph = addUnder f.file unloaded f.graph
    let input = ExprAnswer.Node graph.nodes.[unloadedId]
    Assert.Equal<NodeId list>([], nodeIds (evalOk graph input "child"))
    Assert.Equal<NodeId list>([], nodeIds (evalOk graph input "owned"))
    Assert.Equal<NodeId list>([], nodeIds (evalOk graph input "ref"))
    Assert.Equal(Unloaded, graph.nodes.[unloadedId].childrenStatus)

[<Fact>]
let ``ref and owned are lowercase Name tokens; Ref and Owned are unknown`` () =
    let f = build ()
    let input = rootAnswer f.graph
    Assert.Contains("unknown word", evalErr f.graph input "Ref")
    Assert.Contains("unknown word", evalErr f.graph input "Owned")
    Assert.Contains("a number is only valid", evalErr f.graph input "owned 3")
    match ExprCompile.evalOutcome f.graph input "text owned" with
    | ExprCompile.TypeFailed e -> Assert.Equal("type error", e)
    | other -> failwith $"expected type error, got {other}"
    evalOk f.graph input "owned" |> ignore
    evalOk f.graph input "ref" |> ignore
