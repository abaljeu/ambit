# 51 — Stand the persist stack

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture, project spec

## Context

Current Core says the persist stack is not stood up. File persist goes through [DocumentPersistChange.fs](../../../src/Server/DocumentPersistChange.fs). [18 — Core Persist stack](18-core-persist-stack.md) coded a collector loop: `PersistThread` pulls `PersistWork` and calls those functions. There is no stack of nodes. Graph change persist stack stands that stack. Persist pull is [52 — Log stack pulls](52-log-stack-pulls.md). The launch version is [46 — Record the persist version](46-record-the-persist-version.md).

## What to build

Stand a persist stack of nodes inside Core, and a persist thread that pulls a node from it. The write body stays `DocumentPersistChange`. On a successful write the thread posts `InMsg.NodePersisted`. It does not edit the graph.

### 1. Persist stack

1. [ ] **Stack** — Add a persist stack of node ids inside [Core](../../../src/Server/Core). The stack is not a `MailboxProcessor`. Route registration does not construct it, start it, or hold its handles.
2. [ ] **Thread** — The persist thread pulls one node from that stack and calls `DocumentPersistChange.persistGraphOps` or `DocumentPersistChange.persistGraphChange`. It does not edit the graph. It is not an Actor.
3. [ ] **Finish** — After a successful write, the thread posts `InMsg.NodePersisted` through the private function. It does not set persist state. `SnapshotDone` stays the live-document snapshot. A failed write does not post `NodePersisted`. [46 — Record the persist version](46-record-the-persist-version.md) adds the launch version.

### Acceptance Criteria

1. [ ] **Stack test** — `persist stack holds a node`. The stack lives in Core. Route registration does not hold its handles.
2. [ ] **Pull test** — `persist thread pulls one node`. The thread calls `DocumentPersistChange`. It does not edit the graph.
3. [ ] **Finish test** — `persist finish posts NodePersisted`. A successful write posts `InMsg.NodePersisted`. A failed write does not. The thread does not set persist state.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change persist stack](../arch.md#graph-change-persist-stack)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [46 — Record the persist version](46-record-the-persist-version.md)
- [52 — Log stack pulls](52-log-stack-pulls.md)
- [GLOSSARY](../../GLOSSARY.md)
