# 126 — planParseFile drops its SetDocumentState

**Status:** `defined`

## 1. Type

coding

## 2. Blockers

[ ] [135 — Backend infers document-state Notices from ops](135-backend-infers-document-state-notices-from-ops.md)

## 3. Problem

This is round 2 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md). In `ImportDocument.planParseFile`, `markCurrent` (`Unparsed → Current`) is the first op, and it unlocks the parse ops. `ParseThread.finishParse` and `Api.applyParseFile` post `NodeParsed` only after the Change. That is #404.

## 4. Change

Resolution (Alan 2026-10-26): delete the parse endpoint, `POST /ambit/file/parse`.

- Delete the `/ambit/file/parse` route and `postParseRequest` in `RouteRegistration.fs`, `Api.postParseFile` and `Api.applyParseFile` in `Api.fs`, and the route's entry in `HttpResponseLog.fs`.
- `planParseFile` and its `markCurrent` op stay. Load parses on the server: Load Order, then the mailbox, then ParseStack, then ParseThread, then `DocumentPersistWrite.planParseFile`, then `ImportDocument.planParseFile`. That path must not depend on `/parse`.

126 deletes only the parse endpoint and its tests.

Accepted breakage: web dev depends on the parse endpoint (`src/Client/App.fs` posts to `/ambit/file/parse`), so deleting it breaks web dev. That breakage is accepted; this ticket does not repair the client.

Source: `Api.fs`, `RouteRegistration.fs`, `HttpResponseLog.fs`.

## 5. Tests

[ ] `ChangeEndpointResilienceTests`: drop the fact that posts to `/ambit/file/parse`
[ ] `SessionTests`: drop the fact that calls `postParseRequest`
[ ] `NodeEventExpandTests` `planParseFile logs NodeParsed beside SetDocumentState`: drive the parse through ParseThread, not `Api.postParseFile`

## 6. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
