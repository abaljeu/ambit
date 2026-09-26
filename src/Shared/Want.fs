namespace Gambol.Shared

/// Auto-want list: Included parents that miss Children, then those Children.
/// No durable state. Empty list is allowed.
[<RequireQualifiedAccess>]
module Want =

    /// Included that miss Children first. After install, those Children
    /// become Included and appear next. No reserved or Zoom third tier.
    /// Fold bounds Included. No throttle.
    let compose
        (graph: Graph)
        (siteMap: SiteMap)
        (zoomRoot: NodeId)
        : NodeId list =
        IncludedDescendantIds.expand graph siteMap zoomRoot
        |> List.filter (fun id -> not (GraphChildren.isLoaded graph id))
