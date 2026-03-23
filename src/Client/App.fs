module Gambol.Client.App

open Browser.Dom
open Browser.Types
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client
open Gambol.Client.Update
open Gambol.Client.Controller
open Gambol.Client.View
open Gambol.Client.JsInterop
open Gambol.Client.SessionState

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
      nextSiteId = Sid 1
      zoomRoot = None
      clipboard = None
      pendingChanges = []
      syncState = Synced }

/// Element cache: instanceId → DOM row element.  Populated on first StateLoaded.
let mutable elementCache: Map<SiteId, HTMLElement> = Map.empty

/// Exponential backoff counter for auto-retry on SubmitFailed.
let mutable retryCount = 0

// Idle/pause remote polling after a period of no user interaction (battery-friendly).
let idleTimeoutMs = 5 * 60 * 1000
let mutable lastActivityMs = nowMs ()

// ---------------------------------------------------------------------------
// One-time static DOM setup (hidden-input + settings-bar)
// These elements persist for the lifetime of the page; their event handlers
// read currentModel so they always operate on the latest state.
// ---------------------------------------------------------------------------

let setupStaticDOM (applyOp: Op -> unit) (wakePolling: unit -> unit) : unit =
    let hiddenInput = document.getElementById "hidden-input" :?> HTMLInputElement
    hiddenInput.addEventListener("keydown", fun (ev: Event) ->
        let ke = ev :?> KeyboardEvent
        if ke.key = "Tab" then ev.preventDefault()
        if (ke.ctrlKey || ke.metaKey) && ke.key = "p" && not ke.shiftKey then
            ev.preventDefault()
        handleKey currentModel.mode ke applyOp
    )
    hiddenInput.addEventListener("paste", fun ev -> onPaste ev applyOp)
    hiddenInput.addEventListener("copy",  fun ev -> onCopy  currentModel ev applyOp)
    hiddenInput.addEventListener("cut",   fun ev -> onCut   currentModel ev applyOp)

    let basePath =
        let path = window.location.pathname
        if path.StartsWith("/ambit") then "/ambit" else ""
    let logoutLink = document.getElementById "logout-link" :?> HTMLAnchorElement
    logoutLink.setAttribute("href", basePath + "/logout")

    let reloadBtn = document.getElementById "reload-btn"
    reloadBtn.setAttribute("title", "Full reload (useful if Page is old or assets are cached)")
    reloadBtn.addEventListener("click", fun _ ->
        let path = window.location.pathname
        window.location.assign(path + "?bust=" + string (nowMs ())))

    setLastKeyDisplay None None

    // Deployment stamp (injected as window.__BUILD_TS__ — max of server assembly + wwwroot client artifacts).
    let buildEl = document.getElementById "server-build-stamp"
    if isNull buildEl then () else
        let stampEpochSec = readBuildEpochSec ()
        let txt =
            if stampEpochSec <= 0 then "Deploy: (unknown)"
            else "Deploy: " + epochSecToTorontoString stampEpochSec
        buildEl.textContent <- txt

    document.addEventListener("visibilitychange", fun _ ->
        if isDocumentHidden () then saveSessionState currentModel)
    window.addEventListener("pagehide", fun _ ->
        saveSessionState currentModel)

    let syncStatus = document.getElementById "sync-status"
    syncStatus.addEventListener("click", fun _ ->
        match currentModel.syncState with
        | Inactive ->
            wakePolling ()
        | Stale ->
            let path = window.location.pathname
            window.location.assign(path + "?bust=" + string (nowMs ()))
        | _ -> applyOp (retryPendingOp true))

let rec applyOp (op: Op) : unit =
    let prevModel = currentModel
    currentModel <- op currentModel dispatch
    elementCache <- patchDOM prevModel currentModel applyOp elementCache
    View.renderUndoStatus currentModel
    View.renderCommandPalette currentModel applyOp
    View.renderCssClassPrompt currentModel applyOp

and dispatch (msg: Msg) : unit =
    let prevModel = currentModel
    currentModel <- update msg currentModel dispatch

    match msg with
    | System (StateLoaded _) ->
        currentModel <- restoreSessionState currentModel
        lastActivityMs <- nowMs ()
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
                                syncState = Syncing 1 }
            fireNextPending restoredPending dispatch
        elementCache <- render currentModel applyOp
        View.renderUndoStatus currentModel
        View.renderCommandPalette currentModel applyOp
        View.renderCssClassPrompt currentModel applyOp
    | System (SubmitResponse _) ->
        retryCount <- 0
        View.renderStatus currentModel
    | System SubmitFailed ->
        retryCount <- retryCount + 1
        let maxAutoRetries = 10
        let delaySec = min 60 (1 <<< (min retryCount 6))
        let canAutoRetry = match currentModel.syncState with Pending n -> n < maxAutoRetries | _ -> false
        if currentModel.syncState <> Stale && canAutoRetry then
            setTimeout (fun () ->
                applyOp (retryPendingOp false)
                View.renderStatus currentModel) (delaySec * 1000)
        View.renderStatus currentModel
    | System (ServerAhead _) ->
        View.renderStatus currentModel

    | System PollingInactive
    | System PollingActive ->
        View.renderStatus currentModel

// ---------------------------------------------------------------------------
// Polling
// ---------------------------------------------------------------------------

let pollForRemoteChanges () : unit =
    let now = nowMs ()
    let hidden = isDocumentHidden ()
    let idleForMs = now - lastActivityMs
    let shouldPoll = (not hidden) && idleForMs < idleTimeoutMs

    if not shouldPoll then
        if currentModel.syncState = Synced && currentModel.pendingChanges.IsEmpty then
            dispatch (System PollingInactive)
        ()
    else
        if currentModel.syncState = Inactive then
            dispatch (System PollingActive)

        let url = $"/{Update.currentFile}/poll?_={now}"
        fetchTextNoCache url (fun text ->
            match Update.decodePollResponse text with
            | Ok (serverRev, serverBuild, serverPage) ->
                let dataStale = serverRev > currentModel.revision.Value
                let clientBuild = readBuildEpochSec ()
                let clientPage = readPageBuildEpochSec ()
                let serverNewer = serverBuild <> clientBuild || serverPage <> clientPage
                if dataStale || serverNewer then
                    dispatch (System (ServerAhead (Revision serverRev)))
            | Error _ -> ())

let recordActivity (wakeIfInactive: bool) : unit =
    lastActivityMs <- nowMs ()
    if wakeIfInactive && not (isDocumentHidden ()) && currentModel.syncState = Inactive then
        pollForRemoteChanges ()

let wakePolling () : unit =
    lastActivityMs <- nowMs ()
    if currentModel.syncState = Inactive then dispatch (System PollingActive)
    pollForRemoteChanges ()

let startPolling () : unit =
    setInterval pollForRemoteChanges 5000 |> ignore

    // Keep a cheap "are we active?" signal so we can stop polling when idle.
    document.addEventListener("pointerdown", fun _ -> recordActivity true)
    document.addEventListener("keydown", fun _ -> recordActivity true)
    document.addEventListener("wheel", fun _ -> recordActivity true)
    document.addEventListener("touchstart", fun _ -> recordActivity true)
    window.addEventListener("scroll", fun _ -> recordActivity true)

    window.addEventListener("focus", fun _ ->
        recordActivity false
        pollForRemoteChanges ())

    document.addEventListener("visibilitychange", fun _ ->
        if isDocumentHidden () then
            if currentModel.syncState = Synced && currentModel.pendingChanges.IsEmpty then
                dispatch (System PollingInactive)
        else
            recordActivity false
            pollForRemoteChanges ())
