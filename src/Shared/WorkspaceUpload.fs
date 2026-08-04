namespace Gambol.Shared

open Gambol.Shared.CommandEntry

/// What Upload (file push / parse) should do for the current Load focus.
[<RequireQualifiedAccess>]
type WorkspaceUploadAction =
    /// Workspaces focus: pick folder, create named workspace, map, push.
    | CreateWorkspaceFromFolder
    /// Desktop WebDAV push; Some fileId → Parse that file after push (single-file only).
    | DesktopPush of parseFileId: NodeId option
    /// Web (no desktop): stub-reconcile DataDir children under focus.
    | ReconcileServerDisk
    /// Web (no desktop): parse/reconcile file from DataDir into the graph.
    | ParseServerDisk of NodeId
    | Unavailable of reason: string

[<RequireQualifiedAccess>]
module WorkspaceUpload =

    /// Desktop multi-phase Upload must start from Idle with no pending ops.
    let canStart (syncInfo: SyncInfo) =
        syncInfo.syncState = Idle && syncInfo.pendingChanges.IsEmpty

    /// Web parse/reconcile: empty pending is enough; Polling must not block.
    let canStartWeb (syncInfo: SyncInfo) =
        syncInfo.pendingChanges.IsEmpty
        && match syncInfo.syncState with
           | Idle | Polling -> true
           | _ -> false

    /// Detail when Load is parked; distinguish pending ops from sync busy.
    let queueBlockedDetail (syncInfo: SyncInfo) =
        if not syncInfo.pendingChanges.IsEmpty then
            "load queued behind pending changes"
        else
            match syncInfo.syncState with
            | Polling -> "load queued until poll completes"
            | Sending _ -> "load queued until submit completes"
            | Uploading -> "load queued until current upload completes"
            | WaitingToRetry _ -> "load queued until retry completes"
            | _ -> "load queued until sync settles"

    /// Keep parse/materialization requests from one Upload strictly ordered.
    let sequenceParseEffects (effects: Effect list) =
        if effects.IsEmpty then [] else [ ContinueUploadParses effects ]

    /// Only a completed mapped desktop push should source parse text from desktop.
    let desktopReadPath
        (action: WorkspaceUploadAction)
        (canImportDesktop: bool)
        (path: string option)
        : string option =
        match action with
        | WorkspaceUploadAction.DesktopPush _ when canImportDesktop -> path
        | _ -> None

    /// Palette / key: Load when Workspaces+desktop, or File/Dir/Workspace focus.
    let isAvailable
        (canPush: bool)
        (focusIsWorkspaces: bool)
        (target: ContextualTarget option)
        : bool =
        if focusIsWorkspaces then
            canPush
        else
            match target with
            | Some(ParseFile _)
            | Some(ReconcileWorkspace _)
            | Some(ReconcileDirectory _) -> true
            | None -> false

    /// Desktop push when caps + mapping exist; else graph-only from server DataDir.
    /// Unmapped labels still Parse / Reconcile — do not fail Upload on missing mapping.
    let plan
        (canPush: bool)
        (hasLocalMapping: bool)
        (focusIsWorkspaces: bool)
        (target: ContextualTarget option)
        : WorkspaceUploadAction =
        if focusIsWorkspaces then
            if canPush then
                WorkspaceUploadAction.CreateWorkspaceFromFolder
            else
                WorkspaceUploadAction.Unavailable
                    "desktop unavailable: cannot create workspace from folder"
        else
            match target with
            | Some(ParseFile fileId) ->
                if canPush && hasLocalMapping then
                    WorkspaceUploadAction.DesktopPush(Some fileId)
                else
                    WorkspaceUploadAction.ParseServerDisk fileId
            | Some(ReconcileWorkspace _)
            | Some(ReconcileDirectory _) ->
                if canPush && hasLocalMapping then
                    WorkspaceUploadAction.DesktopPush None
                else
                    WorkspaceUploadAction.ReconcileServerDisk
            | None ->
                WorkspaceUploadAction.Unavailable
                    "focus Workspaces, a File, Directory, or named Workspace"
