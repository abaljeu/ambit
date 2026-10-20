# 51 — Build the persist stack

**Status:** `coded`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture, project spec
Actual: 3h

## Context

Current Core says the persist stack is not built. File persist goes through [DocumentPersistChange.fs](../../../src/Server/DocumentPersistChange.fs). [18 — Core Persist stack](18-core-persist-stack.md) coded a collector loop: `PersistThread` pulls `PersistWork` and calls those functions. There is no stack of nodes. Graph change persist stack adds that stack. Persist pull is [52 — Log stack pulls](52-log-stack-pulls.md). The launch version is [46 — Record the persist version](46-record-the-persist-version.md).

## What to build

Build a persist stack of nodes inside Core, and a persist thread that pulls a node from it. The write body stays `DocumentPersistChange`. On a successful write the thread posts `Notice.NodePersisted`. It does not edit the graph.

### 1. Persist stack

1. [x] **Stack** — Add a persist stack of node ids inside [Core](../../../src/Server/Core). The stack is not a `MailboxProcessor`. Route registration does not construct it, start it, or hold its handles.
2. [x] **Thread** — The persist thread pulls one node from that stack and calls `DocumentPersistChange.persistGraphOps` or `DocumentPersistChange.persistGraphChange`. It does not edit the graph. It is not an Actor.
3. [x] **Finish** — After a successful write, the thread posts `Notice.NodePersisted` through the private function. It does not set persist state. There is no whole-graph snapshot. [58 — Remove dead snapshot code](58-remove-dead-snapshot-code.md) drops the dead flush and the `Graph` payload. A failed write does not post `NodePersisted`. [46 — Record the persist version](46-record-the-persist-version.md) adds the launch version.

### Acceptance Criteria

1. [x] **Stack test** — `persist stack holds a node`. The stack lives in Core. Route registration does not hold its handles.
2. [x] **Pull test** — `persist thread pulls one node`. The thread calls `DocumentPersistChange`. It does not edit the graph.
3. [x] **Finish test** — `persist finish posts NodePersisted`. A successful write posts `Notice.NodePersisted`. A failed write does not. The thread does not set persist state.

## Finish-up

[x] Amendments - `Notice.NodePersisted` of `nodeId` is the rename of `Notice.SnapshotDone` already named in What to build. The launch version stays [46 — Record the persist version](46-record-the-persist-version.md). The Edit wait path stays on `PersistWork`. The node pull calls `DocumentPersistChange.persistGraphChange` with the current graph as both sides. The node id is the `Notice.NodePersisted` target. No production caller pushes a node in this ticket. A failed node pull does not run the Db diff-batch failure hook.
[x] Basis and reference docs — [core-refinement architecture](../arch.md) Graph change persist stack and [Document-state events](../spec.md) Persist stack match this build. `doc/current` claim lines match the node stack and `Notice.NodePersisted` of `nodeId`.
[x] Documentation - [Parse and persist](../../../doc/current/parse-persist.md) and [Mailbox](../../../doc/current/mailbox.md) record the node stack and `Notice.NodePersisted` of `nodeId`. The version pair stays open. `doc/user` has no persist-stack claim.

## See also

- [Graph change persist stack](../arch.md#graph-change-persist-stack)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [46 — Record the persist version](46-record-the-persist-version.md)
- [52 — Log stack pulls](52-log-stack-pulls.md)
- [GLOSSARY](../../GLOSSARY.md)

## Time

- 2026-10-20 3h — node stack, persist pull, `Notice.NodePersisted`, tests `(from chat)`
