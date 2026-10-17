# 42 — Contract the old parse-state write

**Status:** `defined`
**Type:** coding
**Blocked by:** [39 — Migrate upload, import, undo, and stored replay](39-migrate-upload-import-undo-and-stored-replay.md), [40 — Migrate parse, reconcile, and lazy load](40-migrate-parse-reconcile-and-lazy-load.md), [41 — Migrate markUnparsedThenPush onto the loop](41-migrate-markunparsedthenpush-onto-the-loop.md)
**Trackers:** architecture

## Context

[Graph change contract](plan/core-refinement/arch.md#graph-change-contract). Callers rely on the new output. This step removes the old output.

## What to build

The old direct write and the public post of `Op.SetDocumentState` are removed. The old assertions are removed. `Op.SetDocumentState` stays as the Event the client displays.

### Contract

1. [ ] **Old direct writes** — The old direct parse-state writes are removed.
2. [ ] **Public post** — The public post of `Op.SetDocumentState` is removed.
3. [ ] **Assertions** — Tests assert the new output. The assertions of the old write are removed.
4. [ ] **Event** — `Op.SetDocumentState` stays as the Event the client displays.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change contract](plan/core-refinement/arch.md#graph-change-contract)
- [core-refinement](plan/core-refinement/project.md)
