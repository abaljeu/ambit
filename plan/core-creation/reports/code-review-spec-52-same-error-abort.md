# Spec: [52 — Run must not launch when edit commit fails](plan/core-creation/issues/52-run-abort-when-commit-fails.md)

Range: `git diff origin/staging...HEAD`. Ticket Status stays `coded`.

Verdict: nits

## 1. Missing or partial

1. Client Run proof is Shared `SubmitCommand`, not POST. Spec: "Proof: Run while Editing with a SetText that fails CAS does not POST `/command`." [RunEditCommitTests.fs](tests/Shared.Tests/RunEditCommitTests.fs) Facts ``failed Editing SetText CAS does not SubmitCommand`` and ``failed Editing SetText with same prior Error does not SubmitCommand`` assert no `SubmitCommand` on helper `afterEditCommitThenTryStart`, which copies the `tryStart` arm. They do not run Browser `execRunOp` and they do not POST. Production [Commands.fs](src/Client/Commands.fs) `execRunOp` does call the Shared gate. The POST path in [App.fs](src/Client/App.fs) is only `SubmitCommand`.

## 2. Scope creep

1. ActorStart factory is extra. Spec Non-goals: "Stream / Focus≠Command encode ([51 — Browser Run Focus vs Command](plan/core-creation/issues/51-browser-run-focus-vs-command.md) is `done`)." [CommandRequest.fs](src/Shared/CommandRequest.fs) adds `actorStart` and Fact ``oneNodeStart is actorStart with zoom focus and command equal``. Encode is the same; What to build does not ask for this factory.

## 3. Wrong

None. Spec Client Run: "If `commitIfEditing` was required (mode was Editing) and commit failed (error result / no successful text apply when text needed commit), **do not** `SubmitCommand` / Amble Run. Keep the commit error visible." [RunEditCommit.fs](src/Shared/RunEditCommit.fs) `mayLaunchAfterEditCommit` is `| true, Some (CmdLastResult.Error _) -> false`. [RunLaunch.fs](src/Client/RunLaunch.fs) wraps `commitIfEditing`. `execRunOp` and [UpdateAmbleRun.fs](src/Client/UpdateAmbleRun.fs) `runAmbleOp` use that wrap. Abort keeps the commit `lastCmdResult`. Spec Server start: "If `putLive` succeeds and durable `ActorStart` (or schedule) fails, **drop** the live row (same as failed start — no live Focus). Reply Error." [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) on `actorStart` Error calls `dropAndReply` (mailbox order kept). Spec: "Proof: forced ActorStart Event failure leaves Focus out of `liveFocusIds`." Fact ``ActorStart persist Error leaves Focus out of liveFocusIds``. Non-goal 1. Fixing root causes of `old text does not match` and Non-goal 3. Undo/global history are untouched.

## 4. Production claims

1. Successful `commitTextEdit` uses `withLastCmdOk`. [UpdateHelpers.fs](src/Client/UpdateHelpers.fs): `| [] -> withLastCmdOk { model with mode = Selecting }, []` and `| Ok (m, effects) -> withLastCmdOk { m with mode = Selecting }, effects`.
2. Abort is any Error after Editing. [RunEditCommit.fs](src/Shared/RunEditCommit.fs): `| true, Some (CmdLastResult.Error _) -> false` with no `after <> before`.
3. Shared [RunEditCommit.fs](src/Shared/RunEditCommit.fs) is the gate; Browser [RunLaunch.fs](src/Client/RunLaunch.fs) wraps `commitIfEditing`.
4. Fact ``failed Editing SetText with same prior Error does not SubmitCommand`` seeds that Error, fails SetText, and asserts no `SubmitCommand`.
