# Code review — 18 Core Persist stack

Range: uncommitted working tree vs `HEAD`, plus untracked [Persist collectors](../../../src/Server/Core/PersistCollectors.fs), [Persist thread](../../../src/Server/Core/PersistThread.fs), [Persist thread tests](../../../tests/Server.Tests/PersistThreadTests.fs), and [18 — Core Persist stack](../issues/18-core-persist-stack.md). Spec: that ticket, [04 — Parsed/Unparsed and Persisted/Unpersisted](../issues/04-parsed-unparsed-and-persisted-unpersisted.md), and [core-refinement architecture](../arch.md) §3 step 3 **Core Persist stack**, §9 **Persist stack**, and §10 **Persist thread**. This report is not approval. Ticket Status stays `coded`.

## Standards

1. **Hard** — [.agents/rules/fsharp-source.md](../../../.agents/rules/fsharp-source.md): "Don't use Exceptions. Use Error types." `PersistOutcome.Failed of string` is the error type. `Raised of exn` plus `raise` puts the write-body exception back on the caller thread so the mailbox `try/with` still logs `InvalidOperationException`.

```fsharp
type PersistOutcome =
    | Wrote of PersistGraphOk
    | Blocked
    | Failed of string
    | Raised of exn
    | Queued
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

The same `raise` is in [Db agent](../../../src/Server/Core/DbAgent.fs) `liveOutcome`.

Mechanical scan: printed function sizes stay inside the documented limits. No scan finding.

## Spec

1. **Wrong** — A `Change` job calls `persistGraphChange`, which writes every content-changed document root. The Unparsed block applies when a submitted id is Unparsed and its `documentState` is not Current. A workspace submit can still write an Unparsed member that was not a submitted id. [04 — Parsed/Unparsed and Persisted/Unpersisted](../issues/04-parsed-unparsed-and-persisted-unpersisted.md) and [core-refinement architecture](../arch.md) §3 step 3 say an Unparsed file is not open for persist. The ops filter covers ops. The write body stays [Document persist change](../../../src/Server/DocumentPersistChange.fs), so this path is not filtered inside the writer.

2. **Wrong** — On a `Change` write `Error`, the thread still adds `InMsg` `SnapshotDone` with no graph. The core loop then sets that node Persisted. That matches the previous db live snapshot, which posted `SnapshotDone` with `None` on error, and [Core loop InMsg tests](../../../tests/Server.Tests/CoreLoopInMsgTests.fs) `SnapshotDone without a graph sets Persisted`.

## Summary

Standards: 1 finding (worst: `PersistOutcome.Raised` rethrows on the caller thread). Spec: 2 findings (worst: a `Change` job can write an Unparsed member that was not a submitted id).
