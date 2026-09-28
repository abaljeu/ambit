namespace Gambol.Shared

/// Parsed | Unparsed and Persisted | Unpersisted on special nodes.
[<RequireQualifiedAccess>]
module SpecialNodeState =

    let isSpecialAxisNode (node: Node) : bool =
        match node.kind with
        | Special (Workspace | Directory | File) -> true
        | _ -> false

    /// Nearest File, Directory, or Workspace on the owner chain (inclusive).
    let enclosingSpecial (graph: Graph) (nodeId: NodeId) : NodeId option =
        GraphQuery.enclosing graph isSpecialAxisNode nodeId

    /// Graph edit: nearest owning special Unpersisted only. No ancestors.
    let markUnpersisted (graph: Graph) (nodeId: NodeId) : Graph =
        match enclosingSpecial graph nodeId with
        | None -> graph
        | Some specialId ->
            match Map.tryFind specialId graph.nodes with
            | Some node when node.persistState <> PersistState.Unpersisted ->
                { graph with
                    nodes =
                        Map.add
                            specialId
                            { node with persistState = PersistState.Unpersisted }
                            graph.nodes }
            | _ -> graph
