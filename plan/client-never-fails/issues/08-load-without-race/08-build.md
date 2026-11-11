# 08 — Load without a race build

Ticket: [08 — Load without a race](08-load-without-race-ticket.md)

Scope: [08 — Load without a race scope](../../reports/08-load-without-race-scope.md)

## Stage

[x] Stage is build in [08 — Load without a race](CONTEXT.md).

## Edits

[x] `applyLoadResponse` in `src/Shared/SyncLogic.fs` calls `applySyncResponse`. The `raced Load answer` guard is deleted.
[x] LoadDone in `src/Client/Update.fs` calls that apply.
[x] Path 9 — Load without a race in [Client never fails on server data architecture](../../arch.md) is ticked for the three Notice apply steps.

## Tests

[x] ``applyLoadResponse answer-only with pending applies the Load reply`` in `tests/Shared.Tests/SplitOriginTraceTests.fs`. Seam: `SyncLogic.applyLoadResponse`. Pending local work is true and the queue holds the split. The result is `Ok`, the split stays visible, and the pending queue stays.
[x] ``applyLoadResponse applies a Notice with a nodes-only Load answer`` in `tests/Shared.Tests/SplitOriginTraceTests.fs`. Seam: `SyncLogic.applyLoadResponse`. The reply carries `Notice.NodePersisted` and a nodes-only answer. The result is `Ok`, the event id is the Notice id, and the split stays visible.
[x] ``answer-only Load at the same event id applies with pending local work`` in `tests/Shared.Tests/ClientHistoryRuntimeTests.fs`. Seam: `SyncLogic.applyLoadResponse`. Pending local work is true and the queue is empty. The result is `Ok`, ClientHistory stays, the event id stays, and the Workspace child list is `Loaded`.
[x] ``answer-only Load applies when the event id differs`` in `tests/Shared.Tests/ClientHistoryRuntimeTests.fs`. Seam: `SyncLogic.applyLoadResponse`. The response event id differs and the event list is empty. The result is `Ok`, ClientHistory stays, the stored event id stays, and the Workspace child list is `Loaded`.

## Size

[x] Source files changed: 2. Tests changed: 4. Inside the [Size limit](../../context/size-limit.md).

## Check

[x] Verdict line is in [08 — Load without a race check](../../reports/08-load-without-race-check.md).

[x] Ticket status is `coded`.
