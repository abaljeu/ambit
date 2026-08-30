module Gambol.Client.Overlays

open Browser.Dom
open Browser.Types
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.Commands
open Gambol.Client.Controller
open Gambol.Client.JsInterop
open Gambol.Client.Update
open Gambol.Client.StatusView

module CommandMeta = Gambol.Shared.CommandEntry

// ---------------------------------------------------------------------------
// Command palette rendering
// ---------------------------------------------------------------------------

let private paletteWired = ref false

/// Populate the results list of a palette container, highlighting the selected item.
/// Upper-bounds selectedCommand to the list length to handle stale indices.
/// Scrolls the selected item into view so it stays visible when navigating with arrows.
let renderPalette (container: HTMLElement) (items: string list) (selectedCommand: int) : unit =
    let ul = container.querySelector ".amb-dialog-results" :?> HTMLElement
    ul.innerHTML <- ""
    let clampedSel = if items.IsEmpty then 0 else min selectedCommand (items.Length - 1)
    let mutable selectedLi: Element option = None
    items |> List.iteri (fun i label ->
        let li = document.createElement "li"
        li.textContent <- label
        if i = clampedSel then
            li.classList.add "amb-dialog-selected"
            selectedLi <- Some li
        ul.appendChild li |> ignore)
    selectedLi |> Option.iter (fun el -> scrollIntoViewNearest (el :?> HTMLElement))

/// Show or hide the command palette overlay and keep it up to date with the model.
/// Event listeners are wired once on the first call.
let renderCommandPalette (model: VM) (dispatch: Msg -> unit) : unit =
    let container = document.getElementById "command-palette"
    if isNull container then () else

    match model.mode with
    | CommandPalette (q, selectedCommand, ret) ->
        let wasOpen = container.classList.contains "amb-dialog-open"
        container.classList.add "amb-dialog-open"
        let input = document.getElementById "command-palette-input" :?> HTMLInputElement
        if input.value <> q then input.value <- q
        if not wasOpen then
            window.setTimeout((fun _ ->
                focusPreventScroll input
                input.select()), 0) |> ignore
        let items = filteredCommands model ret q |> List.map (fun c -> CommandMeta.displayName c.id)
        renderPalette container items selectedCommand

        if not paletteWired.Value then
            paletteWired.Value <- true
            let ul = document.getElementById "command-palette-results"

            input.addEventListener("input", fun _ ->
                dispatch (ApplyOp (paletteSetQueryOp input.value)))

            input.addEventListener("keydown", fun (ev: Event) ->
                let ke = ev :?> KeyboardEvent
                if (ke.ctrlKey || ke.metaKey) && ke.key = "p" && not ke.shiftKey then
                    ev.preventDefault()
                handlePaletteKey ke dispatch)

            ul.addEventListener("click", fun (ev: Event) ->
                let target = ev.target :?> HTMLElement
                match target.closest "li" with
                | None -> ()
                | Some li ->
                    let lis = ul.querySelectorAll "li"
                    let mutable idx = 0
                    for i in 0 .. int lis.length - 1 do
                        if System.Object.ReferenceEquals(lis.[i], li) then idx <- i
                    dispatch (ApplyOp (fun m ->
                        match m.mode with
                        | CommandPalette (q, _, ret) ->
                            match List.tryItem idx (filteredCommands m ret q) with
                            | None -> { m with mode = ret }, []
                            | Some cmd ->
                                match cmd.run () with
                                | None ->
                                    { m with mode = ret }, []
                                | Some op ->
                                    withDiagnostic
                                        (Some (CommandMeta.displayName cmd.id))
                                        op
                                        { m with mode = ret }
                        | _ -> m, [])))

    | _ ->
        container.classList.remove "amb-dialog-open"
        let input = document.getElementById "command-palette-input" :?> HTMLInputElement
        if not (isNull input) then input.value <- ""

let private cssClassPromptWired = ref false
let private cssClassPromptFilled = ref false

/// Show or hide the CSS class prompt overlay. Uses in-app modal instead of window.prompt for iPad.
let renderCssClassPrompt (model: VM) (dispatch: Msg -> unit) : unit =
    let container = document.getElementById "css-class-prompt"
    if isNull container then () else

    match model.mode with
    | CssClassPrompt (_, initialValue) ->
        container.classList.add "amb-dialog-open"
        let input = document.getElementById "css-class-prompt-input" :?> HTMLInputElement
        if not (isNull input) then
            if not cssClassPromptFilled.Value then
                cssClassPromptFilled.Value <- true
                input.value <- initialValue
            window.setTimeout((fun _ -> focusPreventScroll input), 0) |> ignore
            if not cssClassPromptWired.Value then
                cssClassPromptWired.Value <- true
                input.addEventListener("keydown", fun (ev: Event) ->
                    let ke = ev :?> KeyboardEvent
                    if (ke.ctrlKey || ke.metaKey) && ke.key = "p" && not ke.shiftKey then
                        ev.preventDefault()
                    handleCssClassPromptKey ke dispatch)
    | _ ->
        container.classList.remove "amb-dialog-open"
        cssClassPromptFilled.Value <- false
        let input = document.getElementById "css-class-prompt-input" :?> HTMLInputElement
        if not (isNull input) && input.value <> "" then
            input.value <- ""

let private renamePromptWired = ref false
let private renamePromptFilled = ref false

/// Show or hide the rename prompt overlay.
let renderRenamePrompt (model: VM) (dispatch: Msg -> unit) : unit =
    let container = document.getElementById "rename-prompt"
    if isNull container then () else

    match model.mode with
    | RenamePrompt (_, initialValue) ->
        container.classList.add "amb-dialog-open"
        let input = document.getElementById "rename-prompt-input" :?> HTMLInputElement
        if not (isNull input) then
            // Fill + focus/select only on open. Poll/sync re-renders must not call
            // input.select() or they reset the caret while the user is typing.
            if not renamePromptFilled.Value then
                renamePromptFilled.Value <- true
                input.value <- initialValue
                window.setTimeout((fun _ ->
                    focusPreventScroll input
                    input.select()), 0) |> ignore
            if not renamePromptWired.Value then
                renamePromptWired.Value <- true
                input.addEventListener("keydown", fun (ev: Event) ->
                    let ke = ev :?> KeyboardEvent
                    if (ke.ctrlKey || ke.metaKey) && ke.key = "p" && not ke.shiftKey then
                        ev.preventDefault()
                    handleRenamePromptKey ke dispatch)
    | _ ->
        container.classList.remove "amb-dialog-open"
        renamePromptFilled.Value <- false
        let input = document.getElementById "rename-prompt-input" :?> HTMLInputElement
        if not (isNull input) && input.value <> "" then
            input.value <- ""

let private syncRiskAlertWired = ref false

/// Full-screen sync risk notice (ServerRejected / CodeOutdated / DataOutdated) until acknowledged.
let renderSyncRiskAlert (model: VM) (dispatch: Msg -> unit) : unit =
    let root = document.getElementById "blocking-alert"
    if isNull root then () else

    let shouldShow =
        match model.syncInfo.syncState with
        | ServerRejected | CodeOutdated | DataOutdated -> not model.syncInfo.syncRiskAcknowledged
        | _ -> false

    if shouldShow then
        root.classList.add "amb-blocking-alert-open"
        let titleEl = document.getElementById "blocking-alert-title"
        let msgEl = document.getElementById "blocking-alert-message"
        let okBtn = document.getElementById "blocking-alert-ok" :?> HTMLButtonElement
        if not (isNull titleEl) && not (isNull msgEl) then
            match model.syncInfo.syncState with
            | ServerRejected ->
                titleEl.textContent <- "Server rejected change"
                msgEl.textContent <-
                    "The server could not apply your change (revision mismatch or invalid op). "
                    + "Reload the page to resync. Your unsaved changes will be lost."
            | CodeOutdated ->
                titleEl.textContent <- "New version available"
                msgEl.textContent <-
                    "A new version of this application has been deployed. "
                    + "Reload the page to get the latest version."
            | DataOutdated ->
                titleEl.textContent <- "View is out of date"
                msgEl.textContent <-
                    "The server has newer data than this page. Reload before continuing."
            | _ -> ()

        if not (isNull okBtn) then
            window.setTimeout ((fun _ -> okBtn.focus ()), 0) |> ignore
            if not syncRiskAlertWired.Value then
                syncRiskAlertWired.Value <- true
                okBtn.addEventListener ("click", fun _ -> dispatch AckSyncRisk)
                okBtn.addEventListener ("keydown", fun (ev: Event) ->
                    let ke = ev :?> KeyboardEvent
                    if ke.key = "Enter" || ke.key = " " then
                        ke.preventDefault ()
                        dispatch AckSyncRisk)
    else
        root.classList.remove "amb-blocking-alert-open"

/// Update the last-result display from the model.
let renderDiagnostics (model: VM) : unit =
    setCmdLastResultDisplay model.lastCmdResult

/// Status pill plus sync-risk overlay.
let renderSyncChrome (model: VM) (dispatch: Msg -> unit) : unit =
    renderStatus model
    renderSyncRiskAlert model dispatch
    renderDiagnostics model
