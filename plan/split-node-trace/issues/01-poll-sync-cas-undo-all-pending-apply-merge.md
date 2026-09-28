# 01 — Poll/sync recoverable mismatch: undo all pending, then apply Server merge

**Type:** coding
**Status:** coded
**Actual:** 2h55m
**Blocked by:** None — can start immediately

## Context

A person edits in the Browser and posts Graph Changes. Those Changes sit as pending (optimistic). Poll returns the Server merge. That merge already includes the posted edits plus other Authority work.

Today the Poll fold can soft-skip a mismatched `SetName`, `SetText`, `SetClasses`, or `Replace` (`84d19416`) and invert only same-field pending (`8309e9db`). A split Change then keeps the `SetText` prefix and drops the `Replace` suffix. The line reads `hel`. The suffix Node is gone from the parent list. Soft-skip-without-apply of a mismatched authoritative payload is wrong. Hard-fail of that fold is also wrong.

Want overwrite is a second symptom of the same family: a stale Want `childMap` install can drop the new sibling and restore the old full text. Keep the proving test. Do not keep a prior wrong fix. Corrected handling is undo all pending, then apply the Server merge.

Origin: [split-node-origin-trace](plan/split-node-trace/reports/split-node-origin-trace.md) (PR [#160 — TRACE: childMap refactor does not break node-split](https://github.com/abaljeu/ambit/pull/160)).

## What to build

When Poll or sync apply hits a recoverable field mismatch, the Browser undoes every pending Graph op, applies the Server merge for that field, and continues the fold. The person does not reload. An uncommitted edit-box draft stays in `#edit-input` unless focus or the Node is gone.

### 1. Recoverable Poll/sync field mismatch

On `SetName`, `SetText`, `SetClasses`, or `Replace` CAS mismatch during Poll/sync apply, invert or drop all pending Graph ops, then apply the Server merge for that field, then continue the fold. The Server merge includes the posted edits.

1. [x] 1.1 Undo all pending — Invert or drop every pending Graph op, not only same-field ops on the mismatched target.
2. [x] 1.2 Apply Server merge — After that undo, apply the authoritative Server payload for the mismatched field. Do not leave the field at the undone local value.
3. [x] 1.3 Continue the fold — Later ops in the same Poll/sync list still apply. The fold does not stop on this mismatch.

### 2. Never hard-fail, never soft-skip without apply

The Poll fold stays a success path for recoverable field mismatch.

1. [x] 2.1 Never hard-fail — Recoverable `SetName` / `SetText` / `SetClasses` / `Replace` mismatch does not Error the Poll fold and does not force DataOutdated reload.
2. [x] 2.2 Never soft-skip without apply — Do not skip a mismatched authoritative payload and continue. After undo, apply the Server merge for that field.

### 3. Uncommitted edit-box draft

Post all ops in normal use. Only an uncommitted edit-box draft stays local.

1. [x] 3.1 Leave `#edit-input` — On this conflict, do not rewrite `#edit-input` unless focus or the Node is gone.
2. [x] 3.2 Posted edits are pending — Graph ops that were posted are pending. They are undone, then the Server merge (which includes them) is applied.

### 4. Regression tests

Prove both TRACE symptoms on the corrected path. Seed from [SplitOriginTraceTests](tests/Shared.Tests/SplitOriginTraceTests.fs) when that file is not yet on the workplace tip (PR [#160 — TRACE: childMap refactor does not break node-split](https://github.com/abaljeu/ambit/pull/160)).

1. [x] 4.1 Truncated-line split — The split case that today keeps `hel` and drops the suffix after `Replace` CAS plus same-field invert must, after this ticket, undo all pending (including `SetText`) and apply the Server merge. Prefix-only leftover is a fail.
2. [x] 4.2 Want overwrite proving test — Keep the Want overwrite characterization test. Change its assertions to the corrected path (undo all pending, then apply Server merge). Do not keep a prior wrong fix (skip or ignore the Want payload).

## See also

[split-node-origin-trace](plan/split-node-trace/reports/split-node-origin-trace.md), [Client correction — rewind and replay](plan/event-sourced-ops/details/client-consume.md), [16 — Fix pending+merged-events: undo-then-apply instead of DataOutdated reload](plan/event-sourced-ops/issues/16-fix-pending-merged-events-undo-then-apply.md), [17 — Instrument apply-error → DataOutdated with op type and mismatch reason](plan/event-sourced-ops/issues/17-instrument-apply-error-dataoutdated.md), [applyOpForSync](src/Shared/ResidentProjection.fs), [undoPendingGraph](src/Shared/SyncLogic.fs), [SplitOriginTraceTests](tests/Shared.Tests/SplitOriginTraceTests.fs)

## Comments

- 2026-09-28 — Charted from Alan’s locked grill. Never hard-fail the Poll fold. Undo all pending Graph ops, apply Server merge, continue. Soft-skip-without-apply is wrong. Preserve uncommitted `#edit-input` draft. Want overwrite stays in this family. Status `defined`.
- 2026-09-28 — Alan accepted this chart PR. Status `done`.
- 2026-09-28 — Implemented undo-all-pending then apply Server merge in [applyOpForSync](src/Shared/ResidentProjection.fs) and rewind-then-replay in [applySyncResponse](src/Shared/SyncLogic.fs). Soft-skip-without-apply after undo is gone for these field mismatches. Status `coded`.

## Time

- 2026-09-28 45m — charted coding ticket from locked grill (from chat)
- 2026-09-28 10m — Status `done` on accepted chart land (from chat)
- 2026-09-28 2h — implement undo-all-pending then apply Server merge (from chat)
