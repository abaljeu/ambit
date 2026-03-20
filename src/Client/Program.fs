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

[<Emit("fetch($0, {cache: 'no-store', credentials: 'same-origin'}).then(r => r.ok ? r.text() : Promise.reject(r.status)).then($1).catch(function(){})")>]
let private fetchTextNoCache (url: string) (callback: string -> unit) : unit = jsNative

[<Emit("Date.now()")>]
let private nowMs () : int = jsNative

[<Emit("(typeof window.__BUILD_TS__ !== 'undefined' ? window.__BUILD_TS__ : 0)")>]
let private readBuildEpochSec () : int = jsNative

[<Emit("(function(epochSec){
    var d = new Date(epochSec*1000);
    var parts = new Intl.DateTimeFormat('en-CA', {
        timeZone: 'America/Toronto',
        year: 'numeric', month: '2-digit', day: '2-digit',
        hour: '2-digit', minute: '2-digit', second: '2-digit',
        hour12: false
    }).formatToParts(d);
    var m = {};
    parts.forEach(function(p){ if(p.type !== 'literal') m[p.type] = p.value; });
    return m.year + '-' + m.month + '-' + m.day + ' ' + m.hour + ':' + m.minute + ':' + m.second + ' ET';
})($0)")>]
let private epochSecToTorontoString (epochSec: int) : string = jsNative

[<Emit("(typeof window.__PAGE_BUILD_TS__ !== 'undefined' ? window.__PAGE_BUILD_TS__ : 0)")>]
let private readPageBuildEpochSec () : int = jsNative

[<Emit("sessionStorage.getItem($0)")>]
let private sessionGet (key: string) : string = jsNative

[<Emit("sessionStorage.setItem($0, $1)")>]
let private sessionSet (key: string) (value: string) : unit = jsNative

[<Emit("document.hidden")>]
let private isDocumentHidden () : bool = jsNative

[<Emit("setInterval($0, $1)")>]
let private setInterval (f: unit -> unit) (ms: int) : int = jsNative

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
      pendingChanges = []
      syncState = Synced }

/// Element cache: instanceId → DOM row element.  Populated on first StateLoaded.
let mutable elementCache: Map<int, HTMLElement> = Map.empty

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

    // Server build timestamp (injected by Server.fs into gambol page HTML as window.__BUILD_TS__).
    let buildEl = document.getElementById "server-build-stamp"
    if isNull buildEl then () else
        let stampEpochSec = readBuildEpochSec ()
        let txt =
            if stampEpochSec <= 0 then "Server build: (unknown)"
            else "Server build: " + epochSecToTorontoString stampEpochSec
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
        | _ -> applyOp retryPendingOp)

let rec applyOp (op: Op) : unit =
    let prevModel = currentModel
    currentModel <- op currentModel dispatch
    elementCache <- patchDOM prevModel currentModel applyOp elementCache
    View.renderUndoStatus currentModel
    View.renderCommandPalette currentModel applyOp

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
                                syncState = Syncing }
            fireNextPending restoredPending dispatch
        elementCache <- render currentModel applyOp
        View.renderUndoStatus currentModel
        View.renderCommandPalette currentModel applyOp
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
    | System (ServerAhead _) ->
        View.renderStatus currentModel

    | System PollingInactive
    | System PollingActive ->
        View.renderStatus currentModel

// ---------------------------------------------------------------------------
// Bootstrap
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

setupStaticDOM applyOp wakePolling

fetchText $"/{Update.currentFile}/state" (fun text ->
    match decodeStateResponse text with
    | Ok (graph, revision) ->
        dispatch (System (StateLoaded (graph, revision)))
        startPolling ()
    | Error err ->
        app.textContent <- $"Error: {err}"
)
