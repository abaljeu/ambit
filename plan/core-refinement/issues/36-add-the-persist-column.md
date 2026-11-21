# 36 — Add the persist column

**Status:** `done`
**Actual:** 1.5h
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

1. [x] **Migration** — Database adds `persist_state` on `nodes` with the same `ADD COLUMN IF NOT EXISTS` statement used for `document_state`. Existing rows are Persisted.
2. [x] **Read and write** — Database and Database projection read and write `persist_state` beside `document_state`. `current` remains the stored word for Parsed. `unparsed` remains the stored word for Unparsed. PullingFile is not written. A stored word `noServerFile` reloads as Unparsed, not PullingFile.
3. [x] **Reload** — Graph projection `nodeRowFromNode` and `graphFromPersistence` carry persist state both ways. An Unpersisted node stays Unpersisted after reload. A Persisted node stays Persisted after reload.

### Acceptance Criteria

1. [x] **Workspace graph** — `doc/current/graph.md` states that persist state survives reload in `persist_state`, and that `document_state` stores Parsed or Unparsed only.
2. [x] **Reload test** — `reload keeps persist state`. The test stores one Persisted node and one Unpersisted node, reloads, and reads those same persist states. `document_state` on the reloaded rows is `current` or `unparsed`. A row stored as `noServerFile` reloads as Unparsed, not PullingFile.

## Finish-up

[x] Amendments

1. **Build list** — the build list did not change.
2. **Stored words** — `persist_state` stores `persisted` and `unpersisted`. Existing rows use `DEFAULT 'persisted'`.
3. **Round trip** — `graphRoundTrip preserves no-server-file document state` expected PullingFile to survive reload. It now reloads as Unparsed.
4. **Persist default** — `SQL reload derives parse state and defaults persist to Persisted` expected Unpersisted to reset to Persisted. It now keeps persist state.
5. **Code names** — after [49 — Rename NoServerFile to PullingFile](49-rename-noserverfile-to-pullingfile.md), the code names are `PullingFile` and `pullingFile`. The stored word stays `noServerFile`. `PullingFile round-trip` keeps that JSON word and expects projection reload as Unparsed.

[x] Basis and reference docs — [§1 Special-node Parse and Persist axes](../arch.md#1-special-node-parse-and-persist-axes) already states this column's parse and persist split. No architecture text changed.
[x] Documentation - [Workspace graph](../../../doc/current/graph.md) states the reload column and the `document_state` words. [[doc/user]] has no persist-column page to change.

## Time

- 2026-10-20 1.5h — persist column, reload test, and workspace graph claim (from chat)

## See also

- [§1 Special-node Parse and Persist axes](../arch.md#1-special-node-parse-and-persist-axes)
- [core-refinement](../project.md)
- [Database](../../../src/Server/Database.fs)
- [Database projection](../../../src/Server/DatabaseProjection.fs)
- [Graph projection](../../../src/Shared/GraphProjection.fs)
- [Workspace graph](../../../doc/current/graph.md)
- [GLOSSARY](../../../GLOSSARY.md)
