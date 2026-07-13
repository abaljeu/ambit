namespace Gambol.Shared

/// Graph queries: name conflicts, insert placement, parent/child navigation.
module GraphQuery =

    let fileTreeInsertIndex (graph: Graph) (parentId: NodeId) : int =
        if parentId <> GraphBuild.rootId then
            graph.nodes.[parentId].children.Length
        else
            graph.nodes.[parentId].children
            |> List.tryFindIndex (fun c ->
                c.id = GraphBuild.workspacesId || c.id = GraphBuild.trashId)
            |> Option.defaultValue (graph.nodes.[parentId].children.Length)

    /// True when parent may own a Special File or Directory child.
    let isValidOwnedFileDirectoryParent (graph: Graph) (parentId: NodeId) : bool =
        match Map.tryFind parentId graph.nodes with
        | Some { kind = Special Workspace | Special Directory } -> true
        | _ -> false

    let private nameLowerOk (node: Node) : string option =
        match node.name with
        | Filename.Ok n -> Some(n.ToLowerInvariant())
        | _ -> None

    let childrenOf (graph: Graph) (parentId: NodeId) : ChildNode list =
        match graph.nodes |> Map.tryFind parentId with
        | Some p -> p.children
        | None -> []

    let ownedNameLowers
        (graph: Graph)
        (children: ChildNode list)
        (excludeId: NodeId option)
        : string list
        =
        children
        |> List.choose (fun c ->
            if c.ref <> Ownership.Owner then None
            elif excludeId = Some c.id then None
            else
                graph.nodes
                |> Map.tryFind c.id
                |> Option.bind nameLowerOk)

    /// DataDir top (ROOT/Workspaces): flat ROOT∪Workspaces owned names; else siblings.
    let ownedNameTaken
        (graph: Graph)
        (parentId: NodeId)
        (excludeId: NodeId option)
        (nameLower: string)
        : bool
        =
        let kids =
            if parentId = GraphBuild.rootId || parentId = GraphBuild.workspacesId then
                childrenOf graph GraphBuild.rootId @ childrenOf graph GraphBuild.workspacesId
            else
                childrenOf graph parentId
        ownedNameLowers graph kids excludeId
        |> List.exists (fun n -> n = nameLower)

    let tryFindParentAndIndex (targetId: NodeId) (graph: Graph) : (NodeId * int) option =
        Map.tryFind targetId graph.parentByChild

    /// Child under focus when valid; else sibling beside focus under its parent.
    let resolveOwnedFileDirectoryInsert
        (graph: Graph)
        (focusId: NodeId)
        : (NodeId * int) option =
        if isValidOwnedFileDirectoryParent graph focusId then
            Some(focusId, fileTreeInsertIndex graph focusId)
        else
            match tryFindParentAndIndex focusId graph with
            | Some(parentId, index)
                when isValidOwnedFileDirectoryParent graph parentId ->
                Some(parentId, index + 1)
            | _ -> None

    /// Parent along the canonical `Ownership.Owner` edge. `None` id -> `None`.
    let owner (graph: Graph) (id: NodeId option) : NodeId option =
        id |> Option.bind (fun nid -> Map.tryFind nid graph.ownerParentByChild)

    /// First node on the owner chain (including `nodeId`) matching `predicate`.
    let enclosing
        (graph: Graph)
        (predicate: Node -> bool)
        (nodeId: NodeId)
        : NodeId option =
        let rec walk (current: NodeId) =
            match Map.tryFind current graph.nodes with
            | None -> None
            | Some node when predicate node -> Some current
            | Some _ ->
                match Map.tryFind current graph.ownerParentByChild with
                | None -> None
                | Some parentId -> walk parentId

        walk nodeId

    let private isEnclosingWorkspaceNode (node: Node) : bool =
        node.id = GraphBuild.rootId
        || node.id = GraphBuild.trashId
        || match node.kind with
           | Special Workspace when node.id <> GraphBuild.workspacesId -> true
           | _ -> false

    /// Enclosing workspace on the owner chain (named Workspace, ROOT, or TRASH).
    let enclosingWorkspace (graph: Graph) (nodeId: NodeId) : NodeId option =
        enclosing graph isEnclosingWorkspaceNode nodeId

    let nodeFirstChild (graph: Graph) (id: NodeId option) : NodeId option =
        id
        |> Option.bind (fun nid ->
            Map.tryFind nid graph.nodes
            |> Option.bind (fun node ->
                node.children |> List.tryHead |> Option.map (fun c -> c.id)))

    let nodeLastChild (graph: Graph) (id: NodeId option) : NodeId option =
        id
        |> Option.bind (fun nid ->
            Map.tryFind nid graph.nodes
            |> Option.bind (fun node ->
                let n = List.length node.children
                if n = 0 then
                    None
                else
                    List.tryItem (n - 1) node.children |> Option.map (fun c -> c.id)))

    /// Insert position as the last child of nodeId.
    let makeNodeRangeForInsertingUnder (nodeId: NodeId) (graph: Graph) : NodeRange option =
        match Map.tryFind nodeId graph.nodes with
        | None -> None
        | Some node ->
            let childCount = List.length node.children
            Some { pnode = nodeId; start = childCount; endd = childCount }
