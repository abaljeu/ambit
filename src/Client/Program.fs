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
      history = History.empty
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

fetchGet
    stateUrl
    (fun text ->
        match decodeStateResponse text with
        | Ok response ->
            dispatch (SysMsg (StateLoaded response))
            startPolling pollForRemoteChanges recordActivity
        | Error err ->
            showBootError err)
    (fun status body ->
        let snippet = summarizeHttpBody 400 body
        let detail =
            if snippet = "" then $"HTTP {status}"
            else $"HTTP {status}: {snippet}"
        showBootError detail)
    (fun () -> showBootError "network failure loading /state")
