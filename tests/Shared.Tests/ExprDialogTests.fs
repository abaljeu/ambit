module ExprDialogTests

open Gambol.Shared
open Xunit

type private Fixture =
    { graph: Graph
      theNode: NodeId
      other: NodeId }

let private addUnder parentId child graph =
    let parent = graph.nodes.[parentId]
    let nodes =
        graph.nodes
        |> Map.add child.id child
        |> Map.add parentId
            { parent with children = parent.children @ [ ChildNode.owner child.id ] }
    Graph.fromNodes graph.root nodes

let private build () : Fixture =
    let theId = NodeId.New()
    let otherId = NodeId.New()
    let graph =
        Graph.create ()
        |> addUnder Graph.workspacesId
            (Node.Create(theId, text = "the cat", owner = Graph.workspacesId))
        |> addUnder Graph.workspacesId
            (Node.Create(otherId, text = "dog", owner = Graph.workspacesId))
    { graph = graph
      theNode = theId
      other = otherId }

let private hitIds results =
    results |> List.map (fun r -> r.nodeId)

[<Fact>]
let ``leading equals Search lists Nodes under zoomRoot`` () =
    let f = build ()
    let hits =
        ViewModelSearch.searchNodes
            "= root descendant containing \"the\""
            f.graph.root
            f.graph
    Assert.Equal<NodeId list>([ f.theNode ], hitIds hits)

[<Fact>]
let ``Move equals uses the same Answer set as Search`` () =
    let f = build ()
    let query = "= root descendant containing \"the\""
    let search =
        ViewModelSearch.searchNodes query f.graph.root f.graph
    let move =
        ExprDialog.tryHits query f.graph.root f.graph
        |> Option.defaultValue []
    Assert.Equal<NodeId list>(hitIds search, hitIds move)

[<Fact>]
let ``Node to Text Expression shows no hits`` () =
    let f = build ()
    match ExprDialog.tryHits "= root text" f.graph.root f.graph with
    | Some [] -> ()
    | other -> failwith $"expected no hits, got {other}"

[<Fact>]
let ``line without leading equals stays word search`` () =
    let f = build ()
    Assert.Equal(None, ExprDialog.tryHits "the cat" f.graph.root f.graph)
    let word =
        ViewModelSearch.searchNodes "the cat" f.graph.root f.graph
    Assert.Contains(f.theNode, hitIds word)
    Assert.DoesNotContain(f.other, hitIds word)
