module Gambol.Client.UpdateSave

open Gambol.Client.JsInterop
open Gambol.Client.UpdateCodec
open Gambol.Client.UpdateHelpers
open Gambol.Shared
open Gambol.Shared.ViewModel


let private canGitSave (model: VM) =
    match model.serverCapabilities with
    | Some { canGitSave = true } -> true
    | _ -> false

/// Commit persisted data files to git in the server data directory.
let gitSaveOp (model: VM) : VM * Effect list =
    if not (canGitSave model) then
        model, []
    else
        postEmpty
            (sprintf "/%s/save" currentFile)
            (fun text ->
                match decodeGitSaveResponse text with
                | Ok { ok = true; detail = detail } ->
                    consoleLog ("[Gambol] git save: " + detail)
                | Ok { error = Some err } ->
                    consoleLog ("[Gambol] git save failed: " + err)
                | Ok _ ->
                    consoleLog "[Gambol] git save failed: unknown response"
                | Error err ->
                    consoleLog ("[Gambol] git save decode failed: " + err))
            (fun status text ->
                consoleLog (
                    "[Gambol] git save HTTP "
                    + string status
                    + ": "
                    + LogText.truncateForLog 200 text))
            (fun () -> consoleLog "[Gambol] git save network error")
            (emptyMutatingPostHeaders ())
        model, []
