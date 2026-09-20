# Spec review — 52 Run abort when commit fails

Range: three-dot `origin/staging...HEAD` (merge-base `bd1e8059`, tip `d36c65ef`). Spec: [52 — Run must not launch when edit commit fails](plan/core-creation/issues/52-run-abort-when-commit-fails.md). Type bug-fixing. Status `coded` (unchanged).

## (a) Missing or partial

1. Partial Client proof — spec line: "Proof: Run while Editing with a SetText that fails CAS does not POST `/command`." [CommandRequestTests.fs](tests/Shared.Tests/CommandRequestTests.fs) `failed Editing SetText CAS does not SubmitCommand` fails `GraphMutate.setText` with a bad old text, then builds `mayLaunchAfterEditCommit` and `commandSubmitEffects` by hand. It does not run [Commands.fs](src/Client/Commands.fs) `execRunOp` or [UpdateHelpers.fs](src/Client/UpdateHelpers.fs) `commitIfEditingForRun`, and it does not POST `/command`. The Run abort itself is present: `commitIfEditingForRun` then no `SubmitCommand` and no Amble when that commit wrote a new Error.

## (b) Behaviour not asked for

None in product code. [UpdateAmbleRun.fs](src/Client/UpdateAmbleRun.fs) `runAmbleOp` uses the same abort so Amble Run does not run after a failed Editing commit (spec Client Run item 1). Server keeps putLive then ActorStart Event and drops the live row on persist Error (spec Server start item 2 prefer: match existing mailbox style). Non-goals (CAS root cause, ticket 51 encode, Undo) are not in the diff.

## (c) Implemented but wrong

None. [Commands.fs](src/Client/Commands.fs) `execRunOp` returns the committed model with the commit Error and no `SubmitCommand` when `mayLaunch` is false. [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) `dispatchStartActor` drops the live row and replies Error when ActorStart Event persist fails. [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) `ActorStart persist Error leaves Focus out of liveFocusIds` matches spec Server start item 3.

## Counts

(a) 1 partial / (b) 0 / (c) 0.

Spec: Needs changes (1 partial, 0 creep, 0 wrong)
