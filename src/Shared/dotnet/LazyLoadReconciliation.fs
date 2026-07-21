namespace Gambol.Shared

[<RequireQualifiedAccess>]
module LazyLoadReconciliation =

    module Path = LazyLoadReconciliationPath

    type ChangedPath =
        | Added of path: string
        | Deleted of path: string
        | Renamed of oldPath: string * newPath: string
        | Modified of path: string

    let private applyOps = LazyLoadReconciliationApply.applyOps
    let private markUnparsed = LazyLoadReconciliationApply.markUnparsed

    let private createChild graph parentId kind name =
        match kind with
        | Directory ->
            FileNodeOps.planCreateOwnedDirectory graph parentId name |> Ok
        | File ->
            FileNodeOps.planCreateOwnedFile graph parentId name |> Ok
        | _ -> Error "reconciliation can create only directory and file stubs"

    let private ensureChild graph parentId kind name =
        match Path.ownedArtifactNamed graph parentId name with
        | Some node when node.kind = Special kind -> Ok(node.id, graph, [])
        | Some node ->
            Error $"kind conflict at '{name}': expected {kind}, found {node.kind}"
        | None ->
            match createChild graph parentId kind name with
            | Error err -> Error err
            | Ok(childId, ops) ->
                match applyOps graph ops with
                | Error err -> Error err
                | Ok next -> Ok(childId, next, ops)

    let private planPath (graph: Graph) workspaceId finalKind (parts: string list) =
        let lastIndex = parts.Length - 1
        parts
        |> List.indexed
        |> List.fold (fun result (index, name) ->
            match result with
            | Error err -> Error err
            | Ok(parentId, current, planned) ->
                let kind = if index = lastIndex then finalKind else Directory
                match ensureChild current parentId kind name with
                | Error err -> Error err
                | Ok(childId, next, ops) ->
                    Ok(childId, next, planned @ ops)) (Ok(workspaceId, graph, []))

    let resolveOwnedPath (graph: Graph) (workspaceLabel: string) (path: string) =
        Path.workspaceByLabel graph workspaceLabel
        |> Result.bind (fun workspaceId ->
            Path.classifyValidated path
            |> Result.bind (function
                | None -> Ok None
                | Some info -> Path.resolveInfo graph workspaceId info))

    let private planAddedInfo (graph: Graph) workspaceId (info: Path.PathInfo) =
        if info.parts.IsEmpty then
            Ok(graph, [])
        else
            planPath graph workspaceId info.kind info.parts
            |> Result.map (fun (_, next, createOps) -> next, createOps)

    let private pathChildIds (graph: Graph) parentId =
        GraphQuery.ownedArtifactsInDirectory graph parentId None None
        |> List.choose (fun childId ->
            match graph.nodes.[childId].kind with
            | Special (Directory | File) -> Some childId
            | _ -> None)

    let private refReplacementOps (graph: Graph) nodeId path =
        ViewModel.getAllOccurrences graph nodeId
        |> List.filter (fun (_, _, child) -> child.ref = Ownership.Ref)
        |> List.collect (fun (parentId, index, oldChild) ->
            let replacementId = NodeId.New()
            [ Op.NewNode(replacementId, $"[[{path}]]")
              Op.Replace(
                  parentId,
                  index,
                  [ oldChild ],
                  [ { ref = Ownership.Owner; id = replacementId } ]) ])

    let private planTrashNode (graph: Graph) nodeId =
        match Map.tryFind nodeId graph.ownerParentByChild with
        | None -> Ok(graph, [])
        | Some parentId when parentId = Graph.trashId -> Ok(graph, [])
        | Some parentId ->
            let parent = graph.nodes.[parentId]
            let index =
                parent.children
                |> List.findIndex (fun child ->
                    child.id = nodeId && child.ref = Ownership.Owner)
            let ownerChild = parent.children.[index]
            let path =
                NodeDesktopPath.pathForNodeId graph nodeId
                |> Option.defaultValue ""
            let refOps = refReplacementOps graph nodeId path
            let trashIndex = graph.nodes.[Graph.trashId].children.Length
            let ops =
                refOps
                @ [ Op.Replace(parentId, index, [ ownerChild ], [])
                    Op.Replace(Graph.trashId, trashIndex, [], [ ownerChild ]) ]
            applyOps graph ops |> Result.map (fun next -> next, ops)

    let rec private cleanupEmptyDirectories protectedIds (graph: Graph) parentId =
        if parentId = Graph.rootId
           || parentId = Graph.workspacesId
           || Set.contains parentId protectedIds then
            Ok(graph, [])
        else
            match Map.tryFind parentId graph.nodes with
            | Some { kind = Special Directory }
                when pathChildIds graph parentId |> List.isEmpty ->
                let nextParent =
                    graph.ownerParentByChild
                    |> Map.tryFind parentId
                    |> Option.defaultValue Graph.rootId
                planTrashNode graph parentId
                |> Result.bind (fun (next, ops) ->
                    cleanupEmptyDirectories protectedIds next nextParent
                    |> Result.map (fun (finalGraph, cleanupOps) ->
                        finalGraph, ops @ cleanupOps))
            | _ -> Ok(graph, [])

    let private planDeletedInfo protectedIds
        (graph: Graph) workspaceId (info: Path.PathInfo) =
        if info.isDirInfo then
            Ok(graph, [])
        else
            Path.resolveInfo graph workspaceId info
            |> Result.bind (function
                | None -> Ok(graph, [])
                | Some(nodeId, _) ->
                    let parentId = graph.ownerParentByChild.[nodeId]
                    planTrashNode graph nodeId
                    |> Result.bind (fun (next, ops) ->
                        cleanupEmptyDirectories protectedIds next parentId
                        |> Result.map (fun (finalGraph, cleanupOps) ->
                            finalGraph, ops @ cleanupOps)))

    let private ensureDirectoryPath (graph: Graph) workspaceId parts =
        match parts with
        | [] -> Ok(workspaceId, graph, [])
        | _ -> planPath graph workspaceId Directory parts

    let private planMoveNode (graph: Graph) workspaceId nodeId (newInfo: Path.PathInfo) =
        match List.rev newInfo.parts with
        | [] -> Ok(graph, [])
        | newName :: reversedParent ->
            let parentParts = List.rev reversedParent
            ensureDirectoryPath graph workspaceId parentParts
            |> Result.bind (fun (newParentId, withParents, parentOps) ->
                match Path.ownedArtifactNamed withParents newParentId newName with
                | Some target when target.id <> nodeId ->
                    Error $"kind conflict at rename target '{newName}'"
                | _ ->
                    let node = withParents.nodes.[nodeId]
                    NodeRenameOps.planRenameNode withParents nodeId newName
                    |> Result.map fst
                    |> Result.bind (fun renameOps ->
                        applyOps withParents renameOps
                        |> Result.bind (fun renamed ->
                        let oldParentId = renamed.ownerParentByChild.[nodeId]
                        let reparentOps =
                            if oldParentId = newParentId then
                                []
                            else
                                let oldParent = renamed.nodes.[oldParentId]
                                let oldIndex =
                                    oldParent.children
                                    |> List.findIndex (fun child ->
                                        child.id = nodeId
                                        && child.ref = Ownership.Owner)
                                let ownerChild = oldParent.children.[oldIndex]
                                let newIndex =
                                    Graph.fileTreeInsertIndex renamed newParentId
                                [ Op.Replace(
                                      oldParentId,
                                      oldIndex,
                                      [ ownerChild ],
                                      [])
                                  Op.Replace(
                                      newParentId,
                                      newIndex,
                                      [],
                                      [ ownerChild ]) ]
                        applyOps renamed reparentOps
                        |> Result.bind (fun moved ->
                            cleanupEmptyDirectories Set.empty moved oldParentId
                            |> Result.map (fun (finalGraph, cleanupOps) ->
                                finalGraph,
                                parentOps @ renameOps @ reparentOps @ cleanupOps)))))

    let private planRenamedInfo (graph: Graph) workspaceId
        (oldInfo: Path.PathInfo) (newInfo: Path.PathInfo) =
        if oldInfo.kind <> newInfo.kind then
            Error "kind conflict between rename source and target"
        else
            Path.resolveInfo graph workspaceId oldInfo
            |> Result.bind (function
                | Some(nodeId, _) ->
                    planMoveNode graph workspaceId nodeId newInfo
                | None ->
                    Path.resolveInfo graph workspaceId newInfo
                    |> Result.map (fun _ -> graph, []))

    let private startsWithParts (prefix: string list) (parts: string list) =
        prefix.Length < parts.Length
        && List.forall2 (=) prefix (List.take prefix.Length parts)

    let private markerMoves (renames: (Path.PathInfo * Path.PathInfo) list) =
        renames
        |> List.choose (fun (oldInfo, newInfo) ->
            if oldInfo.isDirInfo && newInfo.isDirInfo
               && not oldInfo.parts.IsEmpty
               && not newInfo.parts.IsEmpty then
                Some(oldInfo.parts, newInfo.parts)
            else
                None)

    let private coveredByDirInfoMove markerPairs
        (oldInfo: Path.PathInfo) (newInfo: Path.PathInfo) =
        markerPairs
        |> List.exists (fun (oldPrefix, newPrefix) ->
            startsWithParts oldPrefix oldInfo.parts
            && startsWithParts newPrefix newInfo.parts
            && List.skip oldPrefix.Length oldInfo.parts
               = List.skip newPrefix.Length newInfo.parts)

    let private foldPlans planner initial items =
        items
        |> List.fold (fun result item ->
            result
            |> Result.bind (fun (graph, planned) ->
                planner graph item
                |> Result.map (fun (next, ops) -> next, planned @ ops))) (Ok initial)

    let private conflictFromDeleteAdd
        (deleted: Path.PathInfo list) (added: Path.PathInfo list) =
        deleted
        |> List.tryPick (fun oldInfo ->
            added
            |> List.tryPick (fun newInfo ->
                if oldInfo.parts = newInfo.parts
                   && oldInfo.kind <> newInfo.kind then
                    Some "kind conflict between deleted and added path"
                else
                    None))

    let private invalidateOrParseModified
        (artifacts: Map<string, string>)
        workspaceId
        (current: Graph)
        (info: Path.PathInfo)
        =
        if info.isDirInfo
           && LazyLoadReconciliationApply.tryArtifactText artifacts info
              |> Option.isSome then
            LazyLoadReconciliationApply.parseDirInfoIfPresent
                current
                workspaceId
                artifacts
                info
        else
            Path.resolveInfo current workspaceId info
            |> Result.bind (function
                | None -> Ok(current, [])
                | Some(nodeId, _) ->
                    let ops = markUnparsed current nodeId
                    applyOps current ops
                    |> Result.map (fun next -> next, ops))

    let planChangedPathsWithArtifacts
        (graph: Graph)
        (workspaceLabel: string)
        (changes: ChangedPath list)
        (artifacts: Map<string, string>)
        : Result<Op list, string> =
        Path.workspaceByLabel graph workspaceLabel
        |> Result.bind (fun workspaceId ->
            let classifyMany paths =
                paths
                |> List.fold (fun result path ->
                    result
                    |> Result.bind (fun infos ->
                        Path.classifyValidated path
                        |> Result.map (function
                            | None -> infos
                            | Some info -> infos @ [ info ]))) (Ok [])
            let deletedPaths =
                changes |> List.choose (function Deleted path -> Some path | _ -> None)
            let addedPaths =
                changes |> List.choose (function Added path -> Some path | _ -> None)
            let modifiedPaths =
                changes |> List.choose (function Modified path -> Some path | _ -> None)
            let renamePaths =
                changes
                |> List.choose (function
                    | Renamed(oldPath, newPath) -> Some(oldPath, newPath)
                    | _ -> None)
            let renameResult =
                renamePaths
                |> List.fold (fun result (oldPath, newPath) ->
                    result
                    |> Result.bind (fun pairs ->
                        Path.classifyValidated oldPath
                        |> Result.bind (fun oldOpt ->
                            Path.classifyValidated newPath
                            |> Result.map (fun newOpt ->
                                match oldOpt, newOpt with
                                | Some oldInfo, Some newInfo ->
                                    pairs @ [ oldInfo, newInfo ]
                                | _ -> pairs)))) (Ok [])
            match
                classifyMany deletedPaths,
                classifyMany addedPaths,
                classifyMany modifiedPaths,
                renameResult
            with
            | Error err, _, _, _
            | _, Error err, _, _
            | _, _, Error err, _
            | _, _, _, Error err -> Error err
            | Ok deleted, Ok added, Ok modified, Ok renames ->
                match conflictFromDeleteAdd deleted added with
                | Some err -> Error err
                | None ->
                    let markers = markerMoves renames
                    let protectedDirectoryIds =
                        renames
                        |> List.choose (fun (oldInfo, newInfo) ->
                            if oldInfo.isDirInfo && newInfo.isDirInfo then
                                match Path.resolveInfo graph workspaceId oldInfo with
                                | Ok(Some(nodeId, _)) -> Some nodeId
                                | _ -> None
                            else
                                None)
                        |> Set.ofList
                    let orderedRenames =
                        renames
                        |> List.sortBy (fun (oldInfo, _) ->
                            if oldInfo.isDirInfo then 0 else 1)
                        |> List.filter (fun (oldInfo, newInfo) ->
                            oldInfo.isDirInfo
                            || not (coveredByDirInfoMove markers oldInfo newInfo))
                    let deletedDeepest =
                        deleted
                        |> List.sortByDescending (fun info -> info.parts.Length)
                    foldPlans
                        (fun current info ->
                            planDeletedInfo
                                protectedDirectoryIds
                                current
                                workspaceId
                                info)
                        (graph, [])
                        deletedDeepest
                    |> Result.bind (fun afterDeletes ->
                        foldPlans
                            (fun current pair ->
                                planRenamedInfo
                                    current
                                    workspaceId
                                    (fst pair)
                                    (snd pair))
                            afterDeletes
                            orderedRenames)
                    |> Result.bind (fun afterRenames ->
                        foldPlans
                            (fun current info ->
                                planAddedInfo current workspaceId info)
                            afterRenames
                            added)
                    |> Result.bind (fun afterAdds ->
                        foldPlans
                            (invalidateOrParseModified artifacts workspaceId)
                            afterAdds
                            modified)
                    |> Result.bind (fun (finalGraph, planned) ->
                        LazyLoadReconciliationApply.markAddedDocumentsUnparsed
                            finalGraph
                            workspaceId
                            added
                            planned
                        |> Result.bind (fun (withFiles, fileOps) ->
                            LazyLoadReconciliationApply.parseDirInfoInfos
                                withFiles
                                workspaceId
                                artifacts
                                added
                            |> Result.map (fun (_, parseOps) ->
                                fileOps @ parseOps))))

    let planChangedPaths
        (graph: Graph)
        (workspaceLabel: string)
        (changes: ChangedPath list)
        : Result<Op list, string> =
        planChangedPathsWithArtifacts graph workspaceLabel changes Map.empty

    let planAddedPaths (graph: Graph) (workspaceLabel: string) (addedPaths: string list) =
        addedPaths
        |> List.map Added
        |> planChangedPaths graph workspaceLabel

    let planAddedPathsWithArtifacts
        (graph: Graph)
        (workspaceLabel: string)
        (addedPaths: string list)
        (artifacts: Map<string, string>)
        =
        addedPaths
        |> List.map Added
        |> fun changes ->
            planChangedPathsWithArtifacts graph workspaceLabel changes artifacts
