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

Range: commit Persist apply admits Ev and applies Ops locally (PR 18). [[src/Shared/History.fs]] is not in the range (Alan lock). PersistStamp stays in History; 05 already added `appendToLastEvent`.

Unexpected files: [[src/Server/Core/CoreMsg.fs]] adds `applyEvent` and keeps `postChange`. That matches “admit Ev” and “Leftover Change still compiles”. [[src/Server/Core/CoreMailboxBackend.fs]] overlays stamp Ops on Ev and fills failed `applyEvent`. That is persist apply, not 08. [[src/Server/Core/CoreChanges.fs]] is comment only. It does not drop `postChange`.

### (a) Missing or partial

1. Arch: “FileAgent / DbAgent persist apply of one Ev (Ops on EventBody, then `appendEvent`)”. [[tests/Server.Tests/PersistApplyTests.fs]] checks graph apply and ACK `submissionId`. FileAgent and DbAgent tests do not call `appendEvent`. `DbAgent.createForTest` already no-ops `appendEvent` when `connectionString` is empty, so the DbAgent seam cannot show EventLog append.

### (b) Not asked

2. Arch Shared segments: “Append Ev to EventLog” marked done. Issue Context already: “then appends Ev”. This range does not add `appendEvent`. Checkbox only.

3. [[src/Server/Core/DbAgent.fs]] `persistGraphProjection` now returns Ok when `connectionString` is empty. Spec does not ask for that skip. It supports the new `createForTest` `applyEvent` test.

Extra arch flips this range does implement: Persist apply sequence; CoreEventDispatch `postEvent`; FileAgent/DbAgent PersistHandlers; Stamp Ops (persist calls `PersistStamp.appendToLastEvent`).

### (c) Looks done, looks wrong

4. Arch FileAgent: “No `processPostChange` of leftover Change list” marked done. Issue: “Leftover Change still compiles.” `postChange` still maps leftover Change with `Ev.ofChange` and runs `processPostEvents`. The door still applies leftover Change. That wrap is 08/12, not a drop here.

5. Issue: “No Ev→Change copy for apply”. CoreEventDispatch persist now calls `applyEvent`. FileAgent/DbAgent apply `ChangeAmendment.applyOps`. DbAgent still does `List.map Ev.asChange` for `DatabaseProjection.plan` on the persist-apply finish path. That is leftover Change wrapping, not Op apply.

Out of scope 08 (drop `postChange`) and 12 (contract deletes) are not done. That is correct.

## Summary

Standards: 7 findings (5 hard, 2 judgement). Worst: [[src/Server/Core/FileAgent.fs]] `createWithDependencies` is about 240 lines (40-line rule).

Spec: 5 findings. Worst: FileAgent and DbAgent persist-apply tests do not call `appendEvent`.
