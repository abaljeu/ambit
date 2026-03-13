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

[<Emit("(typeof window.__BUILD__ !== 'undefined' ? window.__BUILD__ : $0)")>]
let readBuildStamp (fallback: string) : string = jsNative

[<Emit("(typeof window.__PAGE_BUILD__ !== 'undefined' ? window.__PAGE_BUILD__ : $0)")>]
let readPageStamp (fallback: string) : string = jsNative

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
      zoomRoot = None
      clipboard = None
      linkPasteEnabled = false
      pendingChanges = []
      syncState = Synced }

/// Element cache: instanceId → DOM row element.  Populated on first StateLoaded.
let mutable elementCache: Map<int, HTMLElement> = Map.empty

// ---------------------------------------------------------------------------
// One-time static DOM setup (hidden-input + settings-bar)
// These elements persist for the lifetime of the page; their event handlers
// read currentModel so they always operate on the latest state.
// ---------------------------------------------------------------------------

let setupStaticDOM (applyOp: Op -> unit) : unit =
    let hiddenInput = document.createElement "input"
    hiddenInput.id <- "hidden-input"
    hiddenInput.setAttribute("autocomplete", "off")
    hiddenInput.setAttribute("tabindex", "-1")
    hiddenInput.addEventListener("keydown", fun (ev: Event) ->
        let ke = ev :?> KeyboardEvent
        if ke.key = "Tab" then ev.preventDefault()
        match currentModel.selectedNodes with
        | None ->
            let viewRootId = currentModel.zoomRoot |> Option.defaultValue currentModel.graph.root
            let rootNode = currentModel.graph.nodes.[viewRootId]
            match ke.key with
            | "Enter" | "F2" -> applyOp (startEdit rootNode.text)
            | "ArrowDown"    -> applyOp moveSelectionDown
            | _ ->
                if isPrintableKey ke.key && not ke.ctrlKey && not ke.metaKey && not ke.altKey then
                    applyOp (startEdit ke.key)
        | Some sel ->
            let nodeId = focusedNodeId currentModel.graph sel
            let nodeText = currentModel.graph.nodes.[nodeId].text
            let ctx =
                { keyEvent = ke
                  selectedNodeText = nodeText }
            handleKey selectionKeyTable ctx ke applyOp
    )
    hiddenInput.addEventListener("paste", fun ev -> onPaste ev applyOp)
    hiddenInput.addEventListener("copy",  fun ev -> onCopy  currentModel ev applyOp)
    hiddenInput.addEventListener("cut",   fun ev -> onCut   currentModel ev applyOp)
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
    cb.addEventListener("change", fun _ -> applyOp toggleLinkPasteOp)
    label.appendChild cb |> ignore
    label.appendChild (document.createTextNode " Copy/Paste reference to original") |> ignore
    settingsBar.appendChild label |> ignore

    let basePath =
        let path = window.location.pathname
        if path.StartsWith("/ambit") then "/ambit" else ""
    let logoutLink = document.createElement "a"
    logoutLink.setAttribute("href", basePath + "/logout")
    logoutLink.setAttribute("style", "margin-left: 1rem; font-size: .85rem; color: #555;")
    logoutLink.textContent <- "Logout"
    settingsBar.appendChild logoutLink |> ignore

    let buildLabel = document.createElement "span"
    buildLabel.setAttribute("style", "margin-left: auto; font-size: .75rem; color: #666;")
    let serverStamp = readBuildStamp BuildInfo.buildNumber
    let pageStamp = readPageStamp "?"
    buildLabel.textContent <- $"Server: {serverStamp} | Page: {pageStamp}"
    buildLabel.setAttribute("title", "Server = when served; Page = when assets were built. Reload if Page is old.")
    settingsBar.appendChild buildLabel |> ignore

    app.appendChild settingsBar |> ignore

    let syncStatus = document.createElement "div"
    syncStatus.id <- "sync-status"
    syncStatus.addEventListener("click", fun _ -> applyOp retryPendingOp)
    app.appendChild syncStatus |> ignore

    let undoStatus = document.createElement "div"
    undoStatus.id <- "undo-status"
    undoStatus.setAttribute("title", "Undo/Redo status")
    app.appendChild undoStatus |> ignore

let rec applyOp (op: Op) : unit =
    let prevModel = currentModel
    currentModel <- op currentModel dispatch
    elementCache <- patchDOM prevModel currentModel applyOp elementCache
    View.renderUndoStatus currentModel

and dispatch (msg: Msg) : unit =
    let prevModel = currentModel
    currentModel <- update msg currentModel dispatch

    match msg with
    | System (StateLoaded _) ->
        elementCache <- render currentModel applyOp
        View.renderUndoStatus currentModel
    | System (SubmitResponse _) | System SubmitFailed ->
        View.renderStatus currentModel

// ---------------------------------------------------------------------------
// Bootstrap
// ---------------------------------------------------------------------------

setupStaticDOM applyOp

fetchText $"/{Update.currentFile}/state" (fun text ->
    match decodeStateResponse text with
    | Ok (graph, revision) ->
        dispatch (System (StateLoaded (graph, revision)))
    | Error err ->
        app.textContent <- $"Error: {err}"
)
