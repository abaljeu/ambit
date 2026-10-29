# 33 — Merge in progress scope

Slice: A reply that carries our own changes is accepted. A reply with different changes, while more changes are pending, is AckReconcile.Ignored. The Browser posts the rest and waits for a reply that matches.

## Signatures

[ ] `SyncLogic.reconcileAck` returns `AckReconcile` in `src/Shared/SyncLogic.fs`. `Ok "ignore"` becomes `AckReconcile.Ignored`.
[ ] `Update.reconcileSubmit` calls `reconcileAck` in `src/Client/Update.fs`.

## Match sites

[ ] `isConfirmationEcho` accepts a reply that matches the submitted changes.
[ ] ``late duplicate response is ignored when identities are retired`` already expects `AckReconcile.Ignored` for a retired id.

## Existing tests

[ ] ``late duplicate response is ignored when identities are retired`` in [[tests/Shared.Tests/AckReconcileTests.fs]].
[ ] ``Notice in the reply keeps the confirmation echo`` in `tests/Shared.Tests/AckReconcileTests.fs` covers a matching reply.

## Copy-from

[ ] The `Ignored` arm in `reconcileAck` is the result this ticket uses for a different reply while more changes are pending.

## write-ticket

### Source files

[ ] [[src/Shared/SyncLogic.fs]] — `reconcileAck`.
[ ] [[src/Client/Update.fs]] — after `Ignored`, post the rest and wait for a matching reply.

### Tests

[ ] Add ``different post reply while more changes are pending is Ignored`` in `tests/Shared.Tests/AckReconcileTests.fs`.
[ ] Add ``Ignored reply posts the rest of the pending changes`` in `tests/Server.Tests/BrowserResidencyClientTests.fs`.

## Size

[ ] Source files: 2. Under the limit of 8.
[ ] Tests added or changed: 2. Under the limit of 4.
