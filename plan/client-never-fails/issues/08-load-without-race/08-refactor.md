# 08 — Load without a race

Ticket: [08 — Load without a race](08-load-without-race-ticket.md)

Spec: [Client never fails on server data](../../spec.md)

Arch: [Client never fails on server data architecture](../../arch.md)

Review: [08 — Load without a race](08-review.md)

## Stage

[x] Stage is refactor in [08 — Load without a race](CONTEXT.md).

## 1. Shared fixtures

[x] `splitLoadFixture` in `tests/Shared.Tests/SplitOriginTraceTests.fs` builds the pending split and the stale nodes-only answer.
[x] `assertVisibleSplit` checks children `hello`, `suffix`, `sib`, texts `hel` and `lo`, and the same pending queue.
[x] ``applyLoadResponse answer-only with pending applies the Load reply`` applies that fixture with an empty event list.
[x] ``applyLoadResponse applies a Notice with a nodes-only Load answer`` applies that fixture with `Notice.NodePersisted` and expects event id 9.

## 2. Answer-only Load

[x] `applyAnswerOnlyLoad` in `tests/Shared.Tests/ClientHistoryRuntimeTests.fs` applies an empty event list to an Unloaded Workspace.
[x] ``answer-only Load at the same event id applies with pending local work`` passes event id 4 and pending local work true.
[x] ``answer-only Load applies when the event id differs`` passes event id 5 and pending local work false.
[x] Both facts still expect the same ClientHistory, stored event id 4, and `Loaded`.

## 3. Skipped

[x] `applyLoadResponse` stays a call to `applySyncResponse`. `_responseEventId` and `_hasPendingLocal` stay. Dropping them would make the two answer-only facts the same call as ``answer-only Load at the same event id preserves ClientHistory``.

## 4. Suite

[x] Shared tests with `AMBIT_FULL_SUITE=1` passed 1936, skipped 3, failed 0, total 1939.
[x] Server tests with `AMBIT_FULL_SUITE=1` and `WarningsNotAsErrors=FS3511` passed 777, skipped 1, failed 0, total 778.
