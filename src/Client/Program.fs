module Gambol.Client.Program

open Gambol.Shared
open Gambol.Client
open Gambol.Client.App
open Gambol.Client.Update
open Gambol.Client.Controller
open Gambol.Client.JsInterop

setupStaticDOM applyOp wakePolling

fetchText $"/{Update.currentFile}/state" (fun text ->
    match decodeStateResponse text with
    | Ok (graph, revision) ->
        dispatch (SysMsg (StateLoaded (graph, revision)))
        startPolling ()
    | Error err ->
        app.textContent <- $"Error: {err}"
)
