# 36 — Add the persist column

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture

## Context

[§1 Special-node Parse and Persist axes](plan/core-refinement/arch.md#1-special-node-parse-and-persist-axes) keeps persist state separate from parse state. Persisted means the node's data is written to its file on disk ([GLOSSARY.md](GLOSSARY.md)). The column `document_state` already stores the three-way parse state. [Database](src/Server/Database.fs) line 67 adds that column with default `current`. Stored words are `current`, `unparsed`, and `noServerFile`. Reload in [Graph projection](src/Shared/GraphProjection.fs) `graphFromPersistence` reads only that column, and `Node.Create` then defaults persist state to Persisted. Unpersisted does not survive reload.

## What to build

A persist column stores persist state so reload restores it. The column `document_state` stays the three-way parse state.

### Persist column

1. [ ] **Migration** — [Database](src/Server/Database.fs) adds `persist_state` on `nodes` with the same `ADD COLUMN IF NOT EXISTS` statement used for `document_state`. Existing rows are Persisted.
2. [ ] **Read and write** — [Database](src/Server/Database.fs) and [Database projection](src/Server/DatabaseProjection.fs) read and write `persist_state` beside `document_state`. The words in `document_state` stay `current`, `unparsed`, and `noServerFile`. `current` remains the stored word for Parsed.
3. [ ] **Reload** — [Graph projection](src/Shared/GraphProjection.fs) `nodeRowFromNode` and `graphFromPersistence` carry persist state both ways. A node that was Unpersisted is Unpersisted after reload. A node that was Persisted is Persisted after reload.

### Obligations

1. [ ] **Workspace graph** — [Workspace graph](doc/current/graph.md) says persist state survives reload in `persist_state`, and `document_state` stays the three-way parse state.
2. [ ] **Reload test** — A test stores one Persisted node and one Unpersisted node, reloads, and reads those same persist states back. `document_state` on the reloaded rows is still `current`, `unparsed`, or `noServerFile`.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [§1 Special-node Parse and Persist axes](plan/core-refinement/arch.md#1-special-node-parse-and-persist-axes)
- [core-refinement](plan/core-refinement/project.md)
- [Database](src/Server/Database.fs)
- [Database projection](src/Server/DatabaseProjection.fs)
- [Graph projection](src/Shared/GraphProjection.fs)
- [Workspace graph](doc/current/graph.md)
- [GLOSSARY](GLOSSARY.md)
