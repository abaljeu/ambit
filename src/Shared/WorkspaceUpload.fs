namespace Gambol.Shared

open Gambol.Shared.CommandEntry

/// What Upload should do for the current focus + desktop push capability.
[<RequireQualifiedAccess>]
type WorkspaceUploadAction =
    /// Workspaces focus: pick folder, create named workspace, map, push.
    | CreateWorkspaceFromFolder
    /// Desktop WebDAV push; optional ParseFile after for a file focus.
    | DesktopPush of parseFileId: NodeId option
    /// Web (no desktop): stub-reconcile DataDir children under focus.
    | ReconcileServerDisk
    /// Web (no desktop): parse/reconcile file from DataDir into the graph.
    | ParseServerDisk of NodeId
    | Unavailable of reason: string

[<RequireQualifiedAccess>]
module WorkspaceUpload =

    /// A multi-phase Upload must start from an authoritative, settled revision.
    let canStart (syncInfo: SyncInfo) =
        syncInfo.syncState = Idle && syncInfo.pendingChanges.IsEmpty

    /// Keep parse/materialization requests from one Upload strictly ordered.
    let sequenceParseEffects (effects: Effect list) =
        if effects.IsEmpty then [] else [ ContinueUploadParses effects ]

    /// Palette / key: Upload when Workspaces+desktop, or File/Dir/Workspace focus.
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

    /// Desktop push when caps exist; else graph-only from server DataDir.
    let plan
        (canPush: bool)
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
                if canPush then
                    WorkspaceUploadAction.DesktopPush(Some fileId)
                else
                    WorkspaceUploadAction.ParseServerDisk fileId
            | Some(ReconcileWorkspace _)
            | Some(ReconcileDirectory _) ->
                if canPush then
                    WorkspaceUploadAction.DesktopPush None
                else
                    WorkspaceUploadAction.ReconcileServerDisk
            | None ->
                WorkspaceUploadAction.Unavailable
                    "focus Workspaces, a File, Directory, or named Workspace"
