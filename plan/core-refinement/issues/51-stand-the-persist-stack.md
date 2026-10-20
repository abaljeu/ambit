# 51 — Build the persist stack

**Status:** `coded`
**Type:** coding
**Actual:** 3h
**Blocked by:** None — can start immediately
**Trackers:** architecture, project spec

## Context

Current Core says the persist stack is not built. File persist goes through [DocumentPersistChange.fs](../../../src/Server/DocumentPersistChange.fs). [18 — Core Persist stack](18-core-persist-stack.md) coded a collector loop: `PersistThread` pulls `PersistWork` and calls those functions. There is no stack of nodes. Graph change persist stack builds that stack. Persist pull is [52 — Log stack pulls](52-log-stack-pulls.md). The launch version is [46 — Record the persist version](46-record-the-persist-version.md).

## What to build

Build a persist stack of nodes inside Core, and a persist thread that pulls a node from it. The write body stays `DocumentPersistChange`. On a successful write the thread posts `Notice.NodePersisted`. It does not edit the graph.

### 1. Persist stack

1. [x] **Stack** — Add a persist stack inside [Core](../../../src/Server/Core). `PersistRequest` holds the graph snapshot and the node id. The stack is a `BlockingCollection` over a `ConcurrentStack`. It is not a `MailboxProcessor`. Route registration does not construct it, start it, or hold its handles.
2. [x] **Thread** — The persist thread takes one request from that stack and calls `DocumentPersistChange.persistNode` with that request's graph. It does not ask the mailbox and does not read live state. It does not edit the graph. It is not an Actor.
3. [x] **Finish** — After a successful write, the thread posts `Notice.NodePersisted` through the private function. It does not set persist state. There is no whole-graph snapshot. [58 — Remove dead snapshot code](58-remove-dead-snapshot-code.md) drops the dead flush and the `Graph` payload. A failed write does not post `NodePersisted`. [46 — Record the persist version](46-record-the-persist-version.md) adds the launch version.

### Acceptance Criteria

1. [x] **Stack test** — `persist stack holds a request`. The stack lives in Core. Route registration does not hold its handles.
2. [x] **Take test** — `persist thread takes one request`. The thread writes that request's graph through `DocumentPersistChange`. It does not edit the graph.
3. [x] **Finish test** — `persist finish posts NodePersisted`. A successful write posts `Notice.NodePersisted`. A failed write does not. The thread does not set persist state.

## Finish-up

[x] Amendments - `PersistRequest` is `{ graph: Graph; nodeId: NodeId }`. The graph is the snapshot at the push. The stack is a `BlockingCollection<PersistRequest>` over a `ConcurrentStack<PersistRequest>` (LIFO), built in Core. The mailbox pushes when `Notice.NodeParsed` leaves the node Parsed and Unpersisted, and does not wait. The persist thread `Take`s and calls `DocumentPersistChange.persistNode` with that graph. It does not ask the mailbox and does not read live state. Repeats of a node are written again. `Notice.NodePersisted` of `nodeId` renames `Notice.SnapshotDone`. No version field. A failed write does not post and does not fail the graph edit. Our code adds no `ref`, mutable cell, lock, or `Monitor`.
[x] Basis and reference docs — [core-refinement architecture](../arch.md) Graph change persist stack and [core-refinement spec](../spec.md) Persist stack record that decision. [Parse and persist](../../../doc/current/parse-persist.md) and [Mailbox](../../../doc/current/mailbox.md) record the live `Notice.NodePersisted` of `nodeId`. The version pair stays the target.
[x] Documentation - [doc/current](../../../doc/current) records the live node-id notice. [doc/user](../../../doc/user) has no persist-stack page. The version target stays unchecked.

## Time

- 2026-10-20 2h — pure mailbox persist stack, node pull, NodePersisted `(from chat)`
- 2026-10-20 1h — blocking LIFO of graph snapshots; thread Take writes that graph `(from chat)`

## See also

- [Graph change persist stack](../arch.md#graph-change-persist-stack)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [46 — Record the persist version](46-record-the-persist-version.md)
- [52 — Log stack pulls](52-log-stack-pulls.md)
- [GLOSSARY](../../GLOSSARY.md)
