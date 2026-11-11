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
        (dataDir: string)
        (workspaceLabel: string)
        (changedPaths: LazyLoadReconciliation.ChangedPath list)
        : Map<string, string> =
        let root = Path.Combine(dataDir, workspaceLabel)
        let pathsToRead =
            changedPaths
            |> List.collect (fun change ->
                if needsDirInfoArtifactRead graph workspaceLabel change then
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

    type private ReconcileCaps =
        { graphAtEvent: unit -> Async<Result<GraphAtEvent, string>>
          postNotice: Notice -> unit
          postGraphOnly: Ev -> Async<Result<unit, string>> }

    type private ListedReconcile =
        { postNotice: Notice -> unit
          postGraphOnly: Ev -> Async<Result<unit, string>>
          dataDir: string
          workspaceLabel: string
          graph: Graph
          changes: LazyLoadReconciliation.ChangedPath list
          artifactReads: LazyLoadReconciliation.ChangedPath list
          renamedDirIds: NodeId list
          beside: LazyLoadReconciliationReport.Report -> unit }

    let private postNotices
        (postNotice: Notice -> unit)
        (notice: NodeId -> Notice)
        (ids: NodeId list)
        =
        ids |> List.iter (fun id -> postNotice (notice id))

    /// Containing Directory or Workspace from the plan ids.
    /// A PullingFile container gets no NodeUnparsed.
    let private postContainerUnparsed
        (postNotice: Notice -> unit)
        (graph: Graph)
        (ids: NodeId list)
        =
        ids
        |> List.iter (fun id ->
            match Map.tryFind id graph.nodes with
            | Some node when Node.documentStateEnum node <> PullingFile ->
                postNotice (Notice.NodeUnparsed id)
            | _ -> ())

    /// Directories this reconcile finishes with NodeParsed.
    let private loadedDirectoryIds
        (report: LazyLoadReconciliationReport.Report)
        (renamedDirIds: NodeId list)
        =
        List.distinct (report.parsedDirIds @ renamedDirIds)

    /// Child containers. The loaded directory is not marked again.
    let private childContainerIds
        (report: LazyLoadReconciliationReport.Report)
        (renamedDirIds: NodeId list)
        =
        let loaded = loadedDirectoryIds report renamedDirIds |> Set.ofList
        report.containingDirIds
        |> List.filter (fun id -> not (Set.contains id loaded))

    /// Current controlling Special for each id `markUnparsed` received.
    let private postCurrentUnparsed
        (postNotice: Notice -> unit)
        (graph: Graph)
        (ids: NodeId list)
        =
        ids
        |> List.choose (DocumentPartition.documentRootForNode graph)
        |> List.iter (fun id ->
            match Map.tryFind id graph.nodes with
            | Some node when Node.documentStateEnum node = Current ->
                postNotice (Notice.NodeUnparsed id)
            | _ -> ())

    /// NodeUnparsed, then the Change, then NodeParsed for a loaded directory.
    let private postBeforeChange
        (postNotice: Notice -> unit)
        (graph: Graph)
        (report: LazyLoadReconciliationReport.Report)
        =
        postContainerUnparsed postNotice graph report.parsedDirIds
        postNotices postNotice Notice.NodeParsing report.parsedDirIds

    /// Child NodeUnparsed, then the loaded directory's NodeParsed.
    let private postAfterChange
        (listed: ListedReconcile)
        (report: LazyLoadReconciliationReport.Report)
        =
        listed.beside report
        postContainerUnparsed
            listed.postNotice
            listed.graph
            (childContainerIds report listed.renamedDirIds)
        postCurrentUnparsed
            listed.postNotice
            listed.graph
            report.unparsedIds
        postNotices
            listed.postNotice
            Notice.NodeParsed
            (report.parsedDirIds @ listed.renamedDirIds)

    let private acceptedUnit () =
        CoreChanges.accepted EventId.zero true [] false

    let private postChunk
        (post: Ev -> Async<Result<unit, string>>)
        (event: Ev)
        =
        async {
            let! result = post event
            return Result.map (fun () -> acceptedUnit ()) result
        }

    let private postReconcileReport
        (listed: ListedReconcile)
        (report: LazyLoadReconciliationReport.Report)
        =
        async {
            logFailures listed.workspaceLabel report.failures
            postBeforeChange listed.postNotice listed.graph report
            match report.ops with
            | [] ->
                postAfterChange listed report
                return Ok report.failures
            | ops ->
                let! result =
                    GraphOnlyChangePost.postChunks
                        (postChunk listed.postGraphOnly)
                        "Parse"
                        (GraphOnlyChangeChunks.split ops)
                match result with
                | Ok () ->
                    postAfterChange listed report
                    return Ok report.failures
                | Error err ->
                    postNotices
                        listed.postNotice
                        (fun id -> Notice.ParseFailed(id, err))
                        report.parsedDirIds
                    return Error err
        }

    /// Plan and post. `artifactReads` is the set whose text is read.
    let private reconcileListed plan (listed: ListedReconcile) =
        async {
            let artifacts =
                readDirInfoArtifacts
                    listed.graph
                    listed.dataDir
                    listed.workspaceLabel
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

    /// Modified file not yet Unparsed, or an Added PullingFile stub.
    /// Renamed ids are Report.renamedReceivedIds, not ops.
    let private receivedFileIds
        (graph: Graph)
        (workspaceLabel: string)
        (files: LazyLoadReconciliation.ChangedPath list)
        : NodeId list =
        let existingFile path =
            match LazyLoadReconciliation.resolveOwnedPath graph workspaceLabel path with
            | Ok(Some(id, File)) -> Some(id, Node.documentStateEnum graph.nodes.[id])
            | _ -> None
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

    /// A File node this pull creates, absent from the pre-pull graph.
    let private absentFileIds (graph: Graph) (ops: Op list) =
        ops
        |> List.choose (function
            | Op.NewSpecialNode(id, kind, _) when
                kind = File && not (Map.containsKey id graph.nodes) ->
                Some id
            | _ -> None)

    /// PullingFile has no bytes yet. An absent node has not been held.
    let private serverHeldBytes (graph: Graph) (id: NodeId) =
        match Map.tryFind id graph.nodes with
        | Some node -> Node.documentStateEnum node <> PullingFile
        | None -> false

    /// NodeReceived once, for a file the server never held.
    let private postFirstReceipt
        (postNotice: Notice -> unit)
        (graph: Graph)
        (knownIds: NodeId list)
        (ops: Op list)
        =
        knownIds
        |> List.append (absentFileIds graph ops)
        |> List.distinct
        |> List.filter (fun id -> not (serverHeldBytes graph id))
        |> List.iter (fun id -> postNotice (Notice.NodeReceived id))

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

    let private listedFrom
        (caps: ReconcileCaps)
        (dataDir: string)
        (workspaceLabel: string)
        (graph: Graph)
        (changedPaths: LazyLoadReconciliation.ChangedPath list)
        =
        let files =
            LazyLoadReconciliation.receivedFileChanges changedPaths
        let planned = plannedChanges changedPaths
        { postNotice = caps.postNotice
          postGraphOnly = caps.postGraphOnly
          dataDir = dataDir
          workspaceLabel = workspaceLabel
          graph = graph
          changes = planned
          artifactReads = planned
          renamedDirIds =
            renamedDirectoryIds graph workspaceLabel changedPaths
          beside =
            fun report ->
                let known =
                    receivedFileIds graph workspaceLabel files
                    |> List.append report.renamedReceivedIds
                postFirstReceipt
                    caps.postNotice
                    graph
                    known
                    report.ops }

    let private reconcileWith
        (caps: ReconcileCaps)
        (dataDir: string)
        (workspaceLabel: string)
        (changedPaths: LazyLoadReconciliation.ChangedPath list)
        =
        async {
            let! stateResult = caps.graphAtEvent ()
            match stateResult with
            | Error err -> return Error err
            | Ok stateResponse ->
                return!
                    reconcileListed
                        LazyLoadReconciliationReport.planReceivedFiles
                        (listedFrom
                            caps
                            dataDir
                            workspaceLabel
                            stateResponse.graph
                            changedPaths)
        }

    let private doorCaps (door: CoreChangesDoor) : ReconcileCaps =
        { graphAtEvent = fun () -> CoreDoor.graphAtEvent door
          postNotice = CoreMailboxApi.postNotice door.host
          postGraphOnly =
            fun event ->
                async {
                    let! result = CoreDoor.postGraphOnly door event
                    return Result.map ignore result
                } }

    let private changesCaps (changes: ActorChanges) : ReconcileCaps =
        { graphAtEvent = changes.graphAtEvent
          postNotice = changes.postNotice
          postGraphOnly = changes.postGraphOnly }

    /// Git pull. Added and Modified directory files reach
    /// `parseDirInfoIfPresent`. A renamed directory file is a Notice only.
    /// Each received file is marked. A directory this reconcile parses
    /// is NodeUnparsed before the Change and NodeParsed after it.
    /// A child container is NodeUnparsed between those two.
    /// The list does not discover members.
    /// `NodeReceived` fires once, for a file the server never held.
    /// A file already received is marked Unparsed and gets no
    /// `NodeReceived`. A Modified directory file with no text stays
    /// here and lands on `unparsedIds`.
    let reconcileReceivedFiles
        (workspace: WorkspaceReconcile)
        (changedPaths: LazyLoadReconciliation.ChangedPath list)
        =
        reconcileWith
            (doorCaps workspace.handle)
            workspace.dataDir
            workspace.workspaceLabel
            changedPaths

    let internal reconcileFromChanges
        (changes: ActorChanges)
        (dataDir: string)
        (workspaceLabel: string)
        (changedPaths: LazyLoadReconciliation.ChangedPath list)
        =
        reconcileWith
            (changesCaps changes)
            dataDir
            workspaceLabel
            changedPaths

