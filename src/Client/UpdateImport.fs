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
    tryFindFocusedPath model.graph sel

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

let private importEditingCursorPos (model: VM) : int =
    match model.mode with
    | Editing _ -> readEditInputCursor ()
    | _ -> 0

let private commitImportAtFocus
    (model: VM) (focusId: NodeId) (package: DesktopImportPackage)
    : VM * Effect list =
    let cursorPos = importEditingCursorPos model
    let existing = model.graph.nodes.[focusId].children
    let setTextOps = setTextOpsForEditing model focusId

    let baseChange =
        if package.isDirectory then
            ImportText.buildDirectoryMergeChange
                model.graph
                focusId
                existing
                package
                model.revision.Value
                (System.Guid.NewGuid())
        else
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

let private applyImportPackage
    (model: VM) (sel: Selection) (focusId: NodeId) (package: DesktopImportPackage)
    : VM * Effect list =
    match focusFilePath model sel with
    | Some (focusId', path')
        when focusId' = focusId && path' = package.sourcePath ->
        commitImportAtFocus model focusId package
    | _ -> model, []

let private handleImportHttpResponse
    (model: VM) (sel: Selection) (focusId: NodeId) (responseText: string)
    : VM * Effect list =
    match decodeDesktopImportPackage responseText with
    | Error err ->
        consoleLog ("[Gambol desktop] import decode failed: " + err)
        model, []
    | Ok package -> applyImportPackage model sel focusId package

let private requestImportAtPath
    (model: VM) (sel: Selection) (focusId: NodeId) (path: string)
    : VM * Effect list =
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
        handleImportHttpResponse model sel focusId responseText

let private importLocalWhenSelected (model: VM) (sel: Selection) : VM * Effect list =
    match focusFilePath model sel with
    | None -> model, []
    | Some (focusId, path) -> requestImportAtPath model sel focusId path

/// Import local file at `[[path]]` on the focus row; replaces that node's children.
let importLocalOp (model: VM) : VM * Effect list =
    if not (canImportDesktop model) then
        model, []
    elif importBlockedMode model.mode then
        model, []
    else
        match model.selectedNodes with
        | None -> model, []
        | Some sel -> importLocalWhenSelected model sel
