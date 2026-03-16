module Gambol.Client.Program

open Browser.Dom
open Browser.Types
open Fable.Core
open Thoth.Json.Core
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

[<Emit("sessionStorage.getItem($0)")>]
let private sessionGet (key: string) : string = jsNative

[<Emit("sessionStorage.setItem($0, $1)")>]
let private sessionSet (key: string) (value: string) : unit = jsNative

[<Emit("document.hidden")>]
let private isDocumentHidden () : bool = jsNative

// ---------------------------------------------------------------------------
// Session state: persist zoom root + fold state across browser-initiated reloads
// (e.g. iOS Safari tab eviction). Saved to sessionStorage on visibility hide;
// restored once after StateLoaded, before the first render.
// ---------------------------------------------------------------------------

let private sessionKey = "gambol-session-v1"

/// Snapshot the session-specific parts of the VM to sessionStorage.
let saveSessionState (model: VM) : unit =
    let expandedIds =
        model.siteMap.entries
        |> Map.toArray
        |> Array.choose (fun (_, e) ->
            // Root entry (parentInstanceId = None) is always expanded — skip it.
            if e.expanded && e.parentInstanceId <> None
            then Some (e.nodeId.Value.ToString())
            else None)
    let zoomJson =
        match model.zoomRoot with
        | Some (NodeId g) -> "\"" + g.ToString() + "\""
        | None -> "null"
    let idsJson =
        expandedIds |> Array.map (fun s -> "\"" + s + "\"") |> String.concat ","
    sessionSet sessionKey (sprintf "{\"z\":%s,\"e\":[%s]}" zoomJson idsJson)

/// Restore zoom root and fold state into a freshly-loaded VM.
/// Called once immediately after StateLoaded, before the initial render.
let restoreSessionState (model: VM) : VM =
    let json = sessionGet sessionKey
    if isNull json || json = "" then model
    else
        try
            let decoder =
                Decode.object (fun get ->
                    let z = get.Optional.Field "z" Decode.string
                    let e = get.Required.Field "e" (Decode.list Decode.string)
                    z, e)
            match Thoth.Json.JavaScript.Decode.fromString decoder json with
            | Error _ -> model
            | Ok (zoomStr, expandedStrs) ->
                let zoomRoot =
                    zoomStr |> Option.bind (fun s ->
                        match System.Guid.TryParse(s) with
                        | true, g ->
                            let nid = NodeId g
                            if Map.containsKey nid model.graph.nodes then Some nid
                            else None
                        | _ -> None)
                let effectiveRoot = zoomRoot |> Option.defaultValue model.graph.root
                let siteMap0, nextId0 =
                    if effectiveRoot <> model.graph.root
                    then ViewModel.buildSiteMapFrom model.graph effectiveRoot 0
                    else model.siteMap, model.nextInstanceId
                let expandedSet =
                    expandedStrs
                    |> List.choose (fun s ->
                        match System.Guid.TryParse(s) with
                        | true, g -> Some (NodeId g)
                        | _ -> None)
                    |> Set.ofList
                let siteMap1, nextId1 =
                    ViewModel.applyFoldSession expandedSet model.graph siteMap0 nextId0
                { model with
                    zoomRoot = zoomRoot
                    siteMap = siteMap1
                    nextInstanceId = nextId1 }
        with _ -> model

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

/// Exponential backoff counter for auto-retry on SubmitFailed.
let mutable retryCount = 0

// ---------------------------------------------------------------------------
// One-time static DOM setup (hidden-input + settings-bar)
// These elements persist for the lifetime of the page; their event handlers
// read currentModel so they always operate on the latest state.
// ---------------------------------------------------------------------------

let setupStaticDOM (applyOp: Op -> unit) : unit =
    let hiddenInput = document.getElementById "hidden-input" :?> HTMLInputElement
    hiddenInput.addEventListener("keydown", fun (ev: Event) ->
        let ke = ev :?> KeyboardEvent
        recordKeyAndRenderDiagnostic ke
        if ke.key = "Tab" then ev.preventDefault()
        let viewRootId = currentModel.zoomRoot |> Option.defaultValue currentModel.graph.root
        let rootNode = currentModel.graph.nodes.[viewRootId]
        let textToEdit =
            match currentModel.selectedNodes with
            | None -> rootNode.text
            | Some sel ->
                let nodeId = focusedNodeId currentModel.graph sel
                currentModel.graph.nodes.[nodeId].text
        let ctx = { keyEvent = ke; selectedNodeText = textToEdit }
        handleKey selectionKeyTable ctx ke applyOp
    )
    hiddenInput.addEventListener("paste", fun ev -> onPaste ev applyOp)
    hiddenInput.addEventListener("copy",  fun ev -> onCopy  currentModel ev applyOp)
    hiddenInput.addEventListener("cut",   fun ev -> onCut   currentModel ev applyOp)

    let cb = document.getElementById "setting-link-paste" :?> HTMLInputElement
    cb.``checked`` <- currentModel.linkPasteEnabled
    cb.addEventListener("change", fun _ -> applyOp toggleLinkPasteOp)

    let basePath =
        let path = window.location.pathname
        if path.StartsWith("/ambit") then "/ambit" else ""
    let logoutLink = document.getElementById "logout-link" :?> HTMLAnchorElement
    logoutLink.setAttribute("href", basePath + "/logout")

    let buildLabel = document.getElementById "build-label" 
    let serverStamp = readBuildStamp BuildInfo.buildNumber
    let pageStamp = readPageStamp "?"
    buildLabel.textContent <- $"Server: {serverStamp} | Page: {pageStamp} "
    buildLabel.setAttribute("title", "Server = when served; Page = when assets were built.")

    let reloadBtn = document.getElementById "reload-btn" 
    reloadBtn.setAttribute("title", "Full reload (useful if Page is old or assets are cached)")
    reloadBtn.addEventListener("click", fun _ -> window.location.reload())

    let platformInfo = document.getElementById "key-platform-info"
    platformInfo.textContent <- "Platform: " + getPlatformDiagnostic (isIOS ())
    let lastKeyEl = document.getElementById "key-last-key"
    lastKeyEl.textContent <- " | Last key: (none)"

    document.addEventListener("visibilitychange", fun _ ->
        if isDocumentHidden () then saveSessionState currentModel)
    window.addEventListener("pagehide", fun _ ->
        saveSessionState currentModel)

    let syncStatus = document.getElementById "sync-status"
    syncStatus.addEventListener("click", fun _ -> applyOp retryPendingOp)

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
        currentModel <- restoreSessionState currentModel
        let saved = loadPendingQueue ()
        let serverRev = currentModel.revision.Value
        let filtered = saved |> List.filter (fun c -> c.id >= serverRev)
        let localGraph, restoredPending =
            filtered |> List.fold (fun (g, acc) c ->
                let s: State = { graph = g; history = History.empty; revision = Revision 0 }
                match Change.apply c s with
                | ApplyResult.Changed s' -> s'.graph, acc @ [c]
                | _ -> g, acc) (currentModel.graph, [])
        savePendingQueue restoredPending
        if not restoredPending.IsEmpty then
            currentModel <- { currentModel with
                                graph = localGraph
                                pendingChanges = restoredPending
                                syncState = Syncing }
            fireNextPending restoredPending dispatch
        elementCache <- render currentModel applyOp
        View.renderUndoStatus currentModel
    | System (SubmitResponse _) ->
        retryCount <- 0
        View.renderStatus currentModel
    | System SubmitFailed ->
        retryCount <- retryCount + 1
        let delaySec = min 60 (1 <<< (min retryCount 6))
        setTimeout (fun () ->
            applyOp retryPendingOp
            View.renderStatus currentModel) (delaySec * 1000)
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
