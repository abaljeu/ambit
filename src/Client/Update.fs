module Gambol.Client.Update

open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.JsInterop
open Gambol.Client.UpdateCodec
open Gambol.Client.UpdateHelpers
open Gambol.Client.UpdateOps

type ChangeAck = UpdateCodec.ChangeAck

let encodePendingBatchBody = UpdateCodec.encodePendingBatchBody
let decodeStateResponse = UpdateCodec.decodeStateResponse
let decodeChangeAckResponse = UpdateCodec.decodeChangeAckResponse
let currentFile = UpdateHelpers.currentFile

// ---------------------------------------------------------------------------
// update : Msg -> VM -> VM * Effect list
// ---------------------------------------------------------------------------
let firstGraphChild graph =
    defaultArg (Node.at graph (Some graph.root) |> Node.firstChild |> Node.current) graph.root

let update (msg: Msg) (model: VM) : VM * Effect list =
    match msg with
    | ApplyOp op -> op model

    | NodeSearchQuery query ->
        match model.mode with
        | SearchDialog s when s.query <> query ->
            Gambol.Client.SearchDialog.resetSearchResults ()
            { model with
                mode = SearchDialog { s with query = query; selectedIndex = 0 } }, []
        | _ -> model, []

    | FileSearchQuery query ->
        match model.mode with
        | FileSearchDialog s when s.query <> query ->
            Gambol.Client.FileSearchDialog.resetFileSearchResults ()
            { model with
                mode = FileSearchDialog { s with query = query; selectedIndex = 0 } }, []
        | _ -> model, []

    | SysMsg (StateLoaded response) ->
        let graph = response.graph
        let zoomRoot = firstGraphChild graph
        let siteMap, nextId =
            ViewModel.buildSiteMapFrom graph zoomRoot (Sid 0)
        { graph = graph
          revision = response.revision
          history = History.empty
          selectedNodes = None
          mode = Selecting
          siteMap = siteMap
          nextSiteId = nextId
          zoomRoot = zoomRoot
          zoomIngress = ViewModel.ownerPathIngress graph zoomRoot
          clipboard = None
          desktopCapabilities = model.desktopCapabilities
          serverCapabilities = model.serverCapabilities
          desktopFileIndicator = BlankFileIndicator
          workspaceMappedLabels = model.workspaceMappedLabels
          workspaceRoots = model.workspaceRoots
          workspaceSyncFacts = model.workspaceSyncFacts
          syncInfo =
            SyncInfo.initial
            |> SyncInfo.withServerReady response.isReady
          lastCmdResult = None }, []

    | AckSyncRisk ->
        { model with syncInfo = { model.syncInfo with syncRiskAcknowledged = true } }, []

    | SysMsg (SubmitResponse (ackedChangeIds, revision, stampOps, message)) ->
        match model.syncInfo.syncState with
        | ServerRejected | CodeOutdated | DataOutdated ->
            consoleLog (
                "[Gambol sync] SubmitResponse IGNORED blocked-risk serverAck="
                + string revision.Value + " modelRev=" + string model.revision.Value)
            model, []
        | _ ->
            let pendingWas = model.syncInfo.pendingChanges.Length
            let nextSyncInfo, pending, submitEffects =
                SyncPlanner.ackBatch ackedChangeIds revision model.syncInfo
            let effects = (SavePendingQueue pending) :: submitEffects
            let graph' = PersistStamp.applyToGraph stampOps model.graph
            consoleLog (
                "[Gambol sync] SubmitResponse apply prevRev=" + string model.revision.Value
                + " serverAck=" + string revision.Value + " pendingWas=" + string pendingWas
                + " pendingNext=" + string pending.Length
                + " stampOps=" + string stampOps.Length)
            { model with
                graph = graph'
                revision = revision
                history =
                    { model.history with nextId = max model.history.nextId revision.Value }
                syncInfo = nextSyncInfo
                lastCmdResult =
                    match message with
                    | Some msg -> Some(CmdLastResult.Detail(None, msg))
                    | None -> model.lastCmdResult }, effects

    | SysMsg (SubmitRejected detail) ->
        consoleLog (
            "[Gambol sync] SubmitRejected modelRev=" + string model.revision.Value
            + " pending=" + string model.syncInfo.pendingChanges.Length
            + " detail=" + detail)
        let err = Some (CmdLastResult.Error (None, detail))
        if model.syncInfo.pendingChanges.IsEmpty then
            { model with lastCmdResult = err }, []
        else
            // Rejected payload cannot be replayed safely; drop persisted queue so reload starts clean.
            { model with
                lastCmdResult = err
                syncInfo =
                    model.syncInfo
                    |> SyncInfo.withPendingChanges []
                    |> SyncInfo.withSyncState ServerRejected }, [ SavePendingQueue [] ]

    | SysMsg (SubmitNetworkError (baseRev, changes, kind)) ->
        consoleLog (
            "[Gambol sync] SubmitNetworkError modelRev=" + string model.revision.Value
            + " pending=" + string model.syncInfo.pendingChanges.Length
            + " kind=" + string kind)
        if model.syncInfo.pendingChanges.IsEmpty then model, []
        else
            let n =
                match model.syncInfo.syncState with
                | Sending n -> n
                | WaitingToRetry (n, _, _) -> n
                | _ -> 1
            let delayMs = SyncRetry.retryDelayMs n kind
            { model with
                syncInfo =
                    model.syncInfo
                    |> SyncInfo.withSyncState (WaitingToRetry (n, baseRev, changes)) },
            [ ScheduleRetry delayMs ]

    | SysMsg (SetPollingActive active) ->
        { model with syncInfo = { model.syncInfo with isPollingActive = active } }, []

    | SysMsg (DesktopCapabilitiesDetected capabilities) ->
        let model' = { model with desktopCapabilities = capabilities }
        if DesktopCapabilities.canWorkspaceSync capabilities then
            model', [ RequestWorkspacePathSyncSnapshot ]
        else
            { model' with
                workspaceMappedLabels = Set.empty
                workspaceRoots = Map.empty
                workspaceSyncFacts = Map.empty },
            []

    | SysMsg (ServerCapabilitiesDetected capabilities) ->
        { model with serverCapabilities = capabilities }, []

    | SysMsg (DesktopFileStatusReceived (nodeId, path, status, sourceModifiedUtc)) ->
        applyDesktopFileStatus nodeId path status sourceModifiedUtc model, []

    | SysMsg (WorkspacePathSyncSnapshotReceived (mappedLabels, factsByLabel, rootsByLabel)) ->
        let model' = ViewModel.applyWorkspacePathSyncSnapshot mappedLabels factsByLabel model
        { model' with workspaceRoots = rootsByLabel }, []

    | SysMsg PollTick ->
        let si, effects = SyncPlanner.tryStartPoll model.revision model.syncInfo
        { model with syncInfo = si }, effects

    | SysMsg (PollDone (stateOpt, changes, readyOpt)) ->
        let readyModel =
            match readyOpt with
            | Some ready ->
                { model with
                    syncInfo =
                        model.syncInfo
                        |> SyncInfo.withServerReady ready }
            | None -> model
        // While Uploading: apply graph deltas but keep the Uploading indicator.
        match readyModel.syncInfo.syncState with
        | Uploading ->
            match stateOpt with
            | Some DataOutdated
                when not changes.IsEmpty
                    && not (isAutoSyncBlocked readyModel) ->
                let state: State =
                    { graph = readyModel.graph
                      history = readyModel.history
                      revision = readyModel.revision }
                match SyncLogic.applyServerTail changes state with
                | Error _ -> readyModel, []
                | Ok newState ->
                    let kept =
                        { readyModel with
                            graph = newState.graph
                            history = newState.history
                            revision = newState.revision }
                        |> withSiteMap
                        |> adjustModeAfterServerApply readyModel.graph
                    { kept with
                        syncInfo = SyncInfo.withSyncState Uploading kept.syncInfo },
                    []
            | _ -> readyModel, []
        | _ ->
            let si = SyncInfo.withSyncState Idle readyModel.syncInfo
            match stateOpt with
            | None -> { readyModel with syncInfo = si }, []
            | Some CodeOutdated ->
                { readyModel with
                    syncInfo = SyncInfo.withSyncState CodeOutdated si }, []
            | Some DataOutdated
                when changes.IsEmpty || isAutoSyncBlocked readyModel ->
                { readyModel with
                    syncInfo = SyncInfo.withSyncState DataOutdated si }, []
            | Some DataOutdated ->
                let state: State =
                    { graph = readyModel.graph
                      history = readyModel.history
                      revision = readyModel.revision }
                match SyncLogic.applyServerTail changes state with
                | Error _ ->
                    { readyModel with
                        syncInfo = SyncInfo.withSyncState DataOutdated si }, []
                | Ok newState ->
                    consoleLog (
                        "[Gambol sync] PollDone autoSync applied="
                        + string changes.Length + " newRev=" + string newState.revision.Value)
                    let synced =
                        { readyModel with
                            graph = newState.graph
                            history = newState.history
                            revision = newState.revision
                            syncInfo = si }
                        |> withSiteMap
                        |> adjustModeAfterServerApply readyModel.graph
                    synced, []
            | Some s ->
                { readyModel with syncInfo = SyncInfo.withSyncState s si }, []

    | SysMsg (LoadDone (stateOpt, syncResponse, readyOpt)) ->
        let readyModel =
            match readyOpt with
            | Some ready ->
                { model with
                    syncInfo =
                        model.syncInfo
                        |> SyncInfo.withServerReady ready }
            | None -> model
        let si = SyncInfo.withSyncState Idle readyModel.syncInfo
        let hasPayload =
            not (List.isEmpty syncResponse.changes)
            || not (List.isEmpty syncResponse.packages)
        match stateOpt with
        | Some CodeOutdated ->
            { readyModel with
                syncInfo = SyncInfo.withSyncState CodeOutdated si }, []
        | Some DataOutdated when not hasPayload || isAutoSyncBlocked readyModel ->
            { readyModel with
                syncInfo = SyncInfo.withSyncState DataOutdated si }, []
        | None when not hasPayload ->
            { readyModel with syncInfo = si }, []
        | None when isAutoSyncBlocked readyModel ->
            { readyModel with
                syncInfo = SyncInfo.withSyncState DataOutdated si }, []
        | None
        | Some DataOutdated ->
            let state: State =
                { graph = readyModel.graph
                  history = readyModel.history
                  revision = readyModel.revision }
            match SyncLogic.applySyncResponse syncResponse state with
            | Error _ ->
                { readyModel with
                    syncInfo = SyncInfo.withSyncState DataOutdated si }, []
            | Ok newState ->
                consoleLog (
                    "[Gambol sync] LoadDone applied changes="
                    + string syncResponse.changes.Length
                    + " packages="
                    + string syncResponse.packages.Length
                    + " newRev="
                    + string newState.revision.Value)
                let synced =
                    { readyModel with
                        graph = newState.graph
                        history = newState.history
                        revision = newState.revision
                        syncInfo = si }
                    |> withSiteMap
                    |> adjustModeAfterServerApply readyModel.graph
                synced, []
        | Some s ->
            { readyModel with syncInfo = SyncInfo.withSyncState s si }, []

    | SysMsg RetrySubmit ->
        let m, effs = UpdateOps.retryPendingOp false model
        m, effs
