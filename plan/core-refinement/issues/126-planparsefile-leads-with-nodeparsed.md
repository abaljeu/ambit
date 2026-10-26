# 126 — planParseFile leads with NodeParsed

## 1. Type

coding

## 2. Blockers

None.

## 3. Problem

This is round 2 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md). In `ImportDocument.planParseFile`, `markCurrent` (Unparsed to Current) is the first op. That op lets the parse ops through `History.blockedForApply`. `ParseThread.finishParse` and `Api.applyParseFile` post `NodeParsed` only after the ops are accepted, and the shared facts fold `Op.apply` over the op list alone. That is #404.

## 4. Change

- `planParseFile` returns `EventBody list`. An Unparsed file gets `Notice.NodeParsed fileId` as a **lead** Notice. Placement is the rule in [135 — Op readers leave](135-op-readers-leave.md).
- `ParseThread.parseFile` and `Api.applyParseFile` post the list in order. The Notice goes through `CoreMailboxApi.postNotice` and the Change through `postGraphOnly`.
- Delete `Op.SetDocumentState(fileId, Unparsed, Current)` in `markCurrent`.

Source: `ImportDocument.fs`, `DocumentPersistWrite.fs`, `ParseThread.fs`, `Api.fs`.

## 5. Tests

[ ] `ImportDocumentTests`: the `planParseFile` facts fold `Ev.apply` over the list. That is the 8 facts #404 found.
[ ] `NodeEventExpandTests` `planParseFile logs NodeParsed beside SetDocumentState`
[ ] `SpecialNodeStateAxesTests` `disk parse ops set Parsed and keep PersistState`

## 6. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
