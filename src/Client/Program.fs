module Gambol.Client.Program

open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client
open Gambol.Client.App
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
      clipboard = None
      desktopCapabilities = None
      serverCapabilities = None
      desktopFileIndicator = BlankFileIndicator
      syncInfo = SyncInfo.initial
      status = None
      lastSuccessfulKey = ""
      lastSuccessfulOp = "" }

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

fetchText $"/{currentFile}/state" (fun text ->
    match decodeStateResponse text with
    | Ok (graph, revision) ->
        dispatch (SysMsg (StateLoaded (graph, revision)))
        startPolling pollForRemoteChanges recordActivity
    | Error err ->
        app.textContent <- $"Error: {err}"
)
