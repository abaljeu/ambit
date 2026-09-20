# Spec re-review — 52 Run abort when commit fails; no orphan live

Range: three-dot `origin/staging...HEAD` (merge-base `bd1e8059`, tip `90d0da2e`). Spec: [52 — Run must not launch when edit commit fails; no orphan live](plan/core-creation/issues/52-run-abort-when-commit-fails.md). Type bug-fixing. Status `coded` (unchanged). Prior axis: [code-review-spec-52-run-abort-when-commit-fails](code-review-spec-52-run-abort-when-commit-fails.md).

## (a) Missing or partial

None. Spec Client Run item 1: “If `commitIfEditing` was required (mode was Editing) and commit failed … **do not** `SubmitCommand` / Amble Run. Keep the commit error visible.” [RunLaunch.fs](src/Client/RunLaunch.fs) `afterEditCommit` uses Shared `commitIfEditingForRun`; a new commit Error sets `mayLaunch` false and returns that model. [Commands.fs](src/Client/Commands.fs) `execRunOp` and [UpdateAmbleRun.fs](src/Client/UpdateAmbleRun.fs) `runAmbleOp` both abort there.

Spec Client Run item 2: “Proof: Run while Editing with a SetText that fails CAS does not POST `/command`.” [CommandRequestTests.fs](tests/Shared.Tests/CommandRequestTests.fs) `failed Editing SetText CAS does not SubmitCommand` now calls [CommandRequest.fs](src/Shared/CommandRequest.fs) `execRunOp` with a SetText CAS-fail commit (same `withMoveError` path as `commitTextEdit`) and asserts no `SubmitCommand`. [App.fs](src/Client/App.fs) POSTs `/{file}/command` only for that Effect, so no `SubmitCommand` means that POST does not run. Browser Run is the private `execRunOp` that shares this gate through `afterEditCommit`; Shared `execRunOp` is the Run-after-commit the proof calls. `commandSubmitEffects` is gone.

Spec Server start item 1: “If `putLive` succeeds and durable `ActorStart` (or schedule) fails, **drop** the live row … Reply Error.” [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) `dispatchStartActor` persist Error calls [CoreActorPool.fs](src/Server/Core/CoreActorPool.fs) `dropAndReply`. Item 2 prefer: still putLive, then durable ActorStart Event; drop on persist Error (existing mailbox order). Item 3: [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) `ActorStart persist Error leaves Focus out of liveFocusIds`.

## (b) Behaviour not asked for

None in product behaviour. Amble abort is spec Client Run item 1. Shared `actorStartEffects` is only the success arm of Shared `execRunOp`. Non-goals (CAS root cause, [51 — Browser Run Focus vs Command](plan/core-creation/issues/51-browser-run-focus-vs-command.md) encode, Undo) are not in the product diff.

## (c) Implemented but wrong

None. Failed Editing commit keeps the commit Error and emits no `SubmitCommand`. Forced ActorStart Event Error leaves Focus out of `liveFocusIds`.

## Counts

(a) 0 / (b) 0 / (c) 0.

Spec: Good (0 missing, 0 creep, 0 wrong)
