namespace Gambol.Shared

open Gambol.Shared.CommandEntry

[<RequireQualifiedAccess>]
module WorkspaceUpload =

    /// Desktop multi-phase Upload must start from Idle with no pending ops.
    let canStart (syncInfo: SyncInfo) =
        syncInfo.syncState = Idle && syncInfo.pending.IsEmpty

    /// Web parse/reconcile: empty pending is enough; Polling must not block.
    let canStartWeb (syncInfo: SyncInfo) =
        syncInfo.pending.IsEmpty
        && match syncInfo.syncState with
           | Idle | Polling -> true
           | _ -> false

    /// Detail when Load is parked; distinguish pending ops from sync busy.
    let queueBlockedDetail (syncInfo: SyncInfo) =
        if not syncInfo.pending.IsEmpty then
            "load queued behind pending changes"
        else
            match syncInfo.syncState with
            | Polling -> "load queued until poll completes"
            | Sending _ -> "load queued until submit completes"
            | Uploading -> "load queued until current upload completes"
            | Parsing -> "load queued until parse completes"
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

    let private owningFile (graph: Graph) (nodeId: NodeId) : NodeId option =
        match DocumentPartition.documentRootForNode graph nodeId with
        | None -> None
        | Some rootId ->
            match LoadSubject.ofId graph rootId with
            | Ok LoadSubject.File -> Some rootId
            | _ -> None

    /// Workspace, Directory, or File. A node inside a File uses that File.
    /// Several nodes in one File yield that File once.
    let loadSubjects (graph: Graph) (selected: NodeId list) : NodeId list =
        selected
        |> List.choose (fun nodeId ->
            match LoadSubject.ofId graph nodeId with
            | Ok _ -> Some nodeId
            | Error LoadSubjectRejection.NotFound -> None
            | Error LoadSubjectRejection.NotLoadSubject ->
                owningFile graph nodeId)
        |> List.distinct

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
