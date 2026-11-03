namespace Gambol.Shared

/// Auto-want for one Poll. No durable state. Empty list is allowed.
/// Absent ids on the Included walk, then two ranks of children,
/// including children of folded members. No reserved or Zoom tier.
[<RequireQualifiedAccess>]
module Want =

    /// Ids on that walk whose Header is absent. Fold stops the Included
    /// walk. The two child ranks still run. No throttle.
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
        |> List.filter headerIsAbsent
