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
// MVU runtime (model cell + effect interpreter)
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

/// Element cache: instanceId → DOM row. Populated on first StateLoaded.
let mutable elementCache: Map<SiteId, HTMLElement> = Map.empty

// Idle/pause remote polling after a period of no user interaction (battery-friendly).
let idleTimeoutMs = 5 * 60 * 1000
let mutable lastActivityMs = nowMs ()

/// Restore local pending queue onto a fresh server snapshot. Returns model + effects.
let private mergePendingAfterLoad (restored: VM) : VM * Effect list =
    lastActivityMs <- nowMs ()
    let saved = loadPendingQueue ()
    let serverRev = restored.revision.Value
    let filtered = saved |> List.filter (fun c -> c.id >= serverRev)
    let localGraph, restoredPending =
        filtered |> List.fold
            (fun (g, acc) c ->
                let s: State =
                    { graph = g
                      history = History.empty
                      revision = Revision 0 }
                match Change.apply c s with
                | ApplyResult.Changed s' -> s'.graph, acc @ [c]
                | _ -> g, acc)
            (restored.graph, [])
    savePendingQueue restoredPending
    if restoredPending.IsEmpty then
        { restored with graph = localGraph }, []
    else
        let submitEffects =
            restoredPending
            |> List.tryHead
            |> Option.map (fun head -> [ SubmitChange (serverRev, head) ])
            |> Option.defaultValue []
        { restored with
            graph = localGraph
            syncInfo =
                restored.syncInfo
                |> SyncInfo.withPendingChanges restoredPending
                |> SyncInfo.withSyncState (Sending 1) },
        submitEffects

let rec applyOp (op: Op) : unit =
    dispatch (ApplyOp op)

and runEffects (effects: Effect list) : unit =
    for e in effects do
        match e with
        | SubmitChange (_, change) ->
            let url = $"/{currentFile}/changes"
            let body = encodeChangeBody change
            postJson url body
                (fun text ->
                    match decodeChangeAckResponse text with
                    | Ok ack ->
                        dispatch (SysMsg (SubmitResponse (ack.ackChangeId, ack.revision)))
                    | Error _ ->
                        dispatch (SysMsg SubmitRejected))
                (fun () -> dispatch (SysMsg SubmitRejected))
                (fun () -> dispatch (SysMsg SubmitNetworkError))
        | PollServer _ ->
            let url = $"/{currentFile}/poll?_={nowMs ()}"
            fetchTextNoCacheWithFail url
                (fun text ->
                    match Serialization.decodePollResponse text with
                    | Ok poll ->
                        let context =
                            { ClientPollContext.buildEpochSec = readBuildEpochSec ()
                              pageBuildEpochSec = readPageBuildEpochSec () }
                        let outcome =
                            SyncLogic.getPollOutcome poll currentModel.revision.Value context
                        dispatch (SysMsg (PollDone outcome))
                    | Error _ ->
                        dispatch (SysMsg (PollDone None)))
                (fun () -> dispatch (SysMsg (PollDone None)))
        | ScheduleRetry delayMs ->
            setTimeout (fun () -> dispatch (SysMsg RetrySubmit)) delayMs |> ignore
        | SavePendingQueue q ->
            savePendingQueue q

and dispatch (msg: Msg) : unit =
    let prevModel = currentModel

    let newModel, effects =
        match msg with
        | SysMsg (StateLoaded _) ->
            let baseModel, e0 = update msg prevModel
            let restored = restoreSessionState baseModel
            let merged, e1 = mergePendingAfterLoad restored
            merged, e0 @ e1
        | _ ->
            update msg prevModel

    currentModel <- newModel
    elementCache <-
        match msg with
        | SysMsg (StateLoaded _) ->
            render currentModel applyOp dispatch
        | _ ->
            patchDOM prevModel currentModel applyOp dispatch elementCache
    renderUndoStatus currentModel
    renderCommandPalette currentModel applyOp
    renderCssClassPrompt currentModel applyOp
    runEffects effects

// ---------------------------------------------------------------------------
// One-time static DOM setup (hidden-input + settings-bar)
// ---------------------------------------------------------------------------

let setupStaticDOM (applyOpArg: Op -> unit) (wakePolling: unit -> unit) : unit =
    let hiddenInput = document.getElementById "hidden-input" :?> HTMLInputElement
    hiddenInput.addEventListener("keydown", fun (ev: Event) ->
        let ke = ev :?> KeyboardEvent
        if ke.key = "Tab" then ev.preventDefault()
        if (ke.ctrlKey || ke.metaKey) && ke.key = "p" && not ke.shiftKey then
            ev.preventDefault()
        handleKey currentModel.mode ke applyOpArg
    )
    hiddenInput.addEventListener("paste", fun ev -> onPaste ev applyOpArg)
    hiddenInput.addEventListener("copy",  fun ev -> onCopy  currentModel ev applyOpArg)
    hiddenInput.addEventListener("cut",   fun ev -> onCut   currentModel ev applyOpArg)

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
            applyOpArg (retryPendingOp true)
        | ServerRejected | CodeOutdated | DataOutdated ->
            let path = window.location.pathname
            window.location.assign(path + "?bust=" + string (nowMs ()))
        | _ -> ()
    )

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
        dispatch (SysMsg PollTick)

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
