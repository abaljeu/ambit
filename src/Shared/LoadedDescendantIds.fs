namespace Gambol.Shared

/// Loaded descendant id list — expand a Zoom root to a flat NodeId list.
/// Used by Browser Command graphIds, Actors, and later callers.
[<RequireQualifiedAccess>]
module LoadedDescendantIds =

    /// Given a Graph and a start NodeId (Zoom root), return a flat NodeId list.
    /// - Include the start Node
    /// - Recurse only through childrenStatus = Loaded child lists
    /// - Add every child id found there
    /// - Do not descend into Unloaded child lists
    /// - Do not filter or branch on ownership (Owner vs other child kinds); walk Loaded children only
    /// - Result is ids only — not a Graph, not edges, not ownership facts
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
