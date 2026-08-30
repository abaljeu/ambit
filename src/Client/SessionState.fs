module Gambol.Client.SessionState

open Thoth.Json.Core
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.JsInterop

// ---------------------------------------------------------------------------
// Session state: persist zoom root + fold state across browser-initiated reloads
// (e.g. iOS Safari tab eviction). Written to sessionStorage and localStorage on
// visibility hide; read sessionStorage first, then localStorage. Restored once
// after StateLoaded, before the first render.
// ---------------------------------------------------------------------------

let private sessionKey = "gambol-session-v1"

let private tryParseNodeId (s: string) : NodeId option =
    match System.Guid.TryParse(s) with
    | true, g -> Some(NodeId g)
    | _ -> None

/// getItem can throw (e.g. SecurityError when storage is blocked).
let private tryGetItem (get: string -> string) : string option =
    try
        let json = get sessionKey
        if isNull json || json = "" then None else Some json
    with _ -> None

let private tryReadSessionJson () : string option =
    match tryGetItem sessionGet with
    | Some json -> Some json
    | None -> tryGetItem localStorageGet

/// Best-effort bootstrap widen id (before /state).
/// Prefers `b` (widen); falls back to `z` for older session payloads.
/// Reads sessionStorage first, then localStorage.
let tryReadSavedZoomId () : NodeId option =
    match tryReadSessionJson () with
    | None -> None
    | Some json ->
        try
            let decoder =
                Decode.object (fun get ->
                    get.Optional.Field "b" Decode.string,
                    get.Optional.Field "z" Decode.string)
            match Thoth.Json.JavaScript.Decode.fromString decoder json with
            | Error _ -> None
            | Ok (b, z) ->
                b
                |> Option.bind tryParseNodeId
                |> Option.orElse (z |> Option.bind tryParseNodeId)
        with _ -> None

/// Snapshot the session-specific parts of the VM to sessionStorage and localStorage.
let saveSessionState (model: VM) : unit =
    let foldSnapshots = ViewModel.captureFoldOccurrences model.siteMap
    let focusId =
        match model.selectedNodes with
        | Some sel -> Some (ViewModel.focusedNodeId model.graph sel)
        | None -> None
    // `z` = UI zoom restore; `b` = `/state?zoom=` widen (may be focus when zoom
    // stays in-ROOT so F5 still Loads the owning Workspace).
    let (NodeId zg), (NodeId bg) =
        ResidentProjection.sessionTargets model.graph model.zoomRoot focusId
    let zoomJson = "\"" + zg.ToString() + "\""
    let bootJson = "\"" + bg.ToString() + "\""
    let foldJson =
        foldSnapshots
        |> List.map (fun snap ->
            let parentJson =
                match snap.parentIndex with
                | None -> "null"
                | Some i -> string i
            sprintf "{\"p\":%s,\"i\":%d,\"n\":\"%s\"}"
                parentJson snap.childIndex (snap.nodeId.Value.ToString()))
        |> String.concat ","
    let payload = sprintf "{\"z\":%s,\"b\":%s,\"f\":[%s]}" zoomJson bootJson foldJson
    sessionSet sessionKey payload
    try localStorageSet sessionKey payload with _ -> ()

/// Restore zoom root and fold state into a freshly-loaded VM.
/// Called once immediately after StateLoaded, before the initial render.
/// Uses `z` only — bootstrap widen (`b`) must not change UI zoom.
/// Reads sessionStorage first, then localStorage.
/// Legacy `e: string[]` payloads are ignored (all appearances stay collapsed).
let restoreSessionState (model: VM) : VM =
    match tryReadSessionJson () with
    | None -> model
    | Some json ->
        try
            let foldDecoder =
                Decode.object (fun get ->
                    let p = get.Optional.Field "p" Decode.int
                    let i = get.Required.Field "i" Decode.int
                    let n = get.Required.Field "n" Decode.string
                    p, i, n)
            let decoder =
                Decode.object (fun get ->
                    let z = get.Optional.Field "z" Decode.string
                    let f =
                        get.Optional.Field "f" (Decode.list foldDecoder)
                        |> Option.defaultValue []
                    z, f)
            match Thoth.Json.JavaScript.Decode.fromString decoder json with
            | Error _ -> model
            | Ok (zoomStr, foldPairs) ->
                let zoomRoot =
                    zoomStr
                    |> Option.bind tryParseNodeId
                    |> Option.map (fun nid ->
                        ViewModel.resolveZoomRoot model.graph nid)
                    |> Option.defaultValue model.zoomRoot

                let siteMap0, nextId0 =
                    if zoomRoot <> model.zoomRoot then
                        ViewModel.buildSiteMapFrom model.graph zoomRoot (Sid 0)
                    else
                        model.siteMap, model.nextSiteId
                let foldSnapshots =
                    foldPairs
                    |> List.choose (fun (p, i, n) ->
                        tryParseNodeId n
                        |> Option.map (fun nodeId ->
                            { parentIndex = p
                              childIndex = i
                              nodeId = nodeId }))
                let siteMap1, nextId1 =
                    ViewModel.restoreFoldOccurrences
                        foldSnapshots model.graph siteMap0 nextId0
                { model with
                    zoomRoot = zoomRoot
                    siteMap = siteMap1
                    nextSiteId = nextId1
                    zoomIngress = ViewModel.ownerPathIngress model.graph zoomRoot }
        with _ -> model
