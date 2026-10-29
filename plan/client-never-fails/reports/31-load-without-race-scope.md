# 31 — Load without a race scope

Slice: applyLoadResponse applies the reply as any other server reply. The raced Load answer guard is deleted. Update LoadDone uses that apply.

## Signatures

[ ] `SyncLogic.applyLoadResponse` in `src/Shared/SyncLogic.fs` returns `Error` of a string `raced Load answer` when the answer is nodes-only and either pending local work is present or the event id differs.
[ ] `Update.update` matches LoadDone in `src/Client/Update.fs`.

## Match sites

[ ] The guard treats a nodes-only reply with pending work, and a nodes-only reply whose event id differs, as the same `Error` of a string.
[ ] Other replies already go through `applySyncResponse`.

## Existing tests

[ ] ``applyLoadResponse answer-only with pending is raced Load answer`` and ``applyLoadResponse ignores a Notice when guarding a raced Load answer`` in [[tests/Shared.Tests/SplitOriginTraceTests.fs]].
[ ] ``answer-only Load at the same event id refuses pending local work`` and ``answer-only Load refuses an event id mismatch`` in [[tests/Shared.Tests/ClientHistoryRuntimeTests.fs]].

## Copy-from

[ ] `SyncLogic.applySyncResponse` is the apply the Load reply uses after the guard is gone.

## write-ticket

### Source files

[ ] [[src/Shared/SyncLogic.fs]] — `applyLoadResponse`.
[ ] [[src/Client/Update.fs]] — LoadDone calls that apply.

### Tests

[ ] Change the four facts so a Load reply applies and the `raced Load answer` value is gone.

## Size

[ ] Source files: 2. Under the limit of 8.
[ ] Tests added or changed: 4. At the limit of 4.
