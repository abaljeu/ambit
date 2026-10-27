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
          renamedDirIds: NodeId list
          beside: Op list -> unit }

    let private postNotices
        (host: MailboxHost)
        (notice: NodeId -> Notice)
        (ids: NodeId list)
        =
        ids
        |> List.iter (fun id -> CoreMailboxApi.postNotice host (notice id))

    let private postParsedParents (host: MailboxHost) (parentIds: NodeId list) =
        postNotices host Notice.NodeParsed parentIds

    /// Current controlling Special for each id `markUnparsed` received.
    let private postCurrentUnparsed
        (host: MailboxHost)
        (graph: Graph)
        (ids: NodeId list)
        =
        ids
        |> List.choose (DocumentPartition.documentRootForNode graph)
        |> List.iter (fun id ->
            match Map.tryFind id graph.nodes with
            | Some node when Node.documentStateEnum node = Current ->
                CoreMailboxApi.postNotice host (Notice.NodeUnparsed id)
            | _ -> ())

    let private postReconcileReport
        (listed: ListedReconcile)
        (report: LazyLoadReconciliationReport.Report)
        =
        async {
            logFailures listed.workspaceLabel report.failures
            let host = listed.handle.host
            let postMarks () =
                postParsedParents host report.parsedParentIds
                postCurrentUnparsed host listed.graph report.unparsedIds
            let postDirParsing () =
                postNotices host Notice.NodeParsing report.parsedDirIds
            let postDirParsed () =
                postNotices
                    host
                    Notice.NodeParsed
                    (report.parsedDirIds @ listed.renamedDirIds)
            postDirParsing ()
            match report.ops with
            | [] ->
                postMarks ()
                postDirParsed ()
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
                    postMarks ()
                    postDirParsed ()
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

    /// A directory-file rename is not a plan. The existing directory id is
    /// the change Notice.
    let private renamedDirectoryIds
        (graph: Graph)
        (workspaceLabel: string)
        (changes: LazyLoadReconciliation.ChangedPath list)
        =
        changes
        |> List.choose (function
            | LazyLoadReconciliation.Renamed(oldPath, newPath) when
                isDirInfoPath oldPath || isDirInfoPath newPath ->
                let path =
                    if isDirInfoPath oldPath then oldPath else newPath
                match
                    LazyLoadReconciliation.resolveOwnedPath
                        graph
                        workspaceLabel
                        path
                with
                | Ok(Some(id, _)) -> Some id
                | _ -> None
            | _ -> None)
        |> List.distinct

    /// Directory-file renames stay off the plan. Added and Modified
    /// directory files stay, so `parseDirInfoIfPresent` still runs.
    let private plannedChanges
        (changes: LazyLoadReconciliation.ChangedPath list)
        =
        changes
        |> List.filter (function
            | LazyLoadReconciliation.Renamed(oldPath, newPath) ->
                not (isDirInfoPath oldPath || isDirInfoPath newPath)
            | _ -> true)

    /// Git pull. Added and Modified directory files reach
    /// `parseDirInfoIfPresent`. A renamed directory file is a Notice only.
    /// Each received file is marked. An added, removed, or renamed file
    /// marks its containing Directory. The list does not discover members.
    /// `NodeReceived` uses the received-file list.
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
                let planned = plannedChanges changedPaths
                return!
                    reconcileListed
                        LazyLoadReconciliationReport.planReceivedFiles
                        { handle = workspace.handle
                          dataDir = workspace.dataDir
                          workspaceLabel = workspace.workspaceLabel
                          graph = stateResponse.graph
                          changes = planned
                          artifactReads = planned
                          renamedDirIds =
                            renamedDirectoryIds
                                stateResponse.graph
                                workspace.workspaceLabel
                                changedPaths
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

