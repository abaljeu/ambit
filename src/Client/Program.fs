module Gambol.Client.Program

open Browser.Dom
open Browser.Types
open Fable.Core
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client
open Gambol.Client.Update
open Gambol.Client.Controller
open Gambol.Client.View

// ---------------------------------------------------------------------------
// JS interop
// ---------------------------------------------------------------------------

[<Emit("fetch($0).then(r => r.text()).then($1)")>]
let fetchText (url: string) (callback: string -> unit) : unit = jsNative

// ---------------------------------------------------------------------------
// MVU dispatch loop
// ---------------------------------------------------------------------------

let mutable currentModel: VM =
    { graph = { root = NodeId(System.Guid.Empty); nodes = Map.empty }
      revision = Revision.Zero
      history = History.empty
      selectedNodes = None
      mode = Selecting
      siteMap = ViewModel.emptySiteMap
      nextInstanceId = 1
      clipboard = None
      linkPasteEnabled = false }

/// Element cache: instanceId → DOM row element.  Populated on first StateLoaded.
let mutable elementCache: Map<int, HTMLElement> = Map.empty

/// Mutable reference for edit prefill text (set on StartEdit, consumed on render)
let mutable editPrefill: string option = None

// ---------------------------------------------------------------------------
// One-time static DOM setup (hidden-input + settings-bar)
// These elements persist for the lifetime of the page; their event handlers
// read currentModel so they always operate on the latest state.
// ---------------------------------------------------------------------------

let setupStaticDOM (dispatch: Msg -> unit) : unit =
    let hiddenInput = document.createElement "input"
    hiddenInput.id <- "hidden-input"
    hiddenInput.setAttribute("autocomplete", "off")
    hiddenInput.setAttribute("tabindex", "-1")
    hiddenInput.addEventListener("keydown", fun (ev: Event) ->
        let ke = ev :?> KeyboardEvent
        if ke.key = "Tab" then ev.preventDefault()
        match currentModel.selectedNodes with
        | None ->
            let rootNode = currentModel.graph.nodes.[currentModel.graph.root]
            match ke.key with
            | "Enter" | "F2" -> dispatch (StartEdit rootNode.text)
            | "ArrowDown"    -> dispatch MoveSelectionDown
            | _ ->
                if isPrintableKey ke.key && not ke.ctrlKey && not ke.metaKey && not ke.altKey then
                    dispatch (StartEdit ke.key)
        | Some sel ->
            let nodeId = focusedNodeId currentModel.graph sel
            let nodeText = currentModel.graph.nodes.[nodeId].text
            let ctx =
                { keyEvent = ke
                  selectedNodeText = nodeText }
            handleKey selectionKeyTable ctx ke dispatch
    )
    hiddenInput.addEventListener("paste", fun ev -> onPaste ev dispatch)
    hiddenInput.addEventListener("copy",  fun ev -> onCopy  currentModel ev dispatch)
    hiddenInput.addEventListener("cut",   fun ev -> onCut   currentModel ev dispatch)
    app.appendChild hiddenInput |> ignore

    let settingsBar = document.createElement "div"
    settingsBar.id <- "settings-bar"
    let cbId = "setting-link-paste"
    let label = document.createElement "label"
    label.setAttribute("for", cbId)
    let cb = document.createElement "input"
    cb.id <- cbId
    cb.setAttribute("type", "checkbox")
    (cb :?> HTMLInputElement).``checked`` <- currentModel.linkPasteEnabled
    cb.addEventListener("change", fun _ -> dispatch ToggleLinkPaste)
    label.appendChild cb |> ignore
    label.appendChild (document.createTextNode " Copy/Paste reference to original") |> ignore
    settingsBar.appendChild label |> ignore
    app.appendChild settingsBar |> ignore

let rec dispatch (msg: Msg) : unit =
    // Special handling: StartEdit stores the prefill before updating model
    match msg with
    | StartEdit prefill -> editPrefill <- Some prefill
    | _ -> ()

    let prevModel = currentModel
    currentModel <- update msg currentModel dispatch

    match msg with
    | StateLoaded _ ->
        // Full rebuild on initial load; warm the element cache
        elementCache <- render currentModel dispatch
    | _ ->
        // Incremental patch for all other messages
        elementCache <- patchDOM prevModel currentModel dispatch elementCache

    // Apply the prefill text override for StartEdit (edit-input was just created)
    match msg, editPrefill with
    | StartEdit _, Some prefill ->
        let editInput = document.getElementById "edit-input"
        if not (isNull editInput) then
            let inp = editInput :?> HTMLInputElement
            inp.value <- prefill
            inp.focus()
            let len = prefill.Length
            inp.setSelectionRange(len, len)
        editPrefill <- None
    | _ -> ()

// ---------------------------------------------------------------------------
// Bootstrap
// ---------------------------------------------------------------------------

setupStaticDOM dispatch

fetchText $"/{Update.currentFile}/state" (fun text ->
    match decodeStateResponse text with
    | Ok (graph, revision) ->
        dispatch (StateLoaded (graph, revision))
    | Error err ->
        app.textContent <- $"Error: {err}"
)
