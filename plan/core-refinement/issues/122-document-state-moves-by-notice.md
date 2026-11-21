# 122 — Document state moves by Notice

## 1. Type

coding

## 2. Blockers

## 3. Look at

Mark with a check only if it passes the requirement that in addition to sending SetDocumentState, it also sends an equivalent Notice.

[x] src\Server\Api.fs  253
[x] src\Server\DatabaseProjection.fs  95
[x] src\Server\LazyLoadReconciliationServer.fs  360
[x] src\Shared\DocumentOpImpact.fs  40  62
[x] src\Shared\History.fs  119  248  332  333  372  657
[x] src\Shared\ImportText.fs  47
[x] src\Shared\ResidentProjection.fs  20
[x] src\Shared\Serialization.fs  358  412
[x] src\Shared\WorkspaceUploadStructure.fs  143  145  151  153  160  319
[x] src\Shared\documents\DocumentColdParse.fs  294
[x] src\Shared\dotnet\DirectoryReconcile.fs  226
[x] src\Shared\dotnet\ImportDocument.fs  130
[x] src\Shared\dotnet\LazyLoadReconciliation.fs  370
[x] src\Shared\dotnet\LazyLoadReconciliationApply.fs   23   25   32  145  147

## 4. Context
Document state changes by sending Notice from things like Load, Parse, and Persist. Make sure each notice is idempotent and identical to what would happen if SetDocumentState was called.
## 5. Changes

[x] For each of the above files where SetDocumentState is happening, locate where the appropriate notice could be inserted, and send that.  A notice must be sent to through Mailbox or it's useless. Don't remove SetDocumentState.

## 6. Tests

[ ] No new test is required.
[ ] For each edited file, the default test set is run and passes.

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.

See [135 — Backend infers document-state Notices from ops](135-backend-infers-document-state-notices-from-ops.md): §7 maps every `SetDocumentState` site to its replacing Notice. [135 core inventory](../reports/135-core-inventory.md) lists the code that posts Notices today.
