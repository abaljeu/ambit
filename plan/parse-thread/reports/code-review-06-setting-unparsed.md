# Code review — 06 Setting Unparsed

Range: `origin/staging...HEAD`. Ticket: [06 — Setting Unparsed, recursive update](../issues/06-setting-unparsed-recursive-update.md).

## 1. Standards

No findings. The mechanical scan printed no lines. The added test binding sits under `tests/`, and the F# measure skips that path.

## 2. Spec

1. **MarkUnparsed case** — `InMsg` gains `MarkUnparsed of NodeId`. [core-refinement architecture](../../core-refinement/arch.md) §10 Core loop names the cases `ParseFinished` and `SnapshotDone`. [06 — Setting Unparsed, recursive update](../issues/06-setting-unparsed-recursive-update.md) asks for an InMsg that sets that File Node Unparsed and does not name this case.

## 3. Totals

Standards: 0 findings. Spec: 1 finding. Worst spec issue: MarkUnparsed case.
