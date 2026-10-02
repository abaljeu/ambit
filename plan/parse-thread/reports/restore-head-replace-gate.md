# Restore HEAD Replace gate

Workplace: `dev`. No commit.

The uncommitted diff changed the shared `Op.Replace` inaccessible-outline gate. That gate is restored to HEAD.

## Files

- [DocumentPartition](src/Shared/DocumentPartition.fs) — restored to HEAD. `replaceBlockedByInaccessible` is gone.
- [History](src/Shared/History.fs) — restored to HEAD. `Op.Replace` uses the HEAD inaccessible-outline check.
- [DirectoryReconcile](src/Shared/dotnet/DirectoryReconcile.fs) — unchanged. The body append stays one `ChildListWire.append` batch, with no `SetDocumentState` and no Unparsed mark.
- [DirectoryReconcileTests](tests/Shared.Tests/DirectoryReconcileTests.fs) — `missing disk files append alphabetically under the directory` no longer attaches an outline child under the directory.

## Tests

Command: `dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~DirectoryReconcileTests|FullyQualifiedName~HistoryTests"`

First run: 66 passed, 1 failed. `missing disk files append alphabetically under the directory` failed with `operation cannot modify an unparsed document; parse it first`. The fixture kept an outline child on an Unparsed directory. That child is removed.

Second run: 67 passed, 0 failed. The History tests that cover the Replace gate passed on both runs.
