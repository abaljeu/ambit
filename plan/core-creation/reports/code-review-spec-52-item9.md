# Spec — [52 — Run must not launch when edit commit fails](../issues/52-run-abort-when-commit-fails.md)

Range: `git diff origin/staging...HEAD`.

## 1. Missing or partial

1. Partial Client proof — spec: "Proof: Run while Editing with a SetText that fails CAS does not POST `/command`." [RunEditCommitTests.fs](tests/Shared.Tests/RunEditCommitTests.fs) `failed Editing SetText CAS does not SubmitCommand` copies [RunLaunch.fs](src/Client/RunLaunch.fs) `mayLaunchAfterEditCommit` and `afterEditCommit`, then the tryStart arm of [Commands.fs](src/Client/Commands.fs) `execRunOp`. The Fact injects a SetText CAS-fail helper and asserts no `SubmitCommand` Effect. It does not run Browser `execRunOp` or `commitIfEditing`, and it does not POST `/command`. [App.fs](src/Client/App.fs) `runSubmitCommand` POSTs only for `SubmitCommand`; a production `execRunOp` call would be a proxy. This copy is not that call. A later `execRunOp` that appends `SubmitCommand` after a failed Editing commit will not fail this Fact.

## 2. Scope creep

None in product behaviour. [CommandRequest.fs](src/Shared/CommandRequest.fs) `actorStart` is a factory extract; ActorStart fields stay the same. Non-goals (CAS root cause, [51 — Browser Run Focus vs Command](../issues/51-browser-run-focus-vs-command.md), Undo) are not in the product F#.

## 3. Implemented but wrong

1. Abort keys off lastCmdResult identity, not commit failure — spec: "If `commitIfEditing` was required (mode was Editing) and commit failed (error result / no successful text apply when text needed commit), **do not** `SubmitCommand` / Amble Run." [RunLaunch.fs](src/Client/RunLaunch.fs) `mayLaunchAfterEditCommit` returns false only when `wasEditing` and `after` is a new `CmdLastResult.Error` (`after <> before`). A failed Editing SetText that writes the same Error already on the model leaves `mayLaunch` true. `execRunOp` will still append `SubmitCommand`.

Server start item 1 and item 3 match: [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) `dispatchStartActor` drops the live row and replies Error when durable ActorStart fails; [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) `ActorStart persist Error leaves Focus out of liveFocusIds` forces that Event failure.
