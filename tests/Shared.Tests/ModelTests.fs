module ModelTests

open Gambol.Shared
open Xunit

let private owned (ids: NodeId list) : ChildNode list =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private assertValidOwnership (graph: Graph) =
    let allChildren =
        graph.nodes
        |> Map.toList
        |> List.collect (fun (parentId, node) ->
            node.children |> List.map (fun child -> parentId, child))

    let allChildIds = allChildren |> List.map (fun (_, child) -> child.id) |> Set.ofList

    let ownerByChildId =
        allChildren
        |> List.choose (fun (parentId, child) ->
            match child.ref with
            | Ownership.Owner -> Some(child.id, parentId)
            | Ownership.Ref -> None)
        |> List.groupBy fst
        |> List.map (fun (childId, pairs) -> childId, (pairs |> List.map snd))
        |> Map.ofList

    // Every referenced node id must have exactly one owner occurrence.
    for childId in allChildIds do
        let owners = ownerByChildId |> Map.tryFind childId |> Option.defaultValue []
        Assert.True(
            owners.Length = 1,
            $"Expected exactly one owner for {childId}, got {owners.Length}"
        )

    let ownerParentOf childId = ownerByChildId.[childId] |> List.head

    // Owner-parent chain must trace to root without cycles.
    let rec reachesRootWithoutCycle (startId: NodeId) (currentId: NodeId) (visited: Set<NodeId>) =
        if currentId = graph.root then
            true
        elif Set.contains currentId visited then
            false
        elif ownerByChildId |> Map.containsKey currentId then
            let parentId = ownerParentOf currentId
            reachesRootWithoutCycle startId parentId (Set.add currentId visited)
        else
            false

    for childId in allChildIds do
        let owner = ownerParentOf childId
        Assert.True(
            reachesRootWithoutCycle childId owner Set.empty,
            $"Owner chain for {childId} does not trace to root without cycles"
        )

[<Fact>]
let ``Create graph has one node`` () =
    let graph = Graph.create ()
    Assert.Equal(1, Graph.nodeCount graph)
    assertValidOwnership graph

[<Fact>]
let ``Root node exists in graph`` () =
    let graph = Graph.create ()
    Assert.True(Graph.contains graph.root graph)
    assertValidOwnership graph

[<Fact>]
let ``New node increments node count`` () =
    let graph0 = Graph.create ()
    let count0 = Graph.nodeCount graph0
    let graph1, _nodeId = Graph.newNode "hello" graph0
    Assert.Equal(count0 + 1, Graph.nodeCount graph1)
    Assert.Equal(0, graph1.nodes[graph1.root].children.Length)
    assertValidOwnership graph1

[<Fact>]
let ``Set text on canonical root is rejected`` () =
    let graph = Graph.create ()
    let result = Graph.setText graph.root "ROOT" "hello" graph
    Assert.True(Result.isError result)

[<Fact>]
let ``Set text updates non-root node when old matches`` () =
    let graph0 = Graph.create ()
    let graph1, childId = Graph.newNode "hello" graph0

    let graph2 = Graph.replace graph1.root 0 [] (owned [ childId ]) graph1 |> requireOk "replace"

    let result = Graph.setText childId "hello" "bye" graph2

    match result with
    | Ok graph3 -> Assert.Equal("bye", graph3.nodes[childId].text)
    | Error err -> Assert.True(false, $"Expected Ok, got Error: {err}")

[<Fact>]
let ``Replace can insert children into root`` () =
    let graph0 = Graph.create ()
    let graph1, (ids : NodeId list) = ModelBuilder.createNodes [ "a"; "b"; "c" ] graph0
    let result = Graph.replace graph1.root 0 [] (owned ids) graph1

    match result with
    | Ok (graph2 : Graph) -> 
        let children = graph2.nodes[graph2.root].children
        let childIds : NodeId list = children |> List.map (fun child -> child.id)
        Assert.Equal<NodeId>(ids, childIds)
        Assert.All<ChildNode>(children, fun child -> Assert.Equal(Ownership.Owner, child.ref))
        assertValidOwnership graph2
    | Error err -> Assert.True(false, $"Expected Ok, got Error: {err}")

[<Fact>]
let ``Replace can insert duplicate id with owner then ref`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "shared" ] graph0
    let shared = ids |> List.head
    let children =
        [ { ref = Ownership.Owner; id = shared }
          { ref = Ownership.Ref; id = shared } ]

    let result = Graph.replace graph1.root 0 [] children graph1

    match result with
    | Ok graph2 ->
        let inserted = graph2.nodes[graph2.root].children
        Assert.Equal<ChildNode>(children, inserted)
        assertValidOwnership graph2
        Assert.Equal(Some graph2.root, Graph.owner graph2 (Some shared))
    | Error err -> Assert.True(false, $"Expected Ok, got Error: {err}")

[<Fact>]
let ``Graph navigation owner first last on flat tree`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "a"; "b"; "c" ] graph0

    let graph2 =
        Graph.replace graph1.root 0 [] (owned ids) graph1
        |> ModelBuilder.requireOk "nav.replace"

    assertValidOwnership graph2
    Assert.Equal(None, Graph.owner graph2 (Some graph2.root))
    Assert.Equal(None, Graph.owner graph2 None)
    Assert.Equal(Some ids[0], Graph.nodeFirstChild graph2 (Some graph2.root))
    Assert.Equal(Some ids[2], Graph.nodeLastChild graph2 (Some graph2.root))
    Assert.Equal(Some graph2.root, Graph.owner graph2 (Some ids[0]))
    Assert.Equal(Some graph2.root, Graph.owner graph2 (Some ids[1]))
    Assert.Equal(None, Graph.nodeFirstChild graph2 (Some ids[0]))
    Assert.Equal(None, Graph.nodeLastChild graph2 (Some ids[0]))

[<Fact>]
let ``NodeNav owner composes like Graph.owner`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "x"; "y" ] graph0

    let graph2 =
        Graph.replace graph1.root 0 [] (owned ids) graph1
        |> ModelBuilder.requireOk "nodenav.replace"

    let mid = ids[1]
    let fromNav =
        Node.at graph2 (Some mid) |> Node.owner |> Node.current

    Assert.Equal(Graph.owner graph2 (Some mid), fromNav)

let private tryFindParentAndIndexScan (targetId: NodeId) (graph: Graph) =
    graph.nodes
    |> Map.toSeq
    |> Seq.tryPick (fun (parentId, parent) ->
        parent.children
        |> List.tryFindIndex (fun child -> child.id = targetId)
        |> Option.map (fun index -> parentId, index))

let private ownerScan (graph: Graph) (id: NodeId option) =
    id
    |> Option.bind (fun nid ->
        graph.nodes
        |> Map.toSeq
        |> Seq.tryPick (fun (parentId, parent) ->
            parent.children
            |> List.tryPick (fun child ->
                if child.id = nid && child.ref = Ownership.Owner then
                    Some parentId
                else
                    None)))

[<Fact>]
let ``Graph parent indexes match linear scans`` () =
    let graph = ModelBuilder.createDag12 ()
    graph.nodes
    |> Map.iter (fun nid _ ->
        Assert.Equal(tryFindParentAndIndexScan nid graph, Map.tryFind nid graph.parentByChild))
    let childIds =
        graph.nodes
        |> Map.values
        |> Seq.collect (fun n -> n.children |> Seq.map (fun c -> c.id))
        |> Set.ofSeq
    for cid in childIds do
        Assert.Equal(ownerScan graph (Some cid), Map.tryFind cid graph.ownerParentByChild)

[<Fact>]
let ``Graph fromNodes is idempotent on shared-node graph`` () =
    let g = ModelBuilder.createSharedNodeGraph ()
    let g2 = Graph.fromNodes g.root g.nodes
    Assert.Equal<Map<NodeId, NodeId * int>>(g.parentByChild, g2.parentByChild)
    Assert.Equal<Map<NodeId, NodeId>>(g.ownerParentByChild, g2.ownerParentByChild)

// For Graph.replace node index oldList newList -> Result

// replace: when old span matches, parent children are updated
// replace: node count does not change
// replace: errors when parent node id is missing
// replace: errors when any new child id is missing
// replace: errors when index is out of bounds
// replace: errors when old span does not match existing children at index
// replace: supports insert (old ids empty) at index
// replace: supports delete (new ids empty) at index
