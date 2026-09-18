# QA 47 — Event persist duplicate key

Date: 2026-09-18. For [47 — Server rejected Change: duplicate event id](../issues/47-server-rejected-change-duplicate-event-id.md). No fix. Domain language from [[CONTEXT.md]].

## 1. Domain language

An **Event** is one durable record in **EventLog**. A **Change** is the Action body of that Event (an Op list), not a separate record. **event id** is the unique ordered position of an Event in EventLog. The Browser mints a draft Event with event id zero and a new submission id. Core assigns the durable event id on accept. **Persist** writes that Event into EventLog (Database `events` table, or the file EventLog in file mode). **Sync** keeps Browser and Server Graphs aligned by exchanging Actions. **Poll** asks for Events since a known event id. **Load** is Upload, then Parse, then Fetch plus Poll. Parse posts graph-only Change Events; those still persist EventLog (they skip file persist only).

## 2. Intended Browser Change path

The person edits in the App. The Browser applies the Ops locally, queues a Change Event (event id zero), and posts it. Core admits the Authority, applies the Ops to the Graph, assigns the next event id from EventLog, and persists the Event. A later Poll returns Events since the Browser cursor. On success the App stays in Sync. On HTTP reject with pending work, the App enters ServerRejected.

## 3. User-facing boundary

The person sees a blocking overlay: **Server rejected change**, then the Server error string, then “Reload the page to resync. Your unsaved changes will be lost.” The status line says **Server rejected change — reload required**. The pending queue is cleared. The local edit is still on screen until reload. Core persist did not accept a new Event with a unique event id; the Database unique constraint on event id refused the write. Reload is required to resync. Unsaved Changes are lost.

## 4. Related seams (not a diagnosis)

This is Database EventLog persist (constraint `events_pkey` on event id), not the file EventLog. The Server assigned an event id that EventLog already holds. Plausible adjacent seams, not ranked:

1. **EventLog restore after Load / Parse.** Load’s Parse stage posts graph-only Change Events that still persist EventLog. Restore merges by submission id and can keep the next-id cursor unchanged. If Core’s next event id sits behind already-persisted Events, the next Browser Change collides.
2. **Dual persist copies.** File mode and Database mode are separate persist fillings. Production writes Database EventLog. Core also holds an in-memory EventLog for minting. If that in-memory log does not contain Events already in the Database, minting repeats an event id.
3. **event-id minting.** Drafts stay at event id zero. Only EventLog advances the serial. A retry that already inserted the row, or a restore that omitted persisted Events, can mint a colliding event id.

Issue [46 — Workspace Load prepare-push 401](../issues/46-workspace-run-prepare-push-401.md) is the prior Load failure. After 46, a successful Load then an edit is a plausible sequence. 46 itself is cookie/Upload, not Event persist.

## 5. Existing issues

No other core-creation issue reports this overlay or `events_pkey`. Nearby, not the same symptom:

- [42 — Migrate PersistHandlers restore and getEventsSince](../issues/42-migrate-persisthandlers-restore-and-geteventssince.md) — EventLog restore and persist tails. Status `coded`.
- [40 — Expand postEvent, EventLog store, and Event JSON persist](../issues/40-expand-postevent-eventlog-and-event-json.md) — persist Event JSON on file and Database. Status `coded`.
- [13 — Delete runtime mirror and remove production Persistence:Mode](../issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md) — Database vs file persist policy. Status `ready-for-agent`.
- [19 — Database down and probe](../issues/19-database-down-and-host-stop.md) — Database-down Reject. Status `defined`. Blocked by 13.
- [33 — Credentialed Browser Change posts](../issues/33-credentialed-browser-change-posts.md) — credentialed Change posts. Status `coded`.
- [46 — Workspace Load prepare-push 401](../issues/46-workspace-run-prepare-push-401.md) — Load Upload 401. Status `done`.
