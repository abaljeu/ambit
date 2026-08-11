namespace Gambol.Shared

/// Graph mutations: setText/Name/Classes/DocumentState and Replace.
module GraphMutate =

    let private nodeDisplayName (graph: Graph) (id: NodeId) : string option =
        Map.tryFind id graph.nodes
        |> Option.bind (fun n -> Filename.tryValue n.name)
        |> Option.orElseWith (fun () ->
            Map.tryFind id graph.nodes
            |> Option.bind (fun n ->
                if System.String.IsNullOrEmpty n.text then None
                else Some n.text))

    /// Include colliding name, parent label, and parent id tail for diagnosis.
    let private formatNameConflict
        (graph: Graph)
        (parentId: NodeId)
        (name: string)
        : string
        =
        let parentPart =
            match nodeDisplayName graph parentId with
            | Some pn -> $" under '{pn}'"
            | None -> ""
        let parentTail = NodeId.GuidTail8 parentId.Value
        $"name conflict: '{name}'{parentPart} ({parentTail})"

    let private firstIntroducedOwnerName
        (graph: Graph)
        (parentId: NodeId)
        (introduced: ChildNode list)
        : string option
        =
        introduced
        |> List.tryPick (fun c ->
            if Node.childOwnership graph parentId c <> Ownership.Owner then None
            else nodeDisplayName graph c.id)

    let setText
        (nodeId: NodeId)
        (oldText: string)
        (newText: string)
        (graph: Graph)
        : Result<Graph, string>
        =
        if nodeId = GraphBuild.rootId then
            Error "cannot modify canonical root text"
        elif GraphBuild.isSystemFolderNode nodeId then
            Error "cannot modify system folder node text"
        else
            match graph.nodes |> Map.tryFind nodeId with
            | None -> Error "node not found"
            | Some node ->
                if node.text <> oldText then
                    Error "old text does not match"
                else
                    let updatedNode = NodeUpdateTime.touch { node with text = newText }
                    let nodes = graph.nodes |> Map.add nodeId updatedNode
                    Ok { graph with nodes = nodes }

    let setClasses
        (nodeId: NodeId)
        (oldClasses: CssClasses)
        (newClasses: CssClasses)
        (graph: Graph)
        : Result<Graph, string>
        =
        if nodeId = GraphBuild.rootId then
            Error "cannot set classes on canonical root"
        elif GraphBuild.isSystemFolderNode nodeId then
            Error "cannot set classes on system folder node"
        else
            match graph.nodes |> Map.tryFind nodeId with
            | None -> Error "node not found"
            | Some node ->
                if node.cssClasses <> oldClasses then
                    Error "old classes do not match"
                else
                    let updatedNode = NodeUpdateTime.touch { node with cssClasses = newClasses }
                    let nodes = graph.nodes |> Map.add nodeId updatedNode
                    Ok { graph with nodes = nodes }

    let setName
        (nodeId: NodeId)
        (oldName: string)
        (newName: string)
        (graph: Graph)
        : Result<Graph, string>
        =
        if nodeId = GraphBuild.rootId then
            Error "cannot modify canonical root name"
        elif GraphBuild.isSystemFolderNode nodeId then
            Error "cannot modify system folder node name"
        elif GraphBuild.isSpecialSystemDirectoryMember graph nodeId then
            Error "cannot modify Special SYSTEM member name"
        else
            match graph.nodes |> Map.tryFind nodeId with
            | None -> Error "node not found"
            | Some node ->
                // ROOT is Special Workspace but blocked by rootId above.
                if node.kind = Special Workspace then
                    Error "cannot rename a workspace"
                elif node.name <> Filename.create oldName then
                    Error "old name does not match"
                elif
                    match node.kind with
                    | Special (Workspace | Directory | File) ->
                        Filename.isReservedSystemName newName
                    | _ -> false
                then
                    Error "new name is a reserved system name"
                else
                    match Filename.create newName with
                    | Filename.Invalid _ | Filename.Empty ->
                        Error "new name is not a valid filename"
                    | Filename.Ok validName ->
                        let newNameLower = validName.ToLowerInvariant()
                        let hasConflict =
                            match graph.ownerParentByChild |> Map.tryFind nodeId with
                            | None -> false
                            | Some parentId ->
                                match node.kind with
                                | Special (File | Directory) ->
                                    GraphQuery.ownedNameTaken
                                        graph parentId (Some nodeId) newNameLower
                                | _ ->
                                    GraphQuery.ownedNameLowers
                                        graph
                                        parentId
                                        (GraphQuery.childrenOf graph parentId)
                                        (Some nodeId)
                                    |> List.exists (fun n -> n = newNameLower)
                        if hasConflict then
                            match graph.ownerParentByChild |> Map.tryFind nodeId with
                            | Some parentId ->
                                Error (formatNameConflict graph parentId validName)
                            | None ->
                                Error $"name conflict: '{validName}'"
                        else
                            let updatedNode =
                                match node.kind with
                                | Normal ->
                                    NodeUpdateTime.touch { node with name = Filename.Ok validName }
                                | Special _ ->
                                    NodeUpdateTime.touch
                                        { node with name = Filename.Ok validName; text = validName }
                            Ok { graph with nodes = graph.nodes |> Map.add nodeId updatedNode }

    let setDocumentState
        (nodeId: NodeId)
        (oldState: DocumentState)
        (newState: DocumentState)
        (graph: Graph)
        : Result<Graph, string>
        =
        match graph.nodes |> Map.tryFind nodeId with
        | None -> Error "node not found"
        | Some node when node.kind = Special Workspaces ->
            Error "workspaces is not a graph document"
        | Some node when node.kind = Normal ->
            Error "normal nodes do not have document state"
        | Some node when node.documentState <> oldState ->
            Error "old document state does not match"
        | Some node when oldState = newState ->
            Ok graph
        | Some node ->
            let updated = NodeUpdateTime.touch { node with documentState = newState }
            Ok { graph with nodes = graph.nodes |> Map.add nodeId updated }

    let replace
        (parentId: NodeId)
        (index: int)
        (oldChildren: ChildNode list)
        (newChildren: ChildNode list)
        (graph: Graph)
        : Result<Graph, string>
        =
        let parentOpt = graph.nodes |> Map.tryFind parentId

        match parentOpt with
        | None -> Error $"parent not found {NodeId.GuidTail8 parentId.Value}"
        | Some parent ->
            let children = parent.children
            let childCount = List.length children
            let oldCount = List.length oldChildren

            if index < 0 || index > childCount then
                Error "index out of bounds"
            elif index + oldCount > childCount then
                Error "old span out of bounds"
            elif
                newChildren
                |> List.exists (fun child -> not (graph.nodes.ContainsKey child.id))
            then
                Error "new child not found"
            else
                let placementError =
                    newChildren
                    |> List.tryPick (fun child ->
                        let childNode = graph.nodes.[child.id]

                        match
                            Node.childOwnership graph parentId child, childNode.kind
                        with
                        | Ownership.Owner, Special Workspace
                            when parentId <> GraphBuild.workspacesId ->
                            Some "Workspace nodes may only be placed under Workspaces"
                        | Ownership.Owner, _
                            when GraphQuery.invalidOwnedFileDirectoryPlacement
                                     graph
                                     parentId
                                     [ child ] ->
                            Some
                                "File and Directory nodes must have a Workspace or Directory owner ancestor (not under a File)"
                        | _ -> None)

                // Appending at the end removes nothing and shifts no sibling index,
                // so the parent indexes can be updated in place.
                let isAppend = oldCount = 0 && index = childCount
                let commit (updatedChildren: ChildNode list) =
                    let updatedParent =
                        NodeUpdateTime.touch { parent with children = updatedChildren }
                    if isAppend then
                        GraphBuild.appendChildren parentId newChildren updatedParent graph
                    else
                        GraphBuild.fromNodes
                            graph.root
                            (graph.nodes |> Map.add parentId updatedParent)

                match placementError with
                | Some msg -> Error msg
                | None ->
                    let existing =
                        children
                        |> List.skip index
                        |> List.take oldCount

                    if existing <> oldChildren then
                        Error "old span does not match"
                    else
                        let prefix = children |> List.take index
                        let suffix = children |> List.skip (index + oldCount)
                        let updatedChildren = prefix @ newChildren @ suffix

                        let hasNameConflict =
                            // Refs never participate in name uniqueness.
                            newChildren
                            |> List.exists (fun c ->
                                Node.childOwnership graph parentId c = Ownership.Owner)
                            && (GraphQuery.siblingOwnedNameConflict
                                    graph parentId updatedChildren newChildren
                                || GraphQuery.artifactNameConflict graph parentId newChildren)

                        if hasNameConflict then
                            let name =
                                firstIntroducedOwnerName graph parentId newChildren
                                |> Option.defaultValue "?"
                            Error (formatNameConflict graph parentId name)
                        elif parentId = GraphBuild.rootId then
                            let isOwnerChild (fid: NodeId) (kids: ChildNode list) =
                                kids
                                |> List.exists (fun c ->
                                    c.id = fid
                                    && Node.childOwnership graph parentId c
                                       = Ownership.Owner)
                            let ownerCount (fid: NodeId) (kids: ChildNode list) =
                                kids
                                |> List.filter (fun c ->
                                    c.id = fid
                                    && Node.childOwnership graph parentId c
                                       = Ownership.Owner)
                                |> List.length
                            let folderError =
                                GraphBuild.systemFolderNodes
                                |> List.tryPick (fun (fid, label) ->
                                    if isOwnerChild fid children
                                       && not (isOwnerChild fid updatedChildren) then
                                        Some $"cannot remove {label} owner child from root"
                                    elif ownerCount fid updatedChildren <> 1 then
                                        Some
                                            $"{label} must appear exactly once as an Owner child of root"
                                    else
                                        None)
                            match folderError with
                            | Some msg -> Error msg
                            | None -> Ok (commit updatedChildren)
                        elif parentId = GraphBuild.systemId then
                            let ownedIds (kids: ChildNode list) =
                                kids
                                |> List.choose (fun c ->
                                    if Node.childOwnership graph parentId c
                                       = Ownership.Owner then
                                        Some c.id
                                    else
                                        None)
                                |> Set.ofList
                            let before = ownedIds children
                            let after = ownedIds updatedChildren
                            let removed = Set.difference before after
                            let added = Set.difference after before
                            // Add detached Special stubs. Normal outline children remain movable.
                            let isMoveIn id =
                                match
                                    graph.nodes.[id].kind,
                                    Map.tryFind id graph.ownerParentByChild
                                with
                                | Special _, Some p when p <> GraphBuild.systemId -> true
                                | _ -> false
                            if
                                removed
                                |> Seq.exists
                                    (GraphBuild.isSpecialSystemDirectoryMember graph)
                            then
                                Error "cannot remove Special owned children under SYSTEM"
                            elif added |> Seq.exists isMoveIn then
                                Error "cannot move existing Special nodes under SYSTEM"
                            else
                                Ok (commit updatedChildren)
                        elif
                            updatedChildren
                            |> List.exists (fun c ->
                                Node.childOwnership graph parentId c = Ownership.Owner
                                && GraphBuild.isSystemFolderNode c.id)
                        then
                            Error "trash, workspaces, and system may not be OWNED by a non-root parent"
                        elif
                            updatedChildren
                            |> List.exists (fun c ->
                                Node.childOwnership graph parentId c = Ownership.Owner
                                && GraphBuild.isSpecialSystemDirectoryMember graph c.id)
                        then
                            Error
                                "Special SYSTEM members may not be OWNED by a non-SYSTEM parent"
                        else
                            Ok (commit updatedChildren)
