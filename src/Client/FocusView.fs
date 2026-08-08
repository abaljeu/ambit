module Gambol.Client.FocusView

open Browser.Dom
open Browser.Types
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.JsInterop
open Gambol.Client.RowView

// ---------------------------------------------------------------------------
// Focus management
// ---------------------------------------------------------------------------

/// Focus the correct element after a focus-relevant transition (`ManageFocus.shouldInvoke`).
/// `previousModel` = model before this dispatch; None on full `render` (always apply caret).
let manageFocus
        (previousModel: VM option) (model: VM) (rowByInstanceId: Map<SiteId, HTMLElement>)
        : unit =
    let preserveEditCaret = EditingCaretPreserve.shouldPreserveDomCaret previousModel model
    match model.mode with
    | CommandPalette _ | SearchDialog _ | FileSearchDialog _ | CssClassPrompt _ | RenamePrompt _ ->
        () // focus is handled by overlay renderers after the element becomes visible
    | Editing _ ->
        cancelPendingSelectionScroll ()
        deferSelectionScroll <- false
        let editEl = document.getElementById "edit-input"
        if not (isNull editEl) then
            let root = editEl
            let alreadyFocused =
                not (isNull document.activeElement)
                && System.Object.ReferenceEquals(document.activeElement, root)
            // Re-focusing an unfocused contenteditable places the caret at start. When preserving
            // the live caret (same Editing mode ref), skip focus if we already own it.
            if not (preserveEditCaret && alreadyFocused) then
                focusPreventScroll root
            if not preserveEditCaret then
                match model.mode with
                | Editing (_, caret) ->
                    match caret with
                    | EditCaret.EndOfText ->
                        let t = root.textContent
                        let n = if isNull t then 0 else t.Length
                        setEditorCaret root n
                    | EditCaret.Utf16Index p -> setEditorCaret root p
                    | EditCaret.LastVisualLineAtClientX x ->
                        setEditorCaretToLastLineAtX root x
                    | EditCaret.FirstVisualLineAtClientX x ->
                        setEditorCarentToFirstLineAtX root x
                | _ -> ()
            scrollElementIntoViewAboveKeyboard root
    | Selecting ->
        let hiddenInput = document.getElementById "hidden-input"
        if not (isNull hiddenInput) then
            focusPreventScroll (hiddenInput :?> HTMLInputElement)
        let focusedInstId = ManageFocus.focusedSiteId model
        // Only scroll when the focused row changed (navigation) or on a full render.
        // This prevents the wheel-scroll snap-back caused by non-navigation dispatches.
        let prevFocusedInstId =
            match previousModel with
            | None -> None  // full render — always scroll
            | Some prev -> Some (ManageFocus.focusedSiteId prev)
        if prevFocusedInstId <> Some focusedInstId then
            Map.tryFind focusedInstId rowByInstanceId
            |> Option.iter scrollFocusedRow
