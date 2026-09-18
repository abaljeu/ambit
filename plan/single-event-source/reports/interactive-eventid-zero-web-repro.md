# Interactive EventId-zero web repro

Date: 2026-09-17
Ticket: [11 — One serial event id](plan/single-event-source/issues/11-one-serial-event-id.md)

Alan reported a basic edit reject after the staging SES cut and called it a numbers mismatch. He suspected tests that do not post [EventId.zero](src/Shared/History.fs). This run booted the Local Server + Local Client Chrome stack (scripts only, no VS Code Tasks, no WPF) and used HTTP plus a headed Chrome edit as the tight loop.

## Loop

1. `./scripts/client.sh build`
2. Postgres from Ambit `start.sh` (`gambol` / `gambol_dev`)
3. Server `ASPNETCORE_URLS=http://localhost:5215` at `http://localhost:5215/ambit?debug=1`
4. HTTP: `POST /ambit/changes` with `eventId` 0 then a non-zero id
5. Chrome: select a Normal row, Enter, type, Escape; capture the `/changes` body

## Evidence (unfixed staging tip)

[ClientHistory.mintChange](src/Shared/ClientHistory.fs) and [SyncLogic.applyLocalEvent](src/Shared/SyncLogic.fs) already set `EventId.zero`. [SyncBatch.toWireBatch](src/Shared/SyncBatch.fs) was identity. [CoreEventDispatch.prepare](src/Server/Core/CoreEventDispatch.fs) overwrote the posted id with `EventLog.nextId` and did not reject.

With persist up:

- HTTP create `eventId` 0 → 200, ack `r` 1
- HTTP SetText `eventId` 0 → 200, ack `r` 2
- HTTP SetText `eventId` 1 → 200, ack `r` 3
- Chrome SetText posted `{"eventId":0,...}` → 200, no blocking overlay

The live Fable client does not post a non-zero EventId on a basic edit. The unfixed server also accepts a non-zero id. There is no HTTP body text `numbers mismatch`. [Overlays.js](src/Server/wwwroot/Overlays.js) shows any ServerRejected 400 as “revision mismatch or invalid op”.

First boots used [appsettings.Development.json](src/Server/appsettings.Development.json) `postgres`/`postgres` after [Server.addAppSettings](src/Server/Server.fs) reloaded that file over env. Persist fell to read-only: `Database persistence is unavailable; file fallback is read-only.` That 400 is not an EventId reject. Local-only rewrite to `gambol`/`gambol_dev` made persist work. That file was not committed.

## Hypotheses

1. Tests hid the pending-zero wire contract (`toWireBatch` identity; fixtures posted 1/99). Supported.
2. Client posts non-zero EventIds on a basic edit. Falsified (Chrome body `eventId` 0).
3. Current server rejects non-zero EventIds. Falsified on the unfixed tip (HTTP `eventId` 1 → 200).
4. Alan’s phrase is the generic overlay for another 400 (read-only, CAS, system-folder SetText). Possible; not reproduced as EventId on this stack.

## Fix

Peel zeros at the wire. Reject a new posted non-zero id at admit.

- [SyncBatch.toWireBatch](src/Shared/SyncBatch.fs) stamps `id = EventId.zero`
- [UpdateCodec.encodePendingBatchBody](src/Client/UpdateCodec.fs) encodes that batch
- [CoreEventDispatch.persistNew](src/Server/Core/CoreEventDispatch.fs) returns `posted EventId must be zero`
- Replay by `submissionId` is unchanged
- FileAgent/DbAgent apply still uses an assigned id

## Red then green

- `toWireBatch forces EventId.zero on a dirty new client Event` — Expected 0, Actual 7, then pass
- `postEvent rejects a non-zero EventId on a new client Event` — pass after persistNew
- `two live client edits with EventId.zero are admitted` — pass
- Related Server filters (Issue41, StateEndpoint, PersistApply, CoreChanges, ChangeEndpoint, FileAgentFailure, DatabaseProjectionContract, LazyLoad) — 119 pass
- Client compile gate `./scripts/client.sh build` — pass

After the fix, HTTP `eventId` 0 SetText → 200 `r` 6. HTTP `eventId` 4 → 400 `{"error":"posted EventId must be zero"}`. Chrome still posts `eventId` 0 and gets 200. Headed edit of `after-zero` → `walkthrough-ok`, `Escape: OK`, overlay closed.

## Out of scope

Missing-ROOT `/state` did not block this edit. `/state` had ROOT plus specials. Do not change [appsettings.Development.json](src/Server/appsettings.Development.json) for desktop `postgres`/`postgres`.
