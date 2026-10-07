# Code review — 18 Core Persist stack vs staging

Range: `staging...HEAD` on `cursor/core-persist-stack-2f36`, plus the follow-up that removes `PersistOutcome.Raised` and stops a failed Change from posting `SnapshotDone`. Spec: [18 — Core Persist stack](../issues/18-core-persist-stack.md), [core-refinement architecture](../arch.md) §3 step 3 **Core Persist stack**, and [04 — Parsed/Unparsed and Persisted/Unpersisted](../issues/04-parsed-unparsed-and-persisted-unpersisted.md). This report is not approval. Ticket Status stays `coded`.

## Standards

No open finding. A thrown write is caught in [Persist thread](../../../src/Server/Core/PersistThread.fs) and returned as `Failed of string`. [File agent](../../../src/Server/Core/FileAgent.fs) and [Db agent](../../../src/Server/Core/DbAgent.fs) do not rethrow it.

## Spec

No open finding. A failed Change does not add `InMsg` `SnapshotDone` and does not set Persisted. Only a successful finish of an open node posts `SnapshotDone`. The claim that a Change writes an Unparsed member that was not submitted does not hold: `persistGraphChange` writes a document root only when `documentState` is Current, and [18 — Core Persist stack](../issues/18-core-persist-stack.md) leaves a Current document with a stale Unparsed `parseState` open.

## Summary

Standards: 0 findings. Spec: 0 findings.
