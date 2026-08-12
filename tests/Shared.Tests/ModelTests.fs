module ModelTests

open Gambol.Shared
open SpecialNodeTestHelpers
open SpecialNodeTestHelpers
open Xunit

let private owned = ChildNode.owners

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
        [ ChildNode.owner shared
          ChildNode.reference shared ]

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
    Node.Create(id, text = text, kind = Special kind)

let private addSpecialNode (id: NodeId) (kind: SpecialKind) (text: string) (graph: Graph) : Graph =
    graph.nodes
    |> Map.add id (specialNode id kind text)
    |> fun nodes -> Graph.fromNodes graph.root nodes

[<Fact>]
let ``Node.Create applies defaults for omitted fields`` () =
    let id = NodeId.New()
    let node = Node.Create(id, text = "hello")
    Assert.Equal(id, node.id)
    Assert.Equal("hello", node.text)
    Assert.Equal(Filename.Empty, node.name)
    Assert.Equal<ChildNode list>([], node.children)
    Assert.Equal(Loaded, node.childrenStatus)
    Assert.Equal(CssClass.empty, node.cssClasses)
    Assert.Equal(Graph.rootId, node.owner)
    Assert.Equal(Normal, node.kind)
    Assert.Equal(NodeUpdateTime.missing, node.updateTime)

[<Fact>]
let ``Node Unloaded empty is distinct from Loaded empty`` () =
    let id = NodeId.New()
    let unloaded = Node.Create(id, childrenStatus = Unloaded)
    let loadedEmpty = Node.Create(id)
    Assert.Equal(Unloaded, unloaded.childrenStatus)
    Assert.Equal(Loaded, loadedEmpty.childrenStatus)
    Assert.NotEqual(unloaded, loadedEmpty)

[<Fact>]
let ``Node.Create rejects Unloaded with non-empty children`` () =
    let id = NodeId.New()
    let child = ChildNode.New()
    Assert.Throws<System.ArgumentException>(fun () ->
        Node.Create(id, children = [ child ], childrenStatus = Unloaded) |> ignore)
    |> ignore

[<Fact>]
let ``Graph.fromNodes preserves Unloaded and rejects Unloaded with children`` () =
    let g0 = Graph.create ()
    let id = NodeId.New()
    let unloaded = Node.Create(id, text = "hollow", childrenStatus = Unloaded)
    let g1 = Graph.fromNodes g0.root (g0.nodes |> Map.add id unloaded)
    Assert.Equal(Unloaded, g1.nodes.[id].childrenStatus)
    Assert.Equal<ChildNode list>([], g1.nodes.[id].children)

    let invalid =
        { unloaded with
            children = [ ChildNode.New() ]
            childrenStatus = Unloaded }
    Assert.Throws<System.Exception>(fun () ->
        Graph.fromNodes g0.root (g0.nodes |> Map.add id invalid) |> ignore)
    |> ignore

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
let ``isSystemFolderNode covers Workspaces SYSTEM and TRASH only`` () =
    Assert.True(Graph.isSystemFolderNode Graph.workspacesId)
    Assert.True(Graph.isSystemFolderNode Graph.systemId)
    Assert.True(Graph.isSystemFolderNode Graph.trashId)
    Assert.False(Graph.isSystemFolderNode Graph.rootId)
    Assert.False(Graph.isSystemFolderNode (NodeId.New()))

[<Fact>]
let ``isCanonicalDataRoot covers ROOT TRASH and SYSTEM only`` () =
    Assert.True(Graph.isCanonicalDataRoot Graph.rootId)
    Assert.True(Graph.isCanonicalDataRoot Graph.trashId)
    Assert.True(Graph.isCanonicalDataRoot Graph.systemId)
    Assert.False(Graph.isCanonicalDataRoot Graph.workspacesId)
    Assert.False(Graph.isCanonicalDataRoot (NodeId.New()))

[<Fact>]
let ``isSystemDirectoryNode covers TRASH and SYSTEM only`` () =
    Assert.True(Graph.isSystemDirectoryNode Graph.trashId)
    Assert.True(Graph.isSystemDirectoryNode Graph.systemId)
    Assert.False(Graph.isSystemDirectoryNode Graph.workspacesId)
    Assert.False(Graph.isSystemDirectoryNode Graph.rootId)

[<Fact>]
let ``isCanonicalNode covers ROOT and all system folder nodes`` () =
    Assert.True(Graph.isCanonicalNode Graph.rootId)
    Assert.True(Graph.isCanonicalNode Graph.workspacesId)
    Assert.True(Graph.isCanonicalNode Graph.systemId)
    Assert.True(Graph.isCanonicalNode Graph.trashId)
    Assert.False(Graph.isCanonicalNode (NodeId.New()))

[<Fact>]
let ``Graph.create bootstraps SYSTEM under root as Directory with SYSTEM name`` () =
    let graph = Graph.create ()
    let systemNode = graph.nodes.[Graph.systemId]
    match systemNode.kind with
    | Special Directory -> Assert.Equal(Filename.Ok "SYSTEM", systemNode.name)
    | _ -> Assert.True(false, "System node must have kind = Special Directory")
    Assert.Equal("System", systemNode.text)
    let rootNode = graph.nodes.[graph.root]
    let systemChildOpt =
        rootNode.children
        |> List.tryFind (fun c -> c.id = Graph.systemId && c.ref = Ownership.Owner)
    Assert.True(systemChildOpt.IsSome)
    Assert.Equal<NodeId list>(
        [ Graph.workspacesId; Graph.systemId; Graph.trashId ],
        rootNode.children |> List.map (fun c -> c.id))

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
let ``Graph.replace rejects removing system owner from root`` () =
    let graph = Graph.create ()
    let rootId = graph.root
    let rootChildren = graph.nodes.[rootId].children
    let withoutSystem =
        rootChildren |> List.filter (fun c -> c.id <> Graph.systemId)
    match Graph.replace rootId 0 rootChildren withoutSystem graph with
    | Ok _ -> Assert.True(false, "expected Error")
    | Error msg -> Assert.Contains("cannot remove system owner child from root", msg)

[<Fact>]
let ``Graph.replace can reorder Workspaces and TRASH under ROOT`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "a" ] graph0
    let a = ids.[0]
    let graph2 =
        Graph.replace graph1.root 0 [] (owned [ a ]) graph1
        |> requireOk "root->a"
    let rootId = graph2.root
    let oldChildren = graph2.nodes.[rootId].children
    let child id = oldChildren |> List.find (fun c -> c.id = id)
    // Default order: a, Workspaces, SYSTEM, TRASH → move TRASH before Workspaces.
    let reordered =
        [ child a
          child Graph.trashId
          child Graph.workspacesId
          child Graph.systemId ]
    match Graph.replace rootId 0 oldChildren reordered graph2 with
    | Error msg -> Assert.True(false, $"expected Ok: {msg}")
    | Ok graph3 ->
        let children =
            graph3.nodes.[rootId].children |> List.map (fun c -> c.id)
        Assert.Equal<NodeId list>(
            [ a; Graph.trashId; Graph.workspacesId; Graph.systemId ],
            children)

[<Fact>]
let ``Graph.replace rejects Workspaces owned under non-root`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "parent" ] graph0
    let parent = ids.[0]
    let graph2 =
        Graph.replace graph1.root 0 [] (owned [ parent ]) graph1
        |> requireOk "root->parent"
    let wsChild =
        graph2.nodes.[graph2.root].children
        |> List.find (fun c -> c.id = Graph.workspacesId)
    match Graph.replace parent 0 [] [ wsChild ] graph2 with
    | Ok _ -> Assert.True(false, "expected Error")
    | Error msg ->
        Assert.Contains(
            "trash, workspaces, and system may not be OWNED by a non-root parent",
            msg)

[<Fact>]
let ``Graph.replace rejects SYSTEM owned under non-root`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "parent" ] graph0
    let parent = ids.[0]
    let graph2 =
        Graph.replace graph1.root 0 [] (owned [ parent ]) graph1
        |> requireOk "root->parent"
    let systemChild =
        graph2.nodes.[graph2.root].children
        |> List.find (fun c -> c.id = Graph.systemId)
    match Graph.replace parent 0 [] [ systemChild ] graph2 with
    | Ok _ -> Assert.True(false, "expected Error")
    | Error msg ->
        Assert.Contains(
            "trash, workspaces, and system may not be OWNED by a non-root parent",
            msg)

/// Seed owned Special children under SYSTEM via fromNodes (bypasses replace guards).
let private graphWithSpecialSystemMembers
    (kind: SpecialKind)
    (names: string list)
    : Graph * NodeId list =
    let graph0 = Graph.create ()
    let ids = names |> List.map (fun _ -> NodeId.New())
    let memberNodes =
        List.zip ids names
        |> List.map (fun (id, name) ->
            id,
            Node.Create(
                id,
                text = name,
                name = Filename.Ok name,
                kind = Special kind,
                owner = Graph.systemId))
    let system = graph0.nodes.[Graph.systemId]
    let system' =
        { system with children = owned ids }
    let nodes =
        memberNodes
        |> List.fold (fun m (id, n) -> Map.add id n m) graph0.nodes
        |> Map.add Graph.systemId system'
    Graph.fromNodes graph0.root nodes, ids

let private graphWithSystemMembers names =
    graphWithSpecialSystemMembers File names

[<Fact>]
let ``isSpecialSystemDirectoryMember is true only for owned Special children of SYSTEM`` () =
    let graph, ids = graphWithSystemMembers [ "a.amb"; "b.amb" ]
    let a, b = ids.[0], ids.[1]
    Assert.True(Graph.isSpecialSystemDirectoryMember graph a)
    Assert.True(Graph.isSpecialSystemDirectoryMember graph b)
    Assert.False(Graph.isSpecialSystemDirectoryMember graph Graph.systemId)
    Assert.False(Graph.isSpecialSystemDirectoryMember graph Graph.trashId)
    Assert.False(Graph.isSpecialSystemDirectoryMember graph Graph.rootId)

[<Fact>]
let ``Graph.replace allows indent-shaped reparent of Normal child under SYSTEM`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "parent"; "child" ] graph0
    let parent, child = ids.[0], ids.[1]
    let graph2 =
        Graph.replace Graph.systemId 0 [] (owned [ parent; child ]) graph1
        |> requireOk "SYSTEM->normal children"
    let graph3 =
        Graph.replace Graph.systemId 1 (owned [ child ]) [] graph2
        |> requireOk "remove normal child from SYSTEM"
    let graph4 =
        Graph.replace parent 0 [] (owned [ child ]) graph3
        |> requireOk "indent normal child"
    Assert.Equal(Some parent, graph4.ownerParentByChild |> Map.tryFind child)
    Assert.False(Graph.isSpecialSystemDirectoryMember graph2 parent)
    Assert.False(Graph.isSpecialSystemDirectoryMember graph2 child)
    let graph5 =
        Graph.replace parent 0 (owned [ child ]) [] graph4
        |> requireOk "remove normal child from parent"
    let graph6 =
        Graph.replace Graph.systemId 1 [] (owned [ child ]) graph5
        |> requireOk "reparent normal child into SYSTEM"
    Assert.Equal(Some Graph.systemId, graph6.ownerParentByChild |> Map.tryFind child)

[<Fact>]
let ``Graph.setName allows Normal member under SYSTEM`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "normal" ] graph0
    let nodeId = ids.[0]
    let graph2 =
        Graph.replace Graph.systemId 0 [] (owned [ nodeId ]) graph1
        |> requireOk "SYSTEM->normal"
    let graph3 =
        Graph.setName nodeId "" "renamed" graph2
        |> requireOk "rename normal SYSTEM child"
    Assert.Equal(Filename.Ok "renamed", graph3.nodes.[nodeId].name)

[<Fact>]
let ``Graph.replace allows Upload-style File stub under SYSTEM`` () =
    let graph0 = Graph.create ()
    let fileId, ops = FileNodeOps.planCreateOwnedFile graph0 Graph.systemId "x.amb"
    let state0 =
        { graph = graph0
          history = History.empty
          revision = Revision.Zero }
    let state1 =
        ops
        |> List.fold
            (fun s op ->
                match Op.apply op s with
                | ApplyResult.Changed next
                | ApplyResult.Unchanged next -> next
                | ApplyResult.Invalid(_, msg) -> failwith msg)
            state0
    Assert.True(Graph.isSpecialSystemDirectoryMember state1.graph fileId)
    Assert.Contains(
        state1.graph.nodes.[Graph.systemId].children,
        fun c -> c.id = fileId && c.ref = Ownership.Owner)

[<Fact>]
let ``Graph.replace allows Upload-style Directory stub under SYSTEM`` () =
    let graph0 = Graph.create ()
    let dirId, ops =
        FileNodeOps.planCreateOwnedDirectory graph0 Graph.systemId "cfg"
    let state0 =
        { graph = graph0
          history = History.empty
          revision = Revision.Zero }
    let state1 =
        ops
        |> List.fold
            (fun s op ->
                match Op.apply op s with
                | ApplyResult.Changed next
                | ApplyResult.Unchanged next -> next
                | ApplyResult.Invalid(_, msg) -> failwith msg)
            state0
    Assert.True(Graph.isSpecialSystemDirectoryMember state1.graph dirId)

[<Fact>]
let ``Graph.replace allows attaching stub with owner already SYSTEM`` () =
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let node =
        Node.Create(
            fileId,
            text = "pre.amb",
            name = Filename.Ok "pre.amb",
            kind = Special File,
            owner = Graph.systemId)
    let graph1 = Graph.addDetachedNode node graph0
    match Graph.replace Graph.systemId 0 [] (owned [ fileId ]) graph1 with
    | Error msg -> Assert.True(false, $"expected Ok: {msg}")
    | Ok graph2 -> Assert.True(Graph.isSpecialSystemDirectoryMember graph2 fileId)

[<Fact>]
let ``Graph.replace rejects moving existing owned node under SYSTEM`` () =
    let graph0 = Graph.create ()
    let graph1, parentIds = ModelBuilder.createNodes [ "parent" ] graph0
    let parent = parentIds.[0]
    let graph2 =
        Graph.replace graph1.root 0 [] (owned [ parent ]) graph1
        |> requireOk "root->parent"
    let fileId, ops = FileNodeOps.planCreateOwnedFile graph2 parent "x.amb"
    let state0 =
        { graph = graph2
          history = History.empty
          revision = Revision.Zero }
    let state1 =
        ops
        |> List.fold
            (fun s op ->
                match Op.apply op s with
                | ApplyResult.Changed next
                | ApplyResult.Unchanged next -> next
                | ApplyResult.Invalid(_, msg) -> failwith msg)
            state0
    match
        Graph.replace Graph.systemId 0 [] (owned [ fileId ]) state1.graph
    with
    | Ok _ -> Assert.True(false, "expected Error")
    | Error msg ->
        Assert.Contains("cannot move existing Special nodes under SYSTEM", msg)

[<Fact>]
let ``Graph.replace rejects removing owned child under SYSTEM`` () =
    let graph, _ids = graphWithSystemMembers [ "a.amb"; "b.amb" ]
    let oldChildren = graph.nodes.[Graph.systemId].children
    match Graph.replace Graph.systemId 0 oldChildren [] graph with
    | Ok _ -> Assert.True(false, "expected Error")
    | Error msg ->
        Assert.Contains("cannot remove Special owned children under SYSTEM", msg)

[<Fact>]
let ``Graph.replace rejects moving SYSTEM member out to non-SYSTEM parent`` () =
    let graph0, ids = graphWithSystemMembers [ "a.amb" ]
    let memberId = ids.[0]
    let graph1, parentIds = ModelBuilder.createNodes [ "parent" ] graph0
    let parent = parentIds.[0]
    let graph2 =
        Graph.replace graph1.root 0 [] (owned [ parent ]) graph1
        |> requireOk "root->parent"
    match Graph.replace parent 0 [] (owned [ memberId ]) graph2 with
    | Ok _ -> Assert.True(false, "expected Error")
    | Error msg ->
        Assert.Contains(
            "Special SYSTEM members may not be OWNED by a non-SYSTEM parent",
            msg)

[<Fact>]
let ``Graph.replace can reorder owned children under SYSTEM`` () =
    let graph, ids = graphWithSystemMembers [ "a.amb"; "b.amb" ]
    let a, b = ids.[0], ids.[1]
    let oldChildren = graph.nodes.[Graph.systemId].children
    let reordered = [ oldChildren.[1]; oldChildren.[0] ]
    match Graph.replace Graph.systemId 0 oldChildren reordered graph with
    | Error msg -> Assert.True(false, $"expected Ok: {msg}")
    | Ok graph2 ->
        let children =
            graph2.nodes.[Graph.systemId].children |> List.map (fun c -> c.id)
        Assert.Equal<NodeId list>([ b; a ], children)

[<Fact>]
let ``Graph.setName rejects SYSTEM member`` () =
    let graph, ids = graphWithSystemMembers [ "a.amb" ]
    let memberId = ids.[0]
    match Graph.setName memberId "a.amb" "b.amb" graph with
    | Ok _ -> Assert.True(false, "expected Error")
    | Error msg -> Assert.Contains("cannot modify Special SYSTEM member name", msg)

[<Fact>]
let ``Graph guards protect Special Directory member under SYSTEM`` () =
    let graph0, ids = graphWithSpecialSystemMembers Directory [ "config" ]
    let directoryId = ids.[0]
    match Graph.replace Graph.systemId 0 (owned [ directoryId ]) [] graph0 with
    | Ok _ -> Assert.Fail "expected remove Error"
    | Error msg -> Assert.Contains("cannot remove Special owned children", msg)
    let graph1, parentIds = ModelBuilder.createNodes [ "parent" ] graph0
    let parentId = parentIds.[0]
    let graph2 =
        Graph.replace Graph.rootId 0 [] (owned [ parentId ]) graph1
        |> requireOk "root->parent"
    match Graph.replace parentId 0 [] (owned [ directoryId ]) graph2 with
    | Ok _ -> Assert.Fail "expected move-out Error"
    | Error msg -> Assert.Contains("Special SYSTEM members may not be OWNED", msg)
    match Graph.setName directoryId "config" "renamed" graph2 with
    | Ok _ -> Assert.Fail "expected rename Error"
    | Error msg -> Assert.Contains("cannot modify Special SYSTEM member name", msg)

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
let ``Graph.replace rejects Special Directory under Special File`` () =
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
    | Ok _ -> Assert.True(false, "expected Error")
    | Error msg ->
        Assert.Contains(
            "File and Directory nodes must have a Workspace or Directory owner ancestor",
            msg)

[<Fact>]
let ``Graph.replace rejects Special File under Special File`` () =
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
    | Ok _ -> Assert.True(false, "expected Error")
    | Error msg ->
        Assert.Contains(
            "File and Directory nodes must have a Workspace or Directory owner ancestor",
            msg)

[<Fact>]
let ``Graph.replace accepts Special File under Workspaces`` () =
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let graph1 = addSpecialNode fileId File "file" graph0
    match Graph.replace Graph.workspacesId 0 [] (owned [ fileId ]) graph1 with
    | Ok graph2 ->
        let children = graph2.nodes.[Graph.workspacesId].children
        Assert.Equal<NodeId list>([ fileId ], children |> List.map (fun c -> c.id))
    | Error err -> Assert.True(false, $"Expected Ok, got Error: {err}")

[<Fact>]
let ``Graph.replace accepts Special File under TRASH`` () =
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let graph1 = addSpecialNode fileId File "file" graph0
    match Graph.replace Graph.trashId 0 [] (owned [ fileId ]) graph1 with
    | Ok graph2 ->
        let children = graph2.nodes.[Graph.trashId].children
        Assert.Equal<NodeId list>([ fileId ], children |> List.map (fun c -> c.id))
    | Error err -> Assert.True(false, $"Expected Ok, got Error: {err}")

[<Fact>]
let ``Graph.replace accepts Ref Special File under normal parent`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "parent" ] graph0
    let parent = ids.[0]
    let graph2 =
        Graph.replace graph1.root 0 [] (owned [ parent ]) graph1 |> requireOk "root->parent"
    let fileId = NodeId.New()
    let graph3 = addSpecialNode fileId File "file" graph2
    let idx = Graph.fileTreeInsertIndex graph3 Graph.rootId
    let graph4 =
        Graph.replace Graph.rootId idx [] (owned [ fileId ]) graph3
        |> requireOk "root->file"
    match Graph.replace parent 0 [] [ ChildNode.reference fileId ] graph4 with
    | Ok graph5 ->
        let children = graph5.nodes.[parent].children
        Assert.Equal(Ownership.Ref, children.Head.ref)
        Assert.Equal(fileId, children.Head.id)
    | Error err -> Assert.True(false, $"Expected Ok, got Error: {err}")

[<Fact>]
let ``Graph.replace accepts Ref beside Owner sibling with the same name`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "parent" ] graph0
    let parent = ids.[0]
    let ownedDirId, refTargetId = NodeId.New(), NodeId.New()
    let root = graph1.nodes.[Graph.rootId]
    let parentNode = graph1.nodes.[parent]
    let ownedDir =
        Node.Create(
            ownedDirId,
            text = "same",
            name = Filename.create "same",
            owner = parent,
            kind = Special Directory)
    let refTarget =
        Node.Create(
            refTargetId,
            text = "same",
            name = Filename.create "same",
            owner = Graph.rootId,
            kind = Special Directory)
    let nodes =
        graph1.nodes
        |> Map.add Graph.rootId
            { root with children = root.children @ owned [ parent; refTargetId ] }
        |> Map.add parent { parentNode with children = owned [ ownedDirId ] }
        |> Map.add ownedDirId ownedDir
        |> Map.add refTargetId refTarget
    let graph = Graph.fromNodes graph1.root nodes
    match Graph.replace parent 1 [] [ ChildNode.reference refTargetId ] graph with
    | Ok graph2 ->
        let children = graph2.nodes.[parent].children
        Assert.Equal(2, children.Length)
        Assert.Equal(Ownership.Owner, children.[0].ref)
        Assert.Equal(ownedDirId, children.[0].id)
        Assert.Equal(Ownership.Ref, children.[1].ref)
        Assert.Equal(refTargetId, children.[1].id)
    | Error err -> Assert.True(false, $"Expected Ok, got Error: {err}")

[<Fact>]
let ``Graph.replace accepts Ref Special Workspace under normal parent`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "parent" ] graph0
    let parent = ids.[0]
    let graph2 =
        Graph.replace graph1.root 0 [] (owned [ parent ]) graph1 |> requireOk "root->parent"
    let wsId = NodeId.New()
    let graph3 = addSpecialNode wsId Workspace "ws" graph2
    let graph4 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph3
        |> requireOk "workspaces->ws"
    match Graph.replace parent 0 [] [ ChildNode.reference wsId ] graph4 with
    | Ok graph5 ->
        let children = graph5.nodes.[parent].children
        Assert.Equal(Ownership.Ref, children.Head.ref)
        Assert.Equal(wsId, children.Head.id)
    | Error err -> Assert.True(false, $"Expected Ok, got Error: {err}")

[<Fact>]
let ``Graph.replace moves Ref Special Workspace between normal parents`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "a"; "b" ] graph0
    let aId, bId = ids.[0], ids.[1]
    let graph2 =
        Graph.replace graph1.root 0 [] (owned [ aId; bId ]) graph1
        |> requireOk "root->a,b"
    let wsId = NodeId.New()
    let graph3 = addSpecialNode wsId Workspace "ws" graph2
    let graph4 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph3
        |> requireOk "workspaces->ws"
    let wsRef = ChildNode.reference wsId
    let graph5 =
        Graph.replace aId 0 [] [ wsRef ] graph4 |> requireOk "a->ref"
    let graph6 =
        Graph.replace aId 0 [ wsRef ] [] graph5 |> requireOk "a remove ref"
    match Graph.replace bId 0 [] [ wsRef ] graph6 with
    | Ok graph7 ->
        Assert.Equal<ChildNode list>([], graph7.nodes.[aId].children)
        Assert.Equal<ChildNode list>([ wsRef ], graph7.nodes.[bId].children)
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

[<Fact>]
let ``Graph.replace rejects same-named Files under different Normals of one Directory`` () =
    let graph0 = Graph.create ()
    let dirId = NodeId.New()
    let graph1 =
        graph0.nodes
        |> Map.add
            dirId
            (Node.Create(
                dirId,
                text = "docs",
                name = Filename.create "docs",
                kind = Special Directory))
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    let idx = Graph.fileTreeInsertIndex graph1 Graph.rootId
    let graph2 =
        Graph.replace Graph.rootId idx [] (owned [ dirId ]) graph1
        |> requireOk "root->dir"
    let graph3, normals = ModelBuilder.createNodes [ "n1"; "n2" ] graph2
    let n1, n2 = normals.[0], normals.[1]
    let graph4 =
        Graph.replace dirId 0 [] (owned [ n1; n2 ]) graph3 |> requireOk "dir->normals"
    let fileA = NodeId.New()
    let fileB = NodeId.New()
    let graph5 =
        graph4.nodes
        |> Map.add
            fileA
            (Node.Create(
                fileA,
                text = "a.txt",
                name = Filename.create "a.txt",
                kind = Special File))
        |> Map.add
            fileB
            (Node.Create(
                fileB,
                text = "a.txt",
                name = Filename.create "a.txt",
                kind = Special File))
        |> fun nodes -> Graph.fromNodes graph4.root nodes
    let graph6 =
        Graph.replace n1 0 [] (owned [ fileA ]) graph5 |> requireOk "n1->fileA"
    match Graph.replace n2 0 [] (owned [ fileB ]) graph6 with
    | Ok _ -> Assert.True(false, "expected name conflict")
    | Error msg -> Assert.Contains("name conflict", msg)

[<Fact>]
let ``History.applyChange rejects Normal-owning-File moved under File`` () =
    let graph0 = Graph.create ()
    let outerFile = NodeId.New()
    let graph1 = addSpecialNode outerFile File "outer.txt" graph0
    let idx = Graph.fileTreeInsertIndex graph1 Graph.rootId
    let graph2 =
        Graph.replace Graph.rootId idx [] (owned [ outerFile ]) graph1
        |> requireOk "root->outer"
    let graph3, normals = ModelBuilder.createNodes [ "note" ] graph2
    let normalId = normals.[0]
    let rootIdx = Graph.fileTreeInsertIndex graph3 Graph.rootId
    let graph4 =
        Graph.replace Graph.rootId rootIdx [] (owned [ normalId ]) graph3
        |> requireOk "root->normal"
    let innerFile = NodeId.New()
    let graph5 = addSpecialNode innerFile File "inner.txt" graph4
    let graph6 =
        Graph.replace normalId 0 [] (owned [ innerFile ]) graph5
        |> requireOk "normal->inner"
    let normalChild =
        graph6.nodes.[Graph.rootId].children
        |> List.find (fun c -> c.id = normalId)
    let normalIdx =
        graph6.nodes.[Graph.rootId].children
        |> List.findIndex (fun c -> c.id = normalId)
    let change =
        { id = 0
          changeId = System.Guid.NewGuid()
          ops =
            [ Op.Replace(Graph.rootId, normalIdx, [ normalChild ], [])
              Op.Replace(outerFile, 0, [], [ normalChild ]) ] }
    let state =
        { graph = graph6
          history = History.empty
          revision = Revision.Zero }
    match History.applyChange change state with
    | ApplyResult.Invalid(_, msg) ->
        Assert.Contains("File and Directory", msg)
    | _ -> Assert.True(false, "expected Invalid when Normal-owning-File moves under File")

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
let ``Filename.create returns Invalid for exact Directory File basename case-insensitively`` () =
    Assert.Equal(Filename.Invalid ".amb", Filename.create ".amb")
    Assert.Equal(Filename.Invalid ".AMB", Filename.create ".AMB")
    Assert.Equal(Filename.Invalid ".Amb", Filename.create ".Amb")
    Assert.True(Filename.isDirectoryFileBasename ".amb")
    Assert.True(Filename.isDirectoryFileBasename ".AMB")
    Assert.False(Filename.isDirectoryFileBasename "notes.amb")
    Assert.False(Filename.isDirectoryFileBasename ".ambient")

[<Fact>]
let ``Filename.create returns Invalid for strings over 255 chars`` () =
    let long = System.String('a', 256)
    Assert.Equal(Filename.Invalid long, Filename.create long)

[<Fact>]
let ``Filename.create accepts filesystem-safe spaces and punctuation`` () =
    Assert.Equal(Filename.Ok "bad name", Filename.create "bad name")
    Assert.Equal(Filename.Ok "Document (2).doc", Filename.create "Document (2).doc")
    Assert.Equal(Filename.Ok "Alan Baljeu - Siemens Letter.md", Filename.create "Alan Baljeu - Siemens Letter.md")

[<Fact>]
let ``Filename.create returns Invalid for path separators`` () =
    Assert.Equal(Filename.Invalid "a/b",  Filename.create "a/b")
    Assert.Equal(Filename.Invalid "a\\b", Filename.create "a\\b")

[<Fact>]
let ``Filename.create returns Invalid for Windows-forbidden characters`` () =
    Assert.Equal(Filename.Invalid "a:b", Filename.create "a:b")
    Assert.Equal(Filename.Invalid "a*b", Filename.create "a*b")

[<Fact>]
let ``Filename.tryValue returns Some for Ok and None otherwise`` () =
    Assert.Equal(Some "my-file.txt", Filename.tryValue (Filename.create "my-file.txt"))
    Assert.Equal(None, Filename.tryValue (Filename.create ""))
    Assert.Equal(None, Filename.tryValue (Filename.create "a/b"))
