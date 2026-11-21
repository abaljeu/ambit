namespace Gambol.Shared

/// Auto-want for one Poll. No durable state. Empty list is allowed.
/// Absent ids on the Included walk, then three ranks of children,
/// including children of folded members. No reserved or Zoom tier.
/// Each Poll Want keeps at most 100 ids in that walk order.
[<RequireQualifiedAccess>]
module Want =

    /// Ids on that walk whose Header is absent. Fold stops the Included
    /// walk. The three child ranks still run. The list keeps at most 100.
    let compose
        (graph: Graph)
        (siteMap: SiteMap)
        (zoomRoot: NodeId)
        : NodeId list =
        let included =
            IncludedDescendantIds.throughChildrenOfExpandedNodes
                graph
                siteMap
                zoomRoot
        let headerIsAbsent id =
            match Map.tryFind id graph.nodes with
            | None -> true
            | Some _ -> false
        IncludedDescendantIds.plusChildrenOfEach graph included
        |> IncludedDescendantIds.plusChildrenOfEach graph
        |> IncludedDescendantIds.plusChildrenOfEach graph
        |> List.filter headerIsAbsent
        |> List.truncate 100
