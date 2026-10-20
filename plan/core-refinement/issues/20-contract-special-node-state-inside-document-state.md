# 20 — Contract special-node state inside DocumentState

**Status:** `coded`
**Type:** coding
**Blocked by:** [19 — Migrate special-node state onto the DocumentState record](19-migrate-special-node-state-onto-the-document-state-record.md)
**Trackers:** architecture, user manual
Actual: 3h

## Context

After [19 — Migrate special-node state onto the DocumentState record](19-migrate-special-node-state-onto-the-document-state-record.md), the Node stores the temporary four-field `DocumentState` record and still stores standalone `parseState` and `persistState` fields. The record fields are `parseState`, `persistState`, `pullingFile`, and the legacy nested `documentState`.

## What to build

The standalone axes leave the Node. The obsolete nested `documentState` field leaves the record, and `DocumentStateEnum` leaves after its callers have migrated. The Node field `documentState` and the `DocumentState` record remain. The final record is exactly `{ parseState: ParseState; persistState: PersistState; pullingFile: bool }`. The contraction keeps `pullingFile`.

### Contract

1. [x] **Standalone axes leave** — The Node has no standalone `parseState` field and no standalone `persistState` field.
2. [x] **Legacy field leaves** — The `DocumentState` record has no nested `documentState` field, and `DocumentStateEnum` is removed after its callers have migrated.
3. [x] **Exact record remains** — The Node field `documentState` stores `DocumentState`, whose fields are exactly `parseState`, `persistState`, and `pullingFile`.
4. [x] **PullingFile field remains** — The contraction does not remove `pullingFile`.

### Obligations

1. [x] **Workspace graph** — The Workspace graph claim shows `parseState`, `persistState`, and `pullingFile` only inside the `DocumentState` record, with no legacy nested `documentState`.
2. [x] **Read test** — A test reads all three final fields from the record after the standalone axes and legacy nested field are gone.

## Finish-up

[x] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[x] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[x] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## Amendments

1. **Wire enum remains** — `DocumentStateEnum` stays as the `Op.SetDocumentState` and JSON/SQL vocabulary. The nested record field is gone. The type is not deleted while those callers remain.
2. **SetDocumentState writes the record** — `Op.SetDocumentState` writes `parseState` and `pullingFile` together. Current is Parsed. Unparsed is Unparsed. PullingFile implies Unparsed.
3. **JSON siblings remain** — Graph JSON still encodes sibling `parseState` and `persistState` keys and a derived `documentState` enum string.

User-manual pages did not change: Unparsed observation is the same derived enum.

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement map](plan/core-refinement/map.md)
- [core-refinement](plan/core-refinement/project.md)
- [19 — Migrate special-node state onto the DocumentState record](plan/core-refinement/issues/19-migrate-special-node-state-onto-the-document-state-record.md)
- [Workspace graph](doc/current/graph.md)
- [Model](src/Shared/Model.fs)

## Time

- 2026-10-16 3h — contract DocumentState to parseState, persistState, and pullingFile (from chat)
