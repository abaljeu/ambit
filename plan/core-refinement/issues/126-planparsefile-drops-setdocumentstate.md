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

- Delete the `/ambit/file/parse` route and `postParseRequest` in `RouteRegistration.fs`, `Api.postParseFile` and `Api.applyParseFile` in `Api.fs`, and the route's entry in `HttpResponseLog.fs`. `ParseThread` becomes the only caller of `planParseFile`.
- Delete `Op.SetDocumentState(fileId, Unparsed, Current)` in `markCurrent`.

Accepted breakage: web dev depends on the parse endpoint (`src/Client/App.fs` posts to `/ambit/file/parse`), so deleting it breaks web dev. That breakage is accepted; this ticket does not repair the client.

126 deletes and replaces nothing. No Notice takes the place of the removed op, and nothing is inferred. If no call happens, there is nothing to do.

Source: `ImportDocument.fs`, `Api.fs`, `RouteRegistration.fs`, `HttpResponseLog.fs`.

## 5. Tests

[ ] `ImportDocumentTests`: the 8 facts #404 found, including `planParseFile blank input marks Unparsed file Current`
[ ] `NodeEventExpandTests` `planParseFile logs NodeParsed beside SetDocumentState`
[ ] `SpecialNodeStateAxesTests` `disk parse ops set Parsed and keep PersistState`
[ ] `ChangeEndpointResilienceTests`: drop or rewrite the fact that posts to `/ambit/file/parse`

## 6. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
