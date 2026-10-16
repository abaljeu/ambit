# 19 — Migrate special-node state onto the DocumentState record

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture, user manual

## Context

Special nodes already store an enum and two standalone axes. The enum cases are Current, Unparsed, and NoServerFile. The axes are Parsed or Unparsed, and Persisted or Unpersisted. The temporary expanded `DocumentState` record already has four fields: `parseState`, `persistState`, `noServerFile`, and the legacy `documentState`. This is a valid expand-contract state. The Node field `documentState` is still `DocumentStateEnum`.

## What to build

A state change stores that temporary four-field record on the Node. The standalone axes stay on the Node and receive the same values in that change. The record's `noServerFile` field and legacy nested `documentState` field both remain during this migration.

### Migrate

1. [ ] **Record field** — The Node field `documentState` has type `DocumentState`. Until this migrate, that field stays `DocumentStateEnum`.
2. [ ] **Temporary record shape** — The record fields are `parseState`, `persistState`, `noServerFile`, and legacy `documentState`.
3. [ ] **Legacy enum remains for migration** — `DocumentStateEnum` keeps Current, Unparsed, and NoServerFile. The record's `noServerFile` value and its legacy nested `documentState` value stay coherent while callers migrate.
4. [ ] **Dual-write** — A state change writes the record and the standalone `parseState` and `persistState` fields. Old uses follow the axis-write rules.
5. [ ] **Contraction stays later** — This migration does not remove the standalone axes, the record's legacy nested `documentState` field, `DocumentStateEnum`, or `noServerFile`.

### Obligations

1. [ ] **Workspace graph** — The Workspace graph claim shows the Node field as the temporary four-field `DocumentState` record and shows the legacy enum during migration.
2. [ ] **Store test** — A test shows a special node stores the four-field record and still stores the standalone axes.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement map](plan/core-refinement/map.md)
- [core-refinement](plan/core-refinement/project.md)
- [04 — Parsed/Unparsed and Persisted/Unpersisted](plan/core-refinement/issues/04-parsed-unparsed-and-persisted-unpersisted.md)
- [20 — State axes on special nodes](plan/github-transport/issues/20-state-axes-on-special-nodes.md)
- [Workspace graph](doc/current/graph.md)
- [Model](src/Shared/Model.fs)
