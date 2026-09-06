# Commit 14 and implement 15

Date: 2026-09-06

Issue 14 commit is on `dev`. Issue 15 is implemented and left uncommitted.

## Issue 14 commit

- Hash: `874dc926fecd4f6441a571aa1c5096b07170e595`
- Message: Core holds process-lifetime credentials; an inactive sender is Unauthorized and maps to HTTP 401.
- Script: [[scripts/commit.sh]] with an explicit file list under `src/`, `tests/`, and [[plan/core-creation/]]. [[plan/skills-cleanup/]] was not staged.

## Issue 15

[[../issues/15-launch-actor-and-hold-span.md]] — launch a registered Actor on a non-empty span, return a never-reused public number, put the send credential only in the Actor and the Core set, refuse overlap, and show lock-present on live span Nodes without putting lock in SQL or History.

Project Stage stays `active`.

## What 15 implemented

- [[src/Shared/GraphSpan.fs]] — extract parent plus span children and owned descendants; span NodeIds; lock-present overlay on those ids only (empty set is a no-op; not an O(nodes) walk).
- [[src/Shared/Model.fs]] — `Node.lockPresent` defaults to false. [[src/Shared/Serialization.fs]] encode omits the field. Projection rows and SQL statements still have no lock column.
- [[src/Server/Core/CoreActorPool.fs]] — register by name; launch off the apply mailbox; public numbers from 1; credential added to [[14-server-tracks-credentials.md]]'s set; overlap refuse.
- [[src/Server/Core/CoreRuntime.fs]] — process-lifetime pool; `getHandle` overlays lock-present for live state.

No HTTP Command route. Query, cancel, delete-actor, and Browser lock UI stay 16–22.

## Tests

- `dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~GraphSpanTests"` — 5 passed.
- `dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~GraphSpanTests|FullyQualifiedName~SerializationTests"` — 42 passed (includes Node JSON omits lock-present).
- `dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~CoreActorPoolTests"` — 5 passed.
- Earlier combined Server filter with CoreCredentialsTests and CoreChangesTests — 13 passed.
- `./scripts/client.sh build` — Fable and esbuild succeeded after Shared edits.

## Leftover working tree

Issue 15 sources, tests, [[plan/core-creation/project.md]], and this report are uncommitted. [[plan/skills-cleanup/]] was already untracked and was not part of this work. [[.agents/skills/wayfinder/SKILL.md]] is modified and is not part of 14 or 15.
