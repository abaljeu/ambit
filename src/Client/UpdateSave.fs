module Gambol.Client.UpdateSave

open Gambol.Client.JsInterop
open Gambol.Client.UpdateCodec
open Gambol.Client.UpdateHelpers
open Gambol.Shared
open Gambol.Shared.ViewModel


let private canSave (model: VM) =
    match model.serverCapabilities with
    | Some { canGitSave = true } -> true
    | _ -> false

/// Persist data-dir snapshot via the server Save endpoint.
let saveOp (model: VM) : VM * Effect list =
    if not (canSave model) then
        model, []
    else
        postEmpty
            (sprintf "/%s/save" currentFile)
            (fun text ->
                match decodeGitSaveResponse text with
                | Ok { ok = true; detail = detail } ->
                    consoleLog ("[Gambol] save: " + detail)
                | Ok { error = Some err } ->
                    consoleLog ("[Gambol] save failed: " + err)
                | Ok _ ->
                    consoleLog "[Gambol] save failed: unknown response"
                | Error err ->
                    consoleLog ("[Gambol] save decode failed: " + err))
            (fun status text ->
                consoleLog (
                    "[Gambol] save HTTP "
                    + string status
                    + ": "
                    + LogText.truncateForLog 200 text))
            (fun () -> consoleLog "[Gambol] save network error")
            (emptyMutatingPostHeaders ())
        model, []
