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
