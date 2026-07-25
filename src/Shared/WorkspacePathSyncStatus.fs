namespace Gambol.Shared

open System

/// Ledger presence for a relative path under a mapped workspace.
[<RequireQualifiedAccess>]
type WorkspacePathPresence =
    | Both
    | LocalOnly
    | ServerOnly

/// Desktop/server path comparison, plus Unparsed when revisions match.
[<RequireQualifiedAccess>]
type WorkspacePathSyncStatus =
    | NoServerFile
    | OnlyOnServer
    | NewerOnServer
    | NewerOnDesktop
    | OnlyOnDesktop
    | Synced
    | Unparsed

/// UI fields for one WorkspacePathSyncStatus case.
type WorkspacePathSyncStatusPresentation =
    { shortLabel: string
      glyph: string
      rowClass: string }

/// Serializable ledger fact used by Client ViewModel (Fable-safe).
type WorkspaceSyncPathFact =
    { relative: string
      isDirectory: bool
      presence: WorkspacePathPresence
      localMtimeUtc: DateTime option
      serverMtimeUtc: DateTime option }

[<RequireQualifiedAccess>]
module WorkspacePathPresence =

    let ofLedgerString =
        function
        | "both" -> Some WorkspacePathPresence.Both
        | "localOnly" -> Some WorkspacePathPresence.LocalOnly
        | "serverOnly" -> Some WorkspacePathPresence.ServerOnly
        | _ -> None

    let toLedgerString =
        function
        | WorkspacePathPresence.Both -> "both"
        | WorkspacePathPresence.LocalOnly -> "localOnly"
        | WorkspacePathPresence.ServerOnly -> "serverOnly"

[<RequireQualifiedAccess>]
module WorkspacePathSyncStatus =

    /// Presentation for every sync status (glyph comments name the code point).
    let table =
        let p shortLabel glyph rowClass =
            { shortLabel = shortLabel
              glyph = glyph
              rowClass = rowClass }
        Map.ofList [
            WorkspacePathSyncStatus.NoServerFile,
            p "no file on server" "\u2205" (* empty set *)
                "amb-row-sync-no-server-file"
            WorkspacePathSyncStatus.OnlyOnServer,
            p "srv only" "\u2601" (* cloud *)
                "amb-row-sync-server-only"
            WorkspacePathSyncStatus.NewerOnServer,
            p "srv new" "\u2193" (* down arrow *)
                "amb-row-sync-server-newer"
            WorkspacePathSyncStatus.NewerOnDesktop,
            p "desk new" "\u2191" (* up arrow *)
                "amb-row-sync-desktop-newer"
            WorkspacePathSyncStatus.OnlyOnDesktop,
            p "desk only" "\u25A2" (* white square with rounded corners *)
                "amb-row-sync-desktop-only"
            WorkspacePathSyncStatus.Synced,
            p "synced" "\u2713" (* check mark *)
                "amb-row-sync-synced"
            WorkspacePathSyncStatus.Unparsed,
            p "unparsed" "\u2026" (* horizontal ellipsis *)
                "amb-row-sync-unparsed"
        ]

    let private presentation status = Map.find status table

    let shortLabel status = (presentation status).shortLabel
    let glyph status = (presentation status).glyph
    let rowClass status = (presentation status).rowClass

    /// Prefer node-carried server disk stamp when present; else ledger.
    let effectiveServerMtime
        (nodeUpdateTime: DateTime)
        (ledgerServerMtimeUtc: DateTime option)
        : DateTime option =
        if nodeUpdateTime <> NodeUpdateTime.missing then
            Some(NodeUpdateTime.toDbPrecision nodeUpdateTime)
        else
            ledgerServerMtimeUtc

    /// Classify from presence + mtimes. Equal / missing mtimes → Synced.
    /// Compare at DB microsecond precision so FS vs graph stamps do not
    /// spuriously disagree on sub-microsecond noise.
    let classifyComparison
        (presence: WorkspacePathPresence)
        (localMtimeUtc: DateTime option)
        (serverMtimeUtc: DateTime option)
        : WorkspacePathSyncStatus =
        match presence with
        | WorkspacePathPresence.LocalOnly ->
            WorkspacePathSyncStatus.OnlyOnDesktop
        | WorkspacePathPresence.ServerOnly ->
            WorkspacePathSyncStatus.OnlyOnServer
        | WorkspacePathPresence.Both ->
            match localMtimeUtc, serverMtimeUtc with
            | Some local, Some server ->
                let local' = NodeUpdateTime.toDbPrecision local
                let server' = NodeUpdateTime.toDbPrecision server
                if local' > server' then
                    WorkspacePathSyncStatus.NewerOnDesktop
                elif server' > local' then
                    WorkspacePathSyncStatus.NewerOnServer
                else
                    WorkspacePathSyncStatus.Synced
            | _ -> WorkspacePathSyncStatus.Synced

    let withUnparsed (isUnparsed: bool) (status: WorkspacePathSyncStatus) =
        match status with
        | WorkspacePathSyncStatus.Synced when isUnparsed ->
            WorkspacePathSyncStatus.Unparsed
        | other -> other

    /// Host-aware resolve: comparison only when desktop + mapped; else Unparsed only.
    /// Mapped with no live/ledger fact → OnlyOnServer (no local FS row for this path).
    let resolve
        (canCompareDesktopMapped: bool)
        (fact: WorkspaceSyncPathFact option)
        (isUnparsed: bool)
        : WorkspacePathSyncStatus option =
        if canCompareDesktopMapped then
            match fact with
            | Some f ->
                classifyComparison f.presence f.localMtimeUtc f.serverMtimeUtc
                |> withUnparsed isUnparsed
                |> Some
            | None ->
                if isUnparsed then Some WorkspacePathSyncStatus.Unparsed
                else Some WorkspacePathSyncStatus.OnlyOnServer
        elif isUnparsed then
            Some WorkspacePathSyncStatus.Unparsed
        else
            None

    /// Like `resolve`, but prefer `nodeUpdateTime` as server stamp when Current.
    /// Unparsed stubs keep ledger stamps: creation `updateTime` is not DataDir mtime.
    /// Persist `SetUpdateTime` must beat a stale aligned ledger so edit→disk shows NewerOnServer.
    let resolveWithNodeStamp
        (canCompareDesktopMapped: bool)
        (fact: WorkspaceSyncPathFact option)
        (nodeUpdateTime: DateTime)
        (isUnparsed: bool)
        : WorkspacePathSyncStatus option =
        let fact' =
            if isUnparsed then
                fact
            else
                fact
                |> Option.map (fun f ->
                    { f with
                        serverMtimeUtc =
                            effectiveServerMtime
                                nodeUpdateTime
                                f.serverMtimeUtc })
        resolve canCompareDesktopMapped fact' isUnparsed
