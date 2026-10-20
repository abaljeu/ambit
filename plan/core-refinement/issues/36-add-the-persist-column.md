# 36 — Add the persist column

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture

## Context

§1 Special-node Parse and Persist axes keeps persist state separate from parse state. `graphFromPersistence` reads `document_state` only. `Node.Create` sets persist state to Persisted. PullingFile is memory-only. A server reboot does not make PullingFile true.

The stored words in `document_state` are:

- `current`
- `unparsed`
- `noServerFile`

## What to build

1. [ ] **Migration** — Database adds `persist_state` on `nodes` with the same `ADD COLUMN IF NOT EXISTS` statement used for `document_state`. Existing rows are Persisted.
2. [ ] **Read and write** — Database and Database projection read and write `persist_state` beside `document_state`. `current` remains the stored word for Parsed. `unparsed` remains the stored word for Unparsed. PullingFile is not written. A stored word `noServerFile` reloads as Unparsed, not PullingFile.
3. [ ] **Reload** — Graph projection `nodeRowFromNode` and `graphFromPersistence` carry persist state both ways. An Unpersisted node stays Unpersisted after reload. A Persisted node stays Persisted after reload.

### Acceptance Criteria

1. [ ] **Workspace graph** — `doc/current/graph.md` states that persist state survives reload in `persist_state`, and that `document_state` stores Parsed or Unparsed only.
2. [ ] **Reload test** — `reload keeps persist state`. The test stores one Persisted node and one Unpersisted node, reloads, and reads those same persist states. `document_state` on the reloaded rows is `current` or `unparsed`. A row stored as `noServerFile` reloads as Unparsed, not PullingFile.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [§1 Special-node Parse and Persist axes](../arch.md#1-special-node-parse-and-persist-axes)
- [core-refinement](../project.md)
- [Database](../../../src/Server/Database.fs)
- [Database projection](../../../src/Server/DatabaseProjection.fs)
- [Graph projection](../../../src/Shared/GraphProjection.fs)
- [Workspace graph](../../../doc/current/graph.md)
- [GLOSSARY](../../../GLOSSARY.md)
