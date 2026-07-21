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

let private commitParsedFile
    (model: VM)
    (fileId: NodeId)
    (path: string)
    (package: DesktopImportPackage)
    : VM * Effect list =
    if package.isDirectory || package.sourcePath <> path then
        fail model "desktop response did not match the selected File"
    else
        let existing = model.graph.nodes.[fileId].children
        let change =
            ImportText.buildImportChange
                model.graph
                fileId
                existing
                package
                model.revision.Value
                (System.Guid.NewGuid())
        match applyAndPost change model with
        | Ok (parsed, effects) ->
            let result = Some(CmdLastResult.Detail(None, "parsed: " + path))
            { withSiteMap parsed with lastCmdResult = result }, effects
        | Error _ -> fail model "parse change was rejected"

let private handleImportHttpResponse
    (model: VM) (fileId: NodeId) (path: string) (responseText: string)
    : VM * Effect list =
    match decodeDesktopImportPackage responseText with
    | Error err -> fail model ("could not decode parsed file: " + err)
    | Ok package -> commitParsedFile model fileId path package

let private isDesktopFileMissing (status: int) (responseText: string) =
    status = 400 && responseText.Contains("\"error\":\"file not found\"")

let private requestImportAtPath
    (model: VM) (fileId: NodeId) (path: string)
    : VM * Effect list =
    let desktopUrl = "/_desktop/file?path=" + encodeUriComponent path
    let status, responseText = getJsonSync desktopUrl

    if status >= 200 && status < 300 then
        handleImportHttpResponse model fileId path responseText
    elif isDesktopFileMissing status responseText then
        // Directory reconcile can add Unparsed stubs from server DataDir
        // before the desktop clone has the file; fall back to server read.
        let serverUrl = "/ambit/file?path=" + encodeUriComponent path
        let serverStatus, serverText = getJsonSync serverUrl

        if serverStatus < 200 || serverStatus >= 300 then
            fail model (
                "HTTP "
                + string serverStatus
                + ": "
                + LogText.truncateForLog 200 serverText)
        else
            handleImportHttpResponse model fileId path serverText
    else
        fail model (
            "HTTP "
            + string status
            + ": "
            + LogText.truncateForLog 200 responseText)

/// Parse one existing Unparsed File document in place from its desktop path.
let parseUnparsedFileOp (fileId: NodeId) (model: VM) : VM * Effect list =
    if not (canImportDesktop model) then model, []
    else
        match Map.tryFind fileId model.graph.nodes with
        | Some { kind = Special File; documentState = Unparsed } ->
            match NodeDesktopPath.pathForNodeId model.graph fileId with
            | Some path -> requestImportAtPath model fileId path
            | None -> fail model "selected File has no desktop path"
        | _ -> model, []
