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
    { model with lastCmdResult = Some(CmdLastResult.Error message) }, []

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
        | Some parsed, effects ->
            let result = Some(CmdLastResult.Detail("parsed: " + path))
            { withSiteMap parsed with lastCmdResult = result }, effects
        | None, _ -> fail model "parse change was rejected"

let private handleImportHttpResponse
    (model: VM) (fileId: NodeId) (path: string) (responseText: string)
    : VM * Effect list =
    match decodeDesktopImportPackage responseText with
    | Error err -> fail model ("could not decode parsed file: " + err)
    | Ok package -> commitParsedFile model fileId path package

let private requestImportAtPath
    (model: VM) (fileId: NodeId) (path: string)
    : VM * Effect list =
    let url = "/_desktop/file?path=" + encodeUriComponent path
    let status, responseText = getJsonSync url

    if status < 200 || status >= 300 then
        fail model (
            "HTTP "
            + string status
            + ": "
            + LogText.truncateForLog 200 responseText)
    else
        handleImportHttpResponse model fileId path responseText

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
