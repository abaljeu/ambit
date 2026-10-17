# 49 — Rename NoServerFile to PullingFile

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture, project spec

## Context

PullingFile is the parse state. The code still says `NoServerFile` and `noServerFile`. [Document-state events](../spec.md) keeps that state memory-only. This ticket renames the code. It does not change when the state is set, and it does not change what `document_state` stores.

## What to build

Rename the code names to PullingFile. Leave the set and the column alone.

### 1. Rename

1. [ ] **State value** — Rename `NoServerFile` to `PullingFile` and `noServerFile` to `pullingFile` in F# and in the node JSON word.
2. [ ] **Marker** — Rename `WorkspaceUploadStructure.markNoServerFile` to `markPullingFile`. Callers use the new name.

### Acceptance Criteria

1. [ ] **Rename test** — `PullingFile round-trip`. A File Node that is PullingFile stays PullingFile in memory. The JSON word is `pullingFile`. `document_state` is unchanged by this ticket.
2. [ ] **Caller test** — `markPullingFile sets PullingFile`. The old names `NoServerFile`, `noServerFile`, and `markNoServerFile` are gone.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Document-state events](../spec.md)
- [§1 Special-node Parse and Persist axes](../arch.md#1-special-node-parse-and-persist-axes)
- [core-refinement](../project.md)
- [36 — Add the persist column](36-add-the-persist-column.md)
- [GLOSSARY](../../GLOSSARY.md)
