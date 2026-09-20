# Standards — [52 — Run must not launch when edit commit fails](plan/core-creation/issues/52-run-abort-when-commit-fails.md)

Range `origin/staging...HEAD`. F# in range meets [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) 40-line functions and 100-character new lines. Shared [RunEditCommit.fs](src/Shared/RunEditCommit.fs) holds the Editing gate; Browser [RunLaunch.fs](src/Client/RunLaunch.fs) injects `commitIfEditing`; Core [CoreActorPool.fs](src/Server/Core/CoreActorPool.fs) `dropAndReply` drops live Focus on ActorStart persist Error.

## 1. Verdict

Needs changes.

## 2. Hard violations

1. Bare list-item id — [.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md) (number and name every list item). Scan `BARE_ID item 9` in [52 — Run must not launch when edit commit fails](plan/core-creation/issues/52-run-abort-when-commit-fails.md) Comments and Time, and [core creation](plan/core-creation/project.md) Notes. Write the item number with its name, not `item 9`.
2. Bare ids in reports in this range (files not opened here) — same rule. Scan `BARE_ID` `ticket 51`, `item 1`, `item 2`, `item 3` on `plan/core-creation/reports/code-review-52-rereview-client-runlaunch.md`, `code-review-52-rereview-run-abort-when-commit-fails.md`, `code-review-52-run-abort-when-commit-fails.md`, `code-review-spec-52-rereview.md`, `code-review-spec-52-run-abort-when-commit-fails.md`, `code-review-standards-52-rereview.md`, `code-review-standards-52-run-abort-when-commit-fails.md`. Reports are write-once; do not rewrite them unless Alan asks.
3. Blank line between Comments list items in [52 — Run must not launch when edit commit fails](plan/core-creation/issues/52-run-abort-when-commit-fails.md) — [.agents/rules/markdown-writing.md](.agents/rules/markdown-writing.md) (list items have no blank line).
4. Adjacent format-only collapse of `PostGraphOnly` in [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) — [.agents/rules/core-agent-behavior.md](.agents/rules/core-agent-behavior.md) Surgical Changes (do not improve adjacent formatting).
5. Spoken name Client — [.agents/rules/markdown-writing.md](.agents/rules/markdown-writing.md) plus [CONTEXT.md](CONTEXT.md) Browser. Ticket sections `### Client (confirmed)` and `### 1. Client Run` name the Browser project Client.

## 3. Scan printed, not a fail

1. [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) 594→627: test file length is not an [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) fail. Measure-fs-size bindings are under 40 lines. Surgical under-100-line preference is not a script fail ([.agents/rules/core-agent-behavior.md](.agents/rules/core-agent-behavior.md)).

## 4. Judgement smells ([SMELLS.md](.agents/skills/code-review/SMELLS.md))

1. Middle Man — [RunLaunch.fs](src/Client/RunLaunch.fs) `afterEditCommit` only forwards to Shared with Browser `commitIfEditing`. Keep: Browser commit is not Shared.
2. Duplicated Code — [RunEditCommitTests.fs](tests/Shared.Tests/RunEditCommitTests.fs) `afterEditCommitThenTryStart` copies the Browser `execRunOp` tryStart arm. Keep: no Browser test project.
3. Parameter Explosion — [CommandRequest.fs](src/Shared/CommandRequest.fs) `actorStart` takes six parameters to build `ActorStart`. Factory reuses one record; it does not add a new clump.
