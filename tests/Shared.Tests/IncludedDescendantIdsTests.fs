module IncludedDescendantIdsTests

open Gambol.Shared
open Gambol.Shared.ViewModel
open Xunit

let private owned = ChildNode.owners

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

/// Graph.root → zoom → childTexts. SiteMap Zoomed at `zoom`.
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

/// Graph.root → zoom → mid → childTexts.
let private zoomWithMid
    (childTexts: string list)
    : Graph * NodeId * NodeId * NodeId list =
    let g0 = Graph.create ()
    let g1, zoomIds = ModelBuilder.createNodes [ "zoom" ] g0
    let zoomId = zoomIds.[0]
    let g2, midIds = ModelBuilder.createNodes [ "mid" ] g1
    let midId = midIds.[0]
    let g3, childIds = ModelBuilder.createNodes childTexts g2
    let g4 =
        Graph.replace g3.root 0 [] (owned [ zoomId ]) g3
        |> requireOk "zoomWithMid.root"
    let g5 =
        Graph.replace zoomId 0 [] (owned [ midId ]) g4
        |> requireOk "zoomWithMid.zoom"
    let graph =
        Graph.replace midId 0 [] (owned childIds) g5
        |> requireOk "zoomWithMid.mid"
    graph, zoomId, midId, childIds

let private findInstanceId (nodeId: NodeId) (siteMap: SiteMap) : SiteId =
    siteMap.entries
    |> Map.tryPick (fun instId e ->
        if e.nodeId = nodeId then Some instId else None)
    |> Option.defaultWith (fun () ->
        failwith $"instanceId not found for {nodeId}")

[<Fact>]
let ``expand starts with the Zoom root`` () =
    let graph, siteMap, zoomId, childIds = zoomWithChildren [ "a"; "b" ]
    let ids = IncludedDescendantIds.expand graph siteMap zoomId
    Assert.Equal(zoomId, List.head ids)
    Assert.Equal<NodeId list>([ zoomId; childIds.[0]; childIds.[1] ], ids)

[<Fact>]
let ``expand walks unfolded children and includes every child id`` () =
    let graph, zoomId, midId, childIds = zoomWithMid [ "c1"; "c2" ]
    let siteMap0, nextId = buildSiteMapFrom graph zoomId (Sid 0)
    let midInst = findInstanceId midId siteMap0
    let siteMap, _ = expandEntry midInst graph siteMap0 nextId
    let ids = IncludedDescendantIds.expand graph siteMap zoomId
    Assert.Equal<NodeId list>(
        [ zoomId; midId; childIds.[0]; childIds.[1] ],
        ids)

[<Fact>]
let ``expand does not descend folded children`` () =
    let graph, zoomId, midId, childIds = zoomWithMid [ "hidden" ]
    let siteMap, _ = buildSiteMapFrom graph zoomId (Sid 0)
    let ids = IncludedDescendantIds.expand graph siteMap zoomId
    Assert.Equal<NodeId list>([ zoomId; midId ], ids)
    Assert.DoesNotContain(childIds.[0], ids)

[<Fact>]
let ``expand includes Owner and Ref children`` () =
    let g0 = Graph.create ()
    let g1, zoomIds = ModelBuilder.createNodes [ "zoom" ] g0
    let zoomId = zoomIds.[0]
    let g2, ids = ModelBuilder.createNodes [ "owned"; "refed" ] g1
    let ownedId, refId = ids.[0], ids.[1]
    let g3 =
        Graph.replace g2.root 0 [] (owned [ zoomId ]) g2
        |> requireOk "ownership.root"
    let children =
        [ ChildNode.owner ownedId; ChildNode.reference refId ]
    let graph =
        Graph.replace zoomId 0 [] children g3 |> requireOk "ownership.zoom"
    let siteMap, _ = buildSiteMapFrom graph zoomId (Sid 0)
    let got = IncludedDescendantIds.expand graph siteMap zoomId
    Assert.Equal<NodeId list>([ zoomId; ownedId; refId ], got)

[<Fact>]
let ``expand returns node ids only`` () =
    let graph, siteMap, zoomId, childIds = zoomWithChildren [ "leaf" ]
    let ids = IncludedDescendantIds.expand graph siteMap zoomId
    Assert.Equal<NodeId list>([ zoomId; childIds.[0] ], ids)
