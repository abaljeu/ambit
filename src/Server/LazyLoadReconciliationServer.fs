namespace Gambol.Server

open System
open System.IO
open Microsoft.AspNetCore.Http
open Gambol.Shared

[<RequireQualifiedAccess>]
module LazyLoadReconciliationServer =

    module JsonDecode = Thoth.Json.Newtonsoft.Decode

    let decodeGraphState (json: string) : Result<int * Graph, string> =
        let decoder =
            Thoth.Json.Core.Decode.object (fun get ->
                let eventId =
                    get.Required.Field
                        "eventId"
                        EventJson.decodeEventId
                let graph =
                    get.Required.Field
                        "graph"
                        Serialization.decodeGraph
                EventId.value eventId, graph)
        JsonDecode.fromString decoder json

    let private isDirInfoPath (path: string) =
        DocumentArtifactPath.isDirectoryFile path

    /// Core handle, data directory, and workspace label for one reconcile.
    type WorkspaceReconcile =
        { handle: CoreChanges
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
                graph.nodes.[nodeId].documentState <> Current
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

    /// Normalize a workspace-relative directory prefix (no leading/trailing slash).
    let private normalizeDirRel (dirRel: string) =
        dirRel.Replace('\\', '/').Trim('/')

    let private isGitDirName (name: string) =
        String.Equals(name, ".git", StringComparison.OrdinalIgnoreCase)

    /// Empty dirs (no files, no non-git subdirs) as `{rel}/.amb` so Added plans Directory.
    let rec private emptyDirectoryFileRels (full: string) (rel: string) =
        if not (Directory.Exists full) then
            []
        else
            Directory.GetDirectories full
            |> Array.toList
            |> List.collect (fun dir ->
                let name = Path.GetFileName dir
                if isGitDirName name then
                    []
                else
                    let childRel =
                        if rel = "" then name else rel + "/" + name
                    let nested = emptyDirectoryFileRels dir childRel
                    let hasFile =
                        Directory.EnumerateFiles dir |> Seq.isEmpty |> not
                    let hasSubdir =
                        Directory.GetDirectories dir
                        |> Array.exists (fun d ->
                            not (isGitDirName (Path.GetFileName d)))
                    if hasFile || hasSubdir then nested
                    else (childRel.Replace('\\', '/') + "/.amb") :: nested)

    let private discoveryPrefixAndRoot
        (workspace: WorkspaceReconcile)
        (discoveryDirRel: string option)
        =
        let prefix =
            discoveryDirRel
            |> Option.map normalizeDirRel
            |> Option.filter (fun p -> not (String.IsNullOrEmpty p))
        let root =
            match prefix with
            | None ->
                Path.Combine(workspace.dataDir, workspace.workspaceLabel)
            | Some dirRel ->
                Path.Combine(
                    workspace.dataDir,
                    workspace.workspaceLabel,
                    dirRel.Replace('/', Path.DirectorySeparatorChar))
        prefix, root

    let private workspaceRel (prefix: string option) (rel: string) =
        match prefix with
        | None -> rel
        | Some dirRel when String.IsNullOrEmpty rel -> dirRel
        | Some dirRel -> dirRel + "/" + rel

    /// Discover under DataDir/{label} or DataDir/{label}/{dirRel}; map to workspace-relative Added.
    let private discoveredAddedPaths
        (workspace: WorkspaceReconcile)
        (discoveryDirRel: string option)
        =
        let prefix, root =
            discoveryPrefixAndRoot workspace discoveryDirRel
        let fileAdds =
            root
            |> DocumentPersistPath.discoverArtifactRelatives
            |> Result.map (List.map (workspaceRel prefix))
        match fileAdds with
        | Error err -> Error err
        | Ok fileRels ->
            let emptyAmb =
                emptyDirectoryFileRels root ""
                |> List.map (workspaceRel prefix)
            (fileRels @ emptyAmb)
            |> List.distinct
            |> List.map LazyLoadReconciliation.Added
            |> Ok

    let private isDirectoryMember (root: string) (rel: string) =
        let full =
            Path.Combine(
                root,
                rel.Replace('/', Path.DirectorySeparatorChar))
        Directory.Exists full

    let private directoryArtifact (workspacePath: string) =
        workspacePath.Replace('\\', '/').Trim('/') + "/.amb"

    /// Direct files and child directories. Nested paths stay out.
    let private immediateMemberChanges
        (workspace: WorkspaceReconcile)
        (discoveryDirRel: string option)
        =
        let prefix, root =
            discoveryPrefixAndRoot workspace discoveryDirRel
        root
        |> DocumentPersistPath.discoverImmediateMembers
        |> Result.map (fun members ->
            let tagged =
                members
                |> List.map (fun rel ->
                    workspaceRel prefix rel, isDirectoryMember root rel)
            let files =
                tagged
                |> List.choose (fun (path, isDir) ->
                    if isDir then
                        None
                    else
                        Some(LazyLoadReconciliation.Added path))
            let directories =
                tagged
                |> List.choose (fun (path, isDir) ->
                    if isDir then Some path else None)
            files, directories)

    let private currentChildDirectory
        (graph: Graph)
        (workspaceLabel: string)
        (dirPath: string)
        =
        match
            LazyLoadReconciliation.resolveOwnedPath
                graph
                workspaceLabel
                (directoryArtifact dirPath)
        with
        | Ok(Some(nodeId, _)) ->
            match Map.tryFind nodeId graph.nodes with
            | Some node when
                node.kind = Special SpecialKind.Directory
                && node.documentState = Current ->
                true
            | _ -> false
        | _ -> false

    /// A Current child Directory stays out. A new one is an unread Directory File path.
    let private childDirectoryAdds
        (graph: Graph)
        (workspaceLabel: string)
        (directories: string list)
        =
        directories
        |> List.choose (fun path ->
            if currentChildDirectory graph workspaceLabel path then
                None
            else
                Some(
                    LazyLoadReconciliation.Added(directoryArtifact path)))

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
        { handle: CoreChanges
          dataDir: string
          workspaceLabel: string
          graph: Graph
          changes: LazyLoadReconciliation.ChangedPath list
          artifactReads: LazyLoadReconciliation.ChangedPath list }

    let private postReconcileReport
        (handle: CoreChanges)
        (workspaceLabel: string)
        (report: LazyLoadReconciliationReport.Report)
        =
        async {
            logFailures workspaceLabel report.failures
            match report.ops with
            | [] -> return Ok report.failures
            | ops ->
                let! result =
                    GraphOnlyChangePost.postChunks
                        handle.postGraphOnly
                        "Parse"
                        (GraphOnlyChangeChunks.split ops)
                return result |> Result.map (fun () -> report.failures)
        }

    /// Plan and post. `artifactReads` is the set whose text is read.
    let private reconcileListed (listed: ListedReconcile) =
        async {
            let artifacts =
                readDirInfoArtifacts
                    listed.graph
                    { handle = listed.handle
                      dataDir = listed.dataDir
                      workspaceLabel = listed.workspaceLabel }
                    listed.artifactReads
            match
                LazyLoadReconciliationReport.planChangedPathsWithArtifacts
                    listed.graph
                    listed.workspaceLabel
                    listed.changes
                    artifacts
            with
            | Error err -> return Error err
            | Ok report ->
                return!
                    postReconcileReport
                        listed.handle
                        listed.workspaceLabel
                        report
        }

    /// Shared pipeline: discover on chosen root → union with changedPaths → plan → log → post.
    /// `discoveryDirRel` None = workspace root; Some dir = DataDir/{label}/{dir}.
    let reconcileChangedPathsWithDiscovery
        (workspace: WorkspaceReconcile)
        (discoveryDirRel: string option)
        (changedPaths: LazyLoadReconciliation.ChangedPath list)
        : Async<Result<LazyLoadReconciliationReport.Failure list, string>> =
        async {
            let! stateResult = workspace.handle.getState ()
            match stateResult with
            | Error err -> return Error err
            | Ok stateResponse ->
                let graph = stateResponse.graph
                match discoveredAddedPaths workspace discoveryDirRel with
                | Error err -> return Error err
                | Ok discovered ->
                    let allChanges = changedPaths @ discovered
                    return!
                        reconcileListed
                            { handle = workspace.handle
                              dataDir = workspace.dataDir
                              workspaceLabel = workspace.workspaceLabel
                              graph = graph
                              changes = allChanges
                              artifactReads = allChanges }
        }

    let reconcileChangedPaths
        (workspace: WorkspaceReconcile)
        (changedPaths: LazyLoadReconciliation.ChangedPath list)
        : Async<Result<LazyLoadReconciliationReport.Failure list, string>> =
        reconcileChangedPathsWithDiscovery workspace None changedPaths

    /// Load's directory match. One level: direct files and child directories.
    /// A Current child Directory is left alone. A new child Directory is Unparsed.
    let private matchDiscoveredDirectory
        (workspace: WorkspaceReconcile)
        (discoveryDirRel: string option)
        : Async<Result<LazyLoadReconciliationReport.Failure list, string>> =
        async {
            let! stateResult = workspace.handle.getState ()
            match stateResult with
            | Error err -> return Error err
            | Ok stateResponse ->
                match immediateMemberChanges workspace discoveryDirRel with
                | Error err -> return Error err
                | Ok(files, directories) ->
                    let graph = stateResponse.graph
                    let label = workspace.workspaceLabel
                    let held =
                        LazyLoadReconciliation.currentDiscoveredAsModified
                            graph
                            label
                            files
                    let dirAdds = childDirectoryAdds graph label directories
                    let fileChanges = held @ files
                    return!
                        reconcileListed
                            { handle = workspace.handle
                              dataDir = workspace.dataDir
                              workspaceLabel = label
                              graph = graph
                              changes = fileChanges @ dirAdds
                              artifactReads = fileChanges }
        }

    let reconcileDirectory
        (workspace: WorkspaceReconcile)
        (dirRel: string)
        : Async<Result<LazyLoadReconciliationReport.Failure list, string>> =
        matchDiscoveredDirectory workspace (Some dirRel)

    /// One-level discover under DataDir/{label} (workspace root).
    let reconcileWorkspace
        (workspace: WorkspaceReconcile)
        : Async<Result<LazyLoadReconciliationReport.Failure list, string>> =
        matchDiscoveredDirectory workspace None

    let reconcileAddedPaths
        (workspace: WorkspaceReconcile)
        (addedPaths: string list)
        : Async<Result<LazyLoadReconciliationReport.Failure list, string>> =
        addedPaths
        |> List.map LazyLoadReconciliation.Added
        |> reconcileChangedPaths workspace

    type private DirectoryBody =
        { workspace: string
          path: string }

    let private decodeDirectoryBody (json: string) : Result<DirectoryBody, string> =
        let decoder =
            Thoth.Json.Core.Decode.object (fun get ->
                { workspace = get.Required.Field "workspace" Thoth.Json.Core.Decode.string
                  path = get.Required.Field "path" Thoth.Json.Core.Decode.string })
        JsonDecode.fromString decoder json

    type private AddedBody =
        { workspace: string
          paths: string list }

    let private decodeAddedBody (json: string) : Result<AddedBody, string> =
        let decoder =
            Thoth.Json.Core.Decode.object (fun get ->
                { workspace =
                    get.Required.Field
                        "workspace"
                        Thoth.Json.Core.Decode.string
                  paths =
                    get.Required.Field
                        "paths"
                        (Thoth.Json.Core.Decode.list
                            Thoth.Json.Core.Decode.string) })
        JsonDecode.fromString decoder json

    let private failuresResult
        (result: Result<LazyLoadReconciliationReport.Failure list, string>)
        =
        match result with
        | Error err -> Results.BadRequest(err)
        | Ok failures ->
            Results.Content(
                LazyLoadReconciliationDiagnostics.encodeFailures failures,
                "application/json")

    let registerDirectoryRoute
        (this: AmbitApp)
        (getHandle: unit -> CoreChanges)
        =
        let isAuthenticated = this.Auth.IsAuthenticated
        let dataDir = this.DataDir
        this.MapPost(
            "/ambit/workspace/reconciliation/directory",
            Func<HttpRequest, System.Threading.Tasks.Task<IResult>>(fun req ->
                task {
                    if not (isAuthenticated req) then
                        return Results.Unauthorized()
                    else
                        use reader = new StreamReader(req.Body)
                        let! body = reader.ReadToEndAsync()
                        match decodeDirectoryBody body with
                        | Error err ->
                            return Results.BadRequest("invalid body: " + err)
                        | Ok payload when String.IsNullOrWhiteSpace payload.workspace ->
                            return Results.BadRequest("missing workspace")
                        | Ok payload ->
                            let workspace =
                                { handle = getHandle ()
                                  dataDir = dataDir
                                  workspaceLabel = payload.workspace }
                            let! result =
                                if String.IsNullOrWhiteSpace payload.path then
                                    reconcileWorkspace workspace
                                else
                                    reconcileDirectory workspace payload.path
                            return failuresResult result
                })
        )
        |> ignore

    let registerAddedRoute
        (this: AmbitApp)
        (getHandle: unit -> CoreChanges)
        =
        let isAuthenticated = this.Auth.IsAuthenticated
        let dataDir = this.DataDir
        this.MapPost(
            "/ambit/workspace/reconciliation/added",
            Func<HttpRequest, System.Threading.Tasks.Task<IResult>>(fun req ->
                task {
                    if not (isAuthenticated req) then
                        return Results.Unauthorized()
                    else
                        use reader = new StreamReader(req.Body)
                        let! body = reader.ReadToEndAsync()
                        match decodeAddedBody body with
                        | Error err ->
                            return Results.BadRequest("invalid body: " + err)
                        | Ok payload when String.IsNullOrWhiteSpace payload.workspace ->
                            return Results.BadRequest("missing workspace")
                        | Ok payload ->
                            let! result =
                                reconcileAddedPaths
                                    { handle = getHandle ()
                                      dataDir = dataDir
                                      workspaceLabel = payload.workspace }
                                    payload.paths
                            return failuresResult result
                })
        )
        |> ignore
