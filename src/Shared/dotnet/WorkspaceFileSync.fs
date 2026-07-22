namespace Gambol.Shared

open System
open System.IO
open System.Net.Http
open System.Threading
open System.Threading.Tasks

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

    /// Max overlapping WebDAV PUT/MKCOL requests within one wave.
    let private uploadConcurrency = 12

    let private pathDepth (rel: string) =
        if rel = "" then 0
        else rel.Split('/').Length

    /// Dependency-safe waves: directory depth groups, then all files.
    let partitionUploadWaves
        (planned: WorkspaceSyncLimits.PlannedPath list)
        : WorkspaceSyncLimits.PlannedPath list list =
        let dirWaves =
            planned
            |> List.filter (fun p -> p.isDirectory)
            |> List.groupBy (fun p -> pathDepth p.relative)
            |> List.sortBy fst
            |> List.map (fun (_, items) ->
                items |> List.sortBy (fun p -> p.relative))

        let files =
            planned
            |> List.filter (fun p -> not p.isDirectory)
            |> List.sortBy (fun p -> p.relative)

        if files.IsEmpty then dirWaves
        else dirWaves @ [ files ]

    let private orderPlanned (planned: WorkspaceSyncLimits.PlannedPath list) =
        partitionUploadWaves planned |> List.concat

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

    let private runUploadWave
        (client: HttpClient)
        (ambitBase: string)
        (label: string)
        (mappedRoot: string)
        (cookie: string option)
        (hint: string option)
        (wave: WorkspaceSyncLimits.PlannedPath list)
        : Result<WorkspaceSyncLimits.PlannedPath list, string> =
        if wave.IsEmpty then Ok []
        else
            use gate = new SemaphoreSlim(uploadConcurrency)

            let tasks =
                wave
                |> List.map (fun item ->
                    Task.Run(fun () ->
                        gate.Wait()

                        try
                            match
                                uploadOne
                                    client
                                    ambitBase
                                    label
                                    mappedRoot
                                    cookie
                                    hint
                                    item
                            with
                            | Error e ->
                                Error(item.relative + ": " + e)
                            | Ok () -> Ok item
                        finally
                            gate.Release() |> ignore))
                |> Array.ofList

            Task.WaitAll(tasks |> Array.map (fun t -> t :> Task))

            let results =
                tasks
                |> Array.map (fun t -> t.Result)
                |> Array.toList

            match
                results
                |> List.tryPick (function
                    | Error e -> Some e
                    | Ok _ -> None)
            with
            | Some e -> Error e
            | None ->
                results
                |> List.choose (function
                    | Ok item -> Some item
                    | Error _ -> None)
                |> Ok

    let private modeLabel mode =
        match mode with
        | WorkspaceSyncLimits.Mode.Full -> "Full"
        | WorkspaceSyncLimits.Mode.TreeStructure -> "TreeStructure"
        | WorkspaceSyncLimits.Mode.TopLevel -> "TopLevel"

    let private fullWorkspaceScope (label: string) : WorkspaceSyncScope =
        { label = label
          relative = ""
          kind = SyncScopeKind.Workspace }

    let private propfindDepth (scope: WorkspaceSyncScope) =
        match scope.kind with
        | SyncScopeKind.File -> "0"
        | SyncScopeKind.Workspace
        | SyncScopeKind.Directory -> "infinity"

    let private serverMtimeMap (entries: DavInventoryEntry list) =
        entries
        |> List.filter (fun e -> not e.isCollection)
        |> List.map (fun e -> e.relative, e.lastModifiedUtc)
        |> Map.ofList

    let private ensureLedgerSeeded
        (client: HttpClient)
        (ambitBase: string)
        (mappedRoot: string)
        (label: string)
        (cookie: string option)
        =
        match WorkspaceSyncLedger.loadForLabel label with
        | Error e -> Error e
        | Ok ledger ->
            if not (WorkspaceSyncLedger.needsSeed ledger) then Ok ledger
            else
                match
                    WorkspaceDavClient.propfind
                        client
                        ambitBase
                        label
                        ""
                        "infinity"
                        cookie
                with
                | Error e -> Error e
                | Ok serverEntries ->
                    match
                        WorkspaceLocalInventory.listForPush
                            mappedRoot
                            (fullWorkspaceScope label)
                    with
                    | Error e -> Error e
                    | Ok localItems ->
                        let seeded =
                            WorkspaceSyncLedger.seed
                                label
                                mappedRoot
                                serverEntries
                                localItems
                        match WorkspaceSyncLedger.saveForLabel seeded with
                        | Error e -> Error e
                        | Ok () -> Ok seeded

    let private needsScopePropfind
        (ledger: WorkspaceSyncLedger)
        (planned: WorkspaceSyncLimits.PlannedPath list)
        =
        planned
        |> List.exists (fun p ->
            not p.isDirectory
            && WorkspaceSyncLedger.tryServerMtime ledger p.relative
               |> Option.isNone)

    let private fetchScopeServerMtimes
        (client: HttpClient)
        (ambitBase: string)
        (scope: WorkspaceSyncScope)
        (cookie: string option)
        =
        match
            WorkspaceDavClient.propfind
                client
                ambitBase
                scope.label
                scope.relative
                (propfindDepth scope)
                cookie
        with
        | Error e -> Error e
        | Ok entries ->
            Ok(
                entries
                |> List.filter (fun e ->
                    WorkspaceSyncScope.isUnderScope scope e.relative)
                |> serverMtimeMap)

    let private shouldSkipUploadFile
        (ledger: WorkspaceSyncLedger)
        (scopeServer: Map<string, DateTime option>)
        (mappedRoot: string)
        (planned: WorkspaceSyncLimits.PlannedPath)
        =
        if planned.isDirectory then false
        else
            let full = localFull mappedRoot planned.relative

            match tryFileMtime full with
            | None -> false
            | Some localM ->
                let serverM =
                    WorkspaceSyncLedger.tryServerMtime ledger planned.relative
                    |> Option.orElseWith (fun () ->
                        Map.tryFind planned.relative scopeServer
                        |> Option.flatten)
                WorkspaceSyncLedger.shouldSkipUpload localM serverM

    let private shouldSkipDownloadFile
        (mappedRoot: string)
        (entryMap: Map<string, DavInventoryEntry>)
        (planned: WorkspaceSyncLimits.PlannedPath)
        =
        if planned.isDirectory then false
        else
            match
                entryMap
                |> Map.tryFind planned.relative
                |> Option.bind (fun e -> e.lastModifiedUtc)
            with
            | None -> false
            | Some serverM ->
                let localM =
                    tryFileMtime (localFull mappedRoot planned.relative)
                WorkspaceSyncLedger.shouldSkipDownload serverM localM

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

                match
                    ensureLedgerSeeded
                        client
                        ambitBase
                        mappedRoot
                        scope.label
                        cookieHeader
                with
                | Error e -> Error e
                | Ok ledger0 ->
                    let scopeServer =
                        if needsScopePropfind ledger0 ordered then
                            match
                                fetchScopeServerMtimes
                                    client
                                    ambitBase
                                    scope
                                    cookieHeader
                            with
                            | Error e -> Error e
                            | Ok m -> Ok m
                        else Ok Map.empty

                    match scopeServer with
                    | Error e -> Error e
                    | Ok scopeServerMap ->
                        let mutable ledger = ledger0
                        let mutable skipped = 0

                        let recordUploaded
                            (item: WorkspaceSyncLimits.PlannedPath)
                            =
                            if item.isDirectory then
                                ledger <-
                                    WorkspaceSyncLedger.recordUpload
                                        ledger
                                        item.relative
                                        true
                                        DateTime.UtcNow
                                        DateTime.UtcNow
                                        None
                            elif
                                item.file.IsSome
                                && not item.isDirectory
                            then
                                let full =
                                    localFull mappedRoot item.relative

                                match tryFileMtime full with
                                | None -> ()
                                | Some localM ->
                                    ledger <-
                                        WorkspaceSyncLedger.recordUpload
                                            ledger
                                            item.relative
                                            false
                                            localM
                                            localM
                                            None

                        let toUpload =
                            ordered
                            |> List.filter (fun item ->
                                if
                                    shouldSkipUploadFile
                                        ledger
                                        scopeServerMap
                                        mappedRoot
                                        item
                                then
                                    skipped <- skipped + 1
                                    false
                                else
                                    true)

                        let rec uploadWaves remaining count =
                            match remaining with
                            | [] -> Ok count
                            | wave :: rest ->
                                match
                                    runUploadWave
                                        client
                                        ambitBase
                                        scope.label
                                        mappedRoot
                                        cookieHeader
                                        clientHint
                                        wave
                                with
                                | Error e -> Error e
                                | Ok uploadedItems ->
                                    for item in uploadedItems do
                                        recordUploaded item

                                    uploadWaves
                                        rest
                                        (count + List.length uploadedItems)

                        match
                            uploadWaves
                                (partitionUploadWaves toUpload)
                                0
                        with
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
                            | Ok finishBody ->
                                let head =
                                    WorkspaceSyncLedger.tryParseFinishHead finishBody

                                if head.IsSome then
                                    ledger <-
                                        { ledger with
                                            rows =
                                                ledger.rows
                                                |> List.map (fun r ->
                                                    { r with
                                                        lastServerHead = head }) }

                                match WorkspaceSyncLedger.saveForLabel ledger with
                                | Error e -> Error e
                                | Ok () ->
                                    let baseDetail =
                                        if skipped > 0 then
                                            sprintf
                                                "uploaded %d, skipped %d (%s)"
                                                uploaded
                                                skipped
                                                (modeLabel mode)
                                        else
                                            sprintf
                                                "uploaded %d (%s)"
                                                uploaded
                                                (modeLabel mode)

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

    /// Promote staged paths into the mapped root.
    /// Nested File-scope plans have no directory rows — must move those files too.
    let promotePlanned
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

        let files =
            planned
            |> List.filter (fun p -> not p.isDirectory)

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

        files
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

            match
                ensureLedgerSeeded
                    client
                    ambitBase
                    mappedRoot
                    scope.label
                    cookieHeader
            with
            | Error e -> Error e
            | Ok ledger0 ->
                let sized = inventoryFromDav scoped
                let mode, planned = WorkspaceSyncLimits.plan scope.relative sized
                let ordered = orderPlanned planned
                let stage = stagingRoot mappedRoot jobId
                let mutable ledger = ledger0
                let mutable skipped = 0
                let downloadedPaths = ResizeArray<string>()

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
                            if shouldSkipDownloadFile mappedRoot entryMap item then
                                skipped <- skipped + 1
                                download rest count
                            else
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
                                        downloadedPaths.Add item.relative
                                        download rest (count + 1)
                                    | _ -> download rest count

                    match download ordered 0 with
                    | Error e ->
                        discardStaging stage
                        Error e
                    | Ok downloaded ->
                        match promotePlanned mappedRoot stage planned with
                        | Error e ->
                            discardStaging stage
                            Error e
                        | Ok () ->
                            discardStaging stage

                            for rel in downloadedPaths do
                                match
                                    entryMap
                                    |> Map.tryFind rel
                                    |> Option.bind (fun e -> e.lastModifiedUtc)
                                with
                                | None -> ()
                                | Some serverM ->
                                    ledger <-
                                        WorkspaceSyncLedger.recordDownload
                                            ledger
                                            rel
                                            serverM
                                            serverM

                            match WorkspaceSyncLedger.saveForLabel ledger with
                            | Error e -> Error e
                            | Ok () ->
                                let detail =
                                    if skipped > 0 then
                                        sprintf
                                            "downloaded %d files, skipped %d (%s)"
                                            downloaded
                                            skipped
                                            (modeLabel mode)
                                    else
                                        sprintf
                                            "downloaded %d files (%s)"
                                            downloaded
                                            (modeLabel mode)

                                Ok
                                    { uploaded = 0
                                      downloaded = downloaded
                                      detail = detail
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
