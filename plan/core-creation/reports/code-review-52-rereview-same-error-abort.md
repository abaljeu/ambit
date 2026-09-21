# Code review — [52 — Run must not launch when edit commit fails](../issues/52-run-abort-when-commit-fails.md)

Pin: three-dot `origin/staging...HEAD` after fetch. Tip `2bd716a7213a3408f03c32893730feb48028da50`. Staging tip `edea735513e13871acab97e857b6419693910b09`. Diff non-empty. Status stays `coded`.

Prior Spec finding (abort keyed off `after <> before`) is gone in this tip. Shared [RunEditCommit.fs](src/Shared/RunEditCommit.fs) is the gate; Browser [RunLaunch.fs](src/Client/RunLaunch.fs) injects `commitIfEditing`. Axis files: [code-review-standards-52-same-error-abort](code-review-standards-52-same-error-abort.md), [code-review-spec-52-same-error-abort](code-review-spec-52-same-error-abort.md).

## Claims

All four claimed fixes hold in this tip. `git show 2bd716a7` adds Shared [RunEditCommit.fs](src/Shared/RunEditCommit.fs), `withLastCmdOk` on successful `commitTextEdit`, and Fact ``failed Editing SetText with same prior Error does not SubmitCommand``. Focused `dotnet test` on [RunEditCommitTests.fs](tests/Shared.Tests/RunEditCommitTests.fs): 4 passed.

1. Successful `commitTextEdit` sets Ok — [UpdateHelpers.fs](src/Client/UpdateHelpers.fs) empty-ops and apply-Ok arms call `withLastCmdOk`. [ViewModelMoveOps.fs](src/Shared/ViewModelMoveOps.fs) sets `CmdLastResult.Ok None`.
2. Abort on any Error after Editing — [RunEditCommit.fs](src/Shared/RunEditCommit.fs) `| true, Some (CmdLastResult.Error _) -> false`. No `after <> before` in product F#.
3. Shared gate is real — [RunLaunch.fs](src/Client/RunLaunch.fs) is `RunEditCommit.afterEditCommit commitIfEditing`. [Commands.fs](src/Client/Commands.fs) `execRunOp` and [UpdateAmbleRun.fs](src/Client/UpdateAmbleRun.fs) `runAmbleOp` call that wrap. `commitIfEditing` always uses `commitTextEdit` when Editing.
4. Same prior Error Fact — [RunEditCommitTests.fs](tests/Shared.Tests/RunEditCommitTests.fs) seeds that Error, fails SetText via `withMoveError`, asserts no `SubmitCommand`. The helper still copies the Browser `tryStart` arm; it does not POST `/command`.

## Standards

# Standards — [52 — Run must not launch when edit commit fails](../issues/52-run-abort-when-commit-fails.md)

Range `origin/staging...HEAD`. F# in range meets [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) 40-line functions and 100-character new lines. Shared [RunEditCommit.fs](src/Shared/RunEditCommit.fs) holds the Editing gate; Browser [RunLaunch.fs](src/Client/RunLaunch.fs) injects `commitIfEditing`; Core [CoreActorPool.fs](src/Server/Core/CoreActorPool.fs) `dropAndReply` drops live Focus on ActorStart persist Error.

### 1. Verdict

Needs changes.

### 2. Hard violations

1. Bare list-item id — [.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md) (number and name every list item). Scan `BARE_ID item 9` in [52 — Run must not launch when edit commit fails](../issues/52-run-abort-when-commit-fails.md) Comments and Time, and [core creation](../project.md) Notes. Write the item number with its name, not `item 9`.
2. Bare ids in reports in this range (files not opened here) — same rule. Scan `BARE_ID` `ticket 51`, `item 1`, `item 2`, `item 3` on `plan/core-creation/reports/code-review-52-rereview-client-runlaunch.md`, `code-review-52-rereview-run-abort-when-commit-fails.md`, `code-review-52-run-abort-when-commit-fails.md`, `code-review-spec-52-rereview.md`, `code-review-spec-52-run-abort-when-commit-fails.md`, `code-review-standards-52-rereview.md`, `code-review-standards-52-run-abort-when-commit-fails.md`. Reports are write-once; do not rewrite them unless Alan asks.
3. Blank line between Comments list items in [52 — Run must not launch when edit commit fails](../issues/52-run-abort-when-commit-fails.md) — [.agents/rules/markdown-writing.md](.agents/rules/markdown-writing.md) (list items have no blank line).
4. Adjacent format-only collapse of `PostGraphOnly` in [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) — [.agents/rules/core-agent-behavior.md](.agents/rules/core-agent-behavior.md) Surgical Changes (do not improve adjacent formatting).
5. Spoken name Client — [.agents/rules/markdown-writing.md](.agents/rules/markdown-writing.md) plus [CONTEXT.md](CONTEXT.md) Browser. Ticket sections `### Client (confirmed)` and `### 1. Client Run` name the Browser project Client.

### 3. Scan printed, not a fail

1. [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) 594→627: test file length is not an [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) fail. Measure-fs-size bindings are under 40 lines. Surgical under-100-line preference is not a script fail ([.agents/rules/core-agent-behavior.md](.agents/rules/core-agent-behavior.md)).

### 4. Judgement smells ([SMELLS.md](.agents/skills/code-review/SMELLS.md))

1. Middle Man — [RunLaunch.fs](src/Client/RunLaunch.fs) `afterEditCommit` only forwards to Shared with Browser `commitIfEditing`. Keep: Browser commit is not Shared.
2. Duplicated Code — [RunEditCommitTests.fs](tests/Shared.Tests/RunEditCommitTests.fs) `afterEditCommitThenTryStart` copies the Browser `execRunOp` tryStart arm. Keep: no Browser test project.
3. Parameter Explosion — [CommandRequest.fs](src/Shared/CommandRequest.fs) `actorStart` takes six parameters to build `ActorStart`. Factory reuses one record; it does not add a new clump.

## Spec

# Spec — [52 — Run must not launch when edit commit fails](../issues/52-run-abort-when-commit-fails.md)

Range: `git diff origin/staging...HEAD`. Ticket Status stays `coded`.

Verdict: nits

### 1. Missing or partial

1. Browser Run proof is Shared `SubmitCommand`, not POST. Spec: "Proof: Run while Editing with a SetText that fails CAS does not POST `/command`." [RunEditCommitTests.fs](tests/Shared.Tests/RunEditCommitTests.fs) Facts ``failed Editing SetText CAS does not SubmitCommand`` and ``failed Editing SetText with same prior Error does not SubmitCommand`` assert no `SubmitCommand` on helper `afterEditCommitThenTryStart`, which copies the `tryStart` arm. They do not run Browser `execRunOp` and they do not POST. Production [Commands.fs](src/Client/Commands.fs) `execRunOp` does call the Shared gate. The POST path in [App.fs](src/Client/App.fs) is only `SubmitCommand`.

### 2. Scope creep

1. ActorStart factory is extra. Spec Non-goals: "Stream / Focus≠Command encode ([51 — Browser Run Focus vs Command](../issues/51-browser-run-focus-vs-command.md) is `done`)." [CommandRequest.fs](src/Shared/CommandRequest.fs) adds `actorStart` and Fact ``oneNodeStart is actorStart with zoom focus and command equal``. Encode is the same; What to build does not ask for this factory.

### 3. Wrong

None. Spec Browser Run: "If `commitIfEditing` was required (mode was Editing) and commit failed (error result / no successful text apply when text needed commit), **do not** `SubmitCommand` / Amble Run. Keep the commit error visible." [RunEditCommit.fs](src/Shared/RunEditCommit.fs) `mayLaunchAfterEditCommit` is `| true, Some (CmdLastResult.Error _) -> false`. [RunLaunch.fs](src/Client/RunLaunch.fs) wraps `commitIfEditing`. `execRunOp` and [UpdateAmbleRun.fs](src/Client/UpdateAmbleRun.fs) `runAmbleOp` use that wrap. Abort keeps the commit `lastCmdResult`. Spec Server start: "If `putLive` succeeds and durable `ActorStart` (or schedule) fails, **drop** the live row (same as failed start — no live Focus). Reply Error." [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) on `actorStart` Error calls `dropAndReply` (mailbox order kept). Spec: "Proof: forced ActorStart Event failure leaves Focus out of `liveFocusIds`." Fact ``ActorStart persist Error leaves Focus out of liveFocusIds``. Non-goal 1. Fixing root causes of `old text does not match` and Non-goal 3. Undo/global history are untouched.

### 4. Production claims

1. Successful `commitTextEdit` uses `withLastCmdOk`. [UpdateHelpers.fs](src/Client/UpdateHelpers.fs): `| [] -> withLastCmdOk { model with mode = Selecting }, []` and `| Ok (m, effects) -> withLastCmdOk { m with mode = Selecting }, effects`.
2. Abort is any Error after Editing. [RunEditCommit.fs](src/Shared/RunEditCommit.fs): `| true, Some (CmdLastResult.Error _) -> false` with no `after <> before`.
3. Shared [RunEditCommit.fs](src/Shared/RunEditCommit.fs) is the gate; Browser [RunLaunch.fs](src/Client/RunLaunch.fs) wraps `commitIfEditing`.
4. Fact ``failed Editing SetText with same prior Error does not SubmitCommand`` seeds that Error, fails SetText, and asserts no `SubmitCommand`.

## Summary

Standards: 5 hard (refer-by-name on live ticket and project notes, folded-report scan ids, Comments blank line, adjacent `PostGraphOnly` format, spoken Client), 3 judgement (Middle Man, Duplicated Code, Parameter Explosion); worst within Standards is refer-by-name on live ticket and project notes. Spec: 1 partial (Shared `SubmitCommand` proof, not POST `/command`), 1 scope-creep (ActorStart factory), 0 wrong; worst within Spec is the still-partial Browser proof. Prior identity abort is fixed.
