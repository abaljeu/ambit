# 24 — Contract absolute paths out of callers

**Status:** `defined`
**Type:** coding
**Blocked by:** 23 — Migrate callers onto Core path control
**Trackers:** architecture, user manual

## Context

Callers have moved to the node or relative-path protocol, or they were already one of the four Core operations. Some callers can still build or hold a DataDir absolute path.

## What to build

No caller outside Core builds or holds a DataDir absolute path. A caller passes a node, or a relative path derived from a node. Core holds the absolute residency.

### Contract

1. [ ] **No outside absolute path** — No caller outside Core builds or holds a DataDir absolute path.
2. [ ] **Caller passes a node** — A caller passes a node, or a relative path derived from a node.
3. [ ] **Core holds the path** — Core holds the absolute residency through read file, write file, read directory, and write directory.

### Obligations

1. [ ] **Parse and persist claim** — The Parse and persist claim says callers do not hold a DataDir absolute path.
2. [ ] **Absence test** — A test or a repo search shows callers outside Core do not build or hold a DataDir absolute path.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documenattion)), deleting things no longer true and checking off things newly achieved.

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement map](plan/core-refinement/map.md)
- [core-refinement](plan/core-refinement/project.md)
- [23 — Migrate callers onto Core path control](plan/core-refinement/issues/23-migrate-callers-onto-core-path-control.md)
- [Parse and persist](doc/current/parse-persist.md)
