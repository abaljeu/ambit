# 23 — Migrate callers onto Core path control

**Status:** `defined`
**Type:** coding
**Blocked by:** 22 — Expand Core path control
**Trackers:** architecture, user manual

## Context

Core owns read file, write file, read directory, and write directory. Callers still use DataDir. An inventory lists those uses.

## What to build

Each inventoried DataDir use is classified. It is already one of the four Core operations, or it changes to the node or relative-path protocol. This ticket does not add migrate batches beyond that classification.

### Migrate

1. [ ] **Classify** — Each inventoried DataDir use is already one of the four Core operations, or it changes to that protocol.
2. [ ] **Caller passes a node** — A changed caller passes a node, or a relative path derived from a node.
3. [ ] **Core holds the path** — Core holds the absolute residency.
4. [ ] **No extra batches** — The classification is the whole migrate. There is no further batch split.

### Obligations

1. [ ] **Inventory match** — The DataDir caller inventory and the Parse and persist claim agree on the classification.
2. [ ] **Caller test** — A test shows a migrated caller passes a node or a relative path and does not build the absolute path.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documenattion)), deleting things no longer true and checking off things newly achieved.

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement map](plan/core-refinement/map.md)
- [core-refinement](plan/core-refinement/project.md)
- [22 — Expand Core path control](plan/core-refinement/issues/22-expand-core-path-control.md)
- [DataDir caller inventory](plan/core-refinement/reports/datadir-caller-inventory.md)
- [Parse and persist](doc/current/parse-persist.md)
