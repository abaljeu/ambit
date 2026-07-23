namespace Gambol.Shared

/// Graph queries: name conflicts, insert placement, parent/child navigation.
module GraphQuery =

    let fileTreeInsertIndex (graph: Graph) (parentId: NodeId) : int =
        graph.nodes.[parentId].children.Length

    /// File, Directory, or named Workspace node (artifact on disk).
    let isArtifact (node: Node) : bool =
        NodeKind.artifact node.kind

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

    /// Nearest Workspace|Directory on owner chain from nodeId (inclusive).
    /// File before a container → None (same walk; File is not skipped).
    let enclosingContainer (graph: Graph) (nodeId: NodeId) : NodeId option =
        enclosing graph (fun node ->
            NodeKind.container node.kind
            || match node.kind with
               | Special File -> true
               | _ -> false) nodeId
        |> Option.bind (fun id ->
            match Map.tryFind id graph.nodes with
            | Some node when NodeKind.container node.kind -> Some id
            | _ -> None)

    /// Placement valid for owned File/Directory: owner chain from ownerId
    /// reaches Workspace|Directory before File.
    let containerOrDescendant (graph: Graph) (ownerId: NodeId) : bool =
        enclosingContainer graph ownerId |> Option.isSome

    /// Prefer canOwn when childId is known.
    let isValidOwnedFileDirectoryParent (graph: Graph) (parentId: NodeId) : bool =
        containerOrDescendant graph parentId

    /// Owned File/Directory/Workspace nodes in an artifact directory.
    /// Owner-subtree walk: recurse Normal|Workspaces; include artifacts;
    /// do not descend into nested Directory|Workspace containers.
    /// Do not replace with Map.toList over graph.nodes — graphs may be huge.
    let ownedArtifactsInDirectory
        (graph: Graph)
        (artifactDir: NodeId)
        (excludeParentId: NodeId option)
        (excludeId: NodeId option)
        : NodeId list
        =
        let rec walk (parentId: NodeId) (visited: Set<NodeId>) (acc: NodeId list) =
            if Set.contains parentId visited then
                acc
            else
                let visited = Set.add parentId visited
                match Map.tryFind parentId graph.nodes with
                | None -> acc
                | Some parent ->
                    parent.children
                    |> List.fold
                        (fun acc child ->
                            if child.ref <> Ownership.Owner then
                                acc
                            elif Set.contains child.id visited then
                                acc
                            else
                                match Map.tryFind child.id graph.nodes with
                                | None -> acc
                                | Some node when isArtifact node ->
                                    if excludeId = Some child.id then acc
                                    elif excludeParentId = Some parentId then acc
                                    else child.id :: acc
                                | Some _ ->
                                    walk child.id visited acc)
                        acc

        walk artifactDir Set.empty []

    /// File|Directory among artifacts reachable via `ownedArtifactsInDirectory`
    /// walk from nodeId (not `enclosing` — that walks up, not down).
    let ownsFileOrDirectoryThroughSkippables (graph: Graph) (nodeId: NodeId) : bool =
        ownedArtifactsInDirectory graph nodeId None None
        |> List.exists (fun id ->
            match Map.tryFind id graph.nodes with
            | Some { kind = Special (File | Directory) } -> true
            | _ -> false)

    /// True when attaching these Owner children under parentId would place a
    /// File/Directory without a Workspace|Directory ancestor (Refs ignored).
    let invalidOwnedFileDirectoryPlacement
        (graph: Graph)
        (parentId: NodeId)
        (newChildren: ChildNode list)
        : bool
        =
        if containerOrDescendant graph parentId then
            false
        else
            newChildren
            |> List.exists (fun child ->
                match child.ref, Map.tryFind child.id graph.nodes with
                | Ownership.Owner, Some { kind = Special (File | Directory) }
                    when not (GraphBuild.isSystemDirectoryNode child.id) ->
                    true
                | Ownership.Owner, Some { kind = Normal | Special Workspaces } ->
                    ownsFileOrDirectoryThroughSkippables graph child.id
                | _ -> false)

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
        match enclosingContainer graph parentId with
        | None -> false
        | Some artifactDir ->
            artifactNameLowers graph artifactDir None excludeId
            |> List.exists (fun n -> n = nameLower)

    /// True when owned artifacts in `introduced` duplicate a File/Directory/Workspace
    /// name in parentId's artifact directory. Only names from `introduced` are gated;
    /// pre-existing foreign or sibling duplicates alone do not fail the op.
    let artifactNameConflict
        (graph: Graph)
        (parentId: NodeId)
        (introduced: ChildNode list)
        : bool
        =
        match enclosingContainer graph parentId with
        | None -> false
        | Some artifactDir ->
            let introducedArtifacts =
                introduced
                |> List.choose (fun c ->
                    if c.ref <> Ownership.Owner then None
                    else
                        match Map.tryFind c.id graph.nodes with
                        | Some node when isArtifact node ->
                            nameLowerOk node |> Option.map (fun n -> c.id, n)
                        | _ -> None)

            if List.isEmpty introducedArtifacts then
                false
            else
                let names = introducedArtifacts |> List.map snd

                if names.Length <> (names |> List.distinct).Length then
                    true
                else
                    let introducedIds =
                        introducedArtifacts |> List.map fst |> Set.ofList

                    let otherNames =
                        ownedArtifactsInDirectory graph artifactDir None None
                        |> List.filter (fun id -> not (Set.contains id introducedIds))
                        |> List.choose (fun id ->
                            graph.nodes |> Map.tryFind id |> Option.bind nameLowerOk)
                        |> Set.ofList

                    names |> List.exists (fun n -> Set.contains n otherNames)

    /// Sibling owned-name uniqueness for names introduced by `introduced`.
    /// Pre-existing sibling dups among non-introduced children do not fail the op.
    let siblingOwnedNameConflict
        (graph: Graph)
        (updatedChildren: ChildNode list)
        (introduced: ChildNode list)
        : bool
        =
        let introducedNamed =
            introduced
            |> List.choose (fun c ->
                if c.ref <> Ownership.Owner then None
                else
                    graph.nodes
                    |> Map.tryFind c.id
                    |> Option.bind nameLowerOk
                    |> Option.map (fun n -> c.id, n))

        if List.isEmpty introducedNamed then
            false
        else
            let names = introducedNamed |> List.map snd

            if names.Length <> (names |> List.distinct).Length then
                true
            else
                let introducedIds = introducedNamed |> List.map fst |> Set.ofList

                let otherNames =
                    updatedChildren
                    |> List.choose (fun c ->
                        if c.ref <> Ownership.Owner then None
                        elif Set.contains c.id introducedIds then None
                        else
                            graph.nodes
                            |> Map.tryFind c.id
                            |> Option.bind nameLowerOk)
                    |> Set.ofList

                names |> List.exists (fun n -> Set.contains n otherNames)

    /// Full-graph: first File/Directory/Workspace that duplicates a sibling
    /// artifact name in its artifact directory.
    let tryFindArtifactNameDuplicate (graph: Graph) : NodeId option =
        graph.nodes
        |> Map.toList
        |> List.choose (fun (id, node) ->
            if not (isArtifact node) then None
            else
                match Map.tryFind id graph.ownerParentByChild with
                | None -> None
                | Some parentId ->
                    match enclosingContainer graph parentId, nameLowerOk node with
                    | Some artifactDir, Some nameLower ->
                        Some(artifactDir, nameLower, id)
                    | _ -> None)
        |> List.groupBy (fun (dir, _, _) -> dir)
        |> List.tryPick (fun (_, triples) ->
            triples
            |> List.groupBy (fun (_, name, _) -> name)
            |> List.tryPick (fun (_, sameName) ->
                if sameName.Length > 1 then
                    let _, _, id = List.head sameName
                    Some id
                else
                    None))

    /// Full-graph: duplicate File/Directory/Workspace names in any artifact dir.
    let hasArtifactNameDuplicates (graph: Graph) : bool =
        tryFindArtifactNameDuplicate graph |> Option.isSome

    let tryFindParentAndIndex (targetId: NodeId) (graph: Graph) : (NodeId * int) option =
        Map.tryFind targetId graph.parentByChild

    /// Child under focus when valid; else sibling beside focus under its parent.
    let resolveOwnedFileDirectoryInsert
        (graph: Graph)
        (focusId: NodeId)
        : (NodeId * int) option =
        if containerOrDescendant graph focusId then
            Some(focusId, fileTreeInsertIndex graph focusId)
        else
            match tryFindParentAndIndex focusId graph with
            | Some(parentId, index) when containerOrDescendant graph parentId ->
                Some(parentId, index + 1)
            | _ -> None

    /// Parent along the canonical `Ownership.Owner` edge. `None` id -> `None`.
    let owner (graph: Graph) (id: NodeId option) : NodeId option =
        id |> Option.bind (fun nid -> Map.tryFind nid graph.ownerParentByChild)

    let private isEnclosingWorkspaceNode (node: Node) : bool =
        match node.kind with
           | Special Workspace -> true
           | _ -> false

    /// Enclosing workspace on the owner chain (A named Workspace, or ROOT).
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
