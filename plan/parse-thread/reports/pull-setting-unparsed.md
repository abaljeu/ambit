# Pull setting Unparsed

Workplace: `dev`. No commit.

## Ticket

[06 — Setting Unparsed, recursive update](../issues/06-setting-unparsed-recursive-update.md)

Status `defined`. Blocked by none. This ticket does not block [05 — Directory reconcile](../issues/05-directory-reconcile.md), and 05 — Directory reconcile does not block this ticket.

Op.SetDocumentState is not the writer on that ticket. The call is an InMsg through the mailbox private function. The core loop applies that InMsg. The ticket adds no Op.

## Call sites moved

1. **markUnparsedOps** — [Directory reconcile](../../../src/Shared/dotnet/DirectoryReconcile.fs) built `Op.SetDocumentState` to `DocumentState.Unparsed`. That helper is removed. The ticket names this call as an InMsg.
2. **planFromFiles** — That function appended `markUnparsedOps` onto the reconcile ops. The append is removed. Reconcile ops do not carry the Unparsed write.

Core Load, the git-pull handoff, and ParseFinished stay where they are.

## 05 — Directory reconcile

Leaves **Disk-newer**, **Unparsed**, and **Push** moved onto [06 — Setting Unparsed, recursive update](../issues/06-setting-unparsed-recursive-update.md) and are unchecked there. Status stays `coded`. The remaining acceptance still matches the scan: one directory Body, the same reconcile for a Workspace, alphabetical append, and Load then Parse. The push list still names a disk-newer File Node. That naming stays in the scan. The InMsg that sets Unparsed is not implemented.

## Files

- [06 — Setting Unparsed, recursive update](../issues/06-setting-unparsed-recursive-update.md)
- [05 — Directory reconcile](../issues/05-directory-reconcile.md)
- [Parse thread](../project.md) — one note, Updated 2026-10-01, Stage unchanged
- [Directory reconcile](../../../src/Shared/dotnet/DirectoryReconcile.fs)
- [Directory reconcile tests](../../../tests/Shared.Tests/DirectoryReconcileTests.fs)

## Tests

`dotnet test tests/Shared.Tests/Gambol.Shared.Tests.fsproj --filter FullyQualifiedName~DirectoryReconcileTests`

Passed: 3. Failed: 0. Skipped: 0.
