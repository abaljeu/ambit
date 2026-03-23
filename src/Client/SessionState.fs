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
                    then ViewModel.buildSiteMapFrom model.graph effectiveRoot (Sid 0)
                    else model.siteMap, model.nextSiteId
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
                    nextSiteId = nextId1 }
        with _ -> model
