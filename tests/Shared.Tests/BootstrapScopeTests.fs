module BootstrapScopeTests

open Gambol.Shared
open Xunit

let private owned (ids: NodeId list) : ChildNode list =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

let private refChild (id: NodeId) : ChildNode =
    { ref = Ownership.Ref; id = id }

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
        graph0.nodes
        |> Map.add wsId wsNode
        |> Map.add dirId dirNode
        |> Map.add fileId fileNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes

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
    Assert.Equal(Unloaded, scoped.nodes.[wsId].childrenStatus)
    Assert.Empty scoped.nodes.[wsId].children
    Assert.False(scoped.nodes.ContainsKey dirId)
    Assert.False(scoped.nodes.ContainsKey fileId)

[<Fact>]
let ``rootBootstrapGraph keeps ROOT owned content Loaded`` () =
    let graph0 = Graph.create ()
    let noteId = NodeId.New()
    let note = Node.Create(noteId, text = "note", owner = graph0.root)
    let root = graph0.nodes.[graph0.root]
    let graph1 =
        graph0.nodes
        |> Map.add noteId note
        |> Map.add graph0.root { root with children = owned [ noteId ] }
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    let scoped = ResidentProjection.rootBootstrapGraph graph1
    Assert.True(scoped.nodes.ContainsKey noteId)
    Assert.Equal(Loaded, scoped.nodes.[noteId].childrenStatus)

[<Fact>]
let ``rootBootstrapGraph includes Ref header without children`` () =
    let graph0 = Graph.create ()
    let holderId = NodeId.New()
    let refTargetId = NodeId.New()
    let holder =
        Node.Create(
            holderId,
            text = "holder",
            owner = graph0.root,
            children = [ refChild refTargetId ])
    let refTarget =
        Node.Create(refTargetId, text = "external", owner = refTargetId)
    let root = graph0.nodes.[graph0.root]
    let graph1 =
        graph0.nodes
        |> Map.add holderId holder
        |> Map.add refTargetId refTarget
        |> Map.add graph0.root { root with children = owned [ holderId ] }
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    let scoped = ResidentProjection.rootBootstrapGraph graph1
    Assert.True(scoped.nodes.ContainsKey refTargetId)
    Assert.Equal(Unloaded, scoped.nodes.[refTargetId].childrenStatus)
    Assert.Empty scoped.nodes.[refTargetId].children

[<Fact>]
let ``bootstrapGraph FullGraph returns canonical graph unchanged`` () =
    let graph, wsId, _, _ = graphWithNestedWorkspace ()
    let scoped = ResidentProjection.bootstrapGraph BootstrapScope.FullGraph graph
    Assert.True(scoped.nodes.ContainsKey wsId)
    Assert.Equal<ChildNode list>(graph.nodes.[wsId].children, scoped.nodes.[wsId].children)
