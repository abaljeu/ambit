module Gambol.Client.SessionState

open Thoth.Json.Core
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.JsInterop

// ---------------------------------------------------------------------------
// Session state: persist zoom root + fold state across browser-initiated reloads
// (e.g. iOS Safari tab eviction). Saved to sessionStorage on visibility hide;
// restored once after StateLoaded, before the first render.
// ---------------------------------------------------------------------------

let private sessionKey = "gambol-session-v1"

let private tryParseNodeId (s: string) : NodeId option =
    match System.Guid.TryParse(s) with
    | true, g -> Some(NodeId g)
    | _ -> None

/// Best-effort bootstrap widen id from sessionStorage (before /state).
/// Prefers `b` (widen); falls back to `z` for older session payloads.
/// sessionStorage.getItem can throw (e.g. SecurityError when storage is blocked).
let tryReadSavedZoomId () : NodeId option =
    try
        let json = sessionGet sessionKey
        if isNull json || json = "" then None
        else
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
    let idsJson =
        expandedIds |> Array.map (fun s -> "\"" + s + "\"") |> String.concat ","
    sessionSet
        sessionKey
        (sprintf "{\"z\":%s,\"b\":%s,\"e\":[%s]}" zoomJson bootJson idsJson)

/// Restore zoom root and fold state into a freshly-loaded VM.
/// Called once immediately after StateLoaded, before the initial render.
/// Uses `z` only — bootstrap widen (`b`) must not change UI zoom.
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
                    zoomStr
                    |> Option.bind tryParseNodeId
                    |> Option.map (fun nid ->
                        if Map.containsKey nid model.graph.nodes then nid
                        else model.zoomRoot)
                    |> Option.defaultValue model.zoomRoot

                let siteMap0, nextId0 =
                    if zoomRoot <> model.zoomRoot then
                        ViewModel.buildSiteMapFrom model.graph zoomRoot (Sid 0)
                    else
                        model.siteMap, model.nextSiteId
                let expandedSet =
                    expandedStrs
                    |> List.choose tryParseNodeId
                    |> Set.ofList
                let siteMap1, nextId1 =
                    ViewModel.applyFoldSession expandedSet model.graph siteMap0 nextId0
                { model with
                    zoomRoot = zoomRoot
                    siteMap = siteMap1
                    nextSiteId = nextId1
                    zoomIngress = ViewModel.ownerPathIngress model.graph zoomRoot }
        with _ -> model
