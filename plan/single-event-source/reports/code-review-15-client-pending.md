# Code review — 15 Client pending = zero + submissionId

Range: uncommitted vs `HEAD`. Spec: [15 — Client pending = zero + submissionId](../issues/15-client-pending-zero-submissionid.md). Scan: none.

Production 11b (pending `EventId.zero`, `submissionId` match, no `nextEventId` / `PendingTransition`, `SyncInfo.pending: Ev list`, Client `toWireBatch`) is already on `HEAD`. This peel drops leftover test adapters and stamps [AmbitSession.fs](src/Shared/dotnet/AmbitSession.fs) `encodeBatch`.

## Standards

Mechanical scan: none. **No hard documented-standard hits.**

### [AmbitSession.fs](src/Shared/dotnet/AmbitSession.fs)

`encodeBatch` now calls `SyncBatch.toWireBatch`. Lines stay under 100 chars; function stays tiny ([fsharp-source.md](.agents/rules/fsharp-source.md): 100-char lines, 40-line functions). In scope, not adjacent cleanup ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md) Surgical Changes).

### [SyncPlannerTests.fs](tests/Shared.Tests/SyncPlannerTests.fs)

Dropped identity `asPending` / `withKind`: that is Surgical (remove orphans your change made unused).

**Judgement — leftover Change names** ([fsharp-source.md](.agents/rules/fsharp-source.md): do not leave `change` bound to `Ev`). Trigger is “when type becomes Ev”; these were already `Ev`, so not a hard hit. Edited hunks still use `change`:

```
    let change = mkChange 99
    let undo = mkChange 99
    let redo = mkChange 99
+    let items = [ change; undo; redo ]
```

and `+    let saved = [ change ]` in `restorePending`.

**Judgement — Mysterious Name** ([SMELLS.md](.agents/skills/code-review/SMELLS.md)): after `withKind` went away, “C Undo Redo” tests are three identical fixtures:

```
+let ``C Undo Redo remain one SubmitPendingBatch`` () =
+    let items = [ mkChange 0; mkChange 0; mkChange 0 ]
```

`mkChange _n` and call-site numbers (7, 637, …) were already unused; Surgical says leave pre-existing dead params.

### [ClientHistoryRuntimeTests.fs](tests/Shared.Tests/ClientHistoryRuntimeTests.fs)

Rename only. Body still has `let change = textChange …` bound to `Ev`. Same Ev-naming judgement; title-only edit, so Surgical argues leave it.

### [15 — Client pending = zero + submissionId](plan/single-event-source/issues/15-client-pending-zero-submissionid.md) / [project.md](plan/single-event-source/project.md)

Status, Actual, named links (`[11 — One serial event id]`, `[AmbitSession.fs](src/Shared/dotnet/AmbitSession.fs)`). One blank line between blocks; no mid-paragraph breaks ([markdown-writing.md](.agents/rules/markdown-writing.md), [refer-by-name.md](.agents/rules/refer-by-name.md)).

**Totals:** 0 hard; 2 judgement smells (Ev/`change` leftovers; Mysterious Name on C/Undo/Redo fixtures).

## Spec

Range: uncommitted vs `HEAD`. Spec: [15 — Client pending = zero + submissionId](../issues/15-client-pending-zero-submissionid.md). Production 11b (zero pending, `submissionId` match, no `nextEventId` / `PendingTransition`, `SyncInfo.pending: Ev list`, Client `toWireBatch`) is already on `HEAD`. This peel is test-adapter drop + [AmbitSession.fs](src/Shared/dotnet/AmbitSession.fs).

### (a) Missing or partial

**Wire assert is untested on the path this land changed.** Spec: “**Wire assert:** posted new client events have `eventId: 0` (this is the likely live reject).” `encodeBatch` now wraps `SyncBatch.toWireBatch`, but there is no test that `AmbitSession.postOps` JSON contains `"eventId":0`. Existing [EventJsonTests.fs](tests/Shared.Tests/EventJsonTests.fs) coverage is `mintChange` / `toWireBatch` only.

**Green bar SyncLogic suite unchanged.** Spec: “**Green bar:** ClientHistory / SyncLogic tests for zero pending + submissionId match.” This diff only renames one [ClientHistoryRuntimeTests.fs](tests/Shared.Tests/ClientHistoryRuntimeTests.fs) case. `HEAD` already has zero-pending there plus `submissionId` reject in [AckReconcileTests.fs](tests/Shared.Tests/AckReconcileTests.fs) (`unmatched confirmation is rejected atomically`). [SyncLogicTests.fs](tests/Shared.Tests/SyncLogicTests.fs) is untouched; its `submissionId` case is catch-up History stamp (ticket 16).

Listed **In:** files ([ClientHistory.fs](src/Shared/ClientHistory.fs), [SyncLogic.fs](src/Shared/SyncLogic.fs), [SyncPlanner.fs](src/Shared/SyncPlanner.fs), [ViewModelSync.fs](src/Shared/ViewModelSync.fs), Client Update/App/Program) are not in the diff — already landed.

### (b) Scope creep

No new product behavior beyond the leftover peels. Plan Status/`Actual` is bookkeeping. `AmbitSession.encodeBatch` is not in **In:** (“Client Update/App/Program paths that post pending”) but it is a POST `/changes` path for the wire assert.

### (c) Looks implemented, looks wrong

**“mixed C Undo Redo” is still three Change events.** Spec: “Pending events are Ev with `EventId.zero`.” Diff removes identity `withKind` from `mixed C Undo Redo wire batch keeps submissionId and EventId.zero`; all items remain `mkChange`. Zero/`submissionId` asserts hold; mixed Undo/Redo wire does not.

`HEAD` `ClientHistory.approve` also stamps nested Undo/Redo targets — out of scope: “Nested Undo/Redo target stamping and merge-stream rewind … [16 — Approve / merge stamp + beforeAll](plan/single-event-source/issues/16-approve-merge-stamp-beforeall.md).” Not in this diff.

## Summary

Standards: 0 hard; worst judgement leftover `change` bound to `Ev` in edited tests. Spec: 0 wrong lands; worst gap is no AmbitSession-path wire-assert test (existing `toWireBatch` / `mintChange` JSON tests still cover `"eventId":0`).
