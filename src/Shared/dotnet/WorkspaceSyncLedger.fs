namespace Gambol.Shared

open System
open System.IO
open System.Text.Json

/// Per-path sync ledger row (mtime skip now; delete propagation later).
type SyncLedgerRow =
    { relative: string
      isDirectory: bool
      localMtimeUtc: DateTime option
      serverMtimeUtc: DateTime option
      lastServerHead: string option
      presence: string
      lastOp: string option }

type WorkspaceSyncLedger =
    { label: string
      rows: SyncLedgerRow list }

[<RequireQualifiedAccess>]
module WorkspaceSyncLedger =

    [<Literal>]
    let PresenceBoth = "both"

    [<Literal>]
    let PresenceLocalOnly = "localOnly"

    [<Literal>]
    let PresenceServerOnly = "serverOnly"

    [<Literal>]
    let OpSeed = "seed"

    [<Literal>]
    let OpUpload = "upload"

    [<Literal>]
    let OpDownload = "download"

    let gambolAppDataDir () =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "Gambol")

    let ledgerFileName (label: string) = "sync-ledger-" + label + ".json"

    let ledgerPathFor (label: string) =
        Path.Combine(gambolAppDataDir (), ledgerFileName label)

    let private localFull (mappedRoot: string) (relative: string) =
        if relative = "" then mappedRoot
        else
            let parts =
                relative.Split(
                    [| '/' |],
                    StringSplitOptions.RemoveEmptyEntries)
            Path.GetFullPath(Path.Combine(Array.append [| mappedRoot |] parts))

    let tryLocalMtime (mappedRoot: string) (relative: string) (isDirectory: bool) =
        if isDirectory then None
        else
            try
                let full = localFull mappedRoot relative
                if File.Exists full then Some(File.GetLastWriteTimeUtc full)
                else None
            with _ ->
                None

    /// Live FS mtime for status UI (files and directories). Does not change sync skip rules.
    /// Directories: prefer `.amb` (graph/server authority). Plain directory
    /// LastWriteTime is not used — child file writes bump it and falsely show
    /// desk-newer after Download.
    let tryLiveLocalMtime
        (mappedRoot: string)
        (relative: string)
        (isDirectory: bool)
        : DateTime option =
        try
            let full = localFull mappedRoot relative
            if isDirectory then
                if not (Directory.Exists full) then None
                else
                    let amb = Path.Combine(full, ".amb")
                    if File.Exists amb then
                        Some(File.GetLastWriteTimeUtc amb)
                    else
                        None
            elif File.Exists full then
                Some(File.GetLastWriteTimeUtc full)
            else
                None
        with _ ->
            None

    /// Upload: skip PUT when server mtime is same or newer than local (UTC).
    let shouldSkipUpload (localMtimeUtc: DateTime) (serverMtimeUtc: DateTime option) =
        match serverMtimeUtc with
        | None -> false
        | Some server -> server >= localMtimeUtc

    /// Download: skip GET when local mtime is same or newer than server (UTC).
    let shouldSkipDownload (serverMtimeUtc: DateTime) (localMtimeUtc: DateTime option) =
        match localMtimeUtc with
        | None -> false
        | Some local -> local >= serverMtimeUtc

    /// Directory / workspace scopes may mtime-skip; single-file never skips.
    let allowsMtimeSkip (kind: SyncScopeKind) =
        match kind with
        | SyncScopeKind.File -> false
        | SyncScopeKind.Directory
        | SyncScopeKind.Workspace -> true

    let shouldSkipUploadScoped
        (kind: SyncScopeKind)
        (localMtimeUtc: DateTime)
        (serverMtimeUtc: DateTime option)
        =
        if allowsMtimeSkip kind then
            shouldSkipUpload localMtimeUtc serverMtimeUtc
        else
            false

    let shouldSkipDownloadScoped
        (kind: SyncScopeKind)
        (serverMtimeUtc: DateTime)
        (localMtimeUtc: DateTime option)
        =
        if allowsMtimeSkip kind then
            shouldSkipDownload serverMtimeUtc localMtimeUtc
        else
            false

    /// Locked #7: client file, server file, and graph node share one stamp.
    let transferDatestampsMatch
        (clientFileUtc: DateTime)
        (serverFileUtc: DateTime)
        (graphNodeUtc: DateTime)
        =
        clientFileUtc = serverFileUtc && serverFileUtc = graphNodeUtc

    let ledgerRowDatestampsAligned (row: SyncLedgerRow) =
        match row.localMtimeUtc, row.serverMtimeUtc with
        | Some local, Some server -> local = server
        | _ -> false

    let needsSeed (ledger: WorkspaceSyncLedger) = ledger.rows.IsEmpty

    let private tryParseDate (el: JsonElement) =
        if el.ValueKind <> JsonValueKind.String then None
        else
            match el.GetString() with
            | null -> None
            | text ->
                match DateTime.TryParse(text) with
                | true, dt -> Some(dt.ToUniversalTime())
                | _ -> None

    let private parseRow (item: JsonElement) : Result<SyncLedgerRow, string> =
        if item.ValueKind <> JsonValueKind.Object then
            Error "row must be an object"
        else
            let mutable rel = Unchecked.defaultof<JsonElement>
            let mutable isDir = Unchecked.defaultof<JsonElement>
            if not (item.TryGetProperty("relative", &rel)) then Error "row missing relative"
            elif rel.ValueKind <> JsonValueKind.String then Error "relative must be string"
            elif not (item.TryGetProperty("isDirectory", &isDir)) then
                Error "row missing isDirectory"
            elif isDir.ValueKind <> JsonValueKind.True
                 && isDir.ValueKind <> JsonValueKind.False then
                Error "isDirectory must be boolean"
            else
                let mutable localM = Unchecked.defaultof<JsonElement>
                let mutable serverM = Unchecked.defaultof<JsonElement>
                let mutable head = Unchecked.defaultof<JsonElement>
                let mutable presence = Unchecked.defaultof<JsonElement>
                let mutable lastOp = Unchecked.defaultof<JsonElement>
                let localMtime =
                    if item.TryGetProperty("localMtimeUtc", &localM) then
                        tryParseDate localM
                    else None
                let serverMtime =
                    if item.TryGetProperty("serverMtimeUtc", &serverM) then
                        tryParseDate serverM
                    else None
                let lastHead =
                    if
                        item.TryGetProperty("lastServerHead", &head)
                        && head.ValueKind = JsonValueKind.String
                    then
                        match head.GetString() with
                        | null -> None
                        | s when s.Trim().Length = 0 -> None
                        | s -> Some s
                    else None
                let pres =
                    if
                        item.TryGetProperty("presence", &presence)
                        && presence.ValueKind = JsonValueKind.String
                    then
                        match presence.GetString() with
                        | null -> PresenceBoth
                        | s when s.Trim().Length = 0 -> PresenceBoth
                        | s -> s
                    else PresenceBoth
                let op =
                    if
                        item.TryGetProperty("lastOp", &lastOp)
                        && lastOp.ValueKind = JsonValueKind.String
                    then
                        match lastOp.GetString() with
                        | null -> None
                        | s when s.Trim().Length = 0 -> None
                        | s -> Some s
                    else None
                Ok
                    { relative = rel.GetString()
                      isDirectory = isDir.GetBoolean()
                      localMtimeUtc = localMtime
                      serverMtimeUtc = serverMtime
                      lastServerHead = lastHead
                      presence = pres
                      lastOp = op }

    let decode (json: string) : Result<WorkspaceSyncLedger, string> =
        try
            use doc = JsonDocument.Parse json
            let root = doc.RootElement
            let mutable labelEl = Unchecked.defaultof<JsonElement>
            let mutable rowsEl = Unchecked.defaultof<JsonElement>
            if not (root.TryGetProperty("label", &labelEl)) then Error "label is required"
            elif labelEl.ValueKind <> JsonValueKind.String then Error "label must be string"
            elif not (root.TryGetProperty("rows", &rowsEl)) then Error "rows is required"
            elif rowsEl.ValueKind <> JsonValueKind.Array then Error "rows must be an array"
            else
                rowsEl.EnumerateArray()
                |> Seq.toList
                |> List.mapi (fun i r ->
                    parseRow r |> Result.mapError (fun e -> $"row {i}: {e}"))
                |> List.fold
                    (fun acc next ->
                        match acc, next with
                        | Ok xs, Ok x -> Ok (xs @ [ x ])
                        | Error e, _ -> Error e
                        | _, Error e -> Error e)
                    (Ok [])
                |> Result.map (fun rows ->
                    { label = labelEl.GetString()
                      rows = rows })
        with
        | :? JsonException -> Error "malformed_json"

    let encode (ledger: WorkspaceSyncLedger) : string =
        let rowJson (r: SyncLedgerRow) =
            let localM =
                r.localMtimeUtc
                |> Option.map (fun dt -> dt.ToString("O"))
                |> Option.defaultValue ""
            let serverM =
                r.serverMtimeUtc
                |> Option.map (fun dt -> dt.ToString("O"))
                |> Option.defaultValue ""
            {| relative = r.relative
               isDirectory = r.isDirectory
               localMtimeUtc = localM
               serverMtimeUtc = serverM
               lastServerHead = r.lastServerHead |> Option.defaultValue ""
               presence = r.presence
               lastOp = r.lastOp |> Option.defaultValue "" |}
        JsonSerializer.Serialize(
            {| label = ledger.label
               rows = ledger.rows |> List.map rowJson |> List.toArray |})

    let loadFromFile (path: string) (label: string) : Result<WorkspaceSyncLedger, string> =
        if not (File.Exists path) then Ok { label = label; rows = [] }
        else
            try
                let json = File.ReadAllText path
                decode json
                |> Result.map (fun ledger -> { ledger with label = label })
            with
            | :? IOException -> Error "ledger_read_failed"
            | :? UnauthorizedAccessException -> Error "ledger_read_failed"

    let saveToFile (path: string) (ledger: WorkspaceSyncLedger) : Result<unit, string> =
        try
            let dir = Path.GetDirectoryName path
            if not (String.IsNullOrEmpty dir) then
                Directory.CreateDirectory(dir) |> ignore
            File.WriteAllText(path, encode ledger)
            Ok ()
        with
        | :? IOException -> Error "ledger_write_failed"
        | :? UnauthorizedAccessException -> Error "ledger_write_failed"

    let loadForLabel (label: string) : Result<WorkspaceSyncLedger, string> =
        loadFromFile (ledgerPathFor label) label

    let saveForLabel (ledger: WorkspaceSyncLedger) : Result<unit, string> =
        saveToFile (ledgerPathFor ledger.label) ledger

    let rowMap (ledger: WorkspaceSyncLedger) =
        ledger.rows |> List.map (fun r -> r.relative, r) |> Map.ofList

    /// Live status rows for UI: local inventory mtimes + ledger server-only paths.
    /// Does not write the ledger. Local paths use presence=both so the client can
    /// overlay graph `updateTime` as the server stamp.
    let liveStatusRows
        (mappedRoot: string)
        (ledger: WorkspaceSyncLedger)
        : SyncLedgerRow list =
        let scope =
            { label = ledger.label
              relative = ""
              kind = SyncScopeKind.Workspace }
        let ledgerBy = rowMap ledger
        let localItems =
            match WorkspaceLocalInventory.listForPush mappedRoot scope with
            | Ok items ->
                { relative = ""; isDirectory = true } :: items
            | Error _ ->
                match tryLiveLocalMtime mappedRoot "" true with
                | Some _ -> [ { relative = ""; isDirectory = true } ]
                | None -> []
        let fromLocal =
            localItems
            |> List.map (fun item ->
                let prior = Map.tryFind item.relative ledgerBy
                { relative = item.relative
                  isDirectory = item.isDirectory
                  localMtimeUtc =
                      tryLiveLocalMtime
                          mappedRoot
                          item.relative
                          item.isDirectory
                  serverMtimeUtc =
                      prior |> Option.bind (fun r -> r.serverMtimeUtc)
                  lastServerHead =
                      prior |> Option.bind (fun r -> r.lastServerHead)
                  presence = PresenceBoth
                  lastOp = prior |> Option.bind (fun r -> r.lastOp) })
        let localSet =
            fromLocal |> List.map (fun r -> r.relative) |> Set.ofList
        let serverOnly =
            ledger.rows
            |> List.filter (fun r ->
                r.presence = PresenceServerOnly
                && not (Set.contains r.relative localSet))
        fromLocal @ serverOnly

    let tryServerMtime (ledger: WorkspaceSyncLedger) (relative: string) =
        ledger.rows
        |> List.tryFind (fun r -> r.relative = relative)
        |> Option.bind (fun r -> r.serverMtimeUtc)

    let seed
        (label: string)
        (mappedRoot: string)
        (serverEntries: DavInventoryEntry list)
        (localItems: LocalSyncItem list)
        : WorkspaceSyncLedger =
        let serverByRel =
            serverEntries
            |> List.map (fun e -> e.relative, e)
            |> Map.ofList
        let localByRel =
            localItems
            |> List.map (fun i -> i.relative, i)
            |> Map.ofList
        let allRels =
            Set.union (Set.ofList (Map.keys serverByRel |> Seq.toList))
                (Set.ofList (Map.keys localByRel |> Seq.toList))
            |> Set.toList
            |> List.sort
        let rows =
            allRels
            |> List.map (fun relative ->
                let onServer = Map.containsKey relative serverByRel
                let onLocal = Map.containsKey relative localByRel
                let isDir =
                    match Map.tryFind relative localByRel with
                    | Some item -> item.isDirectory
                    | None ->
                        match Map.tryFind relative serverByRel with
                        | Some e -> e.isCollection
                        | None -> false
                let serverM =
                    Map.tryFind relative serverByRel
                    |> Option.bind (fun e -> e.lastModifiedUtc)
                let localM = tryLocalMtime mappedRoot relative isDir
                let presence =
                    match onServer, onLocal with
                    | true, true -> PresenceBoth
                    | false, true -> PresenceLocalOnly
                    | true, false -> PresenceServerOnly
                    | false, false -> PresenceBoth
                { relative = relative
                  isDirectory = isDir
                  localMtimeUtc = localM
                  serverMtimeUtc = serverM
                  lastServerHead = None
                  presence = presence
                  lastOp = Some OpSeed })
        { label = label; rows = rows }

    let private upsertRow (rows: SyncLedgerRow list) (row: SyncLedgerRow) =
        let rest = rows |> List.filter (fun r -> r.relative <> row.relative)
        rest @ [ row ]

    let recordUpload
        (ledger: WorkspaceSyncLedger)
        (relative: string)
        (isDirectory: bool)
        (localMtimeUtc: DateTime)
        (serverMtimeUtc: DateTime)
        (lastServerHead: string option)
        : WorkspaceSyncLedger =
        let row =
            { relative = relative
              isDirectory = isDirectory
              localMtimeUtc = if isDirectory then None else Some localMtimeUtc
              serverMtimeUtc = if isDirectory then None else Some serverMtimeUtc
              lastServerHead = lastServerHead
              presence = PresenceBoth
              lastOp = Some OpUpload }
        { ledger with rows = upsertRow ledger.rows row }

    let recordDownload
        (ledger: WorkspaceSyncLedger)
        (relative: string)
        (localMtimeUtc: DateTime)
        (serverMtimeUtc: DateTime)
        : WorkspaceSyncLedger =
        let row =
            { relative = relative
              isDirectory = false
              localMtimeUtc = Some localMtimeUtc
              serverMtimeUtc = Some serverMtimeUtc
              lastServerHead = None
              presence = PresenceBoth
              lastOp = Some OpDownload }
        { ledger with rows = upsertRow ledger.rows row }

    let tryParseFinishHead (json: string) : string option =
        try
            use doc = JsonDocument.Parse json
            let root = doc.RootElement
            let mutable head = Unchecked.defaultof<JsonElement>
            if
                root.TryGetProperty("head", &head)
                && head.ValueKind = JsonValueKind.String
            then
                match head.GetString() with
                | null -> None
                | s when s.Trim().Length = 0 -> None
                | s -> Some s
            else None
        with _ ->
            None
