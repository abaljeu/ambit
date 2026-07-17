namespace Gambol.Shared

/// Graph mutations: setText/Name/Classes/DocumentState and Replace.
module GraphMutate =

    let setText
        (nodeId: NodeId)
        (oldText: string)
        (newText: string)
        (graph: Graph)
        : Result<Graph, string>
        =
        if nodeId = GraphBuild.rootId then
            Error "cannot modify canonical root text"
        elif nodeId = GraphBuild.trashId then
            Error "cannot modify trash node text"
        elif nodeId = GraphBuild.workspacesId then
            Error "cannot modify workspaces node text"
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
        elif nodeId = GraphBuild.trashId then
            Error "cannot set classes on trash node"
        elif nodeId = GraphBuild.workspacesId then
            Error "cannot set classes on workspaces node"
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
        elif nodeId = GraphBuild.trashId then
            Error "cannot modify trash node name"
        elif nodeId = GraphBuild.workspacesId then
            Error "cannot modify workspaces node name"
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
                                        (GraphQuery.childrenOf graph parentId)
                                        (Some nodeId)
                                    |> List.exists (fun n -> n = newNameLower)
                        if hasConflict then
                            Error "name conflict"
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

                        match child.ref, childNode.kind with
                        | Ownership.Owner, Special Workspace
                            when parentId <> GraphBuild.workspacesId ->
                            Some "Workspace nodes may only be placed under Workspaces"
                        | Ownership.Owner, (Special File | Special Directory)
                            when child.id <> GraphBuild.trashId ->
                            if GraphQuery.canOwn graph parentId child.id then
                                None
                            else
                                Some
                                    "File and Directory nodes must have a Workspace or Directory owner ancestor (not under a File)"
                        | _ -> None)

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
                            GraphQuery.siblingOwnedNameConflict graph updatedChildren
                            || GraphQuery.artifactNameConflict graph parentId updatedChildren

                        if hasNameConflict then
                            Error "name conflict"
                        elif parentId = GraphBuild.rootId then
                            let hadTrashOwner =
                                children
                                |> List.exists (fun c ->
                                    c.id = GraphBuild.trashId && c.ref = Ownership.Owner)
                            let hasTrashOwnerAfter =
                                updatedChildren
                                |> List.exists (fun c ->
                                    c.id = GraphBuild.trashId && c.ref = Ownership.Owner)
                            let hadWorkspacesOwner =
                                children
                                |> List.exists (fun c ->
                                    c.id = GraphBuild.workspacesId && c.ref = Ownership.Owner)
                            let hasWorkspacesOwnerAfter =
                                updatedChildren
                                |> List.exists (fun c ->
                                    c.id = GraphBuild.workspacesId && c.ref = Ownership.Owner)

                            if hadTrashOwner && not hasTrashOwnerAfter then
                                Error "cannot remove trash owner child from root"
                            elif hadWorkspacesOwner && not hasWorkspacesOwnerAfter then
                                Error "cannot remove workspaces owner child from root"
                            elif
                                updatedChildren
                                |> List.filter (fun c ->
                                    c.id = GraphBuild.trashId && c.ref = Ownership.Owner)
                                |> List.length
                                <> 1
                            then
                                Error "trash must appear exactly once as an Owner child of root"
                            elif
                                updatedChildren
                                |> List.filter (fun c ->
                                    c.id = GraphBuild.workspacesId && c.ref = Ownership.Owner)
                                |> List.length
                                <> 1
                            then
                                Error "workspaces must appear exactly once as an Owner child of root"
                            else
                                let updatedParent =
                                    NodeUpdateTime.touch { parent with children = updatedChildren }
                                let nodes = graph.nodes |> Map.add parentId updatedParent
                                Ok (GraphBuild.fromNodes graph.root nodes)
                        elif
                            updatedChildren
                            |> List.exists (fun c ->
                                c.ref = Ownership.Owner
                                && (c.id = GraphBuild.trashId || c.id = GraphBuild.workspacesId))
                        then
                            Error "trash and workspaces may not be OWNED by a non-root parent"
                        else
                            let updatedParent =
                                NodeUpdateTime.touch { parent with children = updatedChildren }
                            let nodes = graph.nodes |> Map.add parentId updatedParent
                            Ok (GraphBuild.fromNodes graph.root nodes)
