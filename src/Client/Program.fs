module Gambol.Client.Program

open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client
open Gambol.Client.App
open Gambol.Client.Update
open Gambol.Client.UpdateCodec
open Gambol.Client.Controller
open Gambol.Client.JsInterop

let initialModel: VM =
    { graph = { root = NodeId(System.Guid.Empty); nodes = Map.empty }
      revision = Revision.Zero
      history = History.empty
      selectedNodes = None
      mode = Selecting
      siteMap = ViewModel.emptySiteMap
      nextSiteId = Sid 1
      zoomRoot = None
      clipboard = None
      syncInfo = SyncInfo.initial }

let dispatch, getModel, wakePolling, pollForRemoteChanges, recordActivity =
    createRuntime initialModel

setupStaticDOM dispatch getModel wakePolling

fetchText $"/{currentFile}/state" (fun text ->
    match decodeStateResponse text with
    | Ok (graph, revision) ->
        dispatch (SysMsg (StateLoaded (graph, revision)))
        startPolling pollForRemoteChanges recordActivity
    | Error err ->
        app.textContent <- $"Error: {err}"
)
