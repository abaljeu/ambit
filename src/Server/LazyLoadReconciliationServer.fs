namespace Gambol.Server

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Gambol.Shared

[<RequireQualifiedAccess>]
module LazyLoadReconciliationServer =

    module JsonDecode = Thoth.Json.Newtonsoft.Decode

    let decodeGraphState (json: string) : Result<int * Graph, string> =
        let decoder =
            Thoth.Json.Core.Decode.object (fun get ->
                let revision =
                    get.Required.Field
                        "revision"
                        Serialization.decodeRevision
                let graph =
                    get.Required.Field
                        "graph"
                        Serialization.decodeGraph
                revision.Value, graph)
        JsonDecode.fromString decoder json

    let private isDirInfoPath (path: string) =
        DocumentArtifactPath.isDirectoryFile path

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

    /// Discover under DataDir/{label} or DataDir/{label}/{dirRel}; map to workspace-relative Added.
    let private discoveredAddedPaths
        (dataDir: string)
        (workspaceLabel: string)
        (discoveryDirRel: string option)
        =
        let prefix =
            discoveryDirRel
            |> Option.map normalizeDirRel
            |> Option.filter (fun p -> not (String.IsNullOrEmpty p))
        let discoveryRoot =
            match prefix with
            | None -> Path.Combine(dataDir, workspaceLabel)
            | Some dirRel ->
                Path.Combine(
                    dataDir,
                    workspaceLabel,
                    dirRel.Replace('/', Path.DirectorySeparatorChar))
        let fileAdds =
            discoveryRoot
            |> DocumentPersistence.discoverArtifactRelatives
            |> Result.map (fun relatives ->
                relatives
                |> List.map (fun rel ->
                    let workspaceRel =
                        match prefix with
                        | None -> rel
                        | Some dirRel when String.IsNullOrEmpty rel -> dirRel
                        | Some dirRel -> dirRel + "/" + rel
                    workspaceRel))
        match fileAdds with
        | Error err -> Error err
        | Ok fileRels ->
            let emptyAmb =
                emptyDirectoryFileRels discoveryRoot ""
                |> List.map (fun rel ->
                    match prefix with
                    | None -> rel
                    | Some dirRel -> dirRel + "/" + rel)
            (fileRels @ emptyAmb)
            |> List.distinct
            |> List.map LazyLoadReconciliation.Added
            |> Ok

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

    /// Shared pipeline: discover on chosen root → union with changedPaths → plan → log → post.
    /// `discoveryDirRel` None = workspace root; Some dir = DataDir/{label}/{dir}.
    let reconcileChangedPathsWithDiscovery
        (handle: AgentHandle)
        (dataDir: string)
        (workspaceLabel: string)
        (discoveryDirRel: string option)
        (changedPaths: LazyLoadReconciliation.ChangedPath list)
        : Async<Result<LazyLoadReconciliationReport.Failure list, string>> =
        async {
            let! stateResult = handle.getState ()
            match stateResult with
            | Error err -> return Error err
            | Ok stateResponse ->
                let revision = stateResponse.revision.Value
                let graph = stateResponse.graph
                match discoveredAddedPaths dataDir workspaceLabel discoveryDirRel with
                    | Error err -> return Error err
                    | Ok discovered ->
                        let allChanges = changedPaths @ discovered
                        let artifacts =
                            readDirInfoArtifacts
                                graph
                                dataDir
                                workspaceLabel
                                allChanges
                        match
                            LazyLoadReconciliationReport.planChangedPathsWithArtifacts
                                graph
                                workspaceLabel
                                allChanges
                                artifacts
                        with
                        | Error err -> return Error err
                        | Ok report ->
                            logFailures workspaceLabel report.failures
                            match report.ops with
                            | [] -> return Ok report.failures
                            | ops ->
                                let! result =
                                    GraphOnlyChangePost.postChunks
                                        handle.postGraphOnlyChange
                                        revision
                                        (GraphOnlyChangeChunks.split ops)
                                return
                                    result
                                    |> Result.map (fun () -> report.failures)
        }

    let reconcileChangedPaths
        (handle: AgentHandle)
        (dataDir: string)
        (workspaceLabel: string)
        (changedPaths: LazyLoadReconciliation.ChangedPath list)
        : Async<Result<LazyLoadReconciliationReport.Failure list, string>> =
        reconcileChangedPathsWithDiscovery
            handle
            dataDir
            workspaceLabel
            None
            changedPaths

    let reconcileDirectory
        (handle: AgentHandle)
        (dataDir: string)
        (workspaceLabel: string)
        (dirRel: string)
        : Async<Result<LazyLoadReconciliationReport.Failure list, string>> =
        reconcileChangedPathsWithDiscovery
            handle
            dataDir
            workspaceLabel
            (Some dirRel)
            []

    /// Discover under DataDir/{label} (workspace root) with no git delta.
    let reconcileWorkspace
        (handle: AgentHandle)
        (dataDir: string)
        (workspaceLabel: string)
        : Async<Result<LazyLoadReconciliationReport.Failure list, string>> =
        reconcileChangedPathsWithDiscovery
            handle
            dataDir
            workspaceLabel
            None
            []

    let reconcileAddedPaths
        (handle: AgentHandle)
        (dataDir: string)
        (workspaceLabel: string)
        (addedPaths: string list)
        : Async<Result<LazyLoadReconciliationReport.Failure list, string>> =
        addedPaths
        |> List.map LazyLoadReconciliation.Added
        |> reconcileChangedPaths handle dataDir workspaceLabel

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
        (app: WebApplication)
        (isAuthenticated: HttpRequest -> bool)
        (dataDir: string)
        (getHandle: unit -> AgentHandle)
        =
        app.MapPost(
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
                            let! result =
                                if String.IsNullOrWhiteSpace payload.path then
                                    reconcileWorkspace
                                        (getHandle ())
                                        dataDir
                                        payload.workspace
                                else
                                    reconcileDirectory
                                        (getHandle ())
                                        dataDir
                                        payload.workspace
                                        payload.path
                            return failuresResult result
                })
        )
        |> ignore

    let registerAddedRoute
        (app: WebApplication)
        (isAuthenticated: HttpRequest -> bool)
        (dataDir: string)
        (getHandle: unit -> AgentHandle)
        =
        app.MapPost(
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
                                    (getHandle ())
                                    dataDir
                                    payload.workspace
                                    payload.paths
                            return failuresResult result
                })
        )
        |> ignore
