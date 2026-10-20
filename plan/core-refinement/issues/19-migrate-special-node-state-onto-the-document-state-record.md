# 19 — Migrate special-node state onto the DocumentState record

**Status:** `coded`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture, user manual
Actual: 2h

## Context

Special nodes already store an enum and two standalone axes. The enum cases are Current, Unparsed, and PullingFile. The axes are Parsed or Unparsed, and Persisted or Unpersisted. The temporary expanded `DocumentState` record already has four fields: `parseState`, `persistState`, `pullingFile`, and the legacy `documentState`. This is a valid expand-contract state. The Node field `documentState` is still `DocumentStateEnum`.

## What to build

A state change stores that temporary four-field record on the Node. The standalone axes stay on the Node and receive the same values in that change. The record's `pullingFile` field and legacy nested `documentState` field both remain during this migration.

### Migrate

1. [x] **Record field** — The Node field `documentState` has type `DocumentState`. Until this migrate, that field stays `DocumentStateEnum`.
2. [x] **Temporary record shape** — The record fields are `parseState`, `persistState`, `pullingFile`, and legacy `documentState`.
3. [x] **Legacy enum remains for migration** — `DocumentStateEnum` keeps Current, Unparsed, and PullingFile. The record's `pullingFile` value and its legacy nested `documentState` value stay coherent while callers migrate.
4. [x] **Dual-write** — A state change writes the record and the standalone `parseState` and `persistState` fields. Old uses follow the axis-write rules.
5. [x] **Contraction stays later** — This migration does not remove the standalone axes, the record's legacy nested `documentState` field, `DocumentStateEnum`, or `pullingFile`.

### Obligations

1. [x] **Workspace graph** — The Workspace graph claim shows the Node field as the temporary four-field `DocumentState` record and shows the legacy enum during migration.
2. [x] **Store test** — A test shows a special node stores the four-field record and still stores the standalone axes.

## Finish-up

[x] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[x] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[x] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

None changed. User-manual pages did not change: Unparsed observation is the same enum.

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement map](plan/core-refinement/map.md)
- [core-refinement](plan/core-refinement/project.md)
- [04 — Parsed/Unparsed and Persisted/Unpersisted](plan/core-refinement/issues/04-parsed-unparsed-and-persisted-unpersisted.md)
- [20 — State axes on special nodes](plan/github-transport/issues/20-state-axes-on-special-nodes.md)
- [Workspace graph](doc/current/graph.md)
- [Model](src/Shared/Model.fs)

## Time

- 2026-10-16 2h — migrate Node.documentState onto the four-field record and dual-write axes (from chat)
