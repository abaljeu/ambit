# Undo Slice 1–2 standards review

**Finding count: 3** — 2 documented-standard violations; 1 judgement-call smell.

## Findings

1. **Hard — per-item list rebuild.** [[src/Shared/ClientHistory.fs]] `appendPending`
   grows a record's queue with `pending @ [ transition, submitted ]`. Repeated rapid
   Undo/Redo transitions therefore copy the accumulated lineage on every append.
   [[.cursor/rules/fsharp-source.mdc]] says never rebuild a whole structure once per
   item and specifically warns against growing lists with `@` because batches can
   become quadratic.

2. **Hard — duplicated inversion table.** The new `Change.inverse` hunk in
   [[src/Shared/History.fs]] repeats the Set/Replace old-new swap cascade already in
   `Change.invert`; only create-Op handling and identity construction differ.
   [[.cursor/rules/core-agent-behavior.mdc]] says “Don't replicate code — put shared
   logic in a reusable place and call it.” This is also a **possible Duplicated Code**
   smell; extract the common reversible-Op mapping while keeping the two create
   policies explicit.

3. **Judgement call — possible Duplicated Code.** [[src/Shared/ClientHistory.fs]]
   `undo` and `redo` repeat the same inverse/record-move/transition/pending/return
   shape:
   `Change.inverse ... historyRecord.applied`, `{ historyRecord with applied =
   inverse }`, transition construction, `appendPending`, and the same result tuple.
   A small private direction-specific transition helper could remove the duplicate
   algorithm without widening the five-function public interface.

## Limit assessment

[[src/Shared/ClientHistory.fs]] is 243 lines; every binding is under 40 lines. It is
pure, immutable, and uses `Option`/`Result`; no production Graph scan or per-Op
rebuild was added. [[tests/Shared.Tests/ClientHistoryTests.fs]] is exactly the
allowed 400-line maximum, with no overlong lines. Its length mostly covers distinct
lineage, identity, confirmation, and create-retention invariants; no further
documented violation is present, though the file has no growth margin.
