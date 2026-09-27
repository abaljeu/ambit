module Gambol.Client.Update

open Gambol.Shared
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.JsInterop
open Gambol.Client.UpdateCodec
open Gambol.Client.UpdateHelpers
open Gambol.Client.UpdateOps
open Gambol.Client.UpdateActorLive

let encodePendingBatchBody = UpdateCodec.encodePendingBatchBody
let decodeStateResponse = UpdateCodec.decodeStateResponse
let decodeChangeSuccessResponse = UpdateCodec.decodeChangeSuccessResponse
let currentFile = UpdateHelpers.currentFile

// ---------------------------------------------------------------------------
// update : Msg -> VM -> VM * Effect list
// ---------------------------------------------------------------------------
let firstGraphChild = ViewModel.firstGraphChild

let private rejectPending detail (model: VM) : VM * Effect list =
    let err = Some (CmdLastResult.Error (None, detail))
    if model.syncInfo.pending.IsEmpty then
        { model with lastCmdResult = err }, []
    else
        { model with
            lastCmdResult = err
            syncInfo =
                model.syncInfo
                |> SyncInfo.withPending []
                |> SyncInfo.withSyncState ServerRejected },
        [ SavePendingQueue [] ]

type private AppliedSubmit =
    { state: ClientSyncState
      syncInfo: SyncInfo
      effects: Effect list
      suffixOps: Op list
      needsCatchUp: bool }

type private SubmitReconciliation =
    { outcome: AckReconcile
      needsCatchUp: bool }

let private continueAutoWant
    (response: SyncResponse)
    (model: VM, effects: Effect list)
    : VM * Effect list =
    let installedAnswer =
        not (List.isEmpty response.nodes)
        || not (Map.isEmpty response.childMap)
    if not installedAnswer || List.isEmpty (currentWant model) then
        model, effects
    else
        let syncInfo, pollEffects =
            SyncPlanner.tryStartPoll model.eventId model.syncInfo
        { model with syncInfo = syncInfo }, effects @ pollEffects

let private reconcileSubmit
    (submitted: Ev list)
    (response: ChangeSuccessResponse)
    (model: VM)
    : SubmitReconciliation =
    let needsCatchUp =
        response.externalChanges
        || not (SyncLogic.isConfirmationEcho submitted response.events)
    let state = clientSyncState model
    let outcome =
        if needsCatchUp then
            SyncLogic.reconcileExternalAck
                submitted response.eventId state model.syncInfo
        else
            SyncLogic.reconcileAck
                submitted response.events response.eventId state model.syncInfo
    { outcome = outcome
      needsCatchUp = needsCatchUp }

let private applyIgnoredSubmitAnswer
    (response: ChangeSuccessResponse)
    (model: VM)
    : VM * Effect list =
    match
        SyncAnswer.applyChangeSuccess response (clientSyncState model)
    with
    | Error _ ->
        { model with
            syncInfo = SyncInfo.withSyncState DataOutdated model.syncInfo },
        []
    | Ok answered ->
        (withAppliedSync answered model
         |> withSiteMap
         |> adjustModeAfterServerApply model.graph,
         [])
        |> continueAutoWant (SyncLogic.changeSuccessToSync response)

let private finishAppliedSubmit
    (response: ChangeSuccessResponse)
    (model: VM) (applied: AppliedSubmit) : VM * Effect list =
    let applied =
        match SyncAnswer.applyChangeSuccess response applied.state with
        | Ok state -> { applied with state = state }
        | Error _ ->
            { applied with
                syncInfo =
                    SyncInfo.withSyncState DataOutdated applied.syncInfo
                effects = [] }
    let updated =
        { model with
            graph = applied.state.graph
            eventId = applied.state.eventId
            history = applied.state.history
            syncInfo = applied.syncInfo
            lastCmdResult =
                match response.message with
                | Some msg -> Some(CmdLastResult.Detail(None, msg))
                | None -> model.lastCmdResult }
        |> withSiteMap
    let updated', autoEffects =
        UpdateWorkspaceDownload.accumulateAutoDownloadFromOps
            applied.suffixOps updated
    let nextSync, pollEffects =
        if
            applied.needsCatchUp
            && applied.syncInfo.pending.IsEmpty
            && applied.syncInfo.catchUp.IsSome
        then
            SyncPlanner.tryStartPoll model.eventId applied.syncInfo
        else
            applied.syncInfo, []
    ({ updated' with syncInfo = nextSync },
     SavePendingQueue nextSync.pending
     :: applied.effects @ pollEffects @ autoEffects)
    |> continueAutoWant (SyncLogic.changeSuccessToSync response)

let private applySubmitResponse
    (submitted: Ev list)
    (response: ChangeSuccessResponse)
    (model: VM)
    : VM * Effect list =
    match model.syncInfo.syncState with
    | ServerRejected | CodeOutdated | DataOutdated ->
        consoleLog (
            "[Gambol sync] SubmitResponse IGNORED blocked-risk serverAck="
            + string response.eventId.Value
            + " modelRev=" + string model.eventId.Value)
        model, []
    | _ ->
        let reconciliation = reconcileSubmit submitted response model
        match reconciliation.outcome with
        | AckReconcile.Ignored -> applyIgnoredSubmitAnswer response model
        | AckReconcile.Rejected detail -> rejectPending detail model
        | AckReconcile.Applied (nextState, nextSync, submitEffects, suffixOps) ->
            consoleLog (
                "[Gambol sync] SubmitResponse apply prevRev="
                + string model.eventId.Value
                + " serverAck=" + string response.eventId.Value
                + " pendingNext=" + string nextSync.pending.Length
                + " external=" + string reconciliation.needsCatchUp)
            finishAppliedSubmit
                response
                model
                { state = nextState
                  syncInfo = nextSync
                  effects = submitEffects
                  suffixOps = suffixOps
                  needsCatchUp = reconciliation.needsCatchUp }

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
          eventId = response.eventId
          history = ClientHistory.clear ()
          actorLiveFocusIds = response.seedLiveFocusIds
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
          pendingAutoDownloads = []
          syncInfo =
            SyncInfo.initial
            |> SyncInfo.withServerReady response.isReady
          lastCmdResult = None }, []

    | AckSyncRisk ->
        { model with syncInfo = { model.syncInfo with syncRiskAcknowledged = true } }, []

    | SysMsg (SubmitResponse (submitted, response)) ->
        applySubmitResponse submitted response model

    | SysMsg (SubmitRejected detail) ->
        consoleLog (
            "[Gambol sync] SubmitRejected modelRev=" + string model.eventId.Value
            + " pending=" + string model.syncInfo.pending.Length
            + " detail=" + detail)
        rejectPending detail model

    | SysMsg (SubmitNetworkError (baseEventId, events, kind)) ->
        consoleLog (
            "[Gambol sync] SubmitNetworkError modelRev=" + string model.eventId.Value
            + " pending=" + string model.syncInfo.pending.Length
            + " kind=" + string kind)
        if model.syncInfo.pending.IsEmpty then model, []
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
                    |> SyncInfo.withSyncState (WaitingToRetry (n, baseEventId, events)) },
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
        let si, effects =
            SyncPlanner.tryStartPoll
                (model.eventId)
                model.syncInfo
        { model with syncInfo = si }, effects

    | SysMsg AutoDownloadTick ->
        UpdateWorkspaceDownload.runAutoDownloadTick model

    | SysMsg (PollDone (stateOpt, syncResponse, readyOpt, responseEventId)) ->
        let events = syncResponse.events
        let readyModel =
            match readyOpt with
            | Some ready ->
                { model with
                    syncInfo =
                        model.syncInfo
                        |> SyncInfo.withServerReady ready }
            | None -> model
        // While Uploading, Parsing, or Loading: keep the busy indicator. Do not apply
        // Poll tails during Loading — a stale poll would advance revision and cause
        // applyLoadResponse to reject answer-only Load payloads.
        let autoDownload model' =
            UpdateWorkspaceDownload.accumulateAutoDownloadFromOps
                (events
                 |> List.collect (fun e ->
                    Ev.ops e |> Option.defaultValue []))
                model'
            |> continueAutoWant syncResponse
        match readyModel.syncInfo.syncState with
        | Loading ->
            readyModel, []
        | Uploading | Parsing as busy ->
            match stateOpt with
            | Some DataOutdated
                when not events.IsEmpty
                    && not (isAutoSyncBlocked readyModel) ->
                match
                    SyncLogic.applySyncResponse
                        syncResponse
                        (clientSyncState readyModel)
                with
                | Error _ -> readyModel, []
                | Ok newState ->
                    let kept =
                        withAppliedSync newState readyModel
                        |> withActorCmdResult events
                        |> withSiteMap
                        |> adjustModeAfterServerApply readyModel.graph
                    { kept with
                        syncInfo = SyncInfo.withSyncState busy kept.syncInfo }
                    |> autoDownload
            | _ -> readyModel, []
        | _ ->
            let si = SyncInfo.withSyncState Idle readyModel.syncInfo
            match readyModel.syncInfo.catchUp, events with
            | Some baseline, _ :: _ ->
                let serverRev =
                    responseEventId
                    |> Option.defaultValue baseline.eventId
                match
                    SyncLogic.consumeCatchUpPoll
                        baseline
                        events
                        serverRev
                        (clientSyncState readyModel)
                with
                | Error _ ->
                    { readyModel with
                        syncInfo = SyncInfo.withSyncState DataOutdated si }, []
                | Ok newState ->
                    match
                        SyncAnswer.apply syncResponse newState
                    with
                    | Error _ ->
                        { readyModel with
                            syncInfo = SyncInfo.withSyncState DataOutdated si }, []
                    | Ok answeredState ->
                        consoleLog (
                            "[Gambol sync] PollDone catchUp applied="
                            + string events.Length
                            + " newRev="
                            + string answeredState.eventId.Value)
                        let synced =
                            withAppliedResult
                                events
                                answeredState
                                (si |> SyncInfo.clearCatchUp)
                                readyModel
                            |> withSiteMap
                            |> adjustModeAfterServerApply readyModel.graph
                        autoDownload synced
            | Some _, [] ->
                match
                    SyncLogic.applySyncResponse
                        syncResponse
                        (clientSyncState readyModel)
                with
                | Error _ ->
                    { readyModel with
                        syncInfo = SyncInfo.withSyncState DataOutdated si }, []
                | Ok answeredState ->
                    withAppliedResult
                        []
                        answeredState
                        (si |> SyncInfo.clearCatchUp)
                        readyModel
                    |> withSiteMap
                    |> fun next -> continueAutoWant syncResponse (next, [])
            | _ ->
                match stateOpt with
                | None ->
                    match
                        SyncLogic.applySyncResponse
                            syncResponse
                            (clientSyncState readyModel)
                    with
                    | Error _ ->
                        { readyModel with
                            syncInfo = SyncInfo.withSyncState DataOutdated si }, []
                    | Ok answeredState ->
                        withAppliedResult
                            events answeredState si readyModel
                        |> withSiteMap
                        |> adjustModeAfterServerApply readyModel.graph
                        |> autoDownload
                | Some CodeOutdated ->
                    { readyModel with
                        syncInfo = SyncInfo.withSyncState CodeOutdated si }, []
                | Some DataOutdated
                    when events.IsEmpty || isAutoSyncBlocked readyModel ->
                    { readyModel with
                        syncInfo = SyncInfo.withSyncState DataOutdated si }, []
                | Some DataOutdated ->
                    match
                        SyncLogic.applySyncResponse
                            syncResponse
                            (clientSyncState readyModel)
                    with
                    | Error _ ->
                        { readyModel with
                            syncInfo = SyncInfo.withSyncState DataOutdated si }, []
                    | Ok newState ->
                        consoleLog (
                            "[Gambol sync] PollDone autoSync applied="
                            + string events.Length
                            + " newRev="
                            + string newState.eventId.Value)
                        let synced =
                            withAppliedResult events newState si readyModel
                            |> withSiteMap
                            |> adjustModeAfterServerApply readyModel.graph
                        autoDownload synced
                | Some s ->
                    { readyModel with syncInfo = SyncInfo.withSyncState s si }, []

    | SysMsg (BootGraphApplied (applied, ready)) ->
        { model with
            graph = applied.projectedGraph
            eventId = applied.projectedEventId
            history = applied.projectedHistory
            actorLiveFocusIds = applied.projectedLiveFocusIds
            syncInfo =
                model.syncInfo
                |> SyncInfo.withServerReady ready
                |> SyncInfo.withSyncState Idle }
        |> withSiteMap
        |> adjustModeAfterServerApply model.graph, []

    | SysMsg (LoadDone (stateOpt, syncResponse, responseRevision, readyOpt)) ->
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
            not (List.isEmpty syncResponse.events)
            || not (List.isEmpty syncResponse.nodes)
            || not (Map.isEmpty syncResponse.childMap)
        let hasPendingLocal =
            not readyModel.syncInfo.pending.IsEmpty
            || match readyModel.syncInfo.syncState with
               | Sending _ | WaitingToRetry _ -> true
               | _ -> false
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
            match
                SyncLogic.applyLoadResponse
                    responseRevision
                    hasPendingLocal
                    syncResponse
                    (clientSyncState readyModel)
            with
            | Error _ ->
                { readyModel with
                    syncInfo = SyncInfo.withSyncState DataOutdated si }, []
            | Ok newState ->
                consoleLog (
                    "[Gambol sync] LoadDone applied events="
                    + string syncResponse.events.Length
                    + " nodes="
                    + string syncResponse.nodes.Length
                    + " newRev="
                    + string newState.eventId.Value)
                let synced =
                    withAppliedResult
                        syncResponse.events newState si readyModel
                    |> withSiteMap
                    |> adjustModeAfterServerApply readyModel.graph
                synced, []
        | Some s ->
            { readyModel with syncInfo = SyncInfo.withSyncState s si }, []

    | SysMsg (CommandDone events) ->
        applyCommandEvents events model

    | SysMsg (CommandFailed detail) ->
        { model with
            lastCmdResult = Some (CmdLastResult.Error (Some "Run", detail)) },
        []

    | SysMsg RetrySubmit ->
        let m, effs = UpdateOps.retryPendingOp false model
        m, effs
