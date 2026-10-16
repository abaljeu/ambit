# 19 — Migrate special-node state onto the DocumentState record

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture, user manual

## Context

Special nodes already store an enum and two axes. The enum cases are Current, Unparsed, and NoServerFile. The axes are Parsed or Unparsed, and Persisted or Unpersisted. The expand form is the DocumentState record. The Node field documentState is still DocumentStateEnum.

## What to build

A state change stores that record on the Node. The record holds the parse axis, the persist axis, and the enum. Current keeps its name. The standalone axes stay on the Node and receive the same values in that change.

### Migrate

1. [ ] **Record field** — The Node field documentState has type DocumentState. Until this migrate, that field stays DocumentStateEnum.
2. [ ] **Record shape** — The record fields are parseState, persistState, and documentState.
3. [ ] **Enum cases** — DocumentStateEnum keeps Current, Unparsed, and NoServerFile. NoServerFile stays its own case. Current keeps its name.
4. [ ] **Dual-write** — A state change writes the record and the standalone parseState and persistState fields. Old uses follow the axis-write rules.
5. [ ] **DocumentState remains** — DocumentState remains. This migrate does not delete it.

### Obligations

1. [ ] **Workspace graph** — The Workspace graph claim shows the Node field as the DocumentState record and shows the enum cases that remain.
2. [ ] **Store test** — A test shows a special node stores the record and still stores the standalone axes.

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
