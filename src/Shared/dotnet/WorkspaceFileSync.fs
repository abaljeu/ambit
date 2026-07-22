namespace Gambol.Shared

open System
open System.IO
open System.Net.Http

/// Desktop Post (Push) / Get (Pull) over WebDAV with volume ladder.
[<RequireQualifiedAccess>]
module WorkspaceFileSync =

    type SyncResult =
        { uploaded: int
          downloaded: int
          detail: string
          mode: WorkspaceSyncLimits.Mode option }

    let private localFull (mappedRoot: string) (relative: string) =
        if relative = "" then mappedRoot
        else
            let parts =
                relative.Split(
                    [| '/' |],
                    StringSplitOptions.RemoveEmptyEntries)
            Path.GetFullPath(Path.Combine(Array.append [| mappedRoot |] parts))

    let private ensureLocalDir (path: string) =
        try
            Directory.CreateDirectory path |> ignore
            Ok ()
        with ex ->
            Error ex.Message

    let private writeFile (path: string) (bytes: byte[]) (mtimeUtc: DateTime option) =
        try
            let parent = Path.GetDirectoryName path
            if not (String.IsNullOrEmpty parent) then
                Directory.CreateDirectory parent |> ignore
            File.WriteAllBytes(path, bytes)
            match mtimeUtc with
            | Some utc -> File.SetLastWriteTimeUtc(path, utc)
            | None -> ()
            Ok ()
        with ex ->
            Error ex.Message

    let private readFile (path: string) =
        try Ok(File.ReadAllBytes path)
        with ex -> Error ex.Message

    let private tryFileMtime (path: string) =
        try
            if File.Exists path then Some(File.GetLastWriteTimeUtc path)
            else None
        with _ ->
            None

    let private tryFileSize (path: string) =
        try
            if File.Exists path then Some(FileInfo(path).Length)
            else None
        with _ ->
            None

    let private toSizedItems (mappedRoot: string) (items: LocalSyncItem list) =
        items
        |> List.map (fun item ->
            if item.isDirectory then
                ({ relative = item.relative
                   isDirectory = true
                   byteSize = 0L }
                 : WorkspaceSyncLimits.SizedItem)
            else
                let full = localFull mappedRoot item.relative
                let size = tryFileSize full |> Option.defaultValue 0L
                ({ relative = item.relative
                   isDirectory = false
                   byteSize = size }
                 : WorkspaceSyncLimits.SizedItem))

    let private inventoryFromDav (entries: DavInventoryEntry list) =
        entries
        |> List.map (fun e ->
            ({ relative = e.relative
               isDirectory = e.isCollection
               byteSize = if e.isCollection then 0L else e.contentLength }
             : WorkspaceSyncLimits.SizedItem))

    let private orderPlanned (planned: WorkspaceSyncLimits.PlannedPath list) =
        let depth (rel: string) =
            if rel = "" then 0
            else rel.Split('/').Length
        let dirs =
            planned
            |> List.filter (fun p -> p.isDirectory)
            |> List.sortBy (fun p -> depth p.relative, p.relative)
        let files =
            planned
            |> List.filter (fun p -> not p.isDirectory)
            |> List.sortBy (fun p -> p.relative)
        dirs @ files

    let private uploadOne
        (client: HttpClient)
        (ambitBase: string)
        (label: string)
        (mappedRoot: string)
        (cookie: string option)
        (hint: string option)
        (planned: WorkspaceSyncLimits.PlannedPath)
        : Result<unit, string> =
        if planned.isDirectory then
            WorkspaceDavClient.mkcol
                client ambitBase label planned.relative cookie hint
        else
            let full = localFull mappedRoot planned.relative
            let mtime = tryFileMtime full
            match planned.file with
            | None -> Error "missing file plan"
            | Some WorkspaceSyncLimits.FilePlan.EmptyPlaceholder ->
                WorkspaceDavClient.putBytes
                    client
                    ambitBase
                    label
                    planned.relative
                    Array.empty
                    cookie
                    hint
                    mtime
            | Some(WorkspaceSyncLimits.FilePlan.Body _) ->
                match readFile full with
                | Error e -> Error e
                | Ok bytes ->
                    WorkspaceDavClient.putBytes
                        client
                        ambitBase
                        label
                        planned.relative
                        bytes
                        cookie
                        hint
                        mtime

    let private modeLabel mode =
        match mode with
        | WorkspaceSyncLimits.Mode.Full -> "Full"
        | WorkspaceSyncLimits.Mode.TreeStructure -> "TreeStructure"
        | WorkspaceSyncLimits.Mode.TopLevel -> "TopLevel"

    /// Push: local inventory → classify/plan → MKCOL/PUT → finish-commit.
    let post
        (client: HttpClient)
        (ambitBase: string)
        (mappedRoot: string)
        (scope: WorkspaceSyncScope)
        (cookieHeader: string option)
        (clientHint: string option)
        : Result<SyncResult, string> =
        match WorkspaceLocalInventory.listForPush mappedRoot scope with
        | Error e -> Error e
        | Ok items ->
            match
                WorkspaceDavClient.preparePush
                    client
                    ambitBase
                    scope.label
                    cookieHeader
                    clientHint
            with
            | Error e -> Error e
            | Ok () ->
                let sized = toSizedItems mappedRoot items
                let mode, planned =
                    WorkspaceSyncLimits.plan scope.relative sized
                let ordered = orderPlanned planned

                let rec upload remaining count =
                    match remaining with
                    | [] -> Ok count
                    | item :: rest ->
                        match
                            uploadOne
                                client
                                ambitBase
                                scope.label
                                mappedRoot
                                cookieHeader
                                clientHint
                                item
                        with
                        | Error e -> Error(item.relative + ": " + e)
                        | Ok () -> upload rest (count + 1)

                match upload ordered 0 with
                | Error e -> Error e
                | Ok uploaded ->
                    match
                        WorkspaceDavClient.finishCommit
                            client
                            ambitBase
                            scope.label
                            cookieHeader
                            clientHint
                    with
                    | Error e -> Error e
                    | Ok body ->
                        let baseDetail =
                            sprintf
                                "uploaded %d (%s); finish-commit ok"
                                uploaded
                                (modeLabel mode)
                            + if body = "" then ""
                              else " (" + body + ")"

                        let detail =
                            if DesktopGit.isAvailable() then
                                baseDetail
                            else
                                baseDetail
                                + "; .gitignore filter skipped (git unavailable)"

                        Ok
                            { uploaded = uploaded
                              downloaded = 0
                              detail = detail
                              mode = Some mode }

    let private stagingRoot (mappedRoot: string) (jobId: Guid) =
        Path.Combine(mappedRoot, ".gambol-dl-tmp", jobId.ToString("N"))

    let private discardStaging (path: string) =
        try
            if Directory.Exists path then Directory.Delete(path, true)
        with _ ->
            ()

    let private mergeDirectory (src: string) (dst: string) =
        try
            if not (Directory.Exists src) then Ok ()
            else
                let parent = Path.GetDirectoryName dst
                if not (String.IsNullOrEmpty parent) then
                    Directory.CreateDirectory parent |> ignore
                if Directory.Exists dst then
                    for file in Directory.GetFiles(src, "*", SearchOption.AllDirectories) do
                        let rel = Path.GetRelativePath(src, file)
                        let target = Path.Combine(dst, rel)
                        let targetParent = Path.GetDirectoryName target
                        if not (String.IsNullOrEmpty targetParent) then
                            Directory.CreateDirectory targetParent |> ignore
                        File.Move(file, target, true)
                    Directory.Delete(src, true)
                    Ok ()
                else
                    Directory.Move(src, dst)
                    Ok ()
        with ex ->
            Error ex.Message

    let private promoteAllPlanned
        (mappedRoot: string)
        (stageRoot: string)
        (planned: WorkspaceSyncLimits.PlannedPath list)
        =
        let dirs =
            planned
            |> List.filter (fun p -> p.isDirectory)
            |> List.sortByDescending (fun p ->
                if p.relative = "" then 0
                else p.relative.Split('/').Length)

        let rootFiles =
            planned
            |> List.filter (fun p ->
                not p.isDirectory && p.relative.IndexOf('/') < 0)

        let promoteDirs =
            dirs
            |> List.fold
                (fun acc p ->
                    acc
                    |> Result.bind (fun () ->
                        mergeDirectory
                            (localFull stageRoot p.relative)
                            (localFull mappedRoot p.relative)))
                (Ok ())

        rootFiles
        |> List.fold
            (fun acc p ->
                acc
                |> Result.bind (fun () ->
                    let src = localFull stageRoot p.relative
                    let dst = localFull mappedRoot p.relative
                    try
                        if File.Exists src then
                            let parent = Path.GetDirectoryName dst

                            if not (String.IsNullOrEmpty parent) then
                                Directory.CreateDirectory parent |> ignore

                            File.Move(src, dst, true)

                        Ok ()
                    with ex ->
                        Error ex.Message))
            promoteDirs

    let private downloadPlannedFile
        (client: HttpClient)
        (ambitBase: string)
        (label: string)
        (stageRoot: string)
        (cookie: string option)
        (entryMap: Map<string, DavInventoryEntry>)
        (planned: WorkspaceSyncLimits.PlannedPath)
        =
        if planned.isDirectory then
            ensureLocalDir (localFull stageRoot planned.relative)
        else
            let mtime =
                entryMap
                |> Map.tryFind planned.relative
                |> Option.bind (fun e -> e.lastModifiedUtc)
            match planned.file with
            | None -> Error "missing file plan"
            | Some WorkspaceSyncLimits.FilePlan.EmptyPlaceholder ->
                writeFile
                    (localFull stageRoot planned.relative)
                    Array.empty
                    mtime
            | Some(WorkspaceSyncLimits.FilePlan.Body _) ->
                match
                    WorkspaceDavClient.getBytes
                        client
                        ambitBase
                        label
                        planned.relative
                        cookie
                with
                | Error err -> Error err
                | Ok bytes ->
                    writeFile
                        (localFull stageRoot planned.relative)
                        bytes
                        mtime

    /// Pull with volume ladder; stage under `.gambol-dl-tmp/{jobId}` then promote.
    let getStaged
        (client: HttpClient)
        (ambitBase: string)
        (mappedRoot: string)
        (scope: WorkspaceSyncScope)
        (cookieHeader: string option)
        (jobId: Guid)
        : Result<SyncResult, string> =
        let depth =
            match scope.kind with
            | SyncScopeKind.File -> "0"
            | _ -> "infinity"

        match
            WorkspaceDavClient.propfind
                client
                ambitBase
                scope.label
                scope.relative
                depth
                cookieHeader
        with
        | Error e -> Error e
        | Ok inventory ->
            let scoped =
                inventory
                |> List.filter (fun e ->
                    WorkspaceSyncScope.isUnderScope scope e.relative)

            let entryMap =
                scoped
                |> List.map (fun e -> e.relative, e)
                |> Map.ofList

            let sized = inventoryFromDav scoped
            let mode, planned = WorkspaceSyncLimits.plan scope.relative sized
            let ordered = orderPlanned planned
            let stage = stagingRoot mappedRoot jobId

            match
                try
                    discardStaging stage
                    Directory.CreateDirectory stage |> ignore
                    Ok ()
                with ex ->
                    Error ex.Message
            with
            | Error e -> Error e
            | Ok () ->
                let rec download remaining count =
                    match remaining with
                    | [] -> Ok count
                    | item :: rest ->
                        match
                            downloadPlannedFile
                                client
                                ambitBase
                                scope.label
                                stage
                                cookieHeader
                                entryMap
                                item
                        with
                        | Error e -> Error(item.relative + ": " + e)
                        | Ok () ->
                            match item.file with
                            | Some _ when not item.isDirectory ->
                                download rest (count + 1)
                            | _ -> download rest count

                match download ordered 0 with
                | Error e ->
                    discardStaging stage
                    Error e
                | Ok downloaded ->
                    match promoteAllPlanned mappedRoot stage planned with
                    | Error e ->
                        discardStaging stage
                        Error e
                    | Ok () ->
                        discardStaging stage
                        Ok
                            { uploaded = 0
                              downloaded = downloaded
                              detail =
                                sprintf
                                    "downloaded %d files (%s)"
                                    downloaded
                                    (modeLabel mode)
                              mode = Some mode }

    /// Pull: PROPFIND inventory → limited GET under mapped root (blocking; manager preferred).
    let get
        (client: HttpClient)
        (ambitBase: string)
        (mappedRoot: string)
        (scope: WorkspaceSyncScope)
        (cookieHeader: string option)
        : Result<SyncResult, string> =
        getStaged client ambitBase mappedRoot scope cookieHeader (Guid.NewGuid())
