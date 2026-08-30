# Undo Slices 1–2 review

Review range: uncommitted Slice 1–2 changes against `HEAD`.

## Standards

1. **Hard — per-item list rebuild.** [[src/Shared/ClientHistory.fs]] `appendPending` grows a record's queue with `pending @ [ transition, submitted ]`. Repeated rapid Undo and Redo transitions copy the accumulated lineage on every append. [[.cursor/rules/fsharp-source.mdc]] forbids growing lists with `@` in a per-item path because the batch becomes quadratic.
2. **Hard — duplicated inversion table.** The new `Change.inverse` in [[src/Shared/History.fs]] repeats the Set and Replace old/new swap cascade already in `Change.invert`; only create-Op handling and identity construction differ. [[.cursor/rules/core-agent-behavior.mdc]] says not to replicate code. Extract the common reversible-Op mapping while keeping the two create policies explicit.
3. **Judgement call — possible Duplicated Code.** [[src/Shared/ClientHistory.fs]] `undo` and `redo` repeat the same inverse, record move, transition, pending append, and result construction. A small private direction-specific helper could remove the duplicate algorithm without widening the five-function public interface.

[[src/Shared/ClientHistory.fs]] is 243 lines and all bindings are below 40 lines. [[tests/Shared.Tests/ClientHistoryTests.fs]] is exactly the allowed 400-line maximum, with no long lines. Its size mostly covers distinct lineage, identity, confirmation, and create-retention invariants, but it has no growth margin.

Full report: [[review-undo-slice-2-standards.md]].

## Spec

No findings. The implementation satisfies Slice 1 characterization and the non-budgeted 2,000-Node baseline. Slice 2 implements ordinary inversion, the five-operation client History seam, confirmation lineage, stable record identity, command-name retention, future folding, and retained detached-Node identities. Runtime migration is intentionally deferred.

Full report: [[review-undo-slice-2-spec.md]].

Summary: Standards has 3 findings; its worst issues are the two documented-standard violations. Spec has 0 findings.
