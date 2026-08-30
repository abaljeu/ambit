namespace Gambol.Shared

open System

/// Focus-derived Push/Pull inventory scope under a named workspace.
[<RequireQualifiedAccess>]
type SyncScopeKind =
    | Workspace
    | Directory
    | File

/// Label + relative path under the mapped / DataDir workspace root.
type WorkspaceSyncScope =
    { label: string
      /// Empty = whole workspace. Directory prefix or single file path.
      relative: string
      kind: SyncScopeKind }

/// Persist-stamped file awaiting a debounced auto-download coalesce.
type AutoDownloadTarget =
    { label: string
      relative: string }

[<RequireQualifiedAccess>]
module WorkspaceSyncScope =

    let private invalidPath = "invalid_path"

    let private splitSegments (path: string) =
        path.Split([| '/' |], StringSplitOptions.None)
        |> Array.toList

    /// Forward slashes, no leading/trailing slash, no `.` / `..` / empty segments.
    /// Empty string means the workspace root.
    let normalizeRelative (path: string) : Result<string, string> =
        let raw = if isNull path then "" else path.Replace('\\', '/').Trim()

        if raw = "" then
            Ok ""
        else
            let trimmed = raw.Trim('/')

            if trimmed = "" then
                Ok ""
            else
                let segments = splitSegments trimmed

                let invalid =
                    segments
                    |> List.exists (fun s ->
                        s = ""
                        || match Filename.create s with
                           | Filename.Ok _ -> false
                           | _ -> true)

                if invalid then
                    Error invalidPath
                else
                    Ok (String.Join("/", segments))
    /// Candidate is the scope path, or under a directory/workspace prefix.
    let isUnderScope
        (scope: WorkspaceSyncScope)
        (candidateRelative: string)
        : bool =
        match normalizeRelative candidateRelative with
        | Error _ -> false
        | Ok candidate ->
            match scope.kind with
            | SyncScopeKind.File -> candidate = scope.relative
            | SyncScopeKind.Workspace when scope.relative = "" -> true
            | SyncScopeKind.Workspace
            | SyncScopeKind.Directory ->
                let prefix = scope.relative

                candidate = prefix
                || candidate.StartsWith(prefix + "/", StringComparison.Ordinal)

    let filterUnderScope
        (scope: WorkspaceSyncScope)
        (candidates: string list)
        : string list =
        candidates |> List.filter (isUnderScope scope)

    let private fileParentSegments (relative: string) : string list =
        match splitSegments relative |> List.rev with
        | _ :: parentReversed -> List.rev parentReversed
        | [] -> []

    let rec private commonPrefix (a: string list) (b: string list) : string list =
        match a, b with
        | x :: xs, y :: ys when x = y -> x :: commonPrefix xs ys
        | _ -> []

    /// Deepest directory containing every file relative (nearest common parent).
    let private nearestCommonDirectory (relatives: string list) : string =
        match relatives |> List.map fileParentSegments with
        | [] -> ""
        | first :: rest ->
            List.fold commonPrefix first rest |> String.concat "/"

    /// Coalesce affected file relatives to at most one download scope per label:
    /// one file -> File; several -> nearest common Directory, else the Workspace.
    let coalesceDownloadTargets
        (targets: AutoDownloadTarget list)
        : WorkspaceSyncScope list =
        targets
        |> List.filter (fun t ->
            not (String.IsNullOrWhiteSpace t.label) && t.relative <> "")
        |> List.groupBy (fun t -> t.label)
        |> List.map (fun (label, items) ->
            match items |> List.map (fun t -> t.relative) |> List.distinct with
            | [ single ] ->
                { label = label; relative = single; kind = SyncScopeKind.File }
            | many ->
                match nearestCommonDirectory many with
                | "" ->
                    { label = label; relative = ""; kind = SyncScopeKind.Workspace }
                | dir ->
                    { label = label; relative = dir; kind = SyncScopeKind.Directory })

    /// Relative path from a `//label/...` desktop path when labels match.
    let tryRelativeUnderLabel
        (label: string)
        (desktopPath: string)
        : Result<string, string> =
        match NodeDesktopPath.tryParseWorkspacePath desktopPath with
        | None -> Error "not a workspace path"
        | Some(pathLabel, tail) when pathLabel = label ->
            normalizeRelative (tail.TrimEnd('/'))
        | Some _ -> Error "path escapes workspace label"

    let private scopeFromParsed
        (kind: SyncScopeKind)
        (label: string)
        (tail: string)
        : Result<WorkspaceSyncScope, string> =
        if String.IsNullOrWhiteSpace label then
            Error "not under a named workspace"
        else
            match normalizeRelative (tail.TrimEnd('/')) with
            | Error e -> Error e
            | Ok relative ->
                match kind with
                | SyncScopeKind.Workspace ->
                    Ok
                        { label = label
                          relative = ""
                          kind = SyncScopeKind.Workspace }
                | SyncScopeKind.Directory when relative = "" ->
                    Error "directory path is empty"
                | SyncScopeKind.File when relative = "" ->
                    Error "file path is empty"
                | _ ->
                    Ok
                        { label = label
                          relative = relative
                          kind = kind }

    /// Resolve Push/Pull scope from focus: Workspace / Directory / File.
    let tryFromFocus
        (graph: Graph)
        (nodeId: NodeId)
        : Result<WorkspaceSyncScope, string> =
        match Map.tryFind nodeId graph.nodes with
        | None -> Error "node not found"
        | Some node ->
            match node.kind with
            | Special Workspace ->
                match Filename.tryValue node.name with
                | Some label when
                    label <> ""
                    && nodeId <> Graph.rootId
                    && not (Graph.isSystemFolderNode nodeId)
                    ->
                    Ok
                        { label = label
                          relative = ""
                          kind = SyncScopeKind.Workspace }
                | _ -> Error "focus is not a named workspace"
            | Special Directory when Graph.isSystemDirectoryNode nodeId ->
                match Filename.tryValue node.name with
                | Some label when label <> "" ->
                    Ok
                        { label = label
                          relative = ""
                          kind = SyncScopeKind.Workspace }
                | _ -> Error "system directory has no label"
            | Special Directory ->
                match NodeDesktopPath.pathForNodeId graph nodeId with
                | None -> Error "directory has no path"
                | Some path ->
                    match NodeDesktopPath.tryParseWorkspacePath path with
                    | Some(label, tail) ->
                        scopeFromParsed SyncScopeKind.Directory label tail
                    | None -> Error "directory is not under a named workspace"
            | Special File ->
                match NodeDesktopPath.pathForNodeId graph nodeId with
                | None -> Error "file has no path"
                | Some path ->
                    match NodeDesktopPath.tryParseWorkspacePath path with
                    | Some(label, tail) ->
                        scopeFromParsed SyncScopeKind.File label tail
                    | None -> Error "file is not under a named workspace"
            | _ -> Error "focus is not a workspace, directory, or file"
