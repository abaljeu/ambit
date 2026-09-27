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
    let throughChildrenOfExpandedNodes
        (graph: Graph)
        (siteMap: SiteMap)
        (startId: NodeId)
        : NodeId list =
        let rec walk (acc: NodeId list) (nodeId: NodeId) (siteId: SiteId) =
            let acc = nodeId :: acc
            match Map.tryFind siteId siteMap.entries with
            | Some entry when entry.expanded ->
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

    /// `found` in order, then each node's direct children that are not
    /// already listed. One rank. An absent childMap adds no children.
    let plusChildrenOfEach (graph: Graph) (found: NodeId list) : NodeId list =
        let extras, _ =
            found
            |> List.fold
                (fun (acc, seen) parentId ->
                    match GraphChildren.tryGet graph parentId with
                    | None -> acc, seen
                    | Some children ->
                        children
                        |> List.fold
                            (fun (acc, seen) child ->
                                if Set.contains child.id seen then
                                    acc, seen
                                else
                                    child.id :: acc, Set.add child.id seen)
                            (acc, seen))
                ([], Set.ofList found)
        found @ List.rev extras
