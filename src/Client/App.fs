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
open Gambol.Client.FileSearchDialogView
open Gambol.Client.WorkspaceConnectView
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
            dispatch (SysMsg (SubmitResponse (ack.ackedChangeIds, ack.revision)))
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

    let onPostFetchFail
        (timeoutId: float)
        (reqId: string)
        (baseRev: int)
        (changes: Change list)
        (dispatch: Msg -> unit)
        ()
        : unit =
        clearTimeout timeoutId
        consoleLog ("[Gambol sync] POST fetch failed req=" + reqId)
        dispatch (SysMsg (SubmitNetworkError (baseRev, changes, SubmitNetworkErrorKind.FetchFailed)))

// Idle/pause remote polling after a period of no user interaction (battery-friendly).
let idleTimeoutMs = 15 * 60 * 1000

let createRuntime (initialModel: VM) =
    let mutable model = initialModel
    let mutable elementCache = Map.empty<SiteId, HTMLElement>
    let mutable lastActivityMs = nowMs ()
    let mutable retryTimeoutId: float option = None

    let clearRetryTimer () =
        match retryTimeoutId with
        | Some id ->
            clearTimeout id
            retryTimeoutId <- None
        | None -> ()

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
                [ SubmitPendingBatch (serverRev, restoredPending) ]
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
        | SubmitPendingBatch (baseRev, changes) -> runSubmitPendingBatch baseRev changes
        | PollServer _ -> runPollServer ()
        | ScheduleRetry delayMs -> runScheduleRetry delayMs
        | SavePendingQueue q -> runSavePendingQueue q
        | RequestDesktopFileStatus (nodeId, path) -> runDesktopFileStatus nodeId path
        | RequestSyncTreeListing nodeId -> runSyncTreeListing nodeId
        | RequestParseFile (nodeId, forceReparse) -> runParseFile nodeId forceReparse
        | RequestWorkspaceConnect (gitRoot, label, _workspaceOps, initialSync, gatewayUrl) ->
            runWorkspaceConnect gitRoot label initialSync gatewayUrl

    and runSubmitPendingBatch (baseRev: int) (changes: Change list) : unit =
        let reqId =
            changes
            |> List.tryHead
            |> Option.map (fun change -> change.changeId.ToString("N").Substring(0, 8))
            |> Option.defaultValue "empty"
        let url = $"/{currentFile}/changes"
        let postChanges = Gambol.Shared.SyncBatch.toDeltaChain baseRev changes
        let body = encodePendingBatchBody postChanges
        let qLen = model.syncInfo.pendingChanges.Length
        consoleLog (
            "[Gambol sync] POST start req=" + reqId + " baseRev=" + string baseRev
            + " batchLen=" + string changes.Length + " qLen=" + string qLen)
        let timeoutId =
            setTimeout
                (fun () ->
                    consoleLog (
                        "[Gambol sync] POST timeout "
                        + string SyncRetry.postTimeoutMs
                        + "ms req=" + reqId)
                    dispatch (
                        SysMsg (
                            SubmitNetworkError (
                                baseRev, changes, SubmitNetworkErrorKind.ClientTimeout))))
                SyncRetry.postTimeoutMs
        postJson
            url
            body
            (SubmitChangeCallbacks.onPostOk timeoutId reqId dispatch)
            (SubmitChangeCallbacks.onPostHttp timeoutId reqId dispatch)
            (SubmitChangeCallbacks.onPostFetchFail timeoutId reqId baseRev changes dispatch)

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
        clearRetryTimer ()
        retryTimeoutId <-
            Some (
                setTimeout
                    (fun () ->
                        retryTimeoutId <- None
                        dispatch (SysMsg RetrySubmit))
                    delayMs)

    and runSavePendingQueue (q: Change list) : unit =
        savePendingQueue q

    and runDesktopFileStatus (nodeId: NodeId) (path: string) : unit =
        let body = encodeDesktopFileStatusRequest path
        let onOk (text: string) : unit =
            match decodeDesktopFileStatusResponse text with
            | Ok response ->
                dispatch (
                    SysMsg (
                        DesktopFileStatusReceived (
                            nodeId,
                            response.path,
                            response.status,
                            response.sourceModifiedUtc)))
            | Error err ->
                consoleLog ("[Gambol desktop] file-status decode failed: " + err)
        let onHttpError (status: int) (text: string) : unit =
            consoleLog (
                "[Gambol desktop] file-status HTTP "
                + string status + ": " + LogText.truncateForLog 200 text)
        let onNetworkFail () : unit =
            consoleLog "[Gambol desktop] file-status request failed"
        postJson "/_desktop/file-status" body onOk onHttpError onNetworkFail

    and runSyncTreeListing (nodeId: NodeId) : unit =
        let url =
            sprintf "/%s/sync-tree?nodeId=%s" currentFile (nodeId.Value.ToString())
        let onOk (text: string) : unit =
            match Serialization.decodeDiskTreeListingFromString text with
            | Ok entries ->
                dispatch (SysMsg (SyncTreeListingReceived (nodeId, entries)))
            | Error err ->
                dispatch (SysMsg (SyncTreeListingFailed (nodeId, err)))
        let onFail () : unit =
            dispatch (SysMsg (SyncTreeListingFailed (nodeId, "request failed")))
        fetchTextNoCacheWithFail url onOk onFail

    and runWorkspaceConnect
        (gitRoot: string)
        (label: string)
        (initialSync: InitialSyncDirection)
        (gatewayUrl: string)
        : unit =
        let fail detail =
            dispatch (SysMsg (WorkspaceConnectFinished(false, detail)))

        let status, mappingsText = getJsonSync "/_desktop/workspace-mappings"

        if status < 200 || status >= 300 then
            fail "Could not read workspace mappings"
        else
            match Thoth.Json.JavaScript.Decode.fromString Serialization.decodeWorkspaceMappings mappingsText with
            | Error err -> fail ("Mappings decode: " + err)
            | Ok mappings ->
                let merged = WorkspaceLocalMapping.mergeMapping mappings label gitRoot

                let putBody =
                    Serialization.encodeWorkspaceMappings merged
                    |> Thoth.Json.JavaScript.Encode.toString 0

                let putStatus, putText = putJsonSync "/_desktop/workspace-mappings" putBody

                if putStatus < 200 || putStatus >= 300 then
                    fail ("Could not save mapping: " + LogText.truncateForLog 200 putText)
                else
                    let remoteBody =
                        Serialization.encodeDesktopGitRemoteSetupRequest
                            { label = label; url = gatewayUrl }
                        |> Thoth.Json.JavaScript.Encode.toString 0

                    let remoteStatus, remoteText =
                        postJsonSync "/_desktop/git-remote-setup" remoteBody

                    if remoteStatus < 200 || remoteStatus >= 300 then
                        fail ("Remote setup failed: " + LogText.truncateForLog 200 remoteText)
                    else
                        match
                            Thoth.Json.JavaScript.Decode.fromString
                                Serialization.decodeDesktopGitOpResponse
                                remoteText
                        with
                        | Ok { ok = false; detail = Some detail } -> fail detail
                        | Ok { ok = false; detail = None } -> fail "Remote setup failed"
                        | Error err -> fail ("Remote setup decode: " + err)
                        | Ok { ok = true; detail = _ } ->
                            match initialSync with
                            | InitialSyncDirection.Skip ->
                                dispatch (
                                    SysMsg(
                                        WorkspaceConnectFinished(
                                            true,
                                            sprintf "Connected workspace '%s'" label)))
                            | InitialSyncDirection.Download ->
                                let pullBody =
                                    Serialization.encodeDesktopGitLabelRequest { label = label }
                                    |> Thoth.Json.JavaScript.Encode.toString 0

                                let pullStatus, pullText =
                                    postJsonSync "/_desktop/git-pull" pullBody

                                if pullStatus < 200 || pullStatus >= 300 then
                                    fail ("Download failed: " + LogText.truncateForLog 200 pullText)
                                else
                                    match
                                        Thoth.Json.JavaScript.Decode.fromString
                                            Serialization.decodeDesktopGitPullResponse
                                            pullText
                                    with
                                    | Ok { ok = false; detail = Some detail } -> fail detail
                                    | Ok { ok = false; detail = None } -> fail "Download failed"
                                    | Error err -> fail ("Download decode: " + err)
                                    | Ok { ok = true; detail = _ } ->
                                        dispatch (
                                            SysMsg(
                                                WorkspaceConnectFinished(
                                                    true,
                                                    sprintf "Connected and downloaded '%s'" label)))
                            | InitialSyncDirection.Upload ->
                                let pushBody =
                                    Serialization.encodeDesktopGitLabelRequest { label = label }
                                    |> Thoth.Json.JavaScript.Encode.toString 0

                                let pushStatus, pushText =
                                    postJsonSync "/_desktop/git-push" pushBody

                                if pushStatus < 200 || pushStatus >= 300 then
                                    fail ("Upload failed: " + LogText.truncateForLog 200 pushText)
                                else
                                    match
                                        Thoth.Json.JavaScript.Decode.fromString
                                            Serialization.decodeDesktopGitOpResponse
                                            pushText
                                    with
                                    | Ok { ok = false; detail = Some detail } -> fail detail
                                    | Ok { ok = false; detail = None } -> fail "Upload failed"
                                    | Error err -> fail ("Upload decode: " + err)
                                    | Ok { ok = true; detail = _ } ->
                                        dispatch (
                                            SysMsg(
                                                WorkspaceConnectFinished(
                                                    true,
                                                    sprintf "Connected and uploaded '%s'" label)))

    and runParseFile (nodeId: NodeId) (forceReparse: bool) : unit =
        let url =
            sprintf "/%s/parse-file?nodeId=%s" currentFile (nodeId.Value.ToString())
        let onOk (text: string) : unit =
            match Serialization.decodeParseFileResponse text with
            | Ok (relativePath, fileText, mtimeUtc) ->
                dispatch (
                    SysMsg (
                        ParseFileContentReceived (nodeId, relativePath, fileText, mtimeUtc, forceReparse)))
            | Error err ->
                dispatch (SysMsg (ParseFileFailed (nodeId, err)))
        let onFail () : unit =
            dispatch (SysMsg (ParseFileFailed (nodeId, "request failed")))
        fetchTextNoCacheWithFail url onOk onFail

    and dispatch (msg: Msg) : unit =
        let prev = model

        let baseModel, baseEffects =
            match msg with
            | SysMsg (StateLoaded _) ->
                let baseModel, e0 = update msg prev
                let restored = restoreSessionState baseModel
                let merged, e1 = mergePendingAfterLoad restored
                merged, e0 @ e1
            | SysMsg (SubmitResponse _) ->
                clearRetryTimer ()
                update msg prev
            | _ ->
                update msg prev

        let indicatorModel, indicatorEffects =
            ViewModel.refreshDesktopFileIndicator baseModel
        let newModel = indicatorModel
        let effects = baseEffects @ indicatorEffects

        model <- newModel
        try
            try
                elementCache <-
                    match msg with
                    | SysMsg (StateLoaded _) ->
                        render newModel dispatch
                    | _ ->
                        patchDOM prev newModel dispatch elementCache
                renderCommandButtons newModel dispatch
                renderCommandPalette newModel dispatch
                renderSearchDialog newModel dispatch
                renderFileSearchDialog newModel dispatch
                renderCssClassPrompt newModel dispatch
                renderRenamePrompt newModel dispatch
                renderWorkspaceConnectWizard newModel dispatch
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

// let buildEl = document.getElementById "server-build-stamp"
// if isNull buildEl then () else
//     let stampEpochSec = readBuildEpochSec ()
//     let txt =
//         if stampEpochSec <= 0 then "Deploy: (unknown)"
//         else "Deploy: " + epochSecToTorontoString stampEpochSec
//     buildEl.textContent <- txt
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

    let dismissOnBackground (ev: Event) : unit =
        let target = ev.target :?> HTMLElement
        match (getModel ()).mode with
        | CommandPalette _ | SearchDialog _ | FileSearchDialog _ | CssClassPrompt _ | RenamePrompt _
        | WorkspaceConnectWizard _ ->
            match target.closest("button,input,a,.amb-dialog,#sync-status") with
            | Some _ -> ()
            | None -> dispatch (ApplyOp closeActiveOverlayOp)
        | Editing _ ->
            match target.closest("button,input,a,.amb-dialog,#sync-status") with
            | Some _ -> ()
            | None -> dispatch (ApplyOp commitToSelectingOp)
        | _ -> ()

    let ambDoc = document.getElementById "amb-document"
    if not (isNull ambDoc) then
        ambDoc.addEventListener("mousedown", dismissOnBackground)
    if not (isNull app) then
        app.addEventListener("mousedown", dismissOnBackground)

    setupVisualViewportLayout ()

    let basePath =
        let path = window.location.pathname
        if path.StartsWith("/ambit") then "/ambit" else ""

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
