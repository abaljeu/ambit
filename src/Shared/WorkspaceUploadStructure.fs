namespace Gambol.Shared

open System

/// Client-first Upload: inventory paths → Directory/File stub ops.
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
        let initial: State =
            { graph = graph
              eventId = EventId.zero }

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
        GraphQuery.trySyncRootByLabel graph label

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
        let directoryFileOwnerParts =
            if item.isDirectory then
                None
            else
                DocumentArtifactPath.tryDirectoryFileOwnerParts item.relative

        let parts =
            directoryFileOwnerParts
            |> Option.defaultWith (fun () -> pathParts item.relative)

        if parts.IsEmpty then
            Ok(graph, [])
        else
            let kind =
                if item.isDirectory || directoryFileOwnerParts.IsSome then
                    Directory
                else
                    File

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

    /// Files confirmed present by PUT or mtime skip become parseable stubs.
    let planServerFilePresentOps
        (graph: Graph)
        (workspaceLabel: string)
        (paths: string list)
        : Op list =
        paths
        |> List.distinct
        |> List.choose (fun relative ->
            tryResolveFileNode graph workspaceLabel relative
            |> Option.bind (fun fileId ->
                match Node.documentStateEnum graph.nodes.[fileId] with
                | PullingFile -> None
                | Current
                | Unparsed -> None))

    /// Locked #11: reparse after mtime-skip applies to Unparsed only.
    /// Current files are already loaded; reparsing them rewrites DataDir and
    /// advances graph stamps past the untouched desktop mtime.
    let shouldReparseAfterMtimeSkip =
        function
        | Unparsed -> true
        | Current
        | PullingFile -> false

    /// Persist stamp `SetUpdateTime` node ids that resolve to a File download
    /// scope, as AutoDownloadTarget values for auto-download coalescing.
    let autoDownloadFileTargets
        (graph: Graph)
        (ops: Op list)
        : AutoDownloadTarget list =
        ops
        |> List.choose (function
            | Op.SetUpdateTime(nodeId, _, _) -> Some nodeId
            | _ -> None)
        |> List.choose (fun nodeId ->
            match WorkspaceSyncScope.tryFromFocus graph nodeId with
            | Ok scope when scope.kind = SyncScopeKind.File ->
                Some { label = scope.label; relative = scope.relative }
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

    /// Ops for one Change: Directory/File stubs, reuse owned paths.
    /// `items` must already be the volume-capped path set (1:1).
    /// Unloaded workspaces skip inventing stubs: empty children would create
    /// new ids that collide with the server's resident names on POST /changes.
    let planStubOps
        (graph: Graph)
        (workspaceLabel: string)
        (items: InventoryItem list)
        : Result<Op list, string> =
        workspaceByLabel graph workspaceLabel
        |> Result.bind (fun workspaceId ->
            match Map.tryFind workspaceId graph.nodes with
            | Some _ when not (GraphChildren.isLoaded graph workspaceId) ->
                Ok []
            | _ ->
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
                |> Result.map (fun (_, reversed) -> List.rev reversed))