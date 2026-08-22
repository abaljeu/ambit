module Gambol.Client.Update

open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.JsInterop
open Gambol.Client.UpdateCodec
open Gambol.Client.UpdateHelpers
open Gambol.Client.UpdateOps

let encodePendingBatchBody = UpdateCodec.encodePendingBatchBody
let decodeStateResponse = UpdateCodec.decodeStateResponse
let decodeChangeSuccessResponse = UpdateCodec.decodeChangeSuccessResponse
let currentFile = UpdateHelpers.currentFile

// ---------------------------------------------------------------------------
// update : Msg -> VM -> VM * Effect list
// ---------------------------------------------------------------------------
let firstGraphChild graph =
    defaultArg (Node.at graph (Some graph.root) |> Node.firstChild |> Node.current) graph.root

let private clientSyncState (model: VM) : ClientSyncState =
    { graph = model.graph
      revision = model.revision
      history = model.history }

let private rejectPending detail (model: VM) : VM * Effect list =
    let err = Some (CmdLastResult.Error (None, detail))
    if model.syncInfo.pendingChanges.IsEmpty then
        { model with lastCmdResult = err }, []
    else
        { model with
            lastCmdResult = err
            syncInfo =
                model.syncInfo
                |> SyncInfo.withPendingChanges []
                |> SyncInfo.withSyncState ServerRejected },
        [ SavePendingQueue [] ]

let private applySubmitResponse
    (submitted: PendingChange list)
    (confirmed: Change list)
    (revision: Revision)
    (externalChanges: bool)
    (message: string option)
    (model: VM)
    : VM * Effect list =
    match model.syncInfo.syncState with
    | ServerRejected | CodeOutdated | DataOutdated ->
        consoleLog (
            "[Gambol sync] SubmitResponse IGNORED blocked-risk serverAck="
            + string revision.Value + " modelRev=" + string model.revision.Value)
        model, []
    | _ ->
        let useExternal =
            externalChanges
            || not (SyncLogic.isConfirmationEcho submitted confirmed)
        let result =
            if useExternal then
                SyncLogic.reconcileExternalAck
                    submitted revision (clientSyncState model) model.syncInfo
            else
                SyncLogic.reconcileAck
                    submitted confirmed revision (clientSyncState model) model.syncInfo
        match result with
        | AckReconcile.Ignored -> model, []
        | AckReconcile.Rejected detail -> rejectPending detail model
        | AckReconcile.Applied (nextState, nextSync, submitEffects, suffixOps) ->
            consoleLog (
                "[Gambol sync] SubmitResponse apply prevRev="
                + string model.revision.Value
                + " serverAck=" + string revision.Value
                + " pendingNext=" + string nextSync.pendingChanges.Length
                + " external=" + string useExternal)
            let updated =
                { model with
                    graph = nextState.graph
                    revision = nextState.revision
                    history = nextState.history
                    syncInfo = nextSync
                    lastCmdResult =
                        match message with
                        | Some msg -> Some(CmdLastResult.Detail(None, msg))
                        | None -> model.lastCmdResult }
            let updated', autoEffects =
                UpdateWorkspaceDownload.accumulateAutoDownloadFromOps suffixOps updated
            let nextSync', pollEffects =
                if
                    useExternal
                    && nextSync.pendingChanges.IsEmpty
                    && nextSync.catchUp.IsSome
                then
                    SyncPlanner.tryStartPoll model.revision nextSync
                else
                    nextSync, []
            { updated' with syncInfo = nextSync' },
            (SavePendingQueue nextSync'.pendingChanges)
            :: submitEffects
            @ pollEffects
            @ autoEffects

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
          history = ClientHistory.clear ()
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

    | SysMsg (SubmitResponse (submitted, confirmed, revision, externalChanges, message)) ->
        applySubmitResponse submitted confirmed revision externalChanges message model

    | SysMsg (SubmitRejected detail) ->
        consoleLog (
            "[Gambol sync] SubmitRejected modelRev=" + string model.revision.Value
            + " pending=" + string model.syncInfo.pendingChanges.Length
            + " detail=" + detail)
        rejectPending detail model

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

    | SysMsg AutoDownloadTick ->
        UpdateWorkspaceDownload.runAutoDownloadTick model

    | SysMsg (PollDone (stateOpt, changes, readyOpt, responseRevision)) ->
        let readyModel =
            match readyOpt with
            | Some ready ->
                { model with
                    syncInfo =
                        model.syncInfo
                        |> SyncInfo.withServerReady ready }
            | None -> model
        // While Uploading, Parsing, or Loading: keep the busy indicator. Do not apply
        // Poll tails during Loading — a stale poll would advance Revision and cause
        // applyLoadResponse to reject package-only Load payloads.
        match readyModel.syncInfo.syncState with
        | Loading ->
            readyModel, []
        | Uploading | Parsing as busy ->
            match stateOpt with
            | Some DataOutdated
                when not changes.IsEmpty
                    && not (isAutoSyncBlocked readyModel) ->
                let clientState: ClientSyncState =
                    { graph = readyModel.graph
                      revision = readyModel.revision
                      history = readyModel.history }
                match SyncLogic.applyServerTail changes clientState with
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
                        syncInfo = SyncInfo.withSyncState busy kept.syncInfo }
                    |> UpdateWorkspaceDownload.accumulateAutoDownloadFromChanges changes
            | _ -> readyModel, []
        | _ ->
            let si = SyncInfo.withSyncState Idle readyModel.syncInfo
            match readyModel.syncInfo.catchUp, changes with
            | Some baseline, _ :: _ ->
                let serverRev =
                    responseRevision
                    |> Option.defaultValue baseline.revision
                let clientState: ClientSyncState =
                    { graph = readyModel.graph
                      history = readyModel.history
                      revision = readyModel.revision }
                match
                    SyncLogic.consumeCatchUpPoll
                        baseline
                        changes
                        serverRev
                        clientState
                with
                | Error _ ->
                    { readyModel with
                        syncInfo = SyncInfo.withSyncState DataOutdated si }, []
                | Ok newState ->
                    consoleLog (
                        "[Gambol sync] PollDone catchUp applied="
                        + string changes.Length
                        + " newRev="
                        + string newState.revision.Value)
                    let synced =
                        { readyModel with
                            graph = newState.graph
                            history = newState.history
                            revision = newState.revision
                            syncInfo = si |> SyncInfo.clearCatchUp }
                        |> withSiteMap
                        |> adjustModeAfterServerApply readyModel.graph
                    UpdateWorkspaceDownload.accumulateAutoDownloadFromChanges changes synced
            | Some _, [] ->
                { readyModel with syncInfo = si |> SyncInfo.clearCatchUp }, []
            | _ ->
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
                    let clientState: ClientSyncState =
                        { graph = readyModel.graph
                          history = readyModel.history
                          revision = readyModel.revision }
                    match SyncLogic.applyServerTail changes clientState with
                    | Error _ ->
                        { readyModel with
                            syncInfo = SyncInfo.withSyncState DataOutdated si }, []
                    | Ok newState ->
                        consoleLog (
                            "[Gambol sync] PollDone autoSync applied="
                            + string changes.Length
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
                        UpdateWorkspaceDownload.accumulateAutoDownloadFromChanges changes synced
                | Some s ->
                    { readyModel with syncInfo = SyncInfo.withSyncState s si }, []

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
            not (List.isEmpty syncResponse.changes)
            || not (List.isEmpty syncResponse.packages)
        let hasPendingLocal =
            not readyModel.syncInfo.pendingChanges.IsEmpty
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
            let clientState: ClientSyncState =
                { graph = readyModel.graph
                  history = readyModel.history
                  revision = readyModel.revision }
            match
                SyncLogic.applyLoadResponse
                    responseRevision
                    hasPendingLocal
                    syncResponse
                    clientState
            with
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
