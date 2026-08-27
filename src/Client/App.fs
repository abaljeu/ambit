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
open Gambol.Client.UpdateWorkspaceSync
open Gambol.Client.UpdateWorkspaceDownload
open Gambol.Client.UpdateWorkspaceLoad
open Gambol.Client.UpdateImport
open Gambol.Client.Controller
open Gambol.Client.CommandDock
open Gambol.Client.Overlays
open Gambol.Client.View
open Gambol.Client.SearchDialogView
open Gambol.Client.FileSearchDialogView
open Gambol.Client.JsInterop
open Gambol.Client.AutoDownloadTimer
open Gambol.Client.SessionState

// ---------------------------------------------------------------------------
// MVU runtime (factory: model cell + effect interpreter)
// ---------------------------------------------------------------------------

module private SubmitChangeCallbacks =
    open Gambol.Shared.LogText
    open Gambol.Shared.ViewModel
    open Gambol.Client.JsInterop
    open Gambol.Client.Update

    let onPostOk
        (timeoutId: float)
        (reqId: string)
        (submitted: PendingChange list)
        (dispatch: Msg -> unit)
        (text: string)
        : unit =
        clearTimeout timeoutId
        let n = text.Length
        match decodeChangeSuccessResponse text with
        | Ok ack ->
            consoleLog (
                "[Gambol sync] POST 200 req=" + reqId
                + " ackRev=" + string ack.revision.Value
                + " bodyLen=" + string n)
            dispatch (
                SysMsg (
                    SubmitResponse (
                        submitted,
                        ack.changes,
                        ack.revision,
                        ack.externalChanges,
                        ack.message)))
        | Error err ->
            consoleLog (
                "[Gambol sync] POST 200 bad ACK JSON req=" + reqId
                + " err=" + err + " bodyLen=" + string n)
            dispatch (SysMsg (SubmitRejected ("ACK decode: " + err)))

    let onPostHttp (timeoutId: float) (reqId: string) (dispatch: Msg -> unit) (httpStatus: int) (bodyText: string) : unit =
        clearTimeout timeoutId
        let snippet = summarizeHttpBody 400 bodyText
        consoleLog (
            "[Gambol sync] GAMBOL_HTTP_ERR POST fail req=" + reqId
            + " http=" + string httpStatus + " body=" + snippet)
        let detail =
            decodePostChangeError bodyText
            |> Option.map (summarizeHttpBody 400)
            |> Option.defaultValue (summarizeHttpBody 400 bodyText)
        dispatch (SysMsg (SubmitRejected detail))

    let onPostFetchFail
        (timeoutId: float)
        (reqId: string)
        (baseRev: int)
        (changes: PendingChange list)
        (dispatch: Msg -> unit)
        ()
        : unit =
        clearTimeout timeoutId
        consoleLog ("[Gambol sync] POST fetch failed req=" + reqId)
        dispatch (
            SysMsg (
                SubmitNetworkError (
                    baseRev,
                    changes,
                    SubmitNetworkErrorKind.FetchFailed)))

// Idle/pause remote polling after a period of no user interaction (battery-friendly).
let idleTimeoutMs = 15 * 60 * 1000

let createRuntime (initialModel: VM) =
    let mutable model = initialModel
    let mutable elementCache = Map.empty<SiteId, HTMLElement>
    let mutable lastActivityMs = nowMs ()
    let mutable retryTimeoutId: float option = None
    let mutable autoDownloadTimeoutId: float option = None

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
        let localState, restoredPending =
            SyncPlanner.restorePending
                restored.revision
                saved
                { graph = restored.graph
                  history = History.empty
                  revision = restored.revision }
        savePendingQueue restoredPending
        if restoredPending.IsEmpty then
            { restored with
                graph = localState.graph }, []
        else
            consoleLog (
                "[Gambol sync] StateLoaded firePending serverRev=" + string serverRev
                + " restoredQLen=" + string restoredPending.Length)
            let submitEffects =
                [ SubmitPendingBatch (serverRev, restoredPending) ]
            { restored with
                graph = localState.graph
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
        | LoadServer (rev, targets) ->
            runLoadServer rev targets
        | ScheduleRetry delayMs -> runScheduleRetry delayMs
        | RunQueuedRequest QueuedLoad -> dispatch (ApplyOp loadOp)
        | RunQueuedRequest (QueuedWorkspacePush (scope, parseFileId)) ->
            dispatch (ApplyOp (startWorkspacePush scope parseFileId))
        | SavePendingQueue q -> runSavePendingQueue q
        | RequestDesktopFileStatus (nodeId, path) -> runDesktopFileStatus nodeId path
        | RequestServerFileStatus (nodeId, path) -> runServerFileStatus nodeId path
        | RequestWorkspacePathSyncSnapshot -> runWorkspacePathSyncSnapshot ()
        | ContinueWorkspaceStubsThenPush (scope, parseFileId) ->
            // Delay past the current frame so Uploading can paint, then async inventory.
            setTimeout
                (fun () ->
                    let body = encodeWorkspaceInventoryBody scope
                    postJson
                        "/_desktop/workspace-inventory"
                        body
                        (fun text ->
                            dispatch (
                                ApplyOp (
                                    completeUploadInventory
                                        scope
                                        parseFileId
                                        text)))
                        (fun status text ->
                            dispatch (
                                ApplyOp (
                                    failWorkspacePushHttp status text)))
                        (fun () ->
                            dispatch (
                                ApplyOp (
                                    failWorkspacePush
                                        "workspace-inventory request failed")))
                        (jsonMutatingPostHeaders ()))
                50
            |> ignore
        | ContinuePostUploadStructure (submitted, scope, parseFileId) ->
            // Stubs already in the model (DOM patched before effects). Async POST.
            let body =
                SyncBatch.toWireBatch model.revision.Value [ submitted ]
                |> encodePendingBatchBody
            let url = sprintf "/%s/changes" currentFile
            let rec post () =
                let retry () =
                    setTimeout post 1000 |> ignore

                postJson
                    url
                    body
                    (fun text ->
                        dispatch (
                            ApplyOp (
                                completeUploadStructurePost
                                    submitted
                                    scope
                                    parseFileId
                                    text)))
                    (fun status text ->
                        if status = 408 || status = 502 || status = 504 then
                            retry ()
                        else
                            dispatch (
                                ApplyOp (
                                    failUploadStructurePostHttp status text)))
                    retry
                    (jsonMutatingPostHeaders ())

            // A timed-out POST may still commit. Retrying the same changeId is
            // idempotent and recovers its authoritative ACK.
            post ()
        | ContinueWorkspacePush (scope, parseFileId) ->
            // Ensure-map may sync-dialog; heavy WebDAV push must use async fetch.
            setTimeout
                (fun () ->
                    match tryPrepareWorkspacePushBody scope with
                    | Error "cancelled" ->
                        dispatch (ApplyOp cancelWorkspacePush)
                    | Error e ->
                        dispatch (ApplyOp (failWorkspacePush e))
                    | Ok body ->
                        postJson
                            "/_desktop/workspace-push"
                            body
                            (fun text ->
                                dispatch (
                                    ApplyOp (
                                        completeWorkspacePush
                                            scope
                                            parseFileId
                                            text)))
                            (fun status text ->
                                dispatch (
                                    ApplyOp (
                                        failWorkspacePushHttp status text)))
                            (fun () ->
                                dispatch (
                                    ApplyOp (
                                        failWorkspacePush
                                            "workspace-push request failed")))
                            (jsonMutatingPostHeaders ()))
                50
            |> ignore
        | ContinueWorkspaceDownload jobId ->
            setTimeout
                (fun () ->
                    let url =
                        "/_desktop/workspace-download?id="
                        + encodeUriComponent jobId
                    fetchTextNoCacheWithFail
                        url
                        (fun text ->
                            dispatch (
                                ApplyOp (pollWorkspaceDownloadJob jobId text)))
                        (fun () ->
                            dispatch (
                                ApplyOp (
                                    failWorkspaceDownload
                                        "workspace-download poll failed"))))
                200
            |> ignore
        | ContinueDirectoryReconcile scope ->
            setTimeout
                (fun () ->
                    let body =
                        encodeReconciliationDirectoryRequest
                            scope.label
                            scope.relative
                    postJson
                        "/ambit/workspace/reconciliation/directory"
                        body
                        (fun text ->
                            dispatch (
                                ApplyOp (completeDirectoryReconcile text)))
                        (fun status text ->
                            dispatch (
                                ApplyOp (
                                    failDirectoryReconcileHttp status text)))
                        (fun () ->
                            dispatch (
                                ApplyOp (
                                    failDirectoryReconcile
                                        "reconcile request failed")))
                        (jsonMutatingPostHeaders ()))
                50
            |> ignore
        | ContinueParseFile (fileId, path, prefix, detail) ->
            runParseFile (fileId, path, prefix, detail) ignore
        | ContinueUploadParses requests ->
            let rec runNext = function
                | [] -> ()
                | ContinueParseFile (fileId, path, prefix, detail) :: rest ->
                    runParseFile
                        (fileId, path, prefix, detail)
                        (fun () -> runNext rest)
                | _ :: rest -> runNext rest
            runNext requests
        | ScheduleAutoDownloadTick delayMs ->
            runScheduleAutoDownloadTick delayMs

    and runScheduleAutoDownloadTick (delayMs: int) : unit =
        AutoDownloadTimer.armOrRearm
            (fun () -> autoDownloadTimeoutId)
            (fun id -> autoDownloadTimeoutId <- id)
            delayMs
            (fun () -> dispatch (SysMsg AutoDownloadTick))

    and runParseFile
        (fileId, desktopReadPath, detailPrefix, detailPath)
        (afterSuccess: unit -> unit)
        =
        let runPost textOpt =
            let body = encodeParseFileRequest fileId textOpt
            postJson
                "/ambit/file/parse"
                body
                (fun text ->
                    dispatch (
                        ApplyOp (
                            completeParseFilePost detailPrefix detailPath text))
                    afterSuccess ())
                (fun status text ->
                    dispatch (ApplyOp (failParseFileHttp status text)))
                (fun () ->
                    dispatch (ApplyOp (failParseFile "parse request failed")))
                (jsonMutatingPostHeaders ())

        let proceedAfterDesktopRead status responseText =
            match decodeDesktopReadForParse status responseText with
            | Error err -> dispatch (ApplyOp (failParseFile err))
            | Ok textOpt ->
                match validateParseTextOpt textOpt with
                | Error err -> dispatch (ApplyOp (failParseFile err))
                | Ok valid -> runPost valid

        setTimeout
            (fun () ->
                match desktopReadPath with
                | Some path ->
                    let url =
                        "/_desktop/file?path="
                        + encodeUriComponent path
                        + "&content=1"
                    fetchGet
                        url
                        (fun text -> proceedAfterDesktopRead 200 text)
                        proceedAfterDesktopRead
                        (fun () ->
                            dispatch (
                                ApplyOp (
                                    failParseFile "desktop file read failed")))
                | None -> runPost None)
            50
        |> ignore

    and runSubmitPendingBatch (baseRev: int) (changes: PendingChange list) : unit =
        let reqId =
            changes
            |> List.tryHead
            |> Option.map (fun item ->
                item.change.changeId.ToString("N").Substring(0, 8))
            |> Option.defaultValue "empty"
        let url = $"/{currentFile}/changes"
        let postChanges = SyncBatch.toWireBatch baseRev changes
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
            (SubmitChangeCallbacks.onPostOk timeoutId reqId changes dispatch)
            (SubmitChangeCallbacks.onPostHttp timeoutId reqId dispatch)
            (SubmitChangeCallbacks.onPostFetchFail timeoutId reqId baseRev changes dispatch)
            (jsonMutatingPostHeaders ())

    and runPollServer () : unit =
        let url =
            $"/{currentFile}/poll?_={nowMs ()}&rev={model.revision.Value}"
        let onPollOk (text: string) : unit =
            match ApiResponseSerialization.decodeChangeSuccessResponse text with
            | Ok poll ->
                let context =
                    { ClientPollContext.buildEpochSec = readBuildEpochSec ()
                      pageBuildEpochSec = readPageBuildEpochSec () }
                let outcome =
                    SyncLogic.getPollOutcome poll model.revision.Value context
                dispatch (
                    SysMsg (
                        PollDone (
                            outcome,
                            poll.changes,
                            Some poll.isReady,
                            Some poll.revision)))
            | Error _ ->
                dispatch (
                    SysMsg (
                        PollDone (None, [], None, None)))
        let onPollFail () : unit =
            dispatch (
                SysMsg (
                    PollDone (None, [], None, None)))
        fetchTextNoCacheWithFail url onPollOk onPollFail

    and runLoadServer
        (revision: int)
        (targets: LoadTarget list)
        : unit =
        let url = $"/{currentFile}/load"
        let body =
            Thoth.Json.JavaScript.Encode.toString 0 (
                ApiResponseSerialization.encodeLoadRequest
                    { revision = revision
                      targets = targets })
        let onLoadOk (text: string) : unit =
            match ApiResponseSerialization.decodeLoadResponse text with
            | Ok load ->
                let context =
                    { ClientPollContext.buildEpochSec = readBuildEpochSec ()
                      pageBuildEpochSec = readPageBuildEpochSec () }
                let outcome =
                    SyncLogic.getPollOutcome
                        (SyncLogic.loadResponseToPoll load)
                        model.revision.Value
                        context
                dispatch (
                    SysMsg (
                        LoadDone (
                            outcome,
                            SyncLogic.loadResponseToSync load,
                            load.revision,
                            Some load.isReady)))
            | Error _ ->
                dispatch (
                    SysMsg (
                        LoadDone (
                            None,
                            { changes = []; packages = [] },
                            model.revision.Value,
                            None)))
        let onLoadHttp (_status: int) (_body: string) : unit =
            dispatch (
                SysMsg (
                    LoadDone (
                        None,
                        { changes = []; packages = [] },
                        model.revision.Value,
                        None)))
        let onLoadFail () : unit =
            dispatch (
                SysMsg (
                    LoadDone (
                        None,
                        { changes = []; packages = [] },
                        model.revision.Value,
                        None)))
        postJson
            url
            body
            onLoadOk
            onLoadHttp
            onLoadFail
            (jsonMutatingPostHeaders ())

    and runScheduleRetry (delayMs: int) : unit =
        clearRetryTimer ()
        retryTimeoutId <-
            Some (
                setTimeout
                    (fun () ->
                        retryTimeoutId <- None
                        dispatch (SysMsg RetrySubmit))
                    delayMs)

    and runSavePendingQueue (q: PendingChange list) : unit =
        savePendingQueue q

    and runDesktopFileStatus (nodeId: NodeId) (path: string) : unit =
        runFileStatusEndpoint "/_desktop/file-status" "desktop" nodeId path

    and runServerFileStatus (nodeId: NodeId) (path: string) : unit =
        runFileStatusEndpoint ($"/{currentFile}/file-status") "server" nodeId path

    and runWorkspacePathSyncSnapshot () : unit =
        let status, text = getJsonSync "/_desktop/workspace-mappings"
        if status < 200 || status >= 300 then
            consoleLog (
                "[Gambol sync] workspace-mappings HTTP "
                + string status)
        else
            match decodeMappedRoots text with
            | Error err ->
                consoleLog (
                    "[Gambol sync] workspace-mappings decode: " + err)
            | Ok entries ->
                let labels = entries |> List.map fst |> Set.ofList
                let rootsByLabel =
                    entries
                    |> List.map (fun (l, p) -> l.Trim().ToLowerInvariant(), p)
                    |> Map.ofList
                let factsByLabel =
                    labels
                    |> Set.toList
                    |> List.fold
                        (fun acc label ->
                            let body =
                                encodeWorkspaceSyncLedgerRequest label
                            let st, bodyText =
                                postJsonSync
                                    "/_desktop/workspace-sync-ledger"
                                    body
                                    (jsonMutatingPostHeaders ())
                            if st < 200 || st >= 300 then acc
                            else
                                match
                                    decodeWorkspaceSyncLedgerResponse bodyText
                                with
                                | Ok resp when resp.mapped ->
                                    let byRel =
                                        resp.rows
                                        |> List.map (fun r -> r.relative, r)
                                        |> Map.ofList
                                    Map.add label byRel acc
                                | _ -> acc)
                        Map.empty
                dispatch (
                    SysMsg (
                        WorkspacePathSyncSnapshotReceived (
                            labels, factsByLabel, rootsByLabel)))

    and runFileStatusEndpoint
        (url: string)
        (source: string)
        (nodeId: NodeId)
        (path: string)
        : unit =
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
                consoleLog ("[Gambol " + source + "] file-status decode failed: " + err)
        let onHttpError (status: int) (text: string) : unit =
            consoleLog (
                "[Gambol " + source + "] file-status HTTP "
                + string status + ": " + LogText.summarizeHttpBody 200 text)
        let onNetworkFail () : unit =
            consoleLog ("[Gambol " + source + "] file-status request failed")
        postJson
            url
            body
            onOk
            onHttpError
            onNetworkFail
            (jsonMutatingPostHeaders ())

    and dispatch (msg: Msg) : unit =
        let prev = model

        let stateLoadedStart =
            match msg with
            | SysMsg (StateLoaded _) -> Some (perfNowMs ())
            | _ -> None

        let baseModel, baseEffects =
            match msg with
            | SysMsg (StateLoaded _) ->
                let baseModel, e0 = update msg prev
                let restoreStart = perfNowMs ()
                let restored = restoreSessionState baseModel
                consoleLog (
                    $"[Gambol boot] restoreSessionState: {int (perfNowMs () - restoreStart)}ms")
                let merged, e1 = mergePendingAfterLoad restored
                merged, e0 @ e1
            | SysMsg (SubmitResponse (submitted, confirmed, _, _, _)) ->
                clearRetryTimer ()
                let next, effects = update msg prev
                let pendingLen = prev.syncInfo.pendingChanges.Length
                let nextLen = next.syncInfo.pendingChanges.Length
                let pendingDropped = pendingLen > nextLen
                let rejected =
                    match next.syncInfo.syncState with
                    | ServerRejected -> true
                    | _ -> false
                if pendingDropped && not rejected then
                    BootCacheStore.appendChanges
                        currentFile
                        (BootCache.acceptedForLog confirmed submitted)
                    BootCacheStore.requestIdleTruncate
                        currentFile
                        (BootCache.scopeKey (tryReadSavedZoomId ()))
                        (tryReadSavedZoomId ())
                        next.revision.Value
                        next.syncInfo.isServerReady
                        next.graph
                next, effects
            | _ ->
                update msg prev

        let indicatorModel, indicatorEffects =
            ViewModel.refreshDesktopFileIndicator baseModel
        let releasedSync, releaseEffects =
            SyncPlanner.tryReleaseQueued indicatorModel.syncInfo
        let newModel = { indicatorModel with syncInfo = releasedSync }
        let effects = baseEffects @ indicatorEffects @ releaseEffects

        model <- newModel
        try
            try
                elementCache <-
                    match msg with
                    | SysMsg (StateLoaded _) ->
                        let renderStart = perfNowMs ()
                        let cache = render newModel dispatch
                        let visibleRows =
                            ViewModel.getVisibleInstanceIds newModel.siteMap
                            |> List.length
                        consoleLog (
                            $"[Gambol boot] View.render: {int (perfNowMs () - renderStart)}ms, "
                            + $"{visibleRows} rows")
                        match stateLoadedStart with
                        | Some start ->
                            consoleLog (
                                $"[Gambol boot] StateLoaded dispatch total: "
                                + $"{int (perfNowMs () - start)}ms")
                        | None -> ()
                        cache
                    | NodeSearchQuery _ | FileSearchQuery _ ->
                        elementCache
                    | _ ->
                        patchDOM prev newModel dispatch elementCache
                renderCommandButtons newModel dispatch
                renderCommandPalette newModel dispatch
                renderSearchDialog newModel dispatch
                renderFileSearchDialog newModel dispatch
                renderCssClassPrompt newModel dispatch
                renderRenamePrompt newModel dispatch
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

    let interactiveChromeSelector =
        "button,input,a,.amb-dialog,#sync-status,#cmd-last-result"

    let dismissOnBackground (ev: Event) : unit =
        let target = ev.target :?> HTMLElement
        match (getModel ()).mode with
        | CommandPalette _ | SearchDialog _ | FileSearchDialog _ | CssClassPrompt _ | RenamePrompt _ ->
            match target.closest interactiveChromeSelector with
            | Some _ -> ()
            | None -> dispatch (ApplyOp closeActiveOverlayOp)
        | Editing _ ->
            match target.closest interactiveChromeSelector with
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

    let cmdLastResult = document.getElementById "cmd-last-result"
    if not (isNull cmdLastResult) then
        cmdLastResult.setAttribute("title", "Copy command result")
        cmdLastResult.addEventListener(
            "click",
            fun _ -> copyCmdLastResult (getModel ()).lastCmdResult)

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
