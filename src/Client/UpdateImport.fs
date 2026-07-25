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
    { model with
        lastCmdResult = Some(CmdLastResult.Error (Some "Upload", message)) },
    []

let private httpError (status: int) (responseText: string) =
    "HTTP "
    + string status
    + ": "
    + LogText.truncateForLog 200 responseText

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
    fail model message

let failParseFileHttp (status: int) (responseText: string) (model: VM) : VM * Effect list =
    failParseFile (httpError status responseText) model

let completeParseFilePost
    (detailPrefix: string)
    (detailPath: string)
    (responseText: string)
    (model: VM)
    : VM * Effect list =
    match decodeParseFileOk responseText with
    | Error err -> fail model err
    | Ok () ->
        // Server may have applied graph-only ops; poll immediately so the outline updates.
        let model' =
            { model with
                lastCmdResult = Some(CmdLastResult.Detail(None, detailPrefix + detailPath)) }
        let si, pollEffs =
            SyncPlanner.tryStartPoll model'.revision model'.syncInfo
        { model' with syncInfo = si }, pollEffs

/// Parse: validate synchronously, then ContinueParseFile for async desktop read + POST.
let parseFileOp (fileId: NodeId) (model: VM) : VM * Effect list =
    match Map.tryFind fileId model.graph.nodes with
    | Some { kind = Special File; documentState = NoServerFile } ->
        fail model "no file on server"
    | Some { kind = Special File; documentState = state; name = name; text = text } ->
        let pathOpt = NodeDesktopPath.pathForNodeId model.graph fileId
        let nameHint =
            Filename.tryValue name |> Option.defaultValue text

        let binaryPath =
            pathOpt
            |> Option.orElse (Some nameHint)
            |> Option.exists DocumentBinary.isBinaryExtension

        if binaryPath then
            fail model DocumentBinary.parseError
        else
            let detailPrefix =
                match state with
                | Unparsed -> "parsed: "
                | Current -> "reconciled: "
                | NoServerFile -> "parsed: "

            let detailPath = pathOpt |> Option.defaultValue ""
            let desktopReadPath =
                match pathOpt with
                | Some path when canImportDesktop model -> Some path
                | _ -> None

            let model' =
                { model with
                    lastCmdResult =
                        Some(CmdLastResult.Detail(None, "parsing: " + detailPath)) }

            model',
            [ Effect.ContinueParseFile(fileId, desktopReadPath, detailPrefix, detailPath) ]
    | _ -> model, []
