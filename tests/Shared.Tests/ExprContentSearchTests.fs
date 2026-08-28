module ExprContentSearchTests

open Gambol.Shared
open Xunit

type private Fixture =
    { graph: Graph
      d: NodeId
      e: NodeId
      hidden: NodeId
      trapped: NodeId
      throughE: NodeId
      fileE: NodeId
      outsideE: NodeId
      a: NodeId
      b: NodeId
      c: NodeId
      c2: NodeId }

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

let private addRef parentId targetId graph =
    let parent = graph.nodes.[parentId]
    let nodes =
        graph.nodes
        |> Map.add parentId
            { parent with
                children = parent.children @ [ ChildNode.reference targetId ] }
    Graph.fromNodes graph.root nodes

let private build () : Fixture =
    let dId = NodeId.New()
    let eId = NodeId.New()
    let hiddenId = NodeId.New()
    let skipId = NodeId.New()
    let trappedId = NodeId.New()
    let unnamedId = NodeId.New()
    let throughId = NodeId.New()
    let nestedFileId = NodeId.New()
    let fileEId = NodeId.New()
    let otherId = NodeId.New()
    let outsideEId = NodeId.New()
    let aId = NodeId.New()
    let bId = NodeId.New()
    let cId = NodeId.New()
    let otherNamedId = NodeId.New()
    let c2Id = NodeId.New()
    let graph =
        Graph.create ()
        |> addUnder Graph.workspacesId
            (specialNode dId Directory "d" Graph.workspacesId)
        |> addUnder dId (namedNormal eId "e" dId)
        |> addUnder eId (namedNormal hiddenId "e" eId)
        |> addUnder dId (namedNormal skipId "skip" dId)
        |> addUnder skipId (namedNormal trappedId "e" skipId)
        |> addUnder dId (Node.Create(unnamedId, text = "unnamed", owner = dId))
        |> addUnder unnamedId (namedNormal throughId "e" unnamedId)
        |> addUnder dId (specialNode nestedFileId File "nested.fs" dId)
        |> addUnder nestedFileId (namedNormal fileEId "e" nestedFileId)
        |> addUnder Graph.workspacesId
            (specialNode otherId Workspace "other" Graph.workspacesId)
        |> addUnder otherId (namedNormal outsideEId "e" otherId)
        |> addRef dId eId
        |> addRef dId outsideEId
        |> addUnder Graph.workspacesId (specialNode aId File "a" Graph.workspacesId)
        |> addUnder aId (namedNormal bId "b" aId)
        |> addUnder bId (namedNormal cId "c" bId)
        |> addUnder aId (namedNormal otherNamedId "x" aId)
        |> addUnder otherNamedId (namedNormal c2Id "c" otherNamedId)
    { graph = graph
      d = dId
      e = eId
      hidden = hiddenId
      trapped = trappedId
      throughE = throughId
      fileE = fileEId
      outsideE = outsideEId
      a = aId
      b = bId
      c = cId
      c2 = c2Id }

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
let ``d hash e equals slash d hash e`` () =
    let f = build ()
    let input = rootAnswer f.graph
    let clustered = evalOk f.graph input "d#e"
    let spaced = evalOk f.graph input "/ \"d\" # \"e\""
    Assert.Equal<NodeId list>(nodeIds clustered, nodeIds spaced)
    Assert.Contains(f.e, nodeIds clustered)
    Assert.Contains(f.throughE, nodeIds clustered)

[<Fact>]
let ``named match and non-match wall; unnamed is transparent`` () =
    let f = build ()
    let fromD = ExprAnswer.Node f.graph.nodes.[f.d]
    let found = nodeIds (evalOk f.graph fromD "#e")
    Assert.Contains(f.e, found)
    Assert.Contains(f.throughE, found)
    Assert.DoesNotContain(f.hidden, found)
    Assert.DoesNotContain(f.trapped, found)
    Assert.DoesNotContain(f.fileE, found)

[<Fact>]
let ``hash follows Ref and dedupes Node identity`` () =
    let f = build ()
    let fromD = ExprAnswer.Node f.graph.nodes.[f.d]
    let found = nodeIds (evalOk f.graph fromD "#e")
    let eCount = found |> List.filter ((=) f.e) |> List.length
    Assert.Equal(1, eCount)
    Assert.Contains(f.outsideE, found)

[<Fact>]
let ``chained hash searches below prior Answers`` () =
    let f = build ()
    let input = rootAnswer f.graph
    let found = nodeIds (evalOk f.graph input "a#b#c")
    Assert.Equal<NodeId list>([ f.c ], found)
    Assert.DoesNotContain(f.c2, found)
