# 20 — Contract special-node state inside DocumentState

**Status:** `defined`
**Type:** coding
**Blocked by:** [19 — Migrate special-node state onto the DocumentState record](19-migrate-special-node-state-onto-the-document-state-record.md)
**Trackers:** architecture, user manual

## Context

After [19 — Migrate special-node state onto the DocumentState record](19-migrate-special-node-state-onto-the-document-state-record.md), the Node stores the temporary four-field `DocumentState` record and still stores standalone `parseState` and `persistState` fields. The record fields are `parseState`, `persistState`, `noServerFile`, and the legacy nested `documentState`.

## What to build

The standalone axes leave the Node. The obsolete nested `documentState` field leaves the record, and `DocumentStateEnum` leaves after its callers have migrated. The Node field `documentState` and the `DocumentState` record remain. The final record is exactly `{ parseState: ParseState; persistState: PersistState; noServerFile: bool }`. The contraction keeps `noServerFile`.

### Contract

1. [ ] **Standalone axes leave** — The Node has no standalone `parseState` field and no standalone `persistState` field.
2. [ ] **Legacy field leaves** — The `DocumentState` record has no nested `documentState` field, and `DocumentStateEnum` is removed after its callers have migrated.
3. [ ] **Exact record remains** — The Node field `documentState` stores `DocumentState`, whose fields are exactly `parseState`, `persistState`, and `noServerFile`.
4. [ ] **No-server-file flag remains** — The contraction does not remove `noServerFile`.

### Obligations

1. [ ] **Workspace graph** — The Workspace graph claim shows `parseState`, `persistState`, and `noServerFile` only inside the `DocumentState` record, with no legacy nested `documentState`.
2. [ ] **Read test** — A test reads all three final fields from the record after the standalone axes and legacy nested field are gone.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documenattion)), deleting things no longer true and checking off things newly achieved.

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement map](plan/core-refinement/map.md)
- [core-refinement](plan/core-refinement/project.md)
- [19 — Migrate special-node state onto the DocumentState record](plan/core-refinement/issues/19-migrate-special-node-state-onto-the-document-state-record.md)
- [Workspace graph](doc/current/graph.md)
- [Model](src/Shared/Model.fs)
