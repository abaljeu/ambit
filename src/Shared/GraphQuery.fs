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

    /// File, Directory, or named Workspace node (artifact on disk).
    let isArtifact (node: Node) : bool =
        match node.kind with
        | Special (File | Directory | Workspace) -> true
        | _ -> false

    /// Nearest Workspace|Directory on owner chain from nodeId (inclusive).
    /// File → None; Normal|Workspaces → continue.
    let owningArtifact (graph: Graph) (nodeId: NodeId) : NodeId option =
        let rec walk currentId =
            match Map.tryFind currentId graph.nodes with
            | Some { kind = Special (Workspace | Directory) } -> Some currentId
            | Some { kind = Special File } -> None
            | Some { kind = Normal | Special Workspaces } ->
                match Map.tryFind currentId graph.ownerParentByChild with
                | Some p -> walk p
                | None -> None
            | _ -> None

        walk nodeId

    /// Placement valid for owned File/Directory: owner chain from ownerId
    /// reaches Workspace|Directory before File.
    let canOwn (graph: Graph) (ownerId: NodeId) (_childId: NodeId) : bool =
        owningArtifact graph ownerId |> Option.isSome

    /// Prefer canOwn when childId is known.
    let isValidOwnedFileDirectoryParent (graph: Graph) (parentId: NodeId) : bool =
        canOwn graph parentId parentId

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

    /// Owned File/Directory/Workspace nodes in an artifact directory.
    let ownedArtifactsInDirectory
        (graph: Graph)
        (artifactDir: NodeId)
        (excludeParentId: NodeId option)
        (excludeId: NodeId option)
        : NodeId list
        =
        graph.nodes
        |> Map.toList
        |> List.choose (fun (id, node) ->
            if excludeId = Some id then None
            elif not (isArtifact node) then None
            else
                match Map.tryFind id graph.ownerParentByChild with
                | Some p when excludeParentId = Some p -> None
                | Some p ->
                    match owningArtifact graph p with
                    | Some d when d = artifactDir -> Some id
                    | _ -> None
                | None -> None)

    let artifactNameLowers
        (graph: Graph)
        (artifactDir: NodeId)
        (excludeParentId: NodeId option)
        (excludeId: NodeId option)
        : string list
        =
        ownedArtifactsInDirectory graph artifactDir excludeParentId excludeId
        |> List.choose (fun id ->
            graph.nodes |> Map.tryFind id |> Option.bind nameLowerOk)

    /// Artifact-directory uniqueness for File/Directory/Workspace names.
    let ownedNameTaken
        (graph: Graph)
        (parentId: NodeId)
        (excludeId: NodeId option)
        (nameLower: string)
        : bool
        =
        match owningArtifact graph parentId with
        | None -> false
        | Some artifactDir ->
            artifactNameLowers graph artifactDir None excludeId
            |> List.exists (fun n -> n = nameLower)

    /// True when updatedChildren would duplicate a File/Directory/Workspace name
    /// in parentId's artifact directory (other parents' specials included).
    let artifactNameConflict
        (graph: Graph)
        (parentId: NodeId)
        (updatedChildren: ChildNode list)
        : bool
        =
        match owningArtifact graph parentId with
        | None -> false
        | Some artifactDir ->
            let fromOthers =
                artifactNameLowers graph artifactDir (Some parentId) None
            let fromParent =
                updatedChildren
                |> List.choose (fun c ->
                    if c.ref <> Ownership.Owner then None
                    else
                        match Map.tryFind c.id graph.nodes with
                        | Some node when isArtifact node -> nameLowerOk node
                        | _ -> None)
            let all = fromOthers @ fromParent
            all.Length <> (all |> List.distinct).Length

    /// Sibling owned-name uniqueness (all kinds with Filename.Ok), including Normals.
    let siblingOwnedNameConflict
        (graph: Graph)
        (updatedChildren: ChildNode list)
        : bool
        =
        let names = ownedNameLowers graph updatedChildren None
        names.Length <> (names |> List.distinct).Length

    /// Full-graph: duplicate File/Directory/Workspace names in any artifact dir.
    let hasArtifactNameDuplicates (graph: Graph) : bool =
        graph.nodes
        |> Map.toList
        |> List.choose (fun (id, node) ->
            if not (isArtifact node) then None
            else
                match Map.tryFind id graph.ownerParentByChild with
                | None -> None
                | Some parentId ->
                    match owningArtifact graph parentId, nameLowerOk node with
                    | Some artifactDir, Some nameLower -> Some(artifactDir, nameLower)
                    | _ -> None)
        |> List.groupBy fst
        |> List.exists (fun (_, pairs) ->
            let names = pairs |> List.map snd
            names.Length <> (names |> List.distinct).Length)

    let tryFindParentAndIndex (targetId: NodeId) (graph: Graph) : (NodeId * int) option =
        Map.tryFind targetId graph.parentByChild

    /// Child under focus when valid; else sibling beside focus under its parent.
    let resolveOwnedFileDirectoryInsert
        (graph: Graph)
        (focusId: NodeId)
        : (NodeId * int) option =
        if canOwn graph focusId focusId then
            Some(focusId, fileTreeInsertIndex graph focusId)
        else
            match tryFindParentAndIndex focusId graph with
            | Some(parentId, index) when canOwn graph parentId focusId ->
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
