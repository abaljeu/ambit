# 118 — Test Actors return their result
**Status**: superseded
One job: The two recording-pool door tests stop a run by posting `Notice.ActorStopped`. Alan, 2026-11-13: this ticket is superseded by [114 — Remove CoreMsg.ActorStop](114-remove-coremsg-actorstop.md). That ticket holds the work. The chain is [113 — Actors return their ActorResult](113-actors-return-their-actorresult.md), then [117 — The pool posts Notice.ActorStopped when an Actor ends](117-the-pool-posts-notice-actorstopped-when-an-actor-ends.md), then 114.

Type: coding
Blockers: 117 — The pool posts Notice.ActorStopped when an Actor ends. 114's blocker is that same ticket.

## Scope

[x] Alan, 2026-11-13: 114 holds the two recording-pool tests from this ticket.
[x] The two recording-pool tests in `tests/Server.Tests/CoreMailboxDoorTests.fs` are ``ActorStopped Notice drops live row`` and ``ActorStopped Notice is logged and drops live row``. Each posts `Notice.ActorStopped` with `CoreMailboxApi.postNotice`.
[x] The chain is 113, then 117, then 114.

## Inputs

### Working

[x] The two tests and their `trySecretForActorNode` change are Inputs of 114.

### Reference

[x] The 2026-11-13 fold decision is in the project file.

## Do not load

[x] This ticket has no build. 114 is the build.

## Process

1. [x] Alan, 2026-11-13, folded this ticket into 114. Stop. Build 114.

## Outputs

[x] No files. 114 names the outputs.

## Stop when

[x] This ticket is superseded. Build 114.

## Check

[x] A reader who reads only the One job line can say that 114 holds this work.
