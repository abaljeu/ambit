# 126 — planParseFile drops its SetDocumentState

## 1. Type

coding

## 2. Blockers

[ ] [135 — Backend infers document-state Notices from ops](135-backend-infers-document-state-notices-from-ops.md)

## 3. Problem

This is round 2 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md). In `ImportDocument.planParseFile`, `markCurrent` (`Unparsed → Current`) is the first op, and it unlocks the parse ops. `ParseThread.finishParse` and `Api.applyParseFile` post `NodeParsed` only after the Change. That is #404.

## 4. Change

- Delete `Op.SetDocumentState(fileId, Unparsed, Current)` in `markCurrent`.

Backend inference: a graph-only Change with command name `Parse` whose ops land inside an Unparsed File root infers a lead `NodeParsed fileId`. The rule goes in `DocumentOpImpact.inferNotices`; `ChangeAmendment.applyPosted` applies it ahead of the ops in `FileAgent` and `DbAgent`. `ParseThread.finishParse` still posts `NodeParsed` after the Change.

Hard: a blank file has no parse ops left, so there is nothing to infer from. Only `finishParse` covers it. The shared facts fold `Op.apply` and never reach the backend.

Source: `ImportDocument.fs`, `DocumentOpImpact.fs`.

## 5. Tests

[ ] `ImportDocumentTests`: the 8 facts #404 found, including `planParseFile blank input marks Unparsed file Current`
[ ] `NodeEventExpandTests` `planParseFile logs NodeParsed beside SetDocumentState`
[ ] `SpecialNodeStateAxesTests` `disk parse ops set Parsed and keep PersistState`

## 6. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
