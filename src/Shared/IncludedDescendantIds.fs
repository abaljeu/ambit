namespace Gambol.Shared

/// Included descendant id list — expand from a Node to the Included context under it.
/// Included context = Nodes shown in SiteMap under Zoom, honoring Fold (CONTEXT.md).
/// Browser/Client produces Command/Actor graphIds by walking SiteMap fold state.
/// This Shared module provides the walk structure; when Client is built, it should
/// walk SiteMap.expanded (fold state), not Graph.childrenStatus (residency).
[<RequireQualifiedAccess>]
module IncludedDescendantIds =

    /// Given a Graph and a start NodeId, return a flat NodeId list.
    /// - Include the start Node
    /// - Recurse only through unfolded (expanded) child lists
    /// - Add every child id found there
    /// - Do not descend into folded children
    /// - Do not filter or branch on ownership (Owner vs other child kinds); walk unfolded children only
    /// - Result is ids only — not a Graph, not edges, not ownership facts
    /// 
    /// NOTE: This implementation walks Graph.childrenStatus (Loaded/Unloaded residency)
    /// as a temporary stand-in. When Browser Command graphIds is built, it should walk
    /// SiteMap.expanded (Fold state) instead, which is the Included context definition.
    let expand (graph: Graph) (startId: NodeId) : NodeId list =
        let rec loop (acc: NodeId list) (stack: NodeId list) : NodeId list =
            match stack with
            | [] -> List.rev acc
            | nodeId :: rest ->
                match Map.tryFind nodeId graph.nodes with
                | None -> loop acc rest
                | Some node ->
                    match node.childrenStatus with
                    | Unloaded -> loop (nodeId :: acc) rest
                    | Loaded ->
                        let childIds = node.children |> List.map (fun c -> c.id)
                        loop (nodeId :: acc) (childIds @ rest)
        loop [] [ startId ]
