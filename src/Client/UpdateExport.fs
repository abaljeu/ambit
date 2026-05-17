module Gambol.Client.UpdateExport

open Gambol.Client.JsInterop
open Gambol.Client.UpdateCodec
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Shared.ViewModelOps

let private exportBlockedMode =
    function
    | CommandPalette _
    | SearchDialog _
    | CssClassPrompt _ -> true
    | _ -> false

let private canExportDesktop (model: VM) =
    match model.desktopCapabilities with
    | Some { file = { canExport = true } } -> true
    | _ -> false

let private focusFilePath (model: VM) (sel: Selection) : (NodeId * string) option =
    let focusId = focusedNodeId model.graph sel

    match Map.tryFind focusId model.graph.nodes with
    | None -> None
    | Some node ->
        match FileReference.parseFirst node.text with
        | FileReference path -> Some (focusId, path)
        | _ -> None

/// Export owned children of the focus row to the local file at `[[path]]`.
let exportLocalOp (model: VM) : VM * Effect list =
    if not (canExportDesktop model) then
        model, []
    elif exportBlockedMode model.mode then
        model, []
    else
        match model.selectedNodes with
        | None -> model, []
        | Some sel ->
            match focusFilePath model sel with
            | None -> model, []
            | Some (focusId, path) ->
                match ExportText.trySerializeOwnedChildren model.graph focusId with
                | Error err ->
                    consoleLog ("[Gambol desktop] export failed: " + err)
                    model, []
                | Ok content ->
                    let body = encodeDesktopExportRequest { path = path; content = content }
                    let status, responseText = postJsonSync "/_desktop/export" body

                    if status < 200 || status >= 300 then
                        consoleLog (
                            "[Gambol desktop] export HTTP "
                            + string status
                            + ": "
                            + LogText.truncateForLog 200 responseText)

                        model, []
                    else
                        match decodeDesktopExportResponse responseText with
                        | Error err ->
                            consoleLog ("[Gambol desktop] export decode failed: " + err)
                            model, []
                        | Ok response ->
                            match focusFilePath model sel with
                            | Some (_, path') when path' = response.path -> model, []
                            | _ -> model, []
