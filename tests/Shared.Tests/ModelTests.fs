module ModelTests

open Gambol.Shared
open SpecialNodeTestHelpers
open SpecialNodeTestHelpers
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
let ``Create graph has canonical root workspace only`` () =
    let graph = Graph.create ()
    Assert.Equal(0, userNodeCount graph)
    match graph.nodes.[graph.root].kind with
    | Special Workspace -> ()
    | _ -> Assert.True(false, "root must have kind = Special Workspace")
    assertValidOwnership graph

[<Fact>]
let ``Root node exists in graph`` () =
    let graph = Graph.create ()
    Assert.True(Graph.contains graph.root graph)
    assertValidOwnership graph

[<Fact>]
let ``New node increments node count`` () =
    let graph0 = Graph.create ()
    let count0 = userNodeCount graph0
    let graph1, _nodeId = Graph.newNode "hello" graph0
    Assert.Equal(count0 + 1, userNodeCount graph1)
    // Root children should still be empty of user nodes.
    Assert.Empty(userRootChildren graph1)
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
let ``Set text updates updateTime`` () =
    let graph0 = Graph.create ()
    let graph1, childId = Graph.newNode "hello" graph0
    let graph2 = Graph.replace graph1.root 0 [] (owned [ childId ]) graph1 |> requireOk "replace"
    let before = graph2.nodes.[childId].updateTime
    let graph3 = Graph.setText childId "hello" "bye" graph2 |> requireOk "setText"
    Assert.True(graph3.nodes.[childId].updateTime > before)

[<Fact>]
let ``Replace can insert children into non-root node`` () =
    let graph0 = Graph.create ()
    let graph1, contIds = ModelBuilder.createNodes [ "container" ] graph0
    let cont = contIds.[0]
    let graph2 = Graph.replace graph1.root 0 [] (owned [ cont ]) graph1 |> requireOk "add cont"
    let graph3, (ids : NodeId list) = ModelBuilder.createNodes [ "a"; "b"; "c" ] graph2
    let result = Graph.replace cont 0 [] (owned ids) graph3

    match result with
    | Ok (graph4 : Graph) ->
        let children = graph4.nodes[cont].children
        let childIds : NodeId list = children |> List.map (fun child -> child.id)
        Assert.Equal<NodeId>(ids, childIds)
        Assert.All<ChildNode>(children, fun child -> Assert.Equal(Ownership.Owner, child.ref))
        assertValidOwnership graph4
    | Error err -> Assert.True(false, $"Expected Ok, got Error: {err}")

[<Fact>]
let ``Replace can insert duplicate id with owner then ref`` () =
    let graph0 = Graph.create ()
    let graph1, contIds = ModelBuilder.createNodes [ "container" ] graph0
    let cont = contIds.[0]
    let graph2 = Graph.replace graph1.root 0 [] (owned [ cont ]) graph1 |> requireOk "add cont"
    let graph3, ids = ModelBuilder.createNodes [ "shared" ] graph2
    let shared = ids |> List.head
    let children =
        [ { ref = Ownership.Owner; id = shared }
          { ref = Ownership.Ref; id = shared } ]

    let result = Graph.replace cont 0 [] children graph3

    match result with
    | Ok graph4 ->
        let inserted = graph4.nodes[cont].children
        Assert.Equal<ChildNode>(children, inserted)
        assertValidOwnership graph4
        Assert.Equal(Some cont, Graph.owner graph4 (Some shared))
    | Error err -> Assert.True(false, $"Expected Ok, got Error: {err}")

[<Fact>]
let ``Graph navigation owner first last on flat tree`` () =
    let graph0 = Graph.create ()
    let graph1, contIds = ModelBuilder.createNodes [ "container" ] graph0
    let cont = contIds.[0]
    let graph2 = Graph.replace graph1.root 0 [] (owned [ cont ]) graph1 |> ModelBuilder.requireOk "nav.cont"
    let graph3, ids = ModelBuilder.createNodes [ "a"; "b"; "c" ] graph2

    let graph4 =
        Graph.replace cont 0 [] (owned ids) graph3
        |> ModelBuilder.requireOk "nav.replace"

    assertValidOwnership graph4
    Assert.Equal(None, Graph.owner graph4 (Some graph4.root))
    Assert.Equal(None, Graph.owner graph4 None)
    Assert.Equal(Some ids[0], Graph.nodeFirstChild graph4 (Some cont))
    Assert.Equal(Some ids[2], Graph.nodeLastChild graph4 (Some cont))
    Assert.Equal(Some cont, Graph.owner graph4 (Some ids[0]))
    Assert.Equal(Some cont, Graph.owner graph4 (Some ids[1]))
    Assert.Equal(None, Graph.nodeFirstChild graph4 (Some ids[0]))
    Assert.Equal(None, Graph.nodeLastChild graph4 (Some ids[0]))

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

[<Fact>]
let ``Graph replace parent missing error ends with last 8 hex of parent id`` () =
    let graph = Graph.create ()
    let missing = NodeId.New ()
    let wantSuffix = NodeId.GuidTail8 missing.Value
    match Graph.replace missing 0 [] [] graph with
    | Error msg ->
        Assert.StartsWith("parent not found ", msg)
        Assert.Equal(wantSuffix, msg.Substring("parent not found ".Length))
    | Ok _ -> Assert.Fail("expected Error")

// For Graph.replace node index oldList newList -> Result

// replace: when old span matches, parent children are updated
// replace: node count does not change
// replace: errors when parent node id is missing
// replace: errors when any new child id is missing
// replace: errors when index is out of bounds
// replace: errors when old span does not match existing children at index
// replace: supports insert (old ids empty) at index
// replace: supports delete (new ids empty) at index

let private specialNode (id: NodeId) (kind: SpecialKind) (text: string) : Node =
    { id = id
      text = text
      name = Filename.Empty
      children = []
      cssClasses = CssClass.empty
      owner = Graph.rootId
      kind = Special kind
      updateTime = NodeUpdateTime.missing }

let private addSpecialNode (id: NodeId) (kind: SpecialKind) (text: string) (graph: Graph) : Graph =
    graph.nodes
    |> Map.add id (specialNode id kind text)
    |> fun nodes -> Graph.fromNodes graph.root nodes

[<Fact>]
let ``Graph.create bootstraps WORKSPACES under root with special kind`` () =
    let graph = Graph.create ()
    let workspacesNode = graph.nodes.[Graph.workspacesId]
    match workspacesNode.kind with
    | Special Workspaces -> ()
    | _ -> Assert.True(false, "Workspaces node must have kind = Special Workspaces")
    let rootNode = graph.nodes.[graph.root]
    let workspacesChildOpt =
        rootNode.children
        |> List.tryFind (fun c -> c.id = Graph.workspacesId && c.ref = Ownership.Owner)
    Assert.True(workspacesChildOpt.IsSome)

[<Fact>]
let ``Graph.replace rejects removing workspaces owner from root`` () =
    let graph = Graph.create ()
    let rootId = graph.root
    let rootChildren = graph.nodes.[rootId].children
    let withoutWorkspaces =
        rootChildren |> List.filter (fun c -> c.id <> Graph.workspacesId)
    match Graph.replace rootId 0 rootChildren withoutWorkspaces graph with
    | Ok _ -> Assert.True(false, "expected Error")
    | Error msg -> Assert.Contains("cannot remove workspaces owner child from root", msg)

[<Fact>]
let ``Graph.setText on workspaces node is rejected`` () =
    let graph = Graph.create ()
    let result = Graph.setText Graph.workspacesId "Workspaces" "Other" graph
    Assert.True(Result.isError result)

[<Fact>]
let ``Graph.replace rejects Special Workspace under normal parent`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "parent" ] graph0
    let parent = ids.[0]
    let graph2 =
        Graph.replace graph1.root 0 [] (owned [ parent ]) graph1 |> requireOk "root->parent"
    let wsId = NodeId.New()
    let graph3 = addSpecialNode wsId Workspace "ws" graph2
    match Graph.replace parent 0 [] (owned [ wsId ]) graph3 with
    | Ok _ -> Assert.True(false, "expected Error")
    | Error msg -> Assert.Contains("Workspace nodes may only be placed under Workspaces", msg)

[<Fact>]
let ``Graph.replace accepts Special Workspace under workspaces node`` () =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let graph1 = addSpecialNode wsId Workspace "ws" graph0
    match Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph1 with
    | Ok graph2 ->
        let children = graph2.nodes.[Graph.workspacesId].children
        Assert.Equal<ChildNode list>(owned [ wsId ], children)
    | Error err -> Assert.True(false, $"Expected Ok, got Error: {err}")

[<Fact>]
let ``Graph.replace accepts Special Directory under normal parent`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "parent" ] graph0
    let parent = ids.[0]
    let graph2 =
        Graph.replace graph1.root 0 [] (owned [ parent ]) graph1 |> requireOk "root->parent"
    let dirId = NodeId.New()
    let graph3 = addSpecialNode dirId Directory "dir" graph2
    match Graph.replace parent 0 [] (owned [ dirId ]) graph3 with
    | Ok graph4 ->
        let children = graph4.nodes.[parent].children
        Assert.Equal<NodeId list>([ dirId ], children |> List.map (fun c -> c.id))
    | Error err -> Assert.True(false, $"Expected Ok, got Error: {err}")

[<Fact>]
let ``Graph.replace accepts Special File under normal parent`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "parent" ] graph0
    let parent = ids.[0]
    let graph2 =
        Graph.replace graph1.root 0 [] (owned [ parent ]) graph1 |> requireOk "root->parent"
    let fileId = NodeId.New()
    let graph3 = addSpecialNode fileId File "file" graph2
    match Graph.replace parent 0 [] (owned [ fileId ]) graph3 with
    | Ok graph4 ->
        let children = graph4.nodes.[parent].children
        Assert.Equal<NodeId list>([ fileId ], children |> List.map (fun c -> c.id))
    | Error err -> Assert.True(false, $"Expected Ok, got Error: {err}")

[<Fact>]
let ``Graph.replace accepts Special Directory under Special Workspace`` () =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let graph1 = addSpecialNode wsId Workspace "ws" graph0
    let graph2 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph1
        |> requireOk "workspaces->ws"
    let graph3 = addSpecialNode dirId Directory "dir" graph2
    match Graph.replace wsId 0 [] (owned [ dirId ]) graph3 with
    | Ok graph4 ->
        let children = graph4.nodes.[wsId].children
        Assert.Equal<NodeId list>([ dirId ], children |> List.map (fun c -> c.id))
    | Error err -> Assert.True(false, $"Expected Ok, got Error: {err}")

[<Fact>]
let ``Graph.replace accepts Special Directory under Special Directory`` () =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let subDirId = NodeId.New()
    let graph1 = addSpecialNode wsId Workspace "ws" graph0
    let graph2 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph1
        |> requireOk "workspaces->ws"
    let graph3 = addSpecialNode dirId Directory "dir" graph2
    let graph4 =
        Graph.replace wsId 0 [] (owned [ dirId ]) graph3 |> requireOk "ws->dir"
    let graph5 = addSpecialNode subDirId Directory "subdir" graph4
    match Graph.replace dirId 0 [] (owned [ subDirId ]) graph5 with
    | Ok graph6 ->
        let children = graph6.nodes.[dirId].children
        Assert.Equal<NodeId list>([ subDirId ], children |> List.map (fun c -> c.id))
    | Error err -> Assert.True(false, $"Expected Ok, got Error: {err}")

[<Fact>]
let ``Graph.replace accepts Special File under Special Directory`` () =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let fileId = NodeId.New()
    let graph1 = addSpecialNode wsId Workspace "ws" graph0
    let graph2 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph1
        |> requireOk "workspaces->ws"
    let graph3 = addSpecialNode dirId Directory "dir" graph2
    let graph4 =
        Graph.replace wsId 0 [] (owned [ dirId ]) graph3 |> requireOk "ws->dir"
    let graph5 = addSpecialNode fileId File "file" graph4
    match Graph.replace dirId 0 [] (owned [ fileId ]) graph5 with
    | Ok graph6 ->
        let children = graph6.nodes.[dirId].children
        Assert.Equal<NodeId list>([ fileId ], children |> List.map (fun c -> c.id))
    | Error err -> Assert.True(false, $"Expected Ok, got Error: {err}")

[<Fact>]
let ``Graph.replace accepts Special Directory under Special File`` () =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let fileId = NodeId.New()
    let dirId = NodeId.New()
    let graph1 = addSpecialNode wsId Workspace "ws" graph0
    let graph2 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph1
        |> requireOk "workspaces->ws"
    let graph3 = addSpecialNode fileId File "file" graph2
    let graph4 =
        Graph.replace wsId 0 [] (owned [ fileId ]) graph3 |> requireOk "ws->file"
    let graph5 = addSpecialNode dirId Directory "dir" graph4
    match Graph.replace fileId 0 [] (owned [ dirId ]) graph5 with
    | Ok graph6 ->
        let children = graph6.nodes.[fileId].children
        Assert.Equal<NodeId list>([ dirId ], children |> List.map (fun c -> c.id))
    | Error err -> Assert.True(false, $"Expected Ok, got Error: {err}")

[<Fact>]
let ``Graph.replace accepts Special File under Special File`` () =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let fileId = NodeId.New()
    let nestedId = NodeId.New()
    let graph1 = addSpecialNode wsId Workspace "ws" graph0
    let graph2 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph1
        |> requireOk "workspaces->ws"
    let graph3 = addSpecialNode fileId File "file" graph2
    let graph4 =
        Graph.replace wsId 0 [] (owned [ fileId ]) graph3 |> requireOk "ws->file"
    let graph5 = addSpecialNode nestedId File "nested" graph4
    match Graph.replace fileId 0 [] (owned [ nestedId ]) graph5 with
    | Ok graph6 ->
        let children = graph6.nodes.[fileId].children
        Assert.Equal<NodeId list>([ nestedId ], children |> List.map (fun c -> c.id))
    | Error err -> Assert.True(false, $"Expected Ok, got Error: {err}")

[<Fact>]
let ``Graph.replace accepts Special Directory under ROOT`` () =
    let graph0 = Graph.create ()
    let dirId = NodeId.New()
    let graph1 = addSpecialNode dirId Directory "dir" graph0
    let idx = Graph.fileTreeInsertIndex graph0 Graph.rootId
    match Graph.replace Graph.rootId idx [] (owned [ dirId ]) graph1 with
    | Ok graph2 ->
        let children = graph2.nodes.[Graph.rootId].children
        Assert.True(children |> List.exists (fun c -> c.id = dirId && c.ref = Ownership.Owner))
    | Error err -> Assert.True(false, $"Expected Ok, got Error: {err}")

[<Fact>]
let ``Graph.replace accepts Special File under ROOT`` () =
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let graph1 = addSpecialNode fileId File "file" graph0
    let idx = Graph.fileTreeInsertIndex graph0 Graph.rootId
    match Graph.replace Graph.rootId idx [] (owned [ fileId ]) graph1 with
    | Ok graph2 ->
        let children = graph2.nodes.[Graph.rootId].children
        Assert.True(children |> List.exists (fun c -> c.id = fileId && c.ref = Ownership.Owner))
    | Error err -> Assert.True(false, $"Expected Ok, got Error: {err}")

[<Fact>]
let ``Graph.replace accepts Normal node under any parent`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "parent"; "child" ] graph0
    let parent = ids.[0]
    let child = ids.[1]
    let graph2 =
        Graph.replace graph1.root 0 [] (owned [ parent ]) graph1 |> requireOk "root->parent"
    match Graph.replace parent 0 [] (owned [ child ]) graph2 with
    | Ok graph3 ->
        let children = graph3.nodes.[parent].children
        Assert.Equal<NodeId list>([ child ], children |> List.map (fun c -> c.id))
    | Error err -> Assert.True(false, $"Expected Ok, got Error: {err}")

// ---------------------------------------------------------------------------
// Filename.create
// ---------------------------------------------------------------------------

[<Fact>]
let ``Filename.create accepts alphanumeric and ._- chars`` () =
    Assert.Equal(Filename.Ok "hello",              Filename.create "hello")
    Assert.Equal(Filename.Ok "Hello-World_2024.txt", Filename.create "Hello-World_2024.txt")
    Assert.Equal(Filename.Ok ".hidden",            Filename.create ".hidden")
    Assert.Equal(Filename.Ok "file.tar.gz",        Filename.create "file.tar.gz")

[<Fact>]
let ``Filename.create returns Empty for empty string`` () =
    Assert.Equal(Filename.Empty, Filename.create "")

[<Fact>]
let ``Filename.create returns Invalid for dot and double-dot`` () =
    Assert.Equal(Filename.Invalid ".",  Filename.create ".")
    Assert.Equal(Filename.Invalid "..", Filename.create "..")

[<Fact>]
let ``Filename.create returns Invalid for strings over 255 chars`` () =
    let long = System.String('a', 256)
    Assert.Equal(Filename.Invalid long, Filename.create long)

[<Fact>]
let ``Filename.create returns Invalid for space`` () =
    Assert.Equal(Filename.Invalid "bad name", Filename.create "bad name")

[<Fact>]
let ``Filename.create returns Invalid for path separators`` () =
    Assert.Equal(Filename.Invalid "a/b",  Filename.create "a/b")
    Assert.Equal(Filename.Invalid "a\\b", Filename.create "a\\b")

[<Fact>]
let ``Filename.tryValue returns Some for Ok and None otherwise`` () =
    Assert.Equal(Some "my-file.txt", Filename.tryValue (Filename.create "my-file.txt"))
    Assert.Equal(None, Filename.tryValue (Filename.create ""))
    Assert.Equal(None, Filename.tryValue (Filename.create "bad name"))
