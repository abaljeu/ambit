# Code review — 18 Core Persist stack vs staging

Range: `staging...HEAD` on `cursor/core-persist-stack-2f36`. One commit: Stand the Core persist thread on the existing write body. Spec: [18 — Core Persist stack](../issues/18-core-persist-stack.md), [core-refinement architecture](../arch.md) §3 step 3 **Core Persist stack**, and [04 — Parsed/Unparsed and Persisted/Unpersisted](../issues/04-parsed-unparsed-and-persisted-unpersisted.md). This report is not approval. Ticket Status stays `coded`.

## Standards

1. **Hard** — [.agents/rules/fsharp-source.md](../../../.agents/rules/fsharp-source.md): "Don't use Exceptions. Use Error types." `PersistOutcome` already has `Failed of string`. `Raised of exn`, the `try/with` in [Persist thread](../../../src/Server/Core/PersistThread.fs) `runWrite`, and `raise ex` in [File agent](../../../src/Server/Core/FileAgent.fs) `fromOutcome` and [Db agent](../../../src/Server/Core/DbAgent.fs) `liveOutcome` ferry a write-body exception onto the caller so the mailbox `try/with` can log it. That is exception control flow. `raise ex` also drops the original stack. On `wait = false` (`enqueueSnapshot`, `scheduleCatchUp`) the reply slot is `None`, so `Raised` is discarded and the mailbox logger never runs.

```fsharp
| Failed of string
| Raised of exn
```

```fsharp
try
    afterWrite deps work ids (invoke deps work.submit ops)
with ex ->
    reply work (PersistOutcome.Raised ex)
```

```fsharp
| PersistOutcome.Failed err -> Error err
| PersistOutcome.Raised ex -> raise ex
```

Mechanical scan: printed function sizes stay inside the documented limits. No scan finding.

## Spec

1. **Wrong** — A `Change` write `Error` still adds `InMsg` `SnapshotDone` with no graph. [Persist thread](../../../src/Server/Core/PersistThread.fs) `afterWrite` calls `notify` with `None`, and `finishWhenBound` posts `SnapshotDone`. [Core mailbox backend](../../../src/Server/Core/CoreMailboxBackend.fs) `applyInMsg` then sets that node Persisted. [18 — Core Persist stack](../issues/18-core-persist-stack.md) **SnapshotDone** and [core-refinement architecture](../arch.md) §3 step 3 **Core Persist stack** add `SnapshotDone` when the thread finishes an open node, and the core loop sets Persisted. [04 — Parsed/Unparsed and Persisted/Unpersisted](../issues/04-parsed-unparsed-and-persisted-unpersisted.md) sets Persisted when graph→disk completes. A failed write is not that finish.

## Summary

Standards: 1 finding (worst: `PersistOutcome.Raised` rethrows on the caller thread, and a non-waiting job drops that exception). Spec: 1 finding (worst: a failed `Change` still posts `SnapshotDone` and the core loop sets Persisted).
