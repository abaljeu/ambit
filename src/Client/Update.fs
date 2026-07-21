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

    | SysMsg (StateLoaded (graph, revision)) ->
        let zoomRoot = firstGraphChild graph
        let siteMap, nextId =
            ViewModel.buildSiteMapFrom graph zoomRoot (Sid 0)
        { graph = graph
          revision = revision
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
          syncInfo = SyncInfo.initial
          lastCmdResult = None }, []

    | AckSyncRisk ->
        { model with syncInfo = { model.syncInfo with syncRiskAcknowledged = true } }, []

    | SysMsg (SubmitResponse (ackedChangeIds, revision)) ->
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
            consoleLog (
                "[Gambol sync] SubmitResponse apply prevRev=" + string model.revision.Value
                + " serverAck=" + string revision.Value + " pendingWas=" + string pendingWas
                + " pendingNext=" + string pending.Length)
            { model with
                revision = revision
                history =
                    { model.history with nextId = max model.history.nextId revision.Value }
                syncInfo = nextSyncInfo }, effects

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
        { model with desktopCapabilities = capabilities }, []

    | SysMsg (ServerCapabilitiesDetected capabilities) ->
        { model with serverCapabilities = capabilities }, []

    | SysMsg (DesktopFileStatusReceived (nodeId, path, status, sourceModifiedUtc)) ->
        ViewModel.applyDesktopFileStatus nodeId path status sourceModifiedUtc model, []

    | SysMsg PollTick ->
        let si, effects = SyncPlanner.tryStartPoll model.revision model.syncInfo
        { model with syncInfo = si }, effects

    | SysMsg (PollDone (stateOpt, changes)) ->
        let si = SyncInfo.withSyncState Idle model.syncInfo
        match stateOpt with
        | None -> { model with syncInfo = si }, []
        | Some CodeOutdated ->
            { model with syncInfo = SyncInfo.withSyncState CodeOutdated si }, []
        | Some DataOutdated when changes.IsEmpty || isAutoSyncBlocked model ->
            { model with syncInfo = SyncInfo.withSyncState DataOutdated si }, []
        | Some DataOutdated ->
            let state: State =
                { graph = model.graph; history = model.history; revision = model.revision }
            match SyncLogic.applyServerTail changes state with
            | Error _ ->
                { model with syncInfo = SyncInfo.withSyncState DataOutdated si }, []
            | Ok newState ->
                consoleLog (
                    "[Gambol sync] PollDone autoSync applied="
                    + string changes.Length + " newRev=" + string newState.revision.Value)
                let synced =
                    { model with
                        graph = newState.graph
                        history = newState.history
                        revision = newState.revision
                        syncInfo = si }
                    |> withSiteMap
                    |> adjustModeAfterServerApply model.graph
                synced, []
        | Some s -> { model with syncInfo = SyncInfo.withSyncState s si }, []

    | SysMsg RetrySubmit ->
        let m, effs = UpdateOps.retryPendingOp false model
        m, effs
