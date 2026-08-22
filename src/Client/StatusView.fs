module Gambol.Client.StatusView

open Browser.Dom
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.JsInterop

let private renderSyncStatus (model: VM) =
    let el = document.getElementById "sync-status"
    if not (isNull el) then
        if not model.syncInfo.isServerReady then
            el.textContent <- "Starting up\u2026"
            el.className <- "amb-sync-status amb-syncing"
        else
            match model.syncInfo.syncState with
            | Idle when model.syncInfo.catchUp.IsSome ->
                el.textContent <- "Merging remote changes\u2026"
                el.className <- "amb-sync-status amb-syncing"
            | Idle ->
                el.textContent <-
                    if model.syncInfo.isPollingActive then "synced" else "idle"
                el.className <- "amb-sync-status amb-synced"
            | Sending attempt ->
                el.textContent <-
                    if attempt = 1 then
                        "Saving\u2026"
                    else
                        $"Saving\u2026 (try {attempt})"
                el.className <- "amb-sync-status amb-syncing"
            | Polling ->
                el.textContent <- "Checking\u2026"
                el.className <- "amb-sync-status amb-synced"
            | Uploading ->
                el.textContent <- "Uploading\u2026"
                el.className <- "amb-sync-status amb-syncing"
            | Parsing ->
                el.textContent <- "Parsing\u2026"
                el.className <- "amb-sync-status amb-syncing"
            | Loading ->
                el.textContent <- "Loading\u2026"
                el.className <- "amb-sync-status amb-syncing"
            | WaitingToRetry (attempt, _, _) ->
                el.textContent <- $"Unsaved \u2014 (try {attempt})"
                el.className <- "amb-sync-status amb-pending"
            | ServerRejected ->
                el.textContent <- "Server rejected change \u2014 reload required"
                el.className <- "amb-sync-status amb-stale"
            | CodeOutdated ->
                el.textContent <- "New version available \u2014 click to reload"
                el.className <- "amb-sync-status amb-stale"
            | DataOutdated ->
                el.textContent <- "Data changed on server \u2014 click to reload"
                el.className <- "amb-sync-status amb-stale"

let private renderDatabaseStatus () =
    let dbEl = document.getElementById "db-status"
    if not (isNull dbEl) then
        match readDbPresent () with
        | "ok" ->
            dbEl.textContent <- "DB synced"
            dbEl.setAttribute(
                "title",
                "PostgreSQL is configured and matches the file state.")
            dbEl.className <- "amb-db-status amb-db-present"
        | "mismatch1" ->
            dbEl.textContent <- "DB mismatch1"
            dbEl.setAttribute(
                "title",
                "PostgreSQL mismatched the file state, was rebuilt, "
                + "and now matches.")
            dbEl.className <- "amb-db-status amb-db-mismatch"
        | "mismatch2" ->
            dbEl.textContent <- "DB mismatch2"
            dbEl.setAttribute(
                "title",
                "PostgreSQL still mismatches the file state after rebuild. "
                + "Using file storage.")
            dbEl.className <- "amb-db-status amb-db-mismatch"
        | _ ->
            dbEl.textContent <- "Files only"
            dbEl.setAttribute(
                "title",
                "PostgreSQL is not configured. Using file storage.")
            dbEl.className <- "amb-db-status amb-db-absent"

/// Update persistent sync and database status text.
let renderStatus (model: VM) : unit =
    renderSyncStatus model
    renderDatabaseStatus ()
