module Gambol.Client.UpdateImport

open Gambol.Client.JsInterop
open Gambol.Client.UpdateCodec
open Gambol.Client.UpdateHelpers
open Gambol.Shared
open Gambol.Shared.ViewModel

let private importBlockedMode =
    function
    | CommandPalette _
    | SearchDialog _
    | CssClassPrompt _ -> true
    | _ -> false

let private canImportDesktop (model: VM) =
    match model.desktopCapabilities with
    | Some { file = { canImport = true } } -> true
    | _ -> false

let private focusFilePath (model: VM) (sel: Selection) : (NodeId * string) option =
    let focusId = focusedNodeId model.graph sel

    match Map.tryFind focusId model.graph.nodes with
    | None -> None
    | Some node ->
        match FileReference.parseFirst node.text with
        | FileReference path -> Some (focusId, path)
        | _ -> None

let private editingModeAfterImport (m: VM) (focusId: NodeId) (caretUtf16: int) : VM =
    let t = m.graph.nodes.[focusId].text
    { m with mode = Editing (t, EditCaret.utf16ClampedToLength caretUtf16 t.Length) }

let private setTextOpsForEditing (model: VM) (focusId: NodeId) : Op list =
    match model.mode with
    | Editing (originalText, _) ->
        let current = readEditInputValue ()

        if current <> originalText then
            [ Op.SetText(focusId, originalText, current) ]
        else
            []
    | _ -> []

/// Import local file at `[[path]]` on the focus row; replaces that node's children.
let importLocalOp (model: VM) : VM * Effect list =
    if not (canImportDesktop model) then
        model, []
    elif importBlockedMode model.mode then
        model, []
    else
        match model.selectedNodes with
        | None -> model, []
        | Some sel ->
            match focusFilePath model sel with
            | None -> model, []
            | Some (focusId, path) ->
                let body = encodeDesktopFileStatusRequest path
                let status, responseText = postJsonSync "/_desktop/import" body

                if status < 200 || status >= 300 then
                    consoleLog (
                        "[Gambol desktop] import HTTP "
                        + string status
                        + ": "
                        + LogText.truncateForLog 200 responseText)

                    model, []
                else
                    match decodeDesktopImportPackage responseText with
                    | Error err ->
                        consoleLog ("[Gambol desktop] import decode failed: " + err)
                        model, []
                    | Ok package ->
                        match focusFilePath model sel with
                        | Some (focusId', path')
                            when focusId' = focusId && path' = package.sourcePath ->
                            let cursorPos =
                                match model.mode with
                                | Editing _ -> readEditInputCursor ()
                                | _ -> 0

                            let existing = model.graph.nodes.[focusId].children
                            let setTextOps = setTextOpsForEditing model focusId

                            let baseChange =
                                ImportText.buildImportChange
                                    focusId
                                    existing
                                    package
                                    model.revision.Value
                                    (System.Guid.NewGuid())

                            let change =
                                { baseChange with ops = setTextOps @ baseChange.ops }

                            match applyAndPost change model with
                            | Some m, effects ->
                                let m =
                                    match model.mode with
                                    | Editing _ -> editingModeAfterImport m focusId cursorPos
                                    | _ -> m

                                withSiteMap m, effects
                            | None, _ -> model, []
                        | _ -> model, []
