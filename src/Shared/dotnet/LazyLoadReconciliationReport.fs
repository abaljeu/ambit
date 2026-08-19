namespace Gambol.Shared

[<RequireQualifiedAccess>]
module LazyLoadReconciliationReport =

    module Path = LazyLoadReconciliationPath

    type Failure =
        { path: string
          message: string }

    type Report =
        { ops: Op list
          failures: Failure list }

    type private PathItem =
        { path: string
          info: Path.PathInfo }

    type private RenameItem =
        { oldPath: string
          newPath: string
          oldInfo: Path.PathInfo
          newInfo: Path.PathInfo }

    let private classifyPaths paths =
        paths
        |> List.fold (fun (items, failures) path ->
            match Path.classifyValidated path with
            | Ok(Some info) -> items @ [ { path = path; info = info } ], failures
            | Ok None -> items, failures
            | Error message ->
                items, failures @ [ { path = path; message = message } ]) ([], [])

    let private classifyRename oldPath newPath =
        let oldResult = Path.classifyValidated oldPath
        let newResult = Path.classifyValidated newPath
        let failures =
            [ match oldResult with
              | Error message -> yield { path = oldPath; message = message }
              | _ -> ()
              match newResult with
              | Error message -> yield { path = newPath; message = message }
              | _ -> () ]
        match oldResult, newResult with
        | Ok(Some oldInfo), Ok(Some newInfo) ->
            Some
                { oldPath = oldPath
                  newPath = newPath
                  oldInfo = oldInfo
                  newInfo = newInfo }, failures
        | _ -> None, failures

    let private classifyRenames paths =
        paths
        |> List.fold (fun (items, failures) (oldPath, newPath) ->
            let item, nextFailures = classifyRename oldPath newPath
            match item with
            | Some value -> items @ [ value ], failures @ nextFailures
            | None -> items, failures @ nextFailures) ([], [])

    let private removeDeleteAddConflicts deleted added =
        let conflicts left right =
            left
            |> List.filter (fun candidate ->
                right
                |> List.exists (fun other ->
                    candidate.info.parts = other.info.parts
                    && candidate.info.kind <> other.info.kind))
        let conflictingDeleted = conflicts deleted added
        let conflictingAdded = conflicts added deleted
        let failure item =
            { path = item.path
              message = "kind conflict between deleted and added path" }
        let isConflicting item conflicts =
            conflicts |> List.exists (fun conflict -> conflict.path = item.path)
        let acceptedDeleted =
            deleted |> List.filter (fun item -> not (isConflicting item conflictingDeleted))
        let acceptedAdded =
            added |> List.filter (fun item -> not (isConflicting item conflictingAdded))
        acceptedDeleted, acceptedAdded, (conflictingDeleted @ conflictingAdded |> List.map failure)

    let private foldItems pathFor planner state items =
        items
        |> List.fold (fun (graph, ops, failures) item ->
            match planner graph item with
            | Ok(next, nextOps) -> next, ops @ nextOps, failures
            | Error message ->
                graph,
                ops,
                failures @ [ { path = pathFor item; message = message } ]) state

    let private isExistingCurrent graph workspaceId (info: Path.PathInfo) =
        match Path.resolveInfo graph workspaceId info with
        | Ok(Some(nodeId, _)) ->
            graph.nodes.[nodeId].documentState = Current
        | _ -> false

    let private foldAdded workspaceId state items =
        items
        |> List.fold (fun ((graph, ops, failures), accepted) item ->
            if isExistingCurrent graph workspaceId item.info then
                ((graph, ops, failures), accepted)
            else
                match LazyLoadReconciliation.planAddedInfo graph workspaceId item.info with
                | Ok(next, nextOps) ->
                    (next, ops @ nextOps, failures),
                    accepted @ [ item, nextOps ]
                | Error message ->
                    (graph, ops, failures @ [ { path = item.path; message = message } ]),
                    accepted) (state, [])

    let private protectedDirectoryIds graph workspaceId renames =
        renames
        |> List.choose (fun rename ->
            if rename.oldInfo.isDirInfo && rename.newInfo.isDirInfo then
                match Path.resolveInfo graph workspaceId rename.oldInfo with
                | Ok(Some(nodeId, _)) -> Some nodeId
                | _ -> None
            else
                None)
        |> Set.ofList

    let private orderedRenames renames =
        let pairs = renames |> List.map (fun item -> item.oldInfo, item.newInfo)
        let directoryFiles = LazyLoadReconciliation.directoryFileMoves pairs
        renames
        |> List.sortBy (fun item -> if item.oldInfo.isDirInfo then 0 else 1)
        |> List.filter (fun item ->
            item.oldInfo.isDirInfo
            || not (
                LazyLoadReconciliation.coveredByDirInfoMove
                    directoryFiles
                    item.oldInfo
                    item.newInfo))

    let private finalizeAdded artifacts workspaceId graph (item, createOps) =
        LazyLoadReconciliationApply.markAddedDocumentsUnparsed
            graph
            workspaceId
            [ item.info ]
            createOps
        |> Result.bind (fun (withFiles, fileOps) ->
            if
                LazyLoadReconciliationApply.skipParseAddedDirInfo
                    withFiles
                    workspaceId
                    createOps
                    item.info
            then
                Ok(withFiles, fileOps)
            else
                LazyLoadReconciliationApply.parseDirInfoIfPresent
                    withFiles
                    workspaceId
                    artifacts
                    item.info
                |> Result.map (fun (finalGraph, parseOps) ->
                    finalGraph, fileOps @ parseOps))

    let planChangedPathsWithArtifacts
        (graph: Graph)
        (workspaceLabel: string)
        (changes: LazyLoadReconciliation.ChangedPath list)
        (artifacts: Map<string, string>)
        : Result<Report, string> =
        GraphQuery.trySyncRootByLabel graph workspaceLabel
        |> Result.bind (fun workspaceId ->
            let deleted, deletedFailures =
                changes
                |> List.choose (function
                    | LazyLoadReconciliation.Deleted path -> Some path
                    | _ -> None)
                |> classifyPaths
            let added, addedFailures =
                changes
                |> List.choose (function
                    | LazyLoadReconciliation.Added path -> Some path
                    | _ -> None)
                |> classifyPaths
            let modified, modifiedFailures =
                changes
                |> List.choose (function
                    | LazyLoadReconciliation.Modified path -> Some path
                    | _ -> None)
                |> classifyPaths
            let renames, renameFailures =
                changes
                |> List.choose (function
                    | LazyLoadReconciliation.Renamed(oldPath, newPath) ->
                        Some(oldPath, newPath)
                    | _ -> None)
                |> classifyRenames
            let deleted, added, conflictFailures =
                removeDeleteAddConflicts deleted added
            let initialFailures =
                deletedFailures
                @ addedFailures
                @ modifiedFailures
                @ renameFailures
                @ conflictFailures
            let protectedIds = protectedDirectoryIds graph workspaceId renames
            let afterDeletes =
                deleted
                |> List.sortByDescending (fun item -> item.info.parts.Length)
                |> foldItems
                    (fun item -> item.path)
                    (fun current item ->
                        LazyLoadReconciliation.planDeletedInfo
                            protectedIds
                            current
                            workspaceId
                            item.info)
                    (graph, [], initialFailures)
            let afterRenames =
                orderedRenames renames
                |> foldItems
                    (fun item -> $"{item.oldPath} -> {item.newPath}")
                    (fun current item ->
                        LazyLoadReconciliation.planRenamedInfo
                            current
                            workspaceId
                            item.oldInfo
                            item.newInfo)
                    afterDeletes
            let afterAdds, acceptedAdds = foldAdded workspaceId afterRenames added
            let afterModified =
                modified
                |> foldItems
                    (fun item -> item.path)
                    (fun current item ->
                        LazyLoadReconciliation.invalidateOrParseModified
                            artifacts
                            workspaceId
                            current
                            item.info)
                    afterAdds
            let _, ops, failures =
                acceptedAdds
                |> foldItems
                    (fun (item, _) -> item.path)
                    (finalizeAdded artifacts workspaceId)
                    afterModified
            Ok { ops = ops; failures = failures })
