# Code review — [52 — Run must not launch when edit commit fails](../issues/52-run-abort-when-commit-fails.md)

Pin: three-dot `origin/staging...HEAD` after fetch. Tip `d7f263059d0a31482b2b6378270e799a1e338cfc`. Staging tip `edea735513e13871acab97e857b6419693910b09`. Diff non-empty. Status stays `coded`.

Claimed tip shape: verified. Shared [CommandRequest.fs](../../../src/Shared/CommandRequest.fs) has no Editing commit and no Shared `execRunOp`; `actorStart` returns History `ActorStart`; `tryStart` and `oneNodeStart` call it. Client [RunLaunch.fs](../../../src/Client/RunLaunch.fs) owns the edit-commit gate; [Commands.fs](../../../src/Client/Commands.fs) `execRunOp` uses `afterEditCommit`. [UpdateHelpers.fs](../../../src/Client/UpdateHelpers.fs) is 402 lines and is not in this diff. [CoreMailboxBackend.fs](../../../src/Server/Core/CoreMailboxBackend.fs) is 389 lines and did not grow past the scan file-length gate.

## Standards

# Standards — [52 — Run must not launch when edit commit fails](../issues/52-run-abort-when-commit-fails.md)

Range `origin/staging...HEAD` at `d7f26305`. Verdict: **Needs changes**. Reports under [reports](./) were not read; scan lines on those files are cited as printed.

## 1. Hard violations

### 1.1 Bare list-item ids — [refer-by-name](.agents/rules/refer-by-name.md)

Never refer by only the id; include the name.

1. **Ticket comments and Time** — [52 — Run must not launch when edit commit fails](../issues/52-run-abort-when-commit-fails.md) lines 53 and 61: `Alan item 9` with no item title.
2. **Project notes** — [project.md](../project.md) line 11: `Alan item 9` with no item title.
3. **Folded reports (scan)** — BARE_ID `ticket 51` / `item 1` / `item 2` / `item 3` in [code-review-52-rereview-run-abort-when-commit-fails](code-review-52-rereview-run-abort-when-commit-fails.md) 17–18, 55, 57, 59, 63; [code-review-52-run-abort-when-commit-fails](code-review-52-run-abort-when-commit-fails.md) 60, 64; [code-review-spec-52-rereview](code-review-spec-52-rereview.md) 7, 9, 11, 15; [code-review-spec-52-run-abort-when-commit-fails](code-review-spec-52-run-abort-when-commit-fails.md) 11, 15; [code-review-standards-52-rereview](code-review-standards-52-rereview.md) 7–8.

### 1.2 Test file length is not a fail — [fsharp-source](.agents/rules/fsharp-source.md)

Scan printed [CoreMailboxDoorTests.fs](../../../tests/Server.Tests/CoreMailboxDoorTests.fs) FILE 594→627. That rule does not apply to tests.

## 2. Judgement (smell baseline)

1. **Duplicated Code** — [RunEditCommitTests.fs](../../../tests/Shared.Tests/RunEditCommitTests.fs) copies `mayLaunchAfterEditCommit` / `commitIfEditingForRun` from [RunLaunch.fs](../../../src/Client/RunLaunch.fs). Shared.Tests cannot call Client; still the same abort shape in two hunks.
2. **Parameter Explosion** — `CommandRequest.actorStart` takes six args (`graph`, `siteMap`, `zoomId`, `focusId`, `commandId`, `eventId`). [fsharp-source](.agents/rules/fsharp-source.md) grouping prefers a named type; `tryStart` already carried this clump.
3. **Middle Man** — `CoreActorPool.dropAndReply` only `pool.drop` then `reply.Reply(Error err)`. [CoreMailboxBackend.fs](../../../src/Server/Core/CoreMailboxBackend.fs) is already 421 lines; not growing it endorses the extract ([fsharp-source](.agents/rules/fsharp-source.md) already-over-400 rule).

## 3. Clean

New F# hunks: no `mutable`, no exceptions, functions ≤14 lines, source lines ≤100, [RunLaunch.fs](../../../src/Client/RunLaunch.fs) 39 lines. [CoreMailboxBackend.fs](../../../src/Server/Core/CoreMailboxBackend.fs) stayed over 400 without growth. Surgical under-100 preference is not a script fail ([core-agent-behavior](.agents/rules/core-agent-behavior.md)).

## Spec

# Spec — [52 — Run must not launch when edit commit fails](../issues/52-run-abort-when-commit-fails.md)

Range: `git diff origin/staging...HEAD`.

## 1. Missing or partial

1. Partial Client proof — spec: "Proof: Run while Editing with a SetText that fails CAS does not POST `/command`." [RunEditCommitTests.fs](tests/Shared.Tests/RunEditCommitTests.fs) `failed Editing SetText CAS does not SubmitCommand` copies [RunLaunch.fs](src/Client/RunLaunch.fs) `mayLaunchAfterEditCommit` and `afterEditCommit`, then the tryStart arm of [Commands.fs](src/Client/Commands.fs) `execRunOp`. The Fact injects a SetText CAS-fail helper and asserts no `SubmitCommand` Effect. It does not run Browser `execRunOp` or `commitIfEditing`, and it does not POST `/command`. [App.fs](src/Client/App.fs) `runSubmitCommand` POSTs only for `SubmitCommand`; a production `execRunOp` call would be a proxy. This copy is not that call. A later `execRunOp` that appends `SubmitCommand` after a failed Editing commit will not fail this Fact.

## 2. Scope creep

None in product behaviour. [CommandRequest.fs](src/Shared/CommandRequest.fs) `actorStart` is a factory extract; ActorStart fields stay the same. Non-goals (CAS root cause, [51 — Browser Run Focus vs Command](../issues/51-browser-run-focus-vs-command.md), Undo) are not in the product F#.

## 3. Implemented but wrong

1. Abort keys off lastCmdResult identity, not commit failure — spec: "If `commitIfEditing` was required (mode was Editing) and commit failed (error result / no successful text apply when text needed commit), **do not** `SubmitCommand` / Amble Run." [RunLaunch.fs](src/Client/RunLaunch.fs) `mayLaunchAfterEditCommit` returns false only when `wasEditing` and `after` is a new `CmdLastResult.Error` (`after <> before`). A failed Editing SetText that writes the same Error already on the model leaves `mayLaunch` true. `execRunOp` will still append `SubmitCommand`.

Server start item 1 and item 3 match: [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) `dispatchStartActor` drops the live row and replies Error when durable ActorStart fails; [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) `ActorStart persist Error leaves Focus out of liveFocusIds` forces that Event failure.

## Summary

Standards: 2 hard (refer-by-name bare ids on the ticket, project, and folded reports; test file-length scan is not a fail), 3 judgement (Duplicated Code, Parameter Explosion, Middle Man); worst within Standards is refer-by-name on live ticket and project notes. Spec: 2 findings (partial Client proof; abort keys off lastCmdResult identity), 0 scope creep; worst within Spec is abort still launching when a failed Editing commit writes the same Error already on the model.
