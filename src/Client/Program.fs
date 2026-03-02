module Gambol.Client.Program

open Browser.Dom
open Browser.Types
open Fable.Core
open Gambol.Shared
open Gambol.Client
open Gambol.Client.Update
open Gambol.Client.View

// ---------------------------------------------------------------------------
// JS interop
// ---------------------------------------------------------------------------

[<Emit("fetch($0).then(r => r.text()).then($1)")>]
let fetchText (url: string) (callback: string -> unit) : unit = jsNative

// ---------------------------------------------------------------------------
// MVU dispatch loop
// ---------------------------------------------------------------------------

let mutable currentModel: Model =
    { graph = { root = NodeId(System.Guid.Empty); nodes = Map.empty }
      revision = Revision.Zero
      selectedNodes = None
      mode = Selecting }

/// Mutable reference for edit prefill text (set on StartEdit, consumed on render)
let mutable editPrefill: string option = None

let rec dispatch (msg: Msg) : unit =
    // Special handling: StartEdit stores the prefill before updating model
    match msg with
    | StartEdit prefill -> editPrefill <- Some prefill
    | _ -> ()

    currentModel <- update msg currentModel dispatch

    // Render, then apply prefill override for StartEdit
    render currentModel dispatch

    match msg, editPrefill with
    | StartEdit _, Some prefill ->
        let editInput = document.getElementById "edit-input"
        if not (isNull editInput) then
            let inp = editInput :?> HTMLInputElement
            inp.value <- prefill
            inp.focus()
            let len = prefill.Length
            inp.setSelectionRange(len, len)
        editPrefill <- None
    | _ -> ()

// ---------------------------------------------------------------------------
// Bootstrap
// ---------------------------------------------------------------------------

fetchText $"/{Update.currentFile}/state" (fun text ->
    match decodeStateResponse text with
    | Ok (graph, revision) ->
        dispatch (StateLoaded (graph, revision))
    | Error err ->
        app.textContent <- $"Error: {err}"
)
