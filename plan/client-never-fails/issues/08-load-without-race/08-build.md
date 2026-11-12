# 08 — Load without a race build

Ticket: [08 — Load without a race](08-load-without-race-ticket.md)

Check: pass

## Stage

[x] Stage is build in [08 — Load without a race](CONTEXT.md).

## Edits

[x] `applyLoadResponse` in `src/Shared/SyncLogic.fs` calls `applySyncResponse`. The `raced Load answer` guard is gone.
[x] LoadDone in `src/Client/Update.fs` calls that apply.
[x] Path 9 — Load without a race in [Client never fails on server data architecture](../../arch.md) is ticked for the three Notice apply steps.

## Seams

[x] `SyncLogic.applySyncResponse` in `src/Shared/SyncLogic.fs` is `(response: SyncResponse) -> (state: ClientSyncState) -> Result<ClientSyncState, string>`.
[x] `SyncLogic.applyLoadResponse` in `src/Shared/SyncLogic.fs` is `(responseEventId: EventId) -> (hasPendingLocal: bool) -> (response: SyncResponse) -> (state: ClientSyncState) -> Result<ClientSyncState, string>`. The body is `applySyncResponse response state`. `_responseEventId` and `_hasPendingLocal` are unused.
[x] `Update.update` in `src/Client/Update.fs` handles `SysMsg (LoadDone (stateOpt, syncResponse, responseRevision, readyOpt))` and calls `SyncLogic.applyLoadResponse`.
[x] Production callers of `applyLoadResponse` are LoadDone. Shared tests call it too.

## Tests

[x] ``applyLoadResponse answer-only with pending applies the Load reply`` in `tests/Shared.Tests/SplitOriginTraceTests.fs`. Seam: `SyncLogic.applyLoadResponse`. Pending local work is true and the queue holds the split. The result is `Ok`, the split stays visible, and the pending queue stays.
[x] ``applyLoadResponse applies a Notice with a nodes-only Load answer`` in `tests/Shared.Tests/SplitOriginTraceTests.fs`. Seam: `SyncLogic.applyLoadResponse`. The reply carries `Notice.NodePersisted` and a nodes-only answer. The result is `Ok`, the event id is the Notice id, and the split stays visible.
[x] ``answer-only Load at the same event id applies with pending local work`` in `tests/Shared.Tests/ClientHistoryRuntimeTests.fs`. Seam: `SyncLogic.applyLoadResponse`. Pending local work is true and the queue is empty. The result is `Ok`, ClientHistory stays, the event id stays, and the Workspace child list is `Loaded`.
[x] ``answer-only Load applies when the event id differs`` in `tests/Shared.Tests/ClientHistoryRuntimeTests.fs`. Seam: `SyncLogic.applyLoadResponse`. The response event id differs and the event list is empty. The result is `Ok`, ClientHistory stays, the stored event id stays, and the Workspace child list is `Loaded`.
[x] ``answer-only Load at the same event id preserves ClientHistory`` passed. That fact stays.

## Size

[x] Source files changed: 2. Tests changed: 4. Inside the [Size limit](../../context/size-limit.md).

## Check

[x] `scripts/agent-test.sh` with no arguments passed 75 tests: ClientHistoryRuntimeTests, SplitOriginTraceTests, SyncLogicTests.
[x] Shared tests with `AMBIT_FULL_SUITE=1` passed 1936, skipped 3, failed 0, total 1939.
[x] Server tests with `AMBIT_FULL_SUITE=1` and `WarningsNotAsErrors=FS3511` passed 777, skipped 1, failed 0, total 778.
[x] `scripts/agent-test.sh --full` does not compile `tests/Server.Tests`. FS3511 is an error in `PersistApplyTests.fs`, `DbAgentTests.fs`, and `StateEndpointTests.fs`. The same three errors are on a clean staging tree.

## Status

[x] Ticket status is `coded`.
