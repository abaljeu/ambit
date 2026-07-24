namespace Gambol.Shared

open System

/// Client-first Upload: inventory paths → Directory/File Unparsed stub ops.
[<RequireQualifiedAccess>]
module WorkspaceUploadStructure =

    let private prependReversed items acc =
        items |> List.fold (fun state item -> item :: state) acc

    /// Inventory row for structure planning (no byte sizes).
    type InventoryItem =
        { relative: string
          isDirectory: bool }

    [<RequireQualifiedAccess>]
    type StructureCap =
        | FullPaths
        | TopLevelOnly

    let private isImmediateChild (scopeRel: string) (relative: string) =
        if relative = scopeRel then
            false
        elif scopeRel = "" then
            relative.IndexOf('/') < 0
        else
            let prefix = scopeRel + "/"

            relative.StartsWith(prefix, StringComparison.Ordinal)
            && relative.IndexOf('/', prefix.Length) < 0

    /// Same path-set rule as volume ladder structure (Full vs TopLevel).
    let capPaths
        (scopeRelative: string)
        (cap: StructureCap)
        (items: InventoryItem list)
        : InventoryItem list =
        match cap with
        | StructureCap.FullPaths -> items
        | StructureCap.TopLevelOnly ->
            items
            |> List.filter (fun i ->
                isImmediateChild scopeRelative i.relative)

    let private applyOps (graph: Graph) (ops: Op list) : Result<Graph, string> =
        let initial =
            { graph = graph
              history = History.empty
              revision = Revision.Zero }

        ops
        |> List.fold
            (fun result op ->
                result
                |> Result.bind (fun state ->
                    match Op.apply op state with
                    | ApplyResult.Changed next
                    | ApplyResult.Unchanged next -> Ok next
                    | ApplyResult.Invalid(_, error) -> Error error))
            (Ok initial)
        |> Result.map (fun state -> state.graph)

    let private ownedArtifactNamed (graph: Graph) parentId name : Node option =
        GraphQuery.ownedArtifactsInDirectory graph parentId None None
        |> List.tryPick (fun nodeId ->
            match Map.tryFind nodeId graph.nodes with
            | Some node ->
                match Filename.tryValue node.name with
                | Some candidate when
                    String.Equals(
                        candidate,
                        name,
                        StringComparison.OrdinalIgnoreCase) ->
                    Some node
                | _ -> None
            | None -> None)

    let private workspaceByLabel (graph: Graph) label : Result<NodeId, string> =
        match
            graph.nodes.[Graph.workspacesId].children
            |> List.tryPick (fun child ->
                if child.ref <> Ownership.Owner then
                    None
                else
                    let node = graph.nodes.[child.id]

                    match node.kind, Filename.tryValue node.name with
                    | Special Workspace, Some n when
                        String.Equals(
                            n,
                            label,
                            StringComparison.OrdinalIgnoreCase) ->
                        Some node
                    | _ -> None)
        with
        | Some node -> Ok node.id
        | None -> Error $"workspace '{label}' not found"

    let private pathParts (relative: string) =
        relative.Replace('\\', '/').Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries)
        |> Array.toList

    let private createChild graph parentId kind name =
        match kind with
        | Directory ->
            FileNodeOps.planCreateOwnedDirectory graph parentId name |> Ok
        | File ->
            FileNodeOps.planCreateOwnedFile graph parentId name |> Ok
        | _ -> Error "upload stubs are directory or file only"

    let private ensureChild graph parentId kind name =
        match ownedArtifactNamed graph parentId name with
        | Some node when node.kind = Special kind ->
            Ok(node.id, graph, [])
        | Some node ->
            Error
                $"kind conflict at '{name}': expected {kind}, found {node.kind}"
        | None ->
            match createChild graph parentId kind name with
            | Error err -> Error err
            | Ok(childId, ops) ->
                match applyOps graph ops with
                | Error err -> Error err
                | Ok next -> Ok(childId, next, ops)

    let private planPath
        (graph: Graph)
        (workspaceId: NodeId)
        (finalKind: SpecialKind)
        (parts: string list)
        : Result<NodeId * Graph * Op list, string> =
        let lastIndex = parts.Length - 1

        parts
        |> List.indexed
        |> List.fold
            (fun result (index, name) ->
                match result with
                | Error err -> Error err
                | Ok(parentId, current, planned) ->
                    let kind =
                        if index = lastIndex then
                            finalKind
                        else
                            Directory

                    match ensureChild current parentId kind name with
                    | Error err -> Error err
                    | Ok(childId, next, ops) ->
                        Ok(childId, next, prependReversed ops planned))
            (Ok(workspaceId, graph, []))
        |> Result.map (fun (nodeId, graph, reversed) ->
            nodeId, graph, List.rev reversed)

    let private markUnparsed (graph: Graph) nodeId =
        match graph.nodes.[nodeId].documentState with
        | Unparsed -> []
        | Current ->
            [ Op.SetDocumentState(nodeId, Current, Unparsed) ]

    let private markParentCurrent (graph: Graph) nodeId =
        match Map.tryFind nodeId graph.nodes with
        | Some { kind = Special (Directory | Workspace)
                 documentState = Unparsed } ->
            [ Op.SetDocumentState(nodeId, Unparsed, Current) ]
        | _ -> []

    let private hasOwnedMember (graph: Graph) (dirId: NodeId) =
        graph.nodes.[dirId].children
        |> List.exists (fun c -> c.ref = Ownership.Owner)

    let private foldStateOps markFn graph nodeIds =
        nodeIds
        |> List.fold
            (fun result nodeId ->
                result
                |> Result.bind (fun (current, ops) ->
                    let stateOps = markFn current nodeId

                    applyOps current stateOps
                    |> Result.map (fun next ->
                        next, prependReversed stateOps ops)))
            (Ok(graph, []))
        |> Result.map (fun (graph, reversed) -> graph, List.rev reversed)

    /// Unparsed Directory/Workspace stubs that already have owned members → Current.
    let private promoteUnparsedDirsWithMembers (graph: Graph) =
        graph.nodes
        |> Map.toList
        |> List.choose (fun (id, node) ->
            match node.kind, node.documentState with
            | Special (Directory | Workspace), Unparsed when hasOwnedMember graph id ->
                Some id
            | _ -> None)
        |> foldStateOps markParentCurrent graph

    let private createdStubIds (planned: Op list) =
        planned
        |> List.choose (function
            | Op.NewSpecialNode(nodeId, (File | Directory), _) ->
                Some nodeId
            | _ -> None)
        |> List.distinct

    let private markNewStubsUnparsed graph planned =
        let stubIds = createdStubIds planned

        foldStateOps markUnparsed graph stubIds
        |> Result.bind (fun (withStubs, stubOps) ->
            let parentIds =
                stubIds
                |> List.choose (fun id ->
                    Map.tryFind id withStubs.ownerParentByChild)
                |> List.distinct

            foldStateOps markParentCurrent withStubs parentIds
            |> Result.bind (fun (withParents, parentOps) ->
                promoteUnparsedDirsWithMembers withParents
                |> Result.map (fun (_, memberOps) ->
                    stubOps @ parentOps @ memberOps)))

    let private orderForStubs (items: InventoryItem list) =
        let depth (rel: string) =
            if rel = "" then
                0
            else
                rel.Split('/').Length

        let dirs =
            items
            |> List.filter (fun i -> i.isDirectory)
            |> List.sortBy (fun i -> depth i.relative, i.relative)

        let files =
            items
            |> List.filter (fun i -> not i.isDirectory)
            |> List.sortBy (fun i -> i.relative)

        dirs @ files

    let private planOne
        (graph: Graph)
        workspaceId
        (item: InventoryItem)
        =
        let parts = pathParts item.relative

        if parts.IsEmpty then
            Ok(graph, [])
        else
            let kind = if item.isDirectory then Directory else File

            planPath graph workspaceId kind parts
            |> Result.map (fun (_, next, ops) -> next, ops)

    /// Resolve an owned File node under workspace by relative path.
    let tryResolveFileNode
        (graph: Graph)
        (workspaceLabel: string)
        (relative: string)
        : NodeId option =
        match workspaceByLabel graph workspaceLabel with
        | Error _ -> None
        | Ok workspaceId ->
            let parts = pathParts relative

            if parts.IsEmpty then
                None
            else
                let lastIndex = parts.Length - 1

                parts
                |> List.indexed
                |> List.fold
                    (fun acc (index, name) ->
                        match acc with
                        | None -> None
                        | Some parentId ->
                            let expected =
                                if index = lastIndex then File else Directory

                            match ownedArtifactNamed graph parentId name with
                            | Some node when node.kind = Special expected ->
                                Some node.id
                            | _ -> None)
                    (Some workspaceId)
                |> Option.bind (fun nodeId ->
                    match Map.tryFind nodeId graph.nodes with
                    | Some { kind = Special File } -> Some nodeId
                    | _ -> None)

    /// After download: SetUpdateTime so graph matches local/server mtime (Locked #7).
    let planAlignFileStampOps
        (graph: Graph)
        (workspaceLabel: string)
        (stamps: (string * DateTime) list)
        : Op list =
        stamps
        |> List.choose (fun (relative, mtimeUtc) ->
            match tryResolveFileNode graph workspaceLabel relative with
            | None -> None
            | Some fileId ->
                let node = graph.nodes.[fileId]
                let stamp = NodeUpdateTime.toDbPrecision mtimeUtc

                if node.updateTime = stamp then
                    None
                else
                    Some(Op.SetUpdateTime(fileId, node.updateTime, stamp)))

    /// Ops for one Change: Directory/File stubs, reuse owned paths, Unparsed.
    /// `items` must already be the volume-capped path set (1:1).
    let planStubOps
        (graph: Graph)
        (workspaceLabel: string)
        (items: InventoryItem list)
        : Result<Op list, string> =
        workspaceByLabel graph workspaceLabel
        |> Result.bind (fun workspaceId ->
            let ordered = orderForStubs items

            ordered
            |> List.fold
                (fun result item ->
                    result
                    |> Result.bind (fun (current, planned) ->
                        planOne current workspaceId item
                        |> Result.map (fun (next, ops) ->
                            next, prependReversed ops planned)))
                (Ok(graph, []))
            |> Result.bind (fun (afterCreates, createOpsReversed) ->
                let createOps = List.rev createOpsReversed
                markNewStubsUnparsed afterCreates createOps
                |> Result.map (fun stateOps -> createOps @ stateOps)))
