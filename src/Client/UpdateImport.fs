module Gambol.Client.UpdateImport

open Gambol.Client.JsInterop
open Gambol.Client.UpdateCodec
open Gambol.Client.UpdateHelpers
open Gambol.Shared

let private canImportDesktop (model: VM) =
    match model.desktopCapabilities with
    | Some { file = { canImport = true } } -> true
    | _ -> false

let private fail (model: VM) (message: string) : VM * Effect list =
    consoleLog ("[Gambol desktop] parse failed: " + message)
    { model with lastCmdResult = Some(CmdLastResult.Error (None, message)) }, []

let private okDetail (model: VM) (message: string) : VM * Effect list =
    { model with lastCmdResult = Some(CmdLastResult.Detail (None, message)) }, []

let private httpError (status: int) (responseText: string) =
    "HTTP "
    + string status
    + ": "
    + LogText.truncateForLog 200 responseText

let private isDesktopFileMissing (status: int) (responseText: string) =
    status = 400 && responseText.Contains("\"error\":\"file not found\"")

/// Desktop raw file text for ParseFile upload (not a subgraph).
let private tryReadDesktopContent (path: string) : Result<string option, string> =
    let url =
        "/_desktop/file?path="
        + encodeUriComponent path
        + "&content=1"
    let status, responseText = getJsonSync url

    if status >= 200 && status < 300 then
        match decodeDesktopFileContent responseText with
        | Ok content -> Ok(Some content)
        | Error err -> Error err
    elif isDesktopFileMissing status responseText then
        Ok None
    else
        Error(httpError status responseText)

let private postParseFile
    (model: VM)
    (fileId: NodeId)
    (textOpt: string option)
    (detailPrefix: string)
    (path: string)
    : VM * Effect list =
    let body = encodeParseFileRequest fileId textOpt
    let status, responseText =
        postJsonSync "/ambit/file/parse" body (jsonMutatingPostHeaders ())

    if status < 200 || status >= 300 then
        fail model (httpError status responseText)
    else
        match decodeParseFileOk responseText with
        | Error err -> fail model err
        | Ok () ->
            // Server may have applied graph-only ops; poll immediately so the outline updates.
            let model' =
                { model with
                    lastCmdResult = Some(CmdLastResult.Detail(None, detailPrefix + path)) }
            let si, pollEffs =
                SyncPlanner.tryStartPoll model'.revision model'.syncInfo
            { model' with syncInfo = si }, pollEffs

/// Parse / Upload: client posts fileId (+ optional desktop text); server applies.
let parseFileOp (fileId: NodeId) (model: VM) : VM * Effect list =
    match Map.tryFind fileId model.graph.nodes with
    | Some { kind = Special File; documentState = state } ->
        let pathOpt = NodeDesktopPath.pathForNodeId model.graph fileId
        let detailPrefix =
            match state with
            | Unparsed -> "parsed: "
            | Current -> "reconciled: "

        let textResult =
            match pathOpt with
            | Some path when canImportDesktop model ->
                tryReadDesktopContent path
            | _ -> Ok None

        match textResult with
        | Error err -> fail model err
        | Ok textOpt ->
            let detailPath = pathOpt |> Option.defaultValue ""
            postParseFile model fileId textOpt detailPrefix detailPath
    | _ -> model, []
