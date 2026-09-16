# Code review — 07 Persist apply

Review only. This report is not approval. Ticket [[../issues/07-persist-apply.md|07 — Persist apply]] stays **Status:** coded.

## Range

Fixed point: Merge pull request #17 from abaljeu/cursor/done-05-06-compile-preamble-8b85.

Tip: Merge pull request #18 from abaljeu/cursor/persist-apply-e547.

Command: three-dot diff from that parent to that merge. The diff is not empty.

Commits in the range:

- Persist apply admits Ev and applies Ops locally
- Merge origin/staging into persist apply
- Merge pull request #18 from abaljeu/cursor/persist-apply-e547

Files:

- [[../arch.md]]
- [[../issues/07-persist-apply.md]]
- [[../project.md]]
- [[src/Server/Core/CoreChanges.fs]]
- [[src/Server/Core/CoreEventDispatch.fs]]
- [[src/Server/Core/CoreMailboxBackend.fs]]
- [[src/Server/Core/CoreMsg.fs]]
- [[src/Server/Core/DbAgent.fs]]
- [[src/Server/Core/FileAgent.fs]]
- [[tests/Server.Tests/Gambol.Server.Tests.fsproj]]
- [[tests/Server.Tests/PersistApplyTests.fs]]

Alan lock: [[src/Shared/History.fs]] is not in the range.

## Mechanical scan

`python3 .agents/skills/code-review/scripts/standards-scan.py --diff <fixed-point>` at the Persist apply merge. Full stdout (includes measure-fs-size):

```
src/Server/Core/DbAgent.fs  .agents/rules/fsharp-source.md  FILE 562->567  already over 400 or new file over 400; change increased it
--- measure-fs-size ---
src/Server/Core/CoreMailboxBackend.fs::withAppliedOps: lines 42-54 (13 lines)
src/Server/Core/CoreMailboxBackend.fs::overlayFreshEvents: lines 55-71 (17 lines)
src/Server/Core/DbAgent.fs::applyOneEvent: lines 118-145 (28 lines)
src/Server/Core/DbAgent.fs::applyBatch: lines 146-160 (15 lines)
src/Server/Core/DbAgent.fs::persistGraphProjection: lines 161-184 (24 lines)
src/Server/Core/DbAgent.fs::processPostEvents: lines 322-349 (28 lines)
```

Scan: listed bindings are under 40 lines. No added long line.

## Standards

Alan lock: [[src/Shared/History.fs]] is not in this range.

**Hard violations**

1. [[src/Server/Core/DbAgent.fs]] — [[.agents/rules/fsharp-source.md]] (400 lines per file). The file is already over 400 lines. This change grows it 562 to 567.

2. [[src/Server/Core/FileAgent.fs]] `createWithDependencies` — [[.agents/rules/fsharp-source.md]] (40 lines per function). The enclosing `let` is about 240 lines (40–279). This diff edits apply, stamp, and `applyEvent` inside that binding. Nested `applyOne` is not a separate module-level function.

3. [[../issues/07-persist-apply.md]] Blocked-by line — [[.agents/rules/markdown-writing.md]] (paths use wikilinks). The edited line uses markdown links `(05-expand-op-list-apply.md)` and `(06-compile-preamble.md)`, not `[[…]]`.

4. Same file Time list — [[.agents/rules/refer-by-name.md]]. The new Time item has no number or name.

**Alan serial ([[.agents/rules/core-api.md]])**

`EventId.next` is not in these hunks. [[src/Server/Core/CoreEventDispatch.fs]] `EventLog.nextId` in `prepare` / `commit` is unchanged mailbox admit. [[src/Server/Core/FileAgent.fs]] `applyOne` and [[src/Server/Core/DbAgent.fs]] `applyOneEvent` do not set `Ev.id`. They still do leftover `s.revision.Value + 1` / `Revision nextRev`. That is an equivalent tip bump on the persist filling, not in the mailbox.

5. FileAgent `applyOne` and DbAgent `applyOneEvent` — [[.agents/rules/core-api.md]] (only the mailbox may bump the serial). Quote: `let nextRev = s.revision.Value + 1` then `{ s' with revision = Revision nextRev }`.

**Judgement (smell baseline, not a hard fail)**

6. Possible Duplicated Code — FileAgent `applyOne` and DbAgent `applyOneEvent` use the same shape: persist lookup by `submissionId`, `Ev.ops`, `ChangeAmendment.applyOps`, then revision bump and `CoreMailboxBackend.withAppliedOps`. [[src/Server/Core/CoreMailboxBackend.fs]] `withAppliedOps` also matches [[src/Server/Core/CoreEventDispatch.fs]] `withConfirmedOps` (rewrite EventBody Ops).

7. Possible Data Clumps — FileAgent fold tuple `(s, confirmations, fresh, changed, externalChanges)` still travels as one unlabeled pack.

## Spec

Match: the [[../issues/05-expand-op-list-apply.md|05 — Expand Op-list apply]] / [[../issues/06-compile-preamble.md|06 — Compile preamble]] / [[../issues/07-persist-apply.md|07 — Persist apply]] target, not [[plan/core-creation/arch.md]]. Leftover Change wrapping, `Ev.ofChange` / `Ev.asChange`, and `postChange` doors stay until 08/12. They are not findings here.

Range: Persist apply admits Ev and applies Ops locally; Merge origin/staging into persist apply; Merge pull request #18 from abaljeu/cursor/persist-apply-e547. [[src/Shared/History.fs]] is not in the range.

**The 07 target is reached.**

### (a) Missing or partial

None. After this range, 05–07 stay true.

05: “`Ev.apply` / invert / ChangeValidation / amend / PersistStamp take `Op list` (or Ops on EventBody). They do not wrap leftover Change. `Change.apply` still compiles.” Those seams stay in [[src/Shared/History.fs]]. This range does not touch that file.

06: “TestBackend Ev type and Authority constructor are in scope. Leftover Change still compiles.” [[src/Shared/History.fs]] is unchanged. [[tests/Server.Tests/TestBackend.fs]] keeps `open Gambol.Shared` and `Authority`.

07 / Sequence: “FileAgent and DbAgent admit Ev, apply Ops locally, then appendEvent.” “CoreEventDispatch does not copy Ev to leftover Change for apply.” “No Ev→Change copy for apply.”

[[src/Server/Core/FileAgent.fs]] and [[src/Server/Core/DbAgent.fs]] add `applyEvent`, take Ev, and call `ChangeAmendment.applyOps` on `Ev.ops`. [[src/Server/Core/CoreEventDispatch.fs]] `persist` calls `applyEvent`; it does not build leftover Change. `commit` still calls `appendEvent` on Ev after apply.

### (b) Not asked for

1. 07: “FileAgent and DbAgent admit Ev, apply Ops locally, then `appendEvent`.” [[src/Server/Core/DbAgent.fs]] `persistGraphProjection` now returns Ok when `connectionString` is empty. 05–07 do not ask for that skip. It serves the new `createForTest` apply test.

### (c) Looks done, target not reached

None. Apply is `ChangeAmendment.applyOps` / `applyEvent`. `appendEvent` after apply already runs in `CoreEventDispatch.commit`. Out of scope 08 (drop `postChange`) and 12 (contract deletes) stay undone. That is correct.

## Summary

Standards: 7 findings (5 hard, 2 judgement). Worst: [[src/Server/Core/FileAgent.fs]] `createWithDependencies` is about 240 lines (40-line rule).

Spec: 1 finding. Worst: [[src/Server/Core/DbAgent.fs]] `persistGraphProjection` skips an empty `connectionString` (not asked). The 07 persist-apply target is reached.
