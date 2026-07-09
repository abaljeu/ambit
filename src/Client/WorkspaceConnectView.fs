module Gambol.Client.WorkspaceConnectView

open Browser.Dom
open Browser.Types
open Gambol.Client.Controller
open Gambol.Client.JsInterop
open Gambol.Client.UpdateWorkspaceConnect
open Gambol.Shared
open Gambol.Shared.ViewModel

let private wizardWired = ref false
let private wizardFilled = ref false

let private setLinkModeUi (linkExisting: bool) : unit =
    let createInput = document.getElementById "workspace-connect-link-create" :?> HTMLInputElement
    let existingInput = document.getElementById "workspace-connect-link-existing" :?> HTMLInputElement
    let labelInput = document.getElementById "workspace-connect-label" :?> HTMLInputElement
    let select = document.getElementById "workspace-connect-existing" :?> HTMLSelectElement

    if not (isNull createInput) then createInput.``checked`` <- not linkExisting

    if not (isNull existingInput) then existingInput.``checked`` <- linkExisting

    if not (isNull labelInput) then
        labelInput.disabled <- linkExisting

    if not (isNull select) then
        if linkExisting then select.removeAttribute "disabled"
        else select.setAttribute("disabled", "")

let private populateExistingLabels (labels: string list) (selected: string) : unit =
    let select = document.getElementById "workspace-connect-existing" :?> HTMLSelectElement
    if isNull select then () else

    select.innerHTML <- ""
    let placeholder = document.createElement "option" :?> HTMLOptionElement
    placeholder.value <- ""
    placeholder.textContent <- "Select workspace…"
    select.appendChild placeholder |> ignore

    labels
    |> List.iter (fun label ->
        let opt = document.createElement "option" :?> HTMLOptionElement
        opt.value <- label
        opt.textContent <- label
        select.appendChild opt |> ignore)

    if selected <> "" then select.value <- selected

let private refreshRemoteUrl (url: string) : unit =
    let remote = document.getElementById "workspace-connect-remote"
    if not (isNull remote) then remote.textContent <- url

let private handleWizardKey (ke: KeyboardEvent) (dispatch: Msg -> unit) : unit =
    match formatKeyCombo ke with
    | "Escape" ->
        ke.preventDefault()
        dispatch (ApplyOp (withDiagnostic "Escape" "Close connect wizard" closeWorkspaceConnectWizardOp))
    | "Enter" ->
        ke.preventDefault()
        dispatch (ApplyOp (withDiagnostic "Enter" "Connect workspace" submitWorkspaceConnectOp))
    | _ -> ()

let renderWorkspaceConnectWizard (model: VM) (dispatch: Msg -> unit) : unit =
    let container = document.getElementById "workspace-connect-wizard"
    if isNull container then () else

    match model.mode with
    | WorkspaceConnectWizard (_, wizard) ->
        container.classList.add "amb-dialog-open"

        let pathEl = document.getElementById "workspace-connect-path"
        let labelInput = document.getElementById "workspace-connect-label" :?> HTMLInputElement

        if not wizardFilled.Value then
            wizardFilled.Value <- true

            if not (isNull pathEl) then
                pathEl.textContent <- wizard.gitRoot
                pathEl.dataset.["gitRoot"] <- wizard.gitRoot

            if not (isNull labelInput) then
                labelInput.value <- wizard.label

            populateExistingLabels wizard.existingLabels wizard.label
            setLinkModeUi (wizard.linkMode = WorkspaceLinkMode.LinkExisting)
            refreshRemoteUrl wizard.gatewayUrl

        if not (isNull labelInput) && labelInput.value <> wizard.label then
            refreshRemoteUrl wizard.gatewayUrl

        window.setTimeout((fun _ ->
            if not (isNull labelInput) then focusPreventScroll labelInput), 0)
        |> ignore

        if not wizardWired.Value then
            wizardWired.Value <- true

            let createInput = document.getElementById "workspace-connect-link-create" :?> HTMLInputElement
            let existingInput = document.getElementById "workspace-connect-link-existing" :?> HTMLInputElement
            let select = document.getElementById "workspace-connect-existing" :?> HTMLSelectElement

            if not (isNull createInput) then
                createInput.addEventListener("change", fun _ -> setLinkModeUi false)

            if not (isNull existingInput) then
                existingInput.addEventListener("change", fun _ -> setLinkModeUi true)

            if not (isNull labelInput) then
                labelInput.addEventListener("input", fun _ ->
                    dispatch (ApplyOp (updateWizardLabelOp labelInput.value)))

            if not (isNull select) then
                select.addEventListener("change", fun _ ->
                    dispatch (ApplyOp (updateWizardLabelOp select.value)))

            let submitBtn = document.getElementById "workspace-connect-submit" :?> HTMLButtonElement

            if not (isNull submitBtn) then
                submitBtn.addEventListener("click", fun _ ->
                    dispatch (ApplyOp (withDiagnostic "" "Connect workspace" submitWorkspaceConnectOp)))

            container.addEventListener("keydown", fun (ev: Event) ->
                handleWizardKey (ev :?> KeyboardEvent) dispatch)
    | _ ->
        container.classList.remove "amb-dialog-open"
        wizardFilled.Value <- false
