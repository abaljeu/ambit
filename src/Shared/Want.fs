namespace Gambol.Shared

/// Auto-want for one Poll. No durable state. Empty list is allowed.
/// Unloaded members of the Included walk, then two ranks of children,
/// including children of folded members. No reserved or Zoom tier.
[<RequireQualifiedAccess>]
module Want =

    /// Unloaded members of the walk, then two ranks of their children.
    /// Fold stops the walk; those children are still wanted. No throttle.
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
        IncludedDescendantIds.plusChildrenOfEach graph included
        |> IncludedDescendantIds.plusChildrenOfEach graph
        |> List.filter (fun id -> not (GraphChildren.isLoaded graph id))
