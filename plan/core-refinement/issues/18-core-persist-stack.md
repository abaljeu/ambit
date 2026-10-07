# 18 — Core Persist stack

**Type:** coding
**Status:** coded
**Blocked by:** None — can start immediately
Actual: 3h

## Context

[core-refinement architecture](../arch.md) §3 step 3 **Core Persist stack** is Expand, then Migrate, then Contract. Collectors and the persist thread stand beside today's sync live-save, callers move onto the collectors, and the old sync call-site shape goes away. The write body stays [Document persist change](../../../src/Server/DocumentPersistChange.fs).

[04 — Parsed/Unparsed and Persisted/Unpersisted](04-parsed-unparsed-and-persisted-unpersisted.md) locks the axes. The persist thread runs when a node is Unpersisted and Parsed. It is blocked while that node is Unparsed. On finish it adds `InMsg` `SnapshotDone` through the private function. It does not edit the graph. The core loop sets Persisted. The persist thread is not an Actor and has no `MailboxProcessor`.

Setup lives in [Core](../../../src/Server/Core). Outside Core, including [Route registration](../../../src/Server/RouteRegistration.fs), does not construct Persist, start it, or hold its handles.

## What to build

Home: [core-refinement architecture](../arch.md) §3 step 3 **Core Persist stack**, §9 **Persist stack**, and §10 **Persist thread**. Claim homes: [Parse and persist](../../../doc/current/parse-persist.md) and [Persistence model](../../../doc/current/persistence-model.md).

### 1. Expand

- [x] Collectors — New collector functions live in [Core](../../../src/Server/Core). The stack is not a `MailboxProcessor`.
- [x] Persist thread — [Persist thread](../../../src/Server/Core/PersistThread.fs) pulls from the collectors.
- [x] Write body — The loop calls `DocumentPersistChange.persistGraphOps` and `DocumentPersistChange.persistGraphChange`. Those functions stay the write body.

### 2. Migrate

- [x] Callers — [File agent](../../../src/Server/Core/FileAgent.fs) and [Db agent](../../../src/Server/Core/DbAgent.fs) call the collectors. They do not call the write body on the post path.
- [x] Setup — Each agent starts the persist thread inside Core. Route registration does not see the handles.

### 3. Contract

- [x] Sync call site — The old direct `persistGraphOps` / `persistGraphChange` call on the post path is gone. The loop is the feeder.
- [x] SnapshotDone — When the thread finishes an open node, it adds `InMsg` `SnapshotDone` through the private function. It does not edit graph axes. The core loop sets Persisted. A Db ops job notifies the open owning file. Snapshot marks move only when that id is in the in-progress workspace batch, so a file `SnapshotDone` does not close the workspace batch. The same id is not notified by both the ops job and the Change job.
- [x] Block — A node whose `parseState` is Unparsed and whose `documentState` is not Current is not open. The thread does not call the write body for that node's ops. A Current document whose `parseState` is still Unparsed stays open until [20 — State axes on special nodes](../../github-transport/issues/20-state-axes-on-special-nodes.md) writes `parseState`. A Change job is blocked when any submitted id is that Unparsed pair. The thread does not call `persistGraphChange` for that job.
- [x] ParseFinished catch-up — [Core mailbox backend](../../../src/Server/Core/CoreMailboxBackend.fs) `ParseFinished` writes Parsed only. It then calls `PersistHandlers.noteParsed`. That handler enqueues a catch-up when the node is Parsed and still Unpersisted. The persist thread does the catch-up. `ParseFinished` does not set Persisted.

## Out of scope

1. **§6 locks catch-up** — Workspace lock, per-member persist locks, and removing `withWorkTreeGate` stay [core-refinement architecture](../arch.md) §3 step 4 **§6 locks catch-up**.
2. **Parse thread setup** — Parse setup stays where it is. This ticket does not move it into Core.
3. **State axes on special nodes** — Dual-write of `DocumentState` and removal of `DocumentState` stay [core-refinement architecture](../arch.md) §3 step 1 **State axes on special nodes**.
4. **New write body** — This ticket does not add a writer beside `DocumentPersistChange`.

## Types

Collector interface, named here because the architecture does not list the records:

1. **PersistKind** — `Ops` or `Change`.
2. **PersistSubmit** — One job: node ids, graphs, ops, kind, notify, and wait.
3. **PersistOutcome** — `Wrote`, `Blocked`, `Failed`, `Raised`, or `Queued`. `Raised` carries a write-body exception back to the caller thread.
4. **PersistHandlers.noteParsed** — Catch-up hook after `ParseFinished`.
5. **PersistHandlers.snapshotDone** — `NodeId -> Graph option -> unit`, so bookkeeping can see which node finished.

## See also

[core-refinement architecture](../arch.md) §3 step 3 **Core Persist stack**; [core-refinement architecture](../arch.md) §10 **Persist thread**; [04 — Parsed/Unparsed and Persisted/Unpersisted](04-parsed-unparsed-and-persisted-unpersisted.md); [Parse and persist](../../../doc/current/parse-persist.md)

## Comments

- 2026-10-07: Drafted from §3 step 3 and §10 Persist thread. Status `coded` with the stack.

## Time

- 2026-10-07 2h — collectors, persist thread, caller migrate, tests `(from chat)`
- 2026-10-07 1h — SnapshotDone node id, Change block, Db catch-up `(from chat)`
