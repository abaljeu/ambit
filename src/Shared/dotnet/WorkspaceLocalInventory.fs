namespace Gambol.Shared

open System
open System.IO

/// One path under a mapped workspace root for Push inventory.
type LocalSyncItem =
    { relative: string
      isDirectory: bool }

/// Walk mapped local scope and apply GitCheckIgnore for Push.
[<RequireQualifiedAccess>]
module WorkspaceLocalInventory =

    let private touchesGit (relative: string) =
        relative.Split([| '/' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.exists (fun s ->
            String.Equals(s, ".git", StringComparison.OrdinalIgnoreCase))

    let private childRelative (parentRel: string) (name: string) =
        if parentRel = "" then name else parentRel + "/" + name

    let private tryInfo (fullPath: string) =
        try
            if Directory.Exists fullPath then Some true
            elif File.Exists fullPath then Some false
            else None
        with _ ->
            None

    let private listDirChildren (rel: string) (full: string) =
        let dirs =
            Directory.GetDirectories full
            |> Array.toList
            |> List.choose (fun dir ->
                let name = Path.GetFileName dir
                if
                    String.Equals(
                        name,
                        ".git",
                        StringComparison.OrdinalIgnoreCase)
                then
                    None
                else
                    let child = childRelative rel name
                    if touchesGit child then None
                    else Some(child, dir, true))

        let files =
            Directory.GetFiles full
            |> Array.toList
            |> List.choose (fun file ->
                let child = childRelative rel (Path.GetFileName file)
                if touchesGit child then None
                else Some(child, file, false))

        dirs @ files

    let rec private walkTree (rel: string) (full: string) : LocalSyncItem list =
        listDirChildren rel full
        |> List.collect (fun (child, childFull, isDir) ->
            let here = { relative = child; isDirectory = isDir }
            if isDir then here :: walkTree child childFull
            else [ here ])

    let private collectRaw
        (root: string)
        (scope: WorkspaceSyncScope)
        : Result<LocalSyncItem list, string> =
        try
            let scopeFull =
                if scope.relative = "" then root
                else
                    let parts =
                        scope.relative.Split(
                            [| '/' |],
                            StringSplitOptions.RemoveEmptyEntries)
                    Path.GetFullPath(
                        Path.Combine(Array.append [| root |] parts))

            match scope.kind, tryInfo scopeFull with
            | SyncScopeKind.File, Some false ->
                Ok [ { relative = scope.relative; isDirectory = false } ]
            | SyncScopeKind.File, Some true ->
                Error "scope path is a directory"
            | SyncScopeKind.File, None -> Error "scope path not found"
            | _, None -> Error "scope path not found"
            | _, Some false -> Error "scope path is a file"
            | _, Some true ->
                let self =
                    if scope.relative = "" then []
                    else
                        [ { relative = scope.relative
                            isDirectory = true } ]
                Ok(self @ walkTree scope.relative scopeFull)
        with ex ->
            Error ex.Message

    let private applyIgnoreFilter
        (root: string)
        (raw: LocalSyncItem list)
        : Result<LocalSyncItem list, string> =
        let paths = raw |> List.map (fun i -> i.relative)

        match GitCheckIgnore.classify root paths with
        | Error e -> Error e
        | Ok classified ->
            let ignored =
                classified
                |> List.choose (fun (p, ign) ->
                    if
                        ign
                        && not (GitCheckIgnore.isGitignorePath p)
                    then
                        Some p
                    else
                        None)
                |> Set.ofList

            raw
            |> List.filter (fun i ->
                not (Set.contains i.relative ignored))
            |> Ok

    /// Local candidates under scope, minus effectively ignored paths.
    /// If git/check-ignore is unavailable, returns the unfiltered walk
    /// (still skips `.git/`).
    let listForPush
        (mappedRoot: string)
        (scope: WorkspaceSyncScope)
        : Result<LocalSyncItem list, string> =
        let root = Path.GetFullPath mappedRoot

        match collectRaw root scope with
        | Error e -> Error e
        | Ok raw ->
            if not (DesktopGit.isAvailable()) then Ok raw
            else applyIgnoreFilter root raw

    /// Immediate children only under `relative` (empty = mapped root).
    /// Same ignore rules as listForPush; does not include the scope dir itself.
    let listImmediateChildren
        (mappedRoot: string)
        (relative: string)
        : Result<LocalSyncItem list, string> =
        let root = Path.GetFullPath mappedRoot

        try
            let scopeFull =
                if relative = "" then root
                else
                    let parts =
                        relative.Split(
                            [| '/' |],
                            StringSplitOptions.RemoveEmptyEntries)
                    Path.GetFullPath(
                        Path.Combine(Array.append [| root |] parts))

            match tryInfo scopeFull with
            | None -> Error "scope path not found"
            | Some false -> Error "scope path is a file"
            | Some true ->
                let raw =
                    listDirChildren relative scopeFull
                    |> List.map (fun (child, _, isDir) ->
                        { relative = child; isDirectory = isDir })
                if not (DesktopGit.isAvailable()) then Ok raw
                else applyIgnoreFilter root raw
        with ex ->
            Error ex.Message

    let private tryItemByteSize (mappedRoot: string) (item: LocalSyncItem) =
        if item.isDirectory then 0L
        else
            let root = Path.GetFullPath mappedRoot
            let parts =
                item.relative.Split(
                    [| '/' |],
                    StringSplitOptions.RemoveEmptyEntries)
            let full =
                Path.GetFullPath(
                    Path.Combine(Array.append [| root |] parts))
            try
                if File.Exists full then FileInfo(full).Length
                else 0L
            with _ ->
                0L

    let toSizedItems (mappedRoot: string) (items: LocalSyncItem list) =
        items
        |> List.map (fun item ->
            ({ relative = item.relative
               isDirectory = item.isDirectory
               byteSize = tryItemByteSize mappedRoot item }
             : WorkspaceSyncLimits.SizedItem))

    /// Scoped ignore-filtered inventory, volume-capped for stubs and PUTs.
    /// Full/TreeStructure → all paths; TopLevel → immediate children only.
    let listForUpload
        (mappedRoot: string)
        (scope: WorkspaceSyncScope)
        : Result<WorkspaceSyncLimits.Mode * LocalSyncItem list, string> =
        match listForPush mappedRoot scope with
        | Error e -> Error e
        | Ok raw ->
            let sized = toSizedItems mappedRoot raw
            let mode, selected =
                WorkspaceSyncLimits.selectForVolume scope.relative sized

            let capped =
                selected
                |> List.map (fun s ->
                    { relative = s.relative
                      isDirectory = s.isDirectory })

            Ok(mode, capped)

    /// Alias used by Desktop inventory endpoint.
    let planUploadInventory = listForUpload

    /// Directories first by depth, then files (stable for MKCOL then PUT).
    let orderForUpload (items: LocalSyncItem list) : LocalSyncItem list =
        let depth (rel: string) =
            if rel = "" then 0
            else rel.Split('/').Length

        let dirs =
            items
            |> List.filter (fun i -> i.isDirectory)
            |> List.sortBy (fun i -> depth i.relative, i.relative)

        let files =
            items
            |> List.filter (fun i -> not i.isDirectory)
            |> List.sortBy (fun i -> i.relative)

        dirs @ files
