namespace Gambol.Server

open System.IO
open Gambol.Shared

[<RequireQualifiedAccess>]
module LazyLoadReconciliationServer =

    let private isDirInfoPath (path: string) =
        DocumentArtifactPath.isDirectoryFile path

    /// Core handle, data directory, and workspace label for one reconcile.
    type WorkspaceReconcile =
        { handle: CoreChangesDoor
          dataDir: string
          workspaceLabel: string }

    /// Added Current Directory Files are skipped at finalize; do not read them.
    /// Modified / renamed / Unparsed / missing stubs still need artifact text.
    let private needsDirInfoArtifactRead
        (graph: Graph)
        (workspaceLabel: string)
        (change: LazyLoadReconciliation.ChangedPath)
        =
        match change with
        | LazyLoadReconciliation.Modified path
        | LazyLoadReconciliation.Renamed(_, path) when isDirInfoPath path ->
            true
        | LazyLoadReconciliation.Added path when isDirInfoPath path ->
            match
                LazyLoadReconciliation.resolveOwnedPath
                    graph
                    workspaceLabel
                    path
            with
            | Ok(Some(nodeId, _)) ->
                Node.documentStateEnum graph.nodes.[nodeId] <> Current
            | Ok None -> true
            | Error _ -> true
        | _ -> false

    let private readDirInfoArtifacts
        (graph: Graph)
        (workspace: WorkspaceReconcile)
        (changedPaths: LazyLoadReconciliation.ChangedPath list)
        : Map<string, string> =
        let root = Path.Combine(workspace.dataDir, workspace.workspaceLabel)
        let pathsToRead =
            changedPaths
            |> List.collect (fun change ->
                if needsDirInfoArtifactRead graph workspace.workspaceLabel change then
                    match change with
                    | LazyLoadReconciliation.Added path
                    | LazyLoadReconciliation.Modified path when
                        isDirInfoPath path ->
                        [ path ]
                    | LazyLoadReconciliation.Renamed(_, newPath) when
                        isDirInfoPath newPath ->
                        [ newPath ]
                    | _ -> []
                else
                    [])
            |> List.distinct
        pathsToRead
        |> List.choose (fun relative ->
            let full =
                Path.Combine(
                    root,
                    relative.Replace('/', Path.DirectorySeparatorChar))
            if File.Exists full then
                try
                    Some(relative.Replace('\\', '/'), File.ReadAllText full)
                with _ ->
                    None
            else
                None)
        |> Map.ofList

    let private logFailures
        (workspaceLabel: string)
        (failures: LazyLoadReconciliationReport.Failure list)
        =
        for failure in failures do
            eprintfn
                "[LazyLoadReconciliation] '%s' path '%s': %s"
                workspaceLabel
                failure.path
                failure.message

    type private ListedReconcile =
        { handle: CoreChangesDoor
          dataDir: string
          workspaceLabel: string
          graph: Graph
          changes: LazyLoadReconciliation.ChangedPath list
          artifactReads: LazyLoadReconciliation.ChangedPath list
          beside: Op list -> unit }

    /// Last `PullingFile` to `Current` per node. A later state op drops it.
    /// `DocumentOpImpact.noticesBeside` has no Notice for that pair.
    let private parsedFromPulling (ops: Op list) : NodeId list =
        ops
        |> List.fold
            (fun acc op ->
                match op with
                | Op.SetDocumentState(id, PullingFile, Current) ->
                    Map.add id true acc
                | Op.SetDocumentState(id, _, _) -> Map.add id false acc
                | _ -> acc)
            Map.empty
        |> Map.fold
            (fun acc id keep -> if keep then id :: acc else acc)
            []

    let private postParsedParents (host: MailboxHost) (parentIds: NodeId list) =
        parentIds
        |> List.iter (fun id ->
            CoreMailboxApi.postNotice host (Notice.NodeParsed id))

    let private postParsedFromPulling (host: MailboxHost) (ops: Op list) =
        postParsedParents host (parsedFromPulling ops)

    let private postReconcileReport
        (listed: ListedReconcile)
        (report: LazyLoadReconciliationReport.Report)
        =
        async {
            logFailures listed.workspaceLabel report.failures
            let postParents () =
                postParsedParents
                    listed.handle.host
                    report.parsedParentIds
            match report.ops with
            | [] ->
                postParents ()
                return Ok report.failures
            | ops ->
                let! result =
                    GraphOnlyChangePost.postChunks
                        (CoreDoor.postGraphOnly listed.handle)
                        "Parse"
                        (GraphOnlyChangeChunks.split ops)
                match result with
                | Ok () ->
                    listed.beside ops
                    postParents ()
                    postParsedFromPulling listed.handle.host ops
                    return Ok report.failures
                | Error err -> return Error err
        }

    /// Plan and post. `artifactReads` is the set whose text is read.
    let private reconcileListed plan (listed: ListedReconcile) =
        async {
            let artifacts =
                readDirInfoArtifacts
                    listed.graph
                    { handle = listed.handle
                      dataDir = listed.dataDir
                      workspaceLabel = listed.workspaceLabel }
                    listed.artifactReads
            match
                plan
                    listed.graph
                    listed.workspaceLabel
                    listed.changes
                    artifacts
            with
            | Error err -> return Error err
            | Ok report ->
                return! postReconcileReport listed report
        }

    /// Existing File nodes the git pull marks Unparsed: a Modified file
    /// not yet Unparsed, an Added path that names a PullingFile stub,
    /// and a Renamed file whose plan marks that node Unparsed.
    let private receivedFileIds
        (graph: Graph)
        (workspaceLabel: string)
        (files: LazyLoadReconciliation.ChangedPath list)
        (ops: Op list)
        : NodeId list =
        let existingFile path =
            match LazyLoadReconciliation.resolveOwnedPath graph workspaceLabel path with
            | Ok(Some(id, File)) -> Some(id, Node.documentStateEnum graph.nodes.[id])
            | _ -> None
        let planMarksUnparsed id =
            ops
            |> List.exists (function
                | Op.SetDocumentState(found, _, Unparsed) -> found = id
                | _ -> false)
        files
        |> List.choose (function
            | LazyLoadReconciliation.Modified path ->
                match existingFile path with
                | Some(id, (Current | PullingFile)) -> Some id
                | _ -> None
            | LazyLoadReconciliation.Added path ->
                match existingFile path with
                | Some(id, PullingFile) -> Some id
                | _ -> None
            | LazyLoadReconciliation.Renamed(oldPath, _) ->
                match existingFile oldPath with
                | Some(id, _) when planMarksUnparsed id -> Some id
                | _ -> None
            | _ -> None)
        |> List.distinct

    /// Git pull. Each received file is marked. An added, removed, or renamed
    /// file marks its containing Directory. The list does not discover members.
    let reconcileReceivedFiles
        (workspace: WorkspaceReconcile)
        (changedPaths: LazyLoadReconciliation.ChangedPath list)
        : Async<Result<LazyLoadReconciliationReport.Failure list, string>> =
        async {
            let! stateResult = CoreDoor.getState workspace.handle
            match stateResult with
            | Error err -> return Error err
            | Ok stateResponse ->
                let files =
                    LazyLoadReconciliation.receivedFileChanges changedPaths
                return!
                    reconcileListed
                        LazyLoadReconciliationReport.planReceivedFiles
                        { handle = workspace.handle
                          dataDir = workspace.dataDir
                          workspaceLabel = workspace.workspaceLabel
                          graph = stateResponse.graph
                          changes = files
                          artifactReads = files
                          beside =
                            fun ops ->
                                receivedFileIds
                                    stateResponse.graph
                                    workspace.workspaceLabel
                                    files
                                    ops
                                |> List.iter (fun id ->
                                    CoreMailboxApi.postNotice
                                        workspace.handle.host
                                        (Notice.NodeReceived id)) }
        }

