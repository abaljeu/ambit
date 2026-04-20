module Gambol.Client.App

open Browser.Dom
open Browser.Types
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client
open Gambol.Client.Update
open Gambol.Client.UpdateCodec
open Gambol.Client.UpdateHelpers
open Gambol.Client.UpdateOps
open Gambol.Client.Controller
open Gambol.Client.View
open Gambol.Client.SearchDialogView
open Gambol.Client.JsInterop
open Gambol.Client.SessionState

// ---------------------------------------------------------------------------
// MVU runtime (factory: model cell + effect interpreter)
// ---------------------------------------------------------------------------

module private SubmitChangeCallbacks =
    open Gambol.Shared.LogText
    open Gambol.Shared.ViewModel
    open Gambol.Client.JsInterop
    open Gambol.Client.Update

    let onPostOk (timeoutId: float) (reqId: string) (dispatch: Msg -> unit) (text: string) : unit =
        clearTimeout timeoutId
        let n = text.Length
        match decodeChangeAckResponse text with
        | Ok ack ->
            consoleLog (
                "[Gambol sync] POST 200 req=" + reqId
                + " ackRev=" + string ack.revision.Value
                + " bodyLen=" + string n)
            dispatch (SysMsg (SubmitResponse (ack.ackChangeId, ack.revision)))
        | Error err ->
            consoleLog (
                "[Gambol sync] POST 200 bad ACK JSON req=" + reqId
                + " err=" + err + " bodyLen=" + string n)
            dispatch (SysMsg (SubmitRejected ("ACK decode: " + err)))

    let onPostHttp (timeoutId: float) (reqId: string) (dispatch: Msg -> unit) (httpStatus: int) (bodyText: string) : unit =
        clearTimeout timeoutId
        let snippet = truncateForLog 400 bodyText
        consoleLog (
            "[Gambol sync] GAMBOL_HTTP_ERR POST fail req=" + reqId
            + " http=" + string httpStatus + " body=" + snippet)
        let detail =
            decodePostChangeError bodyText
            |> Option.defaultValue (truncateForLog 400 bodyText)
        dispatch (SysMsg (SubmitRejected detail))

    let onPostFetchFail (timeoutId: float) (reqId: string) (dispatch: Msg -> unit) () : unit =
        clearTimeout timeoutId
        consoleLog ("[Gambol sync] POST fetch failed req=" + reqId)
        dispatch (SysMsg SubmitNetworkError)

// Idle/pause remote polling after a period of no user interaction (battery-friendly).
let idleTimeoutMs = 15 * 60 * 1000

let createRuntime (initialModel: VM) =
    let mutable model = initialModel
    let mutable elementCache = Map.empty<SiteId, HTMLElement>
    let mutable lastActivityMs = nowMs ()

    /// Restore local pending queue onto a fresh server snapshot. Returns model + effects.
    let mergePendingAfterLoad (restored: VM) : VM * Effect list =
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
                    | ApplyResult.Changed s' -> s'.graph, acc @ [ c ]
                    | _ -> g, acc)
                (restored.graph, [])
        savePendingQueue restoredPending
        if restoredPending.IsEmpty then
            { restored with graph = localGraph }, []
        else
            consoleLog (
                "[Gambol sync] StateLoaded firePending serverRev=" + string serverRev
                + " restoredQLen=" + string restoredPending.Length)
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

    let rec runEffects (effects: Effect list) : unit =
        match effects with
        | [] -> ()
        | e :: rest ->
            runEffect e
            runEffects rest

    and runEffect (e: Effect) : unit =
        match e with
        | SubmitChange (baseRev, change) -> runSubmitChange baseRev change
        | PollServer _ -> runPollServer ()
        | ScheduleRetry delayMs -> runScheduleRetry delayMs
        | SavePendingQueue q -> runSavePendingQueue q

    and runSubmitChange (baseRev: int) (change: Change) : unit =
        let reqId = change.changeId.ToString("N").Substring(0, 8)
        let url = $"/{currentFile}/changes"
        let body = encodeChangeBody { change with id = baseRev }
        let qLen = model.syncInfo.pendingChanges.Length
        consoleLog (
            "[Gambol sync] POST start req=" + reqId + " baseRev=" + string baseRev
            + " qLen=" + string qLen + " headStoredId=" + string change.id)
        let timeoutId =
            setTimeout
                (fun () ->
                    consoleLog ("[Gambol sync] POST timeout 5s req=" + reqId)
                    dispatch (SysMsg SubmitNetworkError))
                5_000
        postJson
            url
            body
            (SubmitChangeCallbacks.onPostOk timeoutId reqId dispatch)
            (SubmitChangeCallbacks.onPostHttp timeoutId reqId dispatch)
            (SubmitChangeCallbacks.onPostFetchFail timeoutId reqId dispatch)

    and runPollServer () : unit =
        let url =
            $"/{currentFile}/poll?_={nowMs ()}&rev={model.revision.Value}"
        let onPollOk (text: string) : unit =
            match Serialization.decodePollResponse text with
            | Ok poll ->
                let context =
                    { ClientPollContext.buildEpochSec = readBuildEpochSec ()
                      pageBuildEpochSec = readPageBuildEpochSec () }
                let outcome =
                    SyncLogic.getPollOutcome poll model.revision.Value context
                dispatch (SysMsg (PollDone (outcome, poll.changes)))
            | Error _ ->
                dispatch (SysMsg (PollDone (None, [])))
        let onPollFail () : unit =
            dispatch (SysMsg (PollDone (None, [])))
        fetchTextNoCacheWithFail url onPollOk onPollFail

    and runScheduleRetry (delayMs: int) : unit =
        setTimeout (fun () -> dispatch (SysMsg RetrySubmit)) delayMs |> ignore

    and runSavePendingQueue (q: Change list) : unit =
        savePendingQueue q

    and dispatch (msg: Msg) : unit =
        let prev = model

        let newModel, effects =
            match msg with
            | SysMsg (StateLoaded _) ->
                let baseModel, e0 = update msg prev
                let restored = restoreSessionState baseModel
                let merged, e1 = mergePendingAfterLoad restored
                merged, e0 @ e1
            | _ ->
                update msg prev

        model <- newModel
        try
            try
                elementCache <-
                    match msg with
                    | SysMsg (StateLoaded _) ->
                        render newModel dispatch
                    | _ ->
                        patchDOM prev newModel dispatch elementCache
                renderUndoStatus newModel
                renderCommandPalette newModel dispatch
                renderSearchDialog newModel dispatch
                renderCssClassPrompt newModel dispatch
            with ex ->
                consoleLog (
                    "[Gambol dispatch] view/render exception: " + ex.Message)
        finally
            renderSyncChrome newModel dispatch
            runEffects effects

    let pollForRemoteChanges () =
        let now = nowMs ()
        let idleForMs = now - lastActivityMs
        let shouldPoll = not (isDocumentHidden ()) && idleForMs < idleTimeoutMs
        if not shouldPoll then
            if model.syncInfo.isPollingActive then
                dispatch (SysMsg (SetPollingActive false))
        else
            if not model.syncInfo.isPollingActive then
                dispatch (SysMsg (SetPollingActive true))
            dispatch (SysMsg PollTick)

    let recordActivity (wakeIfInactive: bool) =
        lastActivityMs <- nowMs ()
        if
            wakeIfInactive
            && not (isDocumentHidden ())
            && not model.syncInfo.isPollingActive
        then
            dispatch (SysMsg (SetPollingActive true))
            pollForRemoteChanges ()

    let wakePolling () =
        lastActivityMs <- nowMs ()
        if not model.syncInfo.isPollingActive then
            dispatch (SysMsg (SetPollingActive true))
        pollForRemoteChanges ()

    document.addEventListener("visibilitychange", fun _ ->
        if isDocumentHidden () then
            saveSessionState model
            if model.syncInfo.isPollingActive then
                dispatch (SysMsg (SetPollingActive false))
        else
            recordActivity false
            pollForRemoteChanges ())
    window.addEventListener("pagehide", fun _ -> saveSessionState model)

    dispatch, (fun () -> model), wakePolling, pollForRemoteChanges, recordActivity

// ---------------------------------------------------------------------------
// One-time static DOM setup (hidden-input + settings-bar)
// ---------------------------------------------------------------------------

let setupStaticDOM (dispatch: Msg -> unit) (getModel: unit -> VM) (_wakePolling: unit -> unit) : unit =
    let hiddenInput = document.getElementById "hidden-input" :?> HTMLInputElement
    hiddenInput.addEventListener("keydown", fun (ev: Event) ->
        let ke = ev :?> KeyboardEvent
        if ke.key = "Tab" then ev.preventDefault()
        if (ke.ctrlKey || ke.metaKey) && ke.key = "p" && not ke.shiftKey then
            ev.preventDefault()
        handleKey (getModel ()).mode ke dispatch
    )
    hiddenInput.addEventListener("paste", fun ev -> onPaste ev dispatch)
    hiddenInput.addEventListener("copy",  fun ev -> onCopy  (getModel ()) ev dispatch)
    hiddenInput.addEventListener("cut",   fun ev -> onCut   (getModel ()) ev dispatch)

    let basePath =
        let path = window.location.pathname
        if path.StartsWith("/ambit") then "/ambit" else ""

    setLastKeyDisplay None None

    let buildEl = document.getElementById "server-build-stamp"
    if isNull buildEl then () else
        let stampEpochSec = readBuildEpochSec ()
        let txt =
            if stampEpochSec <= 0 then "Deploy: (unknown)"
            else "Deploy: " + epochSecToTorontoString stampEpochSec
        buildEl.textContent <- txt

    let syncStatus = document.getElementById "sync-status"
    syncStatus.addEventListener("click", fun _ ->
        match (getModel ()).syncInfo.syncState with
        | WaitingToRetry _ ->
            dispatch (ApplyOp (retryPendingOp true))
        | ServerRejected | CodeOutdated | DataOutdated ->
            let path = window.location.pathname
            window.location.assign(path + "?bust=" + string (nowMs ()))
        | _ -> ()
    )

// ---------------------------------------------------------------------------
// Polling
// ---------------------------------------------------------------------------

let startPolling (pollForRemoteChanges: unit -> unit) (recordActivity: bool -> unit) : unit =
    setInterval pollForRemoteChanges 5000 |> ignore

    document.addEventListener("pointerdown", fun _ -> recordActivity true)
    document.addEventListener("keydown", fun _ -> recordActivity true)
    document.addEventListener("wheel", fun _ -> recordActivity true)
    document.addEventListener("touchstart", fun _ -> recordActivity true)
    window.addEventListener("scroll", fun _ -> recordActivity true)

    window.addEventListener("focus", fun _ ->
        recordActivity false
        pollForRemoteChanges ())
