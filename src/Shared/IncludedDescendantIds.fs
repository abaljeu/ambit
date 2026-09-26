namespace Gambol.Shared

/// Included descendant id list — Zoom-rooted Included context, honoring Fold.
/// Client produces Command graphIds with this walk. Server does not Zoom-expand.
[<RequireQualifiedAccess>]
module IncludedDescendantIds =

    let private startSite (siteMap: SiteMap) (startId: NodeId) : SiteId option =
        match Map.tryFind siteMap.rootId siteMap.entries with
        | Some root when root.nodeId = startId -> Some siteMap.rootId
        | _ ->
            siteMap.entries
            |> Map.tryPick (fun sid e ->
                if e.nodeId = startId then Some sid else None)

    /// Flat NodeId list starting at Zoom root. Recurse unfolded child lists;
    /// stop at folded children; do not filter ownership; ids only.
    let expand
        (graph: Graph)
        (siteMap: SiteMap)
        (startId: NodeId)
        : NodeId list =
        let rec walk (acc: NodeId list) (nodeId: NodeId) (siteId: SiteId) =
            let acc = nodeId :: acc
            match Map.tryFind siteId siteMap.entries with
            | Some entry when entry.expanded ->
                match Map.tryFind nodeId graph.nodes with
                | None -> acc
                | Some node ->
                    let rec addChildren
                        (acc: NodeId list)
                        (children: ChildNode list)
                        (siteIds: SiteId list)
                        =
                        match children, siteIds with
                        | [], _ -> acc
                        | child :: rest, sid :: sids ->
                            addChildren (walk acc child.id sid) rest sids
                        | child :: rest, [] ->
                            addChildren (child.id :: acc) rest []
                    addChildren acc (GraphChildren.get graph nodeId) entry.children
            | _ -> acc
        match startSite siteMap startId with
        | None -> [ startId ]
        | Some sid -> List.rev (walk [] startId sid)
