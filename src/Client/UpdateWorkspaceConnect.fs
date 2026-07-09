module Gambol.Client.UpdateWorkspaceConnect

open Browser.Dom
open Browser.Types
open Gambol.Client.JsInterop
open Gambol.Client.UpdateCodec
open Gambol.Client.UpdateHelpers
open Gambol.Shared
open Gambol.Shared.ViewModel

let private canPickFolder (model: VM) =
    match model.desktopCapabilities with
    | Some { file = { canPickFolder = true } } -> true
    | _ -> false

let private wizardBlockedMode =
    function
    | CommandPalette _
    | SearchDialog _
    | FileSearchDialog _
    | CssClassPrompt _
    | RenamePrompt _
    | WorkspaceConnectWizard _ -> true
    | _ -> false

let private readWizardState () : WorkspaceConnectWizardState option =
    let labelEl = document.getElementById "workspace-connect-label" :?> HTMLInputElement
    let pathEl = document.getElementById "workspace-connect-path"

    if isNull labelEl || isNull pathEl then
        None
    else
        let linkExisting =
            let existing = document.getElementById "workspace-connect-link-existing" :?> HTMLInputElement
            not (isNull existing) && existing.``checked``

        let linkMode =
            if linkExisting then WorkspaceLinkMode.LinkExisting
            else WorkspaceLinkMode.CreateNew

        let initialSync =
            let download =
                document.getElementById "workspace-connect-sync-download" :?> HTMLInputElement

            let upload =
                document.getElementById "workspace-connect-sync-upload" :?> HTMLInputElement

            let skip =
                document.getElementById "workspace-connect-sync-skip" :?> HTMLInputElement

            if not (isNull skip) && skip.``checked`` then InitialSyncDirection.Skip
            elif not (isNull upload) && upload.``checked`` then InitialSyncDirection.Upload
            elif not (isNull download) && download.``checked`` then InitialSyncDirection.Download
            else InitialSyncDirection.Skip

        let existingLabels =
            let select = document.getElementById "workspace-connect-existing" :?> HTMLSelectElement

            if isNull select then
                []
            else
                [ 0 .. select.length - 1 ]
                |> List.choose (fun i ->
                    let opt = select.options.[i] :?> HTMLOptionElement
                    if isNull opt || opt.value = "" then None else Some opt.value)

        let label =
            if linkExisting then
                let select = document.getElementById "workspace-connect-existing" :?> HTMLSelectElement

                if isNull select || select.value = "" then labelEl.value
                else select.value
            else
                labelEl.value

        let remoteEl = document.getElementById "workspace-connect-remote"

        let gatewayUrl =
            if isNull remoteEl then ""
            else remoteEl.textContent

        Some
            { selectedPath = pathEl.textContent
              gitRoot = pathEl.dataset.["gitRoot"]
              label = label
              linkMode = linkMode
              existingLabels = existingLabels
              initialSync = initialSync
              gatewayUrl = gatewayUrl }

let closeWorkspaceConnectWizardOp (model: VM) : VM * Effect list =
    match model.mode with
    | WorkspaceConnectWizard (ret, _) -> { model with mode = ret }, []
    | _ -> model, []

let private openWizard (model: VM) (selectedPath: string) (gitRoot: string) : VM * Effect list =
    let label = WorkspaceConnect.defaultLabelFromRoot gitRoot
    let origin = window.location.origin
    let pathname = window.location.pathname

    let wizard =
        { selectedPath = selectedPath
          gitRoot = gitRoot
          label = label
          linkMode = WorkspaceLinkMode.CreateNew
          existingLabels = WorkspaceGitRemote.listWorkspaceLabels model.graph
          initialSync = InitialSyncDirection.Download
          gatewayUrl = WorkspaceConnect.gatewayUrlForLabel origin pathname label }

    { model with mode = WorkspaceConnectWizard (model.mode, wizard) }, []

let openWorkspaceFolderOp (model: VM) : VM * Effect list =
    if not (canPickFolder model) || wizardBlockedMode model.mode then
        model, []
    else
        let status, responseText = postJsonSync "/_desktop/pick-folder" "{}"

        if status < 200 || status >= 300 then
            let detail =
                if responseText.Contains("cancelled") then "Folder picker cancelled"
                else "Folder picker failed"

            { model with status = Some(StatusMessage.error detail) }, []
        else
            match Thoth.Json.JavaScript.Decode.fromString Serialization.decodeDesktopPickFolderResponse responseText with
            | Error err ->
                { model with status = Some(StatusMessage.error ("Pick folder: " + err)) }, []
            | Ok response -> openWizard model response.path response.gitRoot

let submitWorkspaceConnectOp (model: VM) : VM * Effect list =
    match model.mode with
    | WorkspaceConnectWizard (ret, _) ->
        match readWizardState () with
        | None ->
            { model with mode = ret }, []
        | Some draft ->
            match WorkspaceConnect.resolveEffectiveLabel draft.linkMode draft.label with
            | Error err ->
                { model with status = Some(StatusMessage.error err) }, []
            | Ok label ->
                match WorkspaceConnect.validateLinkTarget model.graph draft.linkMode label with
                | Error err ->
                    { model with status = Some(StatusMessage.error err) }, []
                | Ok () ->
                    let workspaceOps =
                        if WorkspaceConnect.shouldCreateWorkspace model.graph draft.linkMode label then
                            let _, ops = FileNodeOps.planCreateWorkspace model.graph label
                            ops
                        else
                            []

                    let gatewayUrl =
                        WorkspaceConnect.gatewayUrlForLabel
                            window.location.origin
                            window.location.pathname
                            label

                    let modelAfterOps, opEffects =
                        if workspaceOps.IsEmpty then
                            model, []
                        else
                            let change =
                                { id = model.revision.Value
                                  changeId = System.Guid.NewGuid()
                                  ops = workspaceOps }

                            match applyAndPost change model with
                            | Some m, effects -> withSiteMap m, effects
                            | None, _ -> model, []

                    let connectEffect =
                        RequestWorkspaceConnect(
                            draft.gitRoot,
                            label,
                            workspaceOps,
                            draft.initialSync,
                            gatewayUrl)

                    { modelAfterOps with mode = ret },
                    opEffects @ [ connectEffect ]
    | _ -> model, []

let updateWizardLabelOp (label: string) (model: VM) : VM * Effect list =
    match model.mode with
    | WorkspaceConnectWizard (ret, wizard) ->
        let gatewayUrl =
            WorkspaceConnect.gatewayUrlForLabel
                window.location.origin
                window.location.pathname
                label

        { model with
            mode =
                WorkspaceConnectWizard(
                    ret,
                    { wizard with
                        label = label
                        gatewayUrl = gatewayUrl }) },
        []
    | _ -> model, []
