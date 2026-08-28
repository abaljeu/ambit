module Gambol.Client.Program

open Gambol.Shared
open Gambol.Shared.LogText
open Gambol.Shared.ViewModel
open Gambol.Client
open Gambol.Client.App
open Gambol.Client.SessionState
open Gambol.Client.Update
open Gambol.Client.UpdateCodec
open Gambol.Client.Controller
open Gambol.Client.JsInterop

let initialGraph = Graph.create ()

let initialModel: VM =
    { graph = initialGraph
      revision = Revision.Zero
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

let mutable bootLog: Change list = []
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
                    showBootError err)
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

and private applyBootNovel (novel: Change list) (ready: bool) =
    let model = getModel ()
    let clientState: ClientSyncState =
        { graph = model.graph
          revision = model.revision
          history = model.history }
    match SyncLogic.applyServerTail novel clientState with
    | Error _ -> fallbackState "apply"
    | Ok newState ->
        dispatch (
            SysMsg (
                BootGraphApplied (
                    newState.graph,
                    newState.revision,
                    newState.history,
                    ready)))
        BootCacheStore.appendChanges currentFile novel
        bootLog <- bootLog @ novel
        BootCacheStore.requestIdleTruncate
            currentFile
            bootScope
            (tryReadSavedZoomId ())
            newState.revision.Value
            ready
            newState.graph

and private handleBootPoll (clientRev: int) (poll: ChangeSuccessResponse) =
    let context =
        { ClientPollContext.buildEpochSec = readBuildEpochSec ()
          pageBuildEpochSec = readPageBuildEpochSec () }
    let cached =
        BootCache.cachedHashForBootPoll justFetchedState bootHash
    justFetchedState <- false
    match
        BootCache.decideBootPoll
            clientRev bootLog poll context poll.bootstrapHash cached
    with
    | BootCache.BootPoll.Confirmed ready ->
        dispatch (
            SysMsg (PollDone (None, [], Some ready, Some poll.revision)))
    | BootCache.BootPoll.CodeOutdated ->
        dispatch (
            SysMsg (
                PollDone (
                    Some CodeOutdated,
                    [],
                    Some poll.isReady,
                    Some poll.revision)))
    | BootCache.BootPoll.ApplyNovel (novel, ready) ->
        applyBootNovel novel ready
    | BootCache.BootPoll.FallbackState reason ->
        fallbackState reason

and private runBootPoll (clientRev: int) =
    let url = $"/{currentFile}/poll?_={nowMs ()}&rev={clientRev}"
    fetchTextNoCacheWithFail
        url
        (fun text ->
            match decodeChangeSuccessResponse text with
            | Ok poll -> handleBootPoll clientRev poll
            | Error _ -> ())
        (fun () -> ())

and private finishPaint (response: StateResponse) (localLog: Change list) =
    bootLog <- localLog
    dispatch (SysMsg (StateLoaded response))
    ensurePolling ()
    runBootPoll response.revision.Value
    BootCacheStore.requestIdleTruncate
        currentFile
        bootScope
        (tryReadSavedZoomId ())
        response.revision.Value
        response.isReady
        response.graph

loadFromState ()
