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
      syncInfo = SyncInfo.initial }

/// Element cache: instanceId → DOM row element.  Populated on first StateLoaded.
let mutable elementCache: Map<SiteId, HTMLElement> = Map.empty

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
        match currentModel.syncInfo.syncState with
        | WaitingToRetry _ ->
            applyOp (retryPendingOp true)
        | ServerRejected | CodeOutdated | DataOutdated ->
            let path = window.location.pathname
            window.location.assign(path + "?bust=" + string (nowMs ()))
        | _ -> ()
    )

let rec applyOp (op: Op) : unit =
    let prevModel = currentModel
    currentModel <- op currentModel dispatch
    elementCache <- patchDOM prevModel currentModel applyOp dispatch elementCache
    View.renderUndoStatus currentModel
    View.renderCommandPalette currentModel applyOp
    View.renderCssClassPrompt currentModel applyOp

and dispatch (msg: Msg) : unit =
    currentModel <- update msg currentModel dispatch

    match msg with
    | SysMsg (StateLoaded _) ->
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
            let effects =
                restoredPending
                |> List.tryHead
                |> Option.map (fun head -> [ Update.SubmitHeadChange (serverRev, head) ])
                |> Option.defaultValue []
            Update.runClientEffects effects dispatch
            currentModel <-
                { currentModel with
                    graph = localGraph
                    syncInfo =
                        currentModel.syncInfo
                        |> SyncInfo.withPendingChanges restoredPending
                        |> SyncInfo.withSyncState (Sending 1)
                        |> SyncInfo.withSubmitInFlight (not effects.IsEmpty) }
        elementCache <- render currentModel applyOp dispatch
        View.renderUndoStatus currentModel
        View.renderCommandPalette currentModel applyOp
        View.renderCssClassPrompt currentModel applyOp
    | SysMsg (SubmitResponse _) ->
        View.renderSyncChrome currentModel dispatch
    | SysMsg SubmitRejected ->
        View.renderSyncChrome currentModel dispatch
    | SysMsg SubmitNetworkError ->
        let maxAutoRetries = 10
        match currentModel.syncInfo.syncState with
        | WaitingToRetry n when n < maxAutoRetries ->
            let delaySec = min 60 (1 <<< (min n 6))
            setTimeout (fun () -> applyOp (retryPendingOp false)) (delaySec * 1000) |> ignore
        | _ -> ()
        View.renderSyncChrome currentModel dispatch
    | SysMsg (SetPollingActive _) ->
        View.renderSyncChrome currentModel dispatch
    | SysMsg (SetSyncState _) ->
        View.renderSyncChrome currentModel dispatch
    | AckSyncRisk ->
        View.renderSyncChrome currentModel dispatch

// ---------------------------------------------------------------------------
// Polling
// ---------------------------------------------------------------------------

let pollForRemoteChanges () : unit =
    let now = nowMs ()
    let hidden = isDocumentHidden ()
    let idleForMs = now - lastActivityMs
    let shouldPoll = (not hidden) && idleForMs < idleTimeoutMs

    if not shouldPoll then
        if currentModel.syncInfo.isPollingActive then
            dispatch (SysMsg (SetPollingActive false))
    else
        if not currentModel.syncInfo.isPollingActive then
            dispatch (SysMsg (SetPollingActive true))
        // Guard: only fire GET when the queue is clear and no request is in-flight
        if not currentModel.syncInfo.submitInFlight
           && not currentModel.syncInfo.pollInFlight
           && currentModel.syncInfo.pendingChanges.IsEmpty
           && currentModel.syncInfo.syncState = Idle then
            currentModel <-
                { currentModel with
                    syncInfo = SyncInfo.withPollInFlight true currentModel.syncInfo }
            let url = $"/{Update.currentFile}/poll?_={now}"
            fetchTextNoCacheWithFail url
                (fun text ->
                    currentModel <-
                        { currentModel with
                            syncInfo = SyncInfo.withPollInFlight false currentModel.syncInfo }
                    match Serialization.decodePollResponse text with
                    | Ok poll ->
                        let context =
                            { ClientPollContext.buildEpochSec = readBuildEpochSec ()
                              pageBuildEpochSec = readPageBuildEpochSec () }
                        match SyncLogic.getPollOutcome poll currentModel.revision.Value context with
                        | Some state -> dispatch (SysMsg (SetSyncState state))
                        | None -> ()
                    | Error _ -> ())
                (fun () ->
                    currentModel <-
                        { currentModel with
                            syncInfo = SyncInfo.withPollInFlight false currentModel.syncInfo })

let recordActivity (wakeIfInactive: bool) : unit =
    lastActivityMs <- nowMs ()
    if wakeIfInactive && not (isDocumentHidden ()) && not currentModel.syncInfo.isPollingActive then
        dispatch (SysMsg (SetPollingActive true))
        pollForRemoteChanges ()

let wakePolling () : unit =
    lastActivityMs <- nowMs ()
    if not currentModel.syncInfo.isPollingActive then
        dispatch (SysMsg (SetPollingActive true))
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
            if currentModel.syncInfo.isPollingActive then
                dispatch (SysMsg (SetPollingActive false))
        else
            recordActivity false
            pollForRemoteChanges ())
