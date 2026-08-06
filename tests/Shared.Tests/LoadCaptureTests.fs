module LoadCaptureTests

open Gambol.Shared
open Xunit

let private owned (ids: NodeId list) : ChildNode list =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

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
let ``packagesForTarget with includeWorkspace false returns empty`` () =
    let graph, _, _, fileId = graphWithNestedWorkspace ()
    Assert.Empty(
        ResidentProjection.packagesForTarget graph fileId false)

[<Fact>]
let ``packagesForTarget Unloaded includeWorkspace returns owning Workspace subgraph`` () =
    let graph, wsId, dirId, fileId = graphWithNestedWorkspace ()
    let packages =
        ResidentProjection.packagesForTarget graph fileId true
    let byId = packages |> List.map (fun n -> n.id, n) |> Map.ofList
    Assert.True(byId.ContainsKey wsId)
    Assert.Equal(Loaded, byId.[wsId].childrenStatus)
    Assert.True(byId.ContainsKey dirId)
    Assert.True(byId.ContainsKey fileId)

[<Fact>]
let ``packagesForTarget missing target returns empty`` () =
    let graph, _, _, _ = graphWithNestedWorkspace ()
    Assert.Empty(
        ResidentProjection.packagesForTarget graph (NodeId.New()) true)

[<Fact>]
let ``captureLoadResponse shares revision for changes and packages`` () =
    let graph, wsId, _, fileId = graphWithNestedWorkspace ()
    let change =
        { id = 4
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetText(fileId, "old", "new") ] }
    let response =
        ResidentProjection.captureLoadResponse
            9
            100
            200
            true
            [ change ]
            graph
            fileId
            true
    Assert.Equal(9, response.revision)
    Assert.Equal(100, response.buildEpochSec)
    Assert.Equal(200, response.pageBuildEpochSec)
    Assert.True(response.isReady)
    Assert.Equal(1, response.changes.Length)
    Assert.True(response.packages |> List.exists (fun n -> n.id = wsId))

[<Fact>]
let ``LoadResponse toSyncResponse preserves changes and packages`` () =
    let node =
        Node.Create(NodeId.New(), text = "n", owner = Graph.rootId)
    let load: LoadResponse =
        { revision = 3
          buildEpochSec = 1
          pageBuildEpochSec = 2
          isReady = true
          changes = []
          packages = [ node ] }
    let sync = SyncLogic.loadResponseToSync load
    Assert.Empty(sync.changes)
    Assert.Equal(1, sync.packages.Length)
    Assert.Equal(node.id, sync.packages.[0].id)
