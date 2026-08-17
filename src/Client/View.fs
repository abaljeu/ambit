module Gambol.Client.View

open Browser.Dom
open Browser.Types
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.Controller
open Gambol.Client.JsInterop
open Gambol.Client.Update
open Gambol.Client.RowView
open Gambol.Client.FocusView
open Gambol.Client.Overlays

// ---------------------------------------------------------------------------
// Full rebuild (StateLoaded)
// ---------------------------------------------------------------------------

/// Rebuild all row elements from scratch: removes existing rows (children of #amb-document
/// that precede the hidden-input sentinel), then recreates them in preorder.
/// Returns a fresh element cache keyed by instanceId.
let render (vm: VM) (dispatch: Msg -> unit) : Map<SiteId, HTMLElement> =
    let rowRoot =
        if isNull ambDocument then app else ambDocument
    // Remove existing rows — everything before the hidden-input sentinel
    let hiddenInput = document.getElementById "hidden-input"
    if isNull hiddenInput then
        rowRoot.innerHTML <- ""
    else
        let mutable sib = hiddenInput.previousSibling
        while not (isNull sib) do
            let prev = sib.previousSibling
            rowRoot.removeChild sib |> ignore
            sib <- prev

    let mutable cache = Map.empty<SiteId, HTMLElement>
    let visible = ViewModel.getVisibleInstanceIds vm.siteMap
    syncZoomPath vm dispatch rowRoot |> ignore
    for instId in visible do
        let entry = vm.siteMap.entries.[instId]
        let depth = computeDepth vm.siteMap entry
        let row = makeRowElement vm dispatch depth entry
        cache <- Map.add instId row cache
        let sentinel = document.getElementById "hidden-input"
        if isNull sentinel then rowRoot.appendChild row |> ignore
        else rowRoot.insertBefore(row, sentinel) |> ignore

    manageFocus None vm cache
    renderSyncChrome vm dispatch
    cache

// ---------------------------------------------------------------------------
// Incremental DOM patch (all ops except StateLoaded)
// ---------------------------------------------------------------------------

/// Patch the DOM incrementally: diff old and new SiteMap visibility,
/// removes stale rows, creates/moves new rows, updates existing rows in-place.
/// Returns the updated element cache.
let patchDOM
        (oldModel: VM) (newModel: VM) (dispatch: Msg -> unit)
        (cache: Map<SiteId, HTMLElement>)
        : Map<SiteId, HTMLElement> =
    let preserveEditCaret =
        EditingCaretPreserve.shouldPreserveDomCaret (Some oldModel) newModel
    // Capture live caret before row patches; class/indicator writes can clear selection.
    // Restored below when preserveEditCaret (manageFocus may be skipped entirely).
    let savedEditCaret =
        if not preserveEditCaret then None
        else
            let el = document.getElementById "edit-input"
            if isNull el then None
            else Some (getContentEditableCaretOffset el)

    let cachedInstIds = cache |> Map.toSeq |> Seq.map fst |> Set.ofSeq
    let mutations = ViewModel.planPatchDOM oldModel newModel cachedInstIds

    // Index upsert mutations by instId for O(log n) lookup below
    let upsertIndex: Map<SiteId, RowMutation> =
        mutations |> List.choose (fun m ->
            match m with
            | RemoveRow _ -> None
            | CreateRow id  -> Some (id, m)
            | RecreateRow id -> Some (id, m)
            | PatchRow (id, _) -> Some (id, m))
        |> Map.ofList

    let mutable cache' = cache

    // Apply removals
    for mut in mutations do
        match mut with
        | RemoveRow instId ->
            match Map.tryFind instId cache' with
            | Some el -> el.remove()
            | None -> ()
            cache' <- Map.remove instId cache'
        | _ -> ()

    let rowRoot =
        if isNull ambDocument then app else ambDocument

    syncZoomPath newModel dispatch rowRoot |> ignore

    // PatchRow-only is not enough: sibling MoveUp/Down keeps the same rows but changes order.
    if not (ViewModel.needsDomOrderWalk oldModel newModel mutations) then
        // Selection/class-only (or empty): patch named rows; skip full visible walk + DOM order checks.
        for mut in mutations do
            match mut with
            | PatchRow (instId, _) ->
                match Map.tryFind instId newModel.siteMap.entries with
                | None -> ()
                | Some entry ->
                    let depth = computeDepth newModel.siteMap entry
                    let _, cache'' = resolveRow newModel dispatch depth entry instId upsertIndex cache'
                    cache' <- cache''
            | _ -> ()
    else
        // Apply upserts in preorder, correcting DOM position as we go
        let mutable prevNode: Browser.Types.Node option = None

        for instId in ViewModel.getVisibleInstanceIds newModel.siteMap do
            let entry = newModel.siteMap.entries.[instId]
            let depth = computeDepth newModel.siteMap entry

            let row, cache'' = resolveRow newModel dispatch depth entry instId upsertIndex cache'
            cache' <- cache''

            // Ensure the row sits in the correct DOM position (preorder sequence)
            let atCorrectPos =
                match prevNode with
                | None ->
                    let first = firstRowAnchor rowRoot
                    not (isNull first) && System.Object.ReferenceEquals(first, row)
                | Some pe ->
                    let ns = pe.nextSibling
                    not (isNull ns) && System.Object.ReferenceEquals(ns, row)

            if not atCorrectPos then
                let anchor =
                    match prevNode with
                    | None -> firstRowAnchor rowRoot
                    | Some pe -> pe.nextSibling
                rowRoot.insertBefore(row, anchor) |> ignore

            prevNode <- Some (row :> Browser.Types.Node)

    if ManageFocus.shouldInvoke (Some oldModel) newModel then
    //if false then
        manageFocus (Some oldModel) newModel cache'
    //if preserveEditCaret then
    if false then
        match savedEditCaret with
        | Some pos ->
            let el = document.getElementById "edit-input"
            if not (isNull el) then setEditorCaret el pos
        | None -> ()
    renderSyncChrome newModel dispatch
    cache'
