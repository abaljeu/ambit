module WantTests

open Gambol.Shared
open Gambol.Shared.ViewModel
open GraphChildMapHelpers
open Xunit

let private owned = ChildNode.owners

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private specialNode
    (id: NodeId)
    (kind: SpecialKind)
    (name: string)
    (owner: NodeId)
    : Node =
    Node.Create(
        id,
        text = name,
        name = Filename.create name,
        owner = owner,
        kind = Special kind)

/// ROOT → zoom → childTexts. SiteMap Zoomed at zoom.
let private zoomWithChildren
    (childTexts: string list)
    : Graph * SiteMap * NodeId * NodeId list =
    let g0 = Graph.create ()
    let g1, zoomIds = ModelBuilder.createNodes [ "zoom" ] g0
    let zoomId = zoomIds.[0]
    let g2, childIds = ModelBuilder.createNodes childTexts g1
    let g3 =
        Graph.replace g2.root 0 [] (owned [ zoomId ]) g2
        |> requireOk "zoomWithChildren.root"
    let graph =
        Graph.replace zoomId 0 [] (owned childIds) g3
        |> requireOk "zoomWithChildren.zoom"
    let siteMap, _ = buildSiteMapFrom graph zoomId (Sid 0)
    graph, siteMap, zoomId, childIds

let private findInstanceId (nodeId: NodeId) (siteMap: SiteMap) : SiteId =
    siteMap.entries
    |> Map.tryPick (fun sid e ->
        if e.nodeId = nodeId then Some sid else None)
    |> Option.defaultWith (fun () ->
        failwith $"instanceId not found for {nodeId}")

let private graphWithWorkspaceAndRootNote
    ()
    : Graph * NodeId * NodeId * NodeId * NodeId * NodeId =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let fileId = NodeId.New()
    let buriedId = NodeId.New()
    let noteId = NodeId.New()
    let grandId = NodeId.New()
    let ws = specialNode wsId Workspace "home" Graph.workspacesId
    let dir = specialNode dirId Directory "docs" wsId
    let file = specialNode fileId File "readme.txt" dirId
    let buried = specialNode buriedId File "buried.txt" dirId
    let note = Node.Create(noteId, text = "note", owner = graph0.root)
    let grand = Node.Create(grandId, text = "grand", owner = noteId)
    let graph1 =
        graph0
        |> addDetachedMany [ ws; dir; file; buried; note; grand ]
    let graph2 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph1
        |> requireOk "workspaces"
    let graph3 =
        Graph.replace wsId 0 [] (owned [ dirId ]) graph2
        |> requireOk "ws"
    let graph4 =
        Graph.replace dirId 0 [] (owned [ fileId; buriedId ]) graph3
        |> requireOk "dir"
    let graph5 = appendKids graph4.root (owned [ noteId ]) graph4
    let graph =
        Graph.replace noteId 0 [] (owned [ grandId ]) graph5
        |> requireOk "note"
    graph, wsId, dirId, fileId, buriedId, grandId

[<Fact>]
let ``compose lists Included that miss Children first`` () =
    let graph0, siteMap, zoomId, childIds = zoomWithChildren [ "a"; "b" ]
    let graph = unload childIds.[0] graph0
    let want = Want.compose graph siteMap zoomId
    Assert.Equal<NodeId list>([ childIds.[0] ], want)
    Assert.DoesNotContain(childIds.[1], want)
    Assert.DoesNotContain(zoomId, want)

[<Fact>]
let ``compose then installWantAnswer loads wanted parents and children`` () =
    let graph0, siteMap, zoomId, childIds = zoomWithChildren [ "a" ]
    let parentId = childIds.[0]
    let grandId = NodeId.New()
    let grand =
        Node.Create(grandId, text = "grand", owner = parentId)
    let graph = unload parentId graph0
    let want = Want.compose graph siteMap zoomId
    Assert.Equal<NodeId list>([ parentId ], want)
    let edges = Map.ofList [ parentId, owned [ grandId ] ]
    let installed =
        ResidentProjection.installWantAnswer edges [ grand ] graph
        |> requireOk "install"
    Assert.True(GraphChildren.isLoaded installed parentId)
    Assert.True(installed.nodes.ContainsKey grandId)
    Assert.False(GraphChildren.isLoaded installed grandId)
    let siteMap1, nextId = buildSiteMapFrom installed zoomId (Sid 0)
    let parentInst = findInstanceId parentId siteMap1
    let siteMap2, _ = expandEntry parentInst installed siteMap1 nextId
    let want2 = Want.compose installed siteMap2 zoomId
    Assert.Equal<NodeId list>([ grandId ], want2)
    Assert.DoesNotContain(Graph.trashId, want2)
    Assert.DoesNotContain(Graph.systemId, want2)

[<Fact>]
let ``compose wants unloaded children of folded members`` () =
    let g0 = Graph.create ()
    let g1, zoomIds = ModelBuilder.createNodes [ "zoom" ] g0
    let zoomId = zoomIds.[0]
    let g2, midIds = ModelBuilder.createNodes [ "mid" ] g1
    let midId = midIds.[0]
    let g3, hiddenIds = ModelBuilder.createNodes [ "hidden" ] g2
    let hiddenId = hiddenIds.[0]
    let g4 =
        Graph.replace g3.root 0 [] (owned [ zoomId ]) g3
        |> requireOk "fold.root"
    let g5 =
        Graph.replace zoomId 0 [] (owned [ midId ]) g4
        |> requireOk "fold.zoom"
    let graph =
        Graph.replace midId 0 [] (owned [ hiddenId ]) g5
        |> requireOk "fold.mid"
    let siteMap, _ = buildSiteMapFrom graph zoomId (Sid 0)
    let want = Want.compose (unload hiddenId graph) siteMap zoomId
    Assert.Equal<NodeId list>([ hiddenId ], want)
    Assert.DoesNotContain(zoomId, want)
    Assert.DoesNotContain(midId, want)

[<Fact>]
let ``compose wants one rank past a folded loaded child`` () =
    let g0 = Graph.create ()
    let g1, zoomIds = ModelBuilder.createNodes [ "zoom" ] g0
    let zoomId = zoomIds.[0]
    let g2, midIds = ModelBuilder.createNodes [ "mid" ] g1
    let midId = midIds.[0]
    let g3, hiddenIds = ModelBuilder.createNodes [ "hidden" ] g2
    let hiddenId = hiddenIds.[0]
    let g4, grandIds = ModelBuilder.createNodes [ "grand" ] g3
    let grandId = grandIds.[0]
    let g5 =
        Graph.replace g4.root 0 [] (owned [ zoomId ]) g4
        |> requireOk "deep.root"
    let g6 =
        Graph.replace zoomId 0 [] (owned [ midId ]) g5
        |> requireOk "deep.zoom"
    let g7 =
        Graph.replace midId 0 [] (owned [ hiddenId ]) g6
        |> requireOk "deep.mid"
    let graph =
        Graph.replace hiddenId 0 [] (owned [ grandId ]) g7
        |> requireOk "deep.hidden"
    let siteMap, _ = buildSiteMapFrom graph zoomId (Sid 0)
    let want = Want.compose (unload grandId graph) siteMap zoomId
    Assert.Equal<NodeId list>([ grandId ], want)
    Assert.DoesNotContain(hiddenId, want)

[<Fact>]
let ``compose is empty when Included is Loaded`` () =
    let graph, siteMap, zoomId, _ = zoomWithChildren [ "leaf" ]
    let want = Want.compose graph siteMap zoomId
    Assert.Equal<NodeId list>([], want)

[<Fact>]
let ``installWantAnswer refuses a dangling edge`` () =
    let graph, _, _, childIds = zoomWithChildren [ "a" ]
    let missing = NodeId.New()
    let edges = Map.ofList [ childIds.[0], owned [ missing ] ]
    match ResidentProjection.installWantAnswer edges [] graph with
    | Ok _ -> failwith "expected dangling refuse"
    | Error msg -> Assert.Equal("dangling edge", msg)

[<Fact>]
let ``installWantAnswer keeps absent key Unloaded and [] Loaded`` () =
    let graph0, _, _, childIds = zoomWithChildren [ "a"; "b" ]
    let leaf = childIds.[0]
    let other = childIds.[1]
    let graph = unload leaf graph0 |> unload other
    let edges = Map.ofList [ leaf, [] ]
    let installed =
        ResidentProjection.installWantAnswer edges [] graph
        |> requireOk "leaf"
    Assert.True(GraphChildren.isLoaded installed leaf)
    Assert.Empty(GraphChildren.get installed leaf)
    Assert.False(GraphChildren.isLoaded installed other)
    let again =
        ResidentProjection.installWantAnswer edges [] installed
        |> requireOk "again"
    Assert.True(GraphChildren.isLoaded again leaf)
    Assert.False(GraphChildren.isLoaded again other)

[<Fact>]
let ``installWantAnswer accepts a Child already Resident`` () =
    let graph, _, _, childIds = zoomWithChildren [ "a"; "b" ]
    let parent = childIds.[0]
    let residentChild = childIds.[1]
    let edges = Map.ofList [ parent, owned [ residentChild ] ]
    let installed =
        ResidentProjection.installWantAnswer edges [] graph
        |> requireOk "resident-child"
    Assert.True(GraphChildren.isLoaded installed parent)
    Assert.True(installed.nodes.ContainsKey residentChild)

[<Fact>]
let ``wantAnswer returns wanted edges and pointed-at Nodes`` () =
    let graph, _, zoomId, childIds = zoomWithChildren [ "a"; "b" ]
    let edges, nodes =
        ResidentProjection.wantAnswer graph [ zoomId ]
    Assert.Equal<ChildNode list>(owned childIds, edges.[zoomId])
    Assert.Equal<Set<NodeId>>(Set.ofList childIds, nodes |> List.map (_.id) |> Set.ofList)
    let emptyEdges, emptyNodes =
        ResidentProjection.wantAnswer graph []
    Assert.Empty(emptyEdges)
    Assert.Empty(emptyNodes)

[<Fact>]
let ``wantAnswer omits an edge with an absent target Node`` () =
    let graph0, _, zoomId, _ = zoomWithChildren [ "resident" ]
    let missingId = NodeId.New()
    let graph =
        Graph.fromNodes
            graph0.root
            graph0.nodes
            (Map.add zoomId (owned [ missingId ]) graph0.childMap)
    let edges, nodes =
        ResidentProjection.wantAnswer graph [ zoomId ]
    Assert.False(Map.containsKey zoomId edges)
    Assert.Empty(nodes)

[<Fact>]
let ``visibleClosureGraph loads reserved Children and Zoom ancestors`` () =
    let graph, wsId, dirId, fileId, buriedId, grandId =
        graphWithWorkspaceAndRootNote ()
    let scoped =
        ResidentProjection.visibleClosureGraph (Some fileId) graph
    Assert.True(GraphChildren.isLoaded scoped Graph.rootId)
    Assert.True(GraphChildren.isLoaded scoped Graph.trashId)
    Assert.True(GraphChildren.isLoaded scoped Graph.workspacesId)
    Assert.True(GraphChildren.isLoaded scoped Graph.systemId)
    Assert.True(GraphChildren.isLoaded scoped wsId)
    Assert.True(GraphChildren.isLoaded scoped dirId)
    Assert.True(scoped.nodes.ContainsKey fileId)
    Assert.True(scoped.nodes.ContainsKey buriedId)
    Assert.False(scoped.nodes.ContainsKey grandId)
    Assert.Equal(Loaded, Graph.childrenStatus scoped dirId)
    Assert.Equal(Unloaded, Graph.childrenStatus scoped buriedId)

[<Fact>]
let ``visibleClosureGraph bad Zoom stays inside reserved set`` () =
    let graph, wsId, dirId, fileId, buriedId, grandId =
        graphWithWorkspaceAndRootNote ()
    let missing = NodeId.New()
    let scoped =
        ResidentProjection.visibleClosureGraph (Some missing) graph
    Assert.True(scoped.nodes.ContainsKey wsId)
    Assert.False(GraphChildren.isLoaded scoped wsId)
    Assert.False(scoped.nodes.ContainsKey dirId)
    Assert.False(scoped.nodes.ContainsKey fileId)
    Assert.False(scoped.nodes.ContainsKey buriedId)
    Assert.False(scoped.nodes.ContainsKey grandId)
    let rootScoped = ResidentProjection.rootBootstrapGraph graph
    Assert.True(rootScoped.nodes.ContainsKey grandId)

[<Fact>]
let ``visibleClosureWantAnswer installs through installWantAnswer`` () =
    let graph, _, dirId, fileId, _, _ =
        graphWithWorkspaceAndRootNote ()
    let edges, nodes =
        ResidentProjection.visibleClosureWantAnswer (Some fileId) graph
    let installed =
        ResidentProjection.installWantAnswer edges nodes (Graph.create ())
        |> requireOk "bootstrap-package"
    Assert.True(GraphChildren.isLoaded installed dirId)
    Assert.True(installed.nodes.ContainsKey fileId)
