# 49 — Rename NoServerFile to PullingFile

**Status:** `coded`
**Type:** coding
**Actual:** 2h
**Blocked by:** None — can start immediately
**Trackers:** architecture, project spec

## Context

PullingFile is the parse state. The code names are `PullingFile` and `pullingFile`. [Document-state events](../spec.md) keeps that state memory-only. This ticket renames the code. It does not change when the state is set, and it does not change what `document_state` stores.

## What to build

Rename the code names to PullingFile. Leave the set and the column alone.

### 1. Rename

1. [x] **State value** — Rename `NoServerFile` to `PullingFile` and `noServerFile` to `pullingFile` in F#. The node JSON word stays `noServerFile`.
2. [x] **Marker** — Rename `WorkspaceUploadStructure.markNoServerFile` to `markPullingFile`. Callers use the new name.

### Acceptance Criteria

1. [x] **Rename test** — `PullingFile round-trip`. A File Node that is PullingFile stays PullingFile in memory. The JSON word and the stored word stay `noServerFile`. `document_state` is unchanged by this ticket.
2. [x] **Caller test** — `markPullingFile sets PullingFile`. The old names `NoServerFile`, `noServerFile`, and `markNoServerFile` are gone from code. The literal `noServerFile` stays in the codec and in the column word.

## Finish-up

[x] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[x] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[x] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## Amendments

1. **JSON word stays** — The node JSON word is not renamed. [Serialization](../../../src/Shared/Serialization.fs) `encodeDocumentState` and `decodeDocumentState` keep the literal `noServerFile`. Event JSON and snapshots use that codec. There is no second reader and no alias.
2. **Column word stays** — The `document_state` column word stays `noServerFile` in [Graph projection](../../../src/Shared/GraphProjection.fs). The migration does not use another word for this state.
3. **Parse state stays separate** — `PullingFile` stays a `DocumentStateEnum` case and `pullingFile` stays a `DocumentState` field. This ticket does not fold `PullingFile` into `ParseState`.
4. **Acceptance text** — The rename test says the JSON word and the stored word stay `noServerFile`. It does not say the JSON word is `pullingFile`.

## Time

- 2026-10-20 2h — rename the code names; keep the JSON word and the column word `noServerFile` (from chat)

## See also

- [Document-state events](../spec.md)
- [§1 Special-node Parse and Persist axes](../arch.md#1-special-node-parse-and-persist-axes)
- [core-refinement](../project.md)
- [36 — Add the persist column](36-add-the-persist-column.md)
- [GLOSSARY](../../GLOSSARY.md)
