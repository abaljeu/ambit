module Gambol.Client.LoadSaveCommandClient

open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.JsInterop
open Gambol.Client.UpdateCodec
open Gambol.Client.UpdateHelpers
open Gambol.Client.UpdateSave
open Gambol.Client.UpdateWorkspaceLoad

let private dispatchResponse
    (dispatch: Msg -> unit)
    (operation: LoadSaveOperation)
    (response: LoadSaveCommandResponse)
    =
    match response.path, response.command with
    | LoadSavePath.Desk, _ ->
        let updater =
            match operation with
            | LoadSaveOperation.Load -> deskLoadOp
            | LoadSaveOperation.Save -> deskSaveOp
        dispatch (ApplyOp updater)
    | LoadSavePath.Git, Some command ->
        dispatch (SysMsg (CommandDone command.events))
    | LoadSavePath.Git, None ->
        dispatch (
            SysMsg (
                CommandFailed
                    "git Load/Save response omitted command events"))

let run
    (dispatch: Msg -> unit)
    (request: LoadSaveCommandRequest)
    : unit =
    let body = encodeLoadSaveCommandRequest request
    postJson
        ("/" + currentFile + "/load-save-command")
        body
        (fun text ->
            match decodeLoadSaveCommandResponse text with
            | Ok response ->
                dispatchResponse dispatch request.operation response
            | Error err ->
                dispatch (SysMsg (CommandFailed err)))
        (fun status text ->
            let detail =
                "HTTP "
                + string status
                + " "
                + LogText.summarizeHttpBody 400 text
            dispatch (SysMsg (CommandFailed detail)))
        (fun () ->
            dispatch (SysMsg (CommandFailed "fetch failed")))
        (jsonMutatingPostHeaders ())
