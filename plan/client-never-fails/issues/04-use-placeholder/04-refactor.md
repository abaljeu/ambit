# 04 — Use a placeholder

Ticket: [[04-use-placeholder-ticket.md]]

Spec: [[plan/client-never-fails/spec.md]]

Arch: [[plan/client-never-fails/arch.md]]

Review: [[04-review.md]]

## Stage

Stage is refactor in [04 — Use a placeholder](CONTEXT.md).

## 1. Edit caret

1. **startEditWithCaret.** `startEditWithCaret` in `src/Client/UpdateOps.fs` is 13 lines (46–58). It resolves the focused Node. ROOT returns the same model. An absent Header returns the same model. A present Header enters Editing with the given caret.
2. **startEditOp.** `startEditOp` calls `startEditWithCaret` with `EditCaret.EndOfText`.
3. **startEditAtPos.** `startEditAtPos` calls `startEditWithCaret` with `EditCaret.Utf16Index`.

## 2. Absent Header

1. **absentUnloaded.** `absentUnloaded` in `tests/Shared.Tests/GraphChildMapHelpers.fs` makes one id Absent and Unloaded.
2. **placeholderChildUnderRoot.** `placeholderChildUnderRoot` in `tests/Shared.Tests/ViewModelRowStateTests.fs` calls `absentUnloaded`.
3. **Want fact.** ``compose wants a placeholder id with no child list after parent expand`` in `tests/Shared.Tests/WantTests.fs` calls `absentUnloaded`.
4. **Delete fact.** ``classifyDeleteForChildSpan moves a placeholder child with no Header to TRASH`` in `tests/Shared.Tests/DeleteOpsTests.fs` calls `absentUnloaded`. The child is `MoveToTrash`. The id is under TRASH. The Header stays absent.

## 3. Placeholder instance

1. **placeholderInst.** In the Want fact, `placeholderInst` is `findInstanceId` of the placeholder id. `expandEntry` takes that instance. `buildSiteMapFrom` already sets the zoom root `expanded` to true.

## 4. Node text

Copy, Edit classes, and Exec still read the Node text. Edit returns the same model when the Header is absent.

## 5. Suite

1. **Mapped tests.** `scripts/agent-test.sh` ran `tests/Shared.Tests` with filter `FullyQualifiedName~DeleteOpsTests|FullyQualifiedName~ViewModelRowStateTests|FullyQualifiedName~ViewModelTests|FullyQualifiedName~WantTests`. Passed 257, skipped 0, failed 0.
2. **Client build.** `dotnet build src/Client/Gambol.Client.fsproj` succeeded. 0 warnings. 0 errors.
3. **Scan.** `python scripts/standards-scan.py --diff HEAD` exited 1. The hit is a bare id on line 31 of `plan/client-never-fails/map.md`. [03 — See a moved Node](plan/client-never-fails/issues/03-see-moved-node/03-see-moved-node-ticket.md) owns that bullet color. The scan counts `startEditWithCaret` at 13 lines (46–58) and `startEditOp` at 3 lines (60–62).
