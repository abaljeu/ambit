module BootstrapScopeTests

open Gambol.Shared
open GraphChildMapHelpers
open Xunit

let private owned = ChildNode.owners

let private refChild = ChildNode.reference

let private specialNode (id: NodeId) (kind: SpecialKind) (name: string) (owner: NodeId) : Node =
    Node.Create(
        id,
        text = name,
        name = Filename.create name,
        owner = owner,
        kind = Special kind)

let private graphWithNestedWorkspace () : Graph * NodeId * NodeId * NodeId =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let fileId = NodeId.New()
    let wsNode = specialNode wsId Workspace "home" Graph.workspacesId
    let dirNode = specialNode dirId Directory "docs" wsId
    let fileNode = specialNode fileId File "readme.txt" dirId

    let graph1 =
        graph0
        |> addDetachedMany [ wsNode; dirNode; fileNode ]

    let graph2 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph1
        |> function
            | Ok g -> g
            | Error err -> failwith err

    let graph3 =
        Graph.replace wsId 0 [] (owned [ dirId ]) graph2
        |> function
            | Ok g -> g
            | Error err -> failwith err

    let graph4 =
        Graph.replace dirId 0 [] (owned [ fileId ]) graph3
        |> function
            | Ok g -> g
            | Error err -> failwith err

    graph4, wsId, dirId, fileId

[<Fact>]
let ``rootBootstrapGraph includes nested workspace header Unloaded`` () =
    let graph, wsId, dirId, fileId = graphWithNestedWorkspace ()
    let scoped = ResidentProjection.rootBootstrapGraph graph
    Assert.True(scoped.nodes.ContainsKey wsId)
    Assert.Equal(Unloaded, Graph.childrenStatus scoped wsId)
    Assert.Empty(Graph.children scoped wsId)
    Assert.False(scoped.nodes.ContainsKey dirId)
    Assert.False(scoped.nodes.ContainsKey fileId)

[<Fact>]
let ``rootBootstrapGraph keeps ROOT owned content Loaded`` () =
    let graph0 = Graph.create ()
    let noteId = NodeId.New()
    let note = Node.Create(noteId, text = "note", owner = graph0.root)
    let graph1 =
        Graph.addDetachedNode note graph0
        |> fun g ->
            Graph.replace g.root 0 [] (owned [ noteId ]) g
            |> function
                | Ok next -> next
                | Error err -> failwith err
    let scoped = ResidentProjection.rootBootstrapGraph graph1
    Assert.True(scoped.nodes.ContainsKey noteId)
    Assert.Equal(Loaded, Graph.childrenStatus scoped noteId)

[<Fact>]
let ``rootBootstrapGraph includes Ref header without children`` () =
    let graph0 = Graph.create ()
    let holderId = NodeId.New()
    let refTargetId = NodeId.New()
    let holder =
        Node.Create(holderId, text = "holder", owner = graph0.root)
    let refTarget =
        Node.Create(refTargetId, text = "external", owner = refTargetId)
    let graph1 =
        graph0
        |> addDetachedMany [ holder; refTarget ]
        |> fun g ->
            Graph.replace g.root 0 [] (owned [ holderId ]) g
            |> function
                | Ok next -> next
                | Error err -> failwith err
        |> fun g ->
            Graph.replace holderId 0 [] [ refChild refTargetId ] g
            |> function
                | Ok next -> next
                | Error err -> failwith err
    let scoped = ResidentProjection.rootBootstrapGraph graph1
    Assert.True(scoped.nodes.ContainsKey refTargetId)
    Assert.Equal(Unloaded, Graph.childrenStatus scoped refTargetId)
    Assert.Empty(Graph.children scoped refTargetId)

[<Fact>]
let ``bootstrapGraph FullGraph returns canonical graph unchanged`` () =
    let graph, wsId, _, _ = graphWithNestedWorkspace ()
    let scoped =
        ResidentProjection.bootstrapGraph BootstrapScope.FullGraph None graph
    Assert.True(scoped.nodes.ContainsKey wsId)
    Assert.Equal<ChildNode list>(Graph.children graph wsId, Graph.children scoped wsId)

[<Fact>]
let ``bootstrapGraph with zoom outside ROOT adds complete owning Workspace`` () =
    let graph, wsId, dirId, fileId = graphWithNestedWorkspace ()
    let scoped =
        ResidentProjection.bootstrapGraph BootstrapScope.RootClosure (Some fileId) graph
    Assert.True(scoped.nodes.ContainsKey wsId)
    Assert.Equal(Loaded, Graph.childrenStatus scoped wsId)
    Assert.True(scoped.nodes.ContainsKey dirId)
    Assert.Equal(Loaded, Graph.childrenStatus scoped dirId)
    Assert.True(scoped.nodes.ContainsKey fileId)
    Assert.Equal(Loaded, Graph.childrenStatus scoped fileId)

[<Fact>]
let ``bootstrapGraph with no zoom is ROOT only`` () =
    let graph, wsId, dirId, fileId = graphWithNestedWorkspace ()
    let scoped =
        ResidentProjection.bootstrapGraph BootstrapScope.RootClosure None graph
    Assert.True(scoped.nodes.ContainsKey wsId)
    Assert.Equal(Unloaded, Graph.childrenStatus scoped wsId)
    Assert.False(scoped.nodes.ContainsKey dirId)
    Assert.False(scoped.nodes.ContainsKey fileId)

[<Fact>]
let ``bootstrapGraph with zoom inside ROOT does not duplicate residency`` () =
    let graph0 = Graph.create ()
    let noteId = NodeId.New()
    let note = Node.Create(noteId, text = "note", owner = graph0.root)
    let graph1 =
        Graph.addDetachedNode note graph0
        |> fun g ->
            Graph.replace g.root 0 [] (owned [ noteId ]) g
            |> function
                | Ok next -> next
                | Error err -> failwith err
    let scoped =
        ResidentProjection.bootstrapGraph BootstrapScope.RootClosure (Some noteId) graph1
    Assert.True(scoped.nodes.ContainsKey noteId)
    Assert.Equal(Loaded, Graph.childrenStatus scoped noteId)
    // Named workspaces stay Unloaded headers; no extra package.
    Assert.Equal(
        (ResidentProjection.rootBootstrapGraph graph1).nodes.Count,
        scoped.nodes.Count)

[<Fact>]
let ``bootstrapGraph with missing zoom falls back to ROOT only`` () =
    let graph, wsId, dirId, fileId = graphWithNestedWorkspace ()
    let missing = NodeId.New()
    let scoped =
        ResidentProjection.bootstrapGraph BootstrapScope.RootClosure (Some missing) graph
    Assert.True(scoped.nodes.ContainsKey wsId)
    Assert.Equal(Unloaded, Graph.childrenStatus scoped wsId)
    Assert.False(scoped.nodes.ContainsKey dirId)
    Assert.False(scoped.nodes.ContainsKey fileId)

[<Fact>]
let ``bootstrapGraph zoom Workspace keeps nested Workspace header Unloaded`` () =
    let graph0, wsId, _, _ = graphWithNestedWorkspace ()
    let nestedWsId = NodeId.New()
    let nestedFileId = NodeId.New()
    let nestedWs = specialNode nestedWsId Workspace "nested" wsId
    let nestedFile = specialNode nestedFileId File "inner.txt" nestedWsId
    let graph1 =
        graph0
        |> addDetachedMany [ nestedWs; nestedFile ]
        |> fun g ->
            setChildren
                wsId
                (Graph.children g wsId @ owned [ nestedWsId ])
                g
    let graph2 =
        Graph.replace nestedWsId 0 [] (owned [ nestedFileId ]) graph1
        |> function
            | Ok g -> g
            | Error err -> failwith err
    let scoped =
        ResidentProjection.bootstrapGraph BootstrapScope.RootClosure (Some wsId) graph2
    Assert.True(scoped.nodes.ContainsKey nestedWsId)
    Assert.Equal(Unloaded, Graph.childrenStatus scoped nestedWsId)
    Assert.Empty(Graph.children scoped nestedWsId)
    Assert.False(scoped.nodes.ContainsKey nestedFileId)

[<Fact>]
let ``bootstrapGraph zoom Workspace includes Ref header without children`` () =
    let graph0, wsId, dirId, _ = graphWithNestedWorkspace ()
    let refTargetId = NodeId.New()
    let refTarget =
        Node.Create(refTargetId, text = "external", owner = refTargetId)
    let graph1 =
        Graph.addDetachedNode refTarget graph0
        |> fun g ->
            setChildren
                dirId
                (Graph.children g dirId @ [ refChild refTargetId ])
                g
    let scoped =
        ResidentProjection.bootstrapGraph BootstrapScope.RootClosure (Some dirId) graph1
    Assert.True(scoped.nodes.ContainsKey refTargetId)
    Assert.Equal(Unloaded, Graph.childrenStatus scoped refTargetId)
    Assert.Empty(Graph.children scoped refTargetId)

[<Fact>]
let ``sessionBootstrapTarget keeps zoom when zoom is outside ROOT`` () =
    let graph, _, _, fileId = graphWithNestedWorkspace ()
    let target =
        ResidentProjection.sessionBootstrapTarget graph fileId (Some graph.root)
    Assert.Equal(fileId, target)

[<Fact>]
let ``sessionBootstrapTarget uses focus when zoom stays in ROOT`` () =
    // Interactive F5: Load a Workspace, select a sub-node, never Zoom — zoomRoot
    // remains an in-ROOT default while focus lies in the loaded Workspace.
    let graph, wsId, _, fileId = graphWithNestedWorkspace ()
    let inRootZoom = Graph.workspacesId
    let target =
        ResidentProjection.sessionBootstrapTarget graph inRootZoom (Some fileId)
    Assert.Equal(fileId, target)
    let scoped =
        ResidentProjection.bootstrapGraph BootstrapScope.RootClosure (Some target) graph
    Assert.Equal(Loaded, Graph.childrenStatus scoped wsId)
    Assert.True(scoped.nodes.ContainsKey fileId)

[<Fact>]
let ``sessionTargets keeps zoom restore and widens bootstrap via focus`` () =
    // F5 must Load the owning Workspace without zooming into the selection.
    let graph, _, _, fileId = graphWithNestedWorkspace ()
    let inRootZoom = Graph.workspacesId
    let zoom, bootstrap =
        ResidentProjection.sessionTargets graph inRootZoom (Some fileId)
    Assert.Equal(inRootZoom, zoom)
    Assert.Equal(fileId, bootstrap)

[<Fact>]
let ``sessionTargets uses zoom for both when zoom is outside ROOT`` () =
    let graph, _, _, fileId = graphWithNestedWorkspace ()
    let zoom, bootstrap =
        ResidentProjection.sessionTargets graph fileId (Some graph.root)
    Assert.Equal(fileId, zoom)
    Assert.Equal(fileId, bootstrap)

[<Fact>]
let ``sessionBootstrapTarget ignores focus that is missing or still in ROOT`` () =
    let graph, _, _, _ = graphWithNestedWorkspace ()
    let inRootZoom = Graph.workspacesId
    Assert.Equal(
        inRootZoom,
        ResidentProjection.sessionBootstrapTarget graph inRootZoom (Some Graph.trashId))
    Assert.Equal(
        inRootZoom,
        ResidentProjection.sessionBootstrapTarget graph inRootZoom (Some (NodeId.New())))
    Assert.Equal(
        inRootZoom,
        ResidentProjection.sessionBootstrapTarget graph inRootZoom None)
