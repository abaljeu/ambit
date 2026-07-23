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
    | OnlyOnServer
    | NewerOnServer
    | NewerOnDesktop
    | OnlyOnDesktop
    | Synced
    | Unparsed

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
            | Some local, Some server when local > server ->
                WorkspacePathSyncStatus.NewerOnDesktop
            | Some local, Some server when server > local ->
                WorkspacePathSyncStatus.NewerOnServer
            | _ -> WorkspacePathSyncStatus.Synced

    let withUnparsed (isUnparsed: bool) (status: WorkspacePathSyncStatus) =
        match status with
        | WorkspacePathSyncStatus.Synced when isUnparsed ->
            WorkspacePathSyncStatus.Unparsed
        | other -> other

    /// Host-aware resolve: comparison only when desktop + mapped; else Unparsed only.
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
                else None
        elif isUnparsed then
            Some WorkspacePathSyncStatus.Unparsed
        else
            None

    /// Like `resolve`, but prefer `nodeUpdateTime` as server stamp when Current.
    /// Unparsed stubs keep ledger stamps: creation `updateTime` is not DataDir mtime.
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

    let shortLabel =
        function
        | WorkspacePathSyncStatus.OnlyOnServer -> "srv only"
        | WorkspacePathSyncStatus.NewerOnServer -> "srv new"
        | WorkspacePathSyncStatus.NewerOnDesktop -> "desk new"
        | WorkspacePathSyncStatus.OnlyOnDesktop -> "desk only"
        | WorkspacePathSyncStatus.Synced -> "synced"
        | WorkspacePathSyncStatus.Unparsed -> "unparsed"

    let glyph =
        function
        | WorkspacePathSyncStatus.Synced -> "\u2713" // check mark
        | WorkspacePathSyncStatus.OnlyOnServer -> "\u2601" // cloud
        | WorkspacePathSyncStatus.NewerOnServer -> "\u2193" // down arrow
        | WorkspacePathSyncStatus.OnlyOnDesktop -> "\u25A2" // white square with rounded corners
        | WorkspacePathSyncStatus.NewerOnDesktop -> "\u2191" // up arrow
        | WorkspacePathSyncStatus.Unparsed -> "\u2026" // horizontal ellipsis

    let rowClass =
        function
        | WorkspacePathSyncStatus.OnlyOnServer ->
            "amb-row-sync-server-only"
        | WorkspacePathSyncStatus.NewerOnServer ->
            "amb-row-sync-server-newer"
        | WorkspacePathSyncStatus.NewerOnDesktop ->
            "amb-row-sync-desktop-newer"
        | WorkspacePathSyncStatus.OnlyOnDesktop ->
            "amb-row-sync-desktop-only"
        | WorkspacePathSyncStatus.Synced -> "amb-row-sync-synced"
        | WorkspacePathSyncStatus.Unparsed -> "amb-row-sync-unparsed"
