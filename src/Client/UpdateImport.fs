module Gambol.Client.UpdateImport

open Gambol.Client.JsInterop
open Gambol.Client.UpdateCodec
open Gambol.Client.UpdateHelpers
open Gambol.Shared

let private fail (model: VM) (message: string) : VM * Effect list =
    consoleLog ("[Gambol desktop] parse failed: " + message)
    { model with
        lastCmdResult = Some(CmdLastResult.Error (Some "Load", message)) },
    []

let private httpError (status: int) (responseText: string) =
    "HTTP "
    + string status
    + ": "
    + LogText.summarizeHttpBody 200 responseText

let private isDesktopFileMissing (status: int) (responseText: string) =
    status = 400 && responseText.Contains("\"error\":\"file not found\"")

let decodeDesktopReadForParse (status: int) (responseText: string) : Result<string option, string> =
    if status >= 200 && status < 300 then
        match decodeDesktopFileContent responseText with
        | Ok content -> Ok(Some content)
        | Error err -> Error err
    elif isDesktopFileMissing status responseText then
        Ok None
    else
        Error(httpError status responseText)

let validateParseTextOpt (textOpt: string option) : Result<string option, string> =
    match textOpt with
    | Some content ->
        if DocumentBinary.looksLikeBinaryContent content then
            Error DocumentBinary.parseError
        else
            Ok textOpt
    | None -> Ok None

let failParseFile (message: string) (model: VM) : VM * Effect list =
    fail
        { model with
            syncInfo = SyncInfo.withSyncState Idle model.syncInfo }
        message

let failParseFileHttp (status: int) (responseText: string) (model: VM) : VM * Effect list =
    failParseFile (httpError status responseText) model

let completeParseFilePost
    (detailPrefix: string)
    (detailPath: string)
    (responseText: string)
    (model: VM)
    : VM * Effect list =
    match decodeParseFileOk responseText with
    | Error err -> failParseFile err model
    | Ok () ->
        // Server may have applied graph-only ops; poll immediately so the outline updates.
        let model' =
            { model with
                lastCmdResult =
                    Some(CmdLastResult.Detail(None, detailPrefix + detailPath))
                syncInfo = SyncInfo.withSyncState Idle model.syncInfo }
        let si, pollEffs = tryStartLoadFetch model'
        { model' with syncInfo = si }, pollEffs
