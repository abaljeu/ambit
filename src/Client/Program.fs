module Gambol.Client.Program

open Gambol.Shared
open Gambol.Shared
open Gambol.Shared.LogText
open Gambol.Shared.ViewModel
open Gambol.Client
open Gambol.Client.App
open Gambol.Client.SessionState
open Gambol.Client.Update
open Gambol.Client.UpdateCodec
open Gambol.Client.UpdateHelpers
open Gambol.Client.Controller
open Gambol.Client.JsInterop

let initialGraph = Graph.create ()

let initialModel: VM =
    { graph = initialGraph
      eventId = EventId.zero
      history = ClientHistory.clear ()
      selectedNodes = None
      mode = Selecting
      siteMap = ViewModel.emptySiteMap
      nextSiteId = Sid 1
      zoomRoot = initialGraph.root
      zoomIngress = []
      clipboard = None
      desktopCapabilities = None
      serverCapabilities = None
      desktopFileIndicator = BlankFileIndicator
      workspaceMappedLabels = Set.empty
      workspaceRoots = Map.empty
      workspaceSyncFacts = Map.empty
      pendingAutoDownloads = []
      syncInfo = SyncInfo.initial
      lastCmdResult = None }

let dispatch, getModel, wakePolling, pollForRemoteChanges, recordActivity =
    createRuntime initialModel

setupStaticDOM dispatch getModel wakePolling

fetchTextNoCacheWithFail
    "/_desktop/capabilities"
    (fun text ->
        match decodeDesktopCapabilities text with
        | Ok capabilities ->
            dispatch (SysMsg (DesktopCapabilitiesDetected (Some capabilities)))
        | Error err ->
            consoleLog ("[Gambol desktop] capability decode failed: " + err)
            dispatch (SysMsg (DesktopCapabilitiesDetected None)))
    (fun () -> dispatch (SysMsg (DesktopCapabilitiesDetected None)))

fetchTextNoCacheWithFail
    (sprintf "/%s/capabilities" currentFile)
    (fun text ->
        match decodeServerCapabilities text with
        | Ok capabilities ->
            dispatch (SysMsg (ServerCapabilitiesDetected (Some capabilities)))
        | Error err ->
            consoleLog ("[Gambol] server capability decode failed: " + err)
            dispatch (SysMsg (ServerCapabilitiesDetected None)))
    (fun () -> dispatch (SysMsg (ServerCapabilitiesDetected None)))

let private showBootError (msg: string) =
    app.textContent <- $"Error: {msg}"

let private stateUrl =
    match tryReadSavedZoomId () with
    | None -> $"/{currentFile}/state"
    | Some (NodeId g) ->
        $"/{currentFile}/state?zoom={g.ToString()}"

let private bootScope = BootCache.scopeKey (tryReadSavedZoomId ())

let mutable bootLog: Ev list = []
let mutable pollingStarted = false
let mutable bootHash = ""
let mutable justFetchedState = false

let private ensurePolling () =
    if not pollingStarted then
        pollingStarted <- true
        startPolling pollForRemoteChanges recordActivity

let rec private loadFromState () =
    justFetchedState <- true
    fetchGet
        stateUrl
        (fun text ->
            try
                if looksCompressed text then
                    showBootError
                        "state response is compressed but not decompressed (Content-Encoding?)"
                else
                    let decodeStart = perfNowMs ()
                    match decodeStateResponse text with
                    | Ok response ->
                        let decodeMs = int (perfNowMs () - decodeStart)
                        let nodeCount = Map.count response.graph.nodes
                        consoleLog (
                            $"[Gambol boot] decodeStateResponse: {decodeMs}ms, "
                            + $"{text.Length} chars, {nodeCount} nodes")
                        finishPaint response []
                        setTimeout
                            (fun () ->
                                BootCacheStore.persistAfterState
                                    currentFile
                                    bootScope
                                    text
                                    response)
                            0
                        |> ignore
                    | Error err ->
                        showBootError ("failed to decode /state: " + err)
            with ex ->
                showBootError ("failed to apply /state: " + ex.Message))
        (fun status body ->
            let snippet = summarizeHttpBody 400 body
            let detail =
                if snippet = "" then $"HTTP {status}"
                else $"HTTP {status}: {snippet}"
            showBootError detail)
        (fun () -> showBootError "network failure loading /state")

and private fallbackState (reason: string) =
    consoleLog ("[Gambol boot] poll fallback " + reason + " → /state")
    BootCacheStore.deleteCache currentFile ignore
    loadFromState ()

and private applyBootNovel (novel: Ev list) (ready: bool) =
    let model = getModel ()
    match
        SyncLogic.applyServerTail novel (clientSyncState model)
    with
    | Error _ -> fallbackState "apply"
    | Ok newState ->
        dispatch (
            SysMsg (
                BootGraphApplied (
                    newState.graph,
                    newState.eventId,
                    newState.history,
                    ready)))
        BootCacheStore.appendEvents currentFile novel
        bootLog <- bootLog @ novel
        BootCacheStore.requestIdleTruncate
            currentFile
            bootScope
            (tryReadSavedZoomId ())
            newState.eventId
            ready
            newState.graph

and private handleBootPoll (clientEventId: EventId) (poll: ChangeSuccessResponse) =
    reseedDeployEpochOnServerSignal poll.buildEpochSec |> ignore
    let cached =
        BootCache.cachedHashForBootPoll justFetchedState bootHash
    justFetchedState <- false
    match
        BootCache.decideBootPoll
            clientEventId bootLog poll poll.bootstrapHash cached
    with
    | BootCache.BootPoll.Confirmed ready ->
        dispatch (
            SysMsg (
                PollDone (
                    None,
                    [],
                    Some ready,
                    Some (poll.eventId))))
    | BootCache.BootPoll.CodeOutdated ->
        dispatch (
            SysMsg (
                PollDone (
                    Some CodeOutdated,
                    [],
                    Some poll.isReady,
                    Some (poll.eventId))))
    | BootCache.BootPoll.ApplyNovel (novel, ready) ->
        applyBootNovel novel ready
    | BootCache.BootPoll.FallbackState reason ->
        fallbackState reason

and private runBootPoll (clientEventId: EventId) =
    let url = $"/{currentFile}/poll?_={nowMs ()}&rev={EventId.value clientEventId}"
    fetchTextNoCacheWithFail
        url
        (fun text ->
            match decodeChangeSuccessResponse text with
            | Ok poll -> handleBootPoll clientEventId poll
            | Error _ -> ())
        (fun () -> ())

and private finishPaint (response: StateResponse) (localLog: Ev list) =
    bootLog <- localLog
    dispatch (SysMsg (StateLoaded response))
    ensurePolling ()
    runBootPoll response.eventId
    BootCacheStore.requestIdleTruncate
        currentFile
        bootScope
        (tryReadSavedZoomId ())
        response.eventId
        response.isReady
        response.graph

loadFromState ()
