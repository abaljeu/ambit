module LoadCaptureTests

open Gambol.Shared
open Xunit

let private owned = ChildNode.owners

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

let private graphWithTwoWorkspaces () : Graph * NodeId * NodeId * NodeId * NodeId =
    let graph0 = Graph.create ()
    let wsA = NodeId.New()
    let fileA = NodeId.New()
    let wsB = NodeId.New()
    let fileB = NodeId.New()
    let nodes =
        graph0.nodes
        |> Map.add wsA (specialNode wsA Workspace "a" Graph.workspacesId)
        |> Map.add fileA (specialNode fileA File "a.txt" wsA)
        |> Map.add wsB (specialNode wsB Workspace "b" Graph.workspacesId)
        |> Map.add fileB (specialNode fileB File "b.txt" wsB)
    let graph1 = Graph.fromNodes graph0.root nodes
    let graph2 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsA; wsB ]) graph1
        |> function
            | Ok g -> g
            | Error err -> failwith err
    let graph3 =
        Graph.replace wsA 0 [] (owned [ fileA ]) graph2
        |> function
            | Ok g -> g
            | Error err -> failwith err
    let graph4 =
        Graph.replace wsB 0 [] (owned [ fileB ]) graph3
        |> function
            | Ok g -> g
            | Error err -> failwith err
    graph4, wsA, fileA, wsB, fileB
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
    match
        ResidentProjection.captureLoadResponse
            9
            100
            200
            true
            [ change ]
            graph
            [ { targetId = fileId; includeWorkspace = true } ]
    with
    | Error _ -> failwith "expected LoadResponse"
    | Ok response ->
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

[<Fact>]
let ``packagesForTargets same Workspace Unloaded targets dedupe one package`` () =
    let graph, wsId, dirId, fileId = graphWithNestedWorkspace ()
    let targets =
        [ { targetId = dirId; includeWorkspace = true }
          { targetId = fileId; includeWorkspace = true } ]
    match ResidentProjection.packagesForTargets graph targets with
    | Error ResidentProjection.LoadRefuse.MultiWorkspace ->
        failwith "expected packages"
    | Ok packages ->
        let wsNodes = packages |> List.filter (fun n -> n.id = wsId)
        Assert.Equal(1, wsNodes.Length)
        Assert.Equal(Loaded, wsNodes.[0].childrenStatus)

[<Fact>]
let ``packagesForTargets Loaded and Unloaded same Workspace send package once`` () =
    let graph, wsId, dirId, fileId = graphWithNestedWorkspace ()
    let targets =
        [ { targetId = dirId; includeWorkspace = false }
          { targetId = fileId; includeWorkspace = true } ]
    match ResidentProjection.packagesForTargets graph targets with
    | Error _ -> failwith "expected packages"
    | Ok packages ->
        Assert.True(packages |> List.exists (fun n -> n.id = wsId))
        let wsCount =
            packages |> List.filter (fun n -> n.id = wsId) |> List.length
        Assert.Equal(1, wsCount)

[<Fact>]
let ``packagesForTargets all includeWorkspace false returns empty`` () =
    let graph, _, dirId, fileId = graphWithNestedWorkspace ()
    let targets =
        [ { targetId = dirId; includeWorkspace = false }
          { targetId = fileId; includeWorkspace = false } ]
    match ResidentProjection.packagesForTargets graph targets with
    | Error _ -> failwith "expected packages"
    | Ok packages -> Assert.Empty(packages)

[<Fact>]
let ``packagesForTargets refuses when selection spans two Workspaces`` () =
    let graph, _, fileA, _, fileB = graphWithTwoWorkspaces ()
    let targets =
        [ { targetId = fileA; includeWorkspace = true }
          { targetId = fileB; includeWorkspace = true } ]
    match ResidentProjection.packagesForTargets graph targets with
    | Error ResidentProjection.LoadRefuse.MultiWorkspace -> ()
    | Ok _ -> failwith "expected MultiWorkspace refuse"

[<Fact>]
let ``selectionSpansMultipleWorkspaces is true across two Workspaces`` () =
    let graph, _, fileA, _, fileB = graphWithTwoWorkspaces ()
    Assert.True(
        ResidentProjection.selectionSpansMultipleWorkspaces
            graph
            [ fileA; fileB ])

[<Fact>]
let ``selectionSpansMultipleWorkspaces is false within one Workspace`` () =
    let graph, _, dirId, fileId = graphWithNestedWorkspace ()
    Assert.False(
        ResidentProjection.selectionSpansMultipleWorkspaces
            graph
            [ dirId; fileId ])
