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

let deskSaveUrl (fileName: string) =
    sprintf "/%s/save" fileName

/// Persist data-dir snapshot via the server Save endpoint.
let deskSaveOp (model: VM) : VM * Effect list =
    if not (canSave model) then
        model, []
    else
        model, [ ContinueDeskSave ]

let runDeskSaveWith
    (decodeResponse: string -> Result<GitSaveResponse, string>)
    log
    post
    fileName
    =
    post
        (deskSaveUrl fileName)
        (fun text ->
            match decodeResponse text with
            | Ok { ok = true; detail = detail } ->
                log ("[Gambol] save: " + detail)
            | Ok { error = Some err } ->
                log ("[Gambol] save failed: " + err)
            | Ok _ ->
                log "[Gambol] save failed: unknown response"
            | Error err ->
                log ("[Gambol] save decode failed: " + err))
        (fun status text ->
            log (
                "[Gambol] save HTTP "
                + string status
                + ": "
                + LogText.summarizeHttpBody 200 text))
        (fun () -> log "[Gambol] save network error")

let saveOpFor
    (prePick: LoadSavePrePick)
    (model: VM)
    : VM * Effect list =
    model,
    [ SubmitLoadSaveCommand
        (loadSaveCommandRequest
            LoadSaveOperation.Save
            prePick
            model) ]

let saveOp = saveOpFor LoadSavePrePick.Plain
