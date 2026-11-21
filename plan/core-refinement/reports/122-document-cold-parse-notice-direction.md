# 122 — Document state moves by Notice — document cold parse direction

Ticket: [122 — Document state moves by Notice](../issues/122-document-state-moves-by-notice.md).

Scope: [122 — Document state moves by Notice — document cold parse](122-document-cold-parse-scope.md).

## 1. Direction

`DocumentColdParse` stays a shared Op-list planner.

`targetsDocumentRoot` classifies a root `Op.SetDocumentState` so `peelDocumentRootOps` leaves it out of the nested list.

`planOpsFromGraphs` builds content ops.

The Mailbox post for a File whose body ops come from `DocumentColdParse.planApplyCold` is `Notice.NodeParsed`, sent by `ParseThread.finishParse` and by `Api.applyParseFile`.

## 2. Toward the mailbox

Parse of a File on the server:

- `src/Shared/documents/DocumentColdParse.fs` `planApplyCold` then `planOpsFromGraphs` (cold), or `planOpsFromGraphs` alone (warm, via `DocumentParseOps.planApplyArtifact`)
- `src/Shared/dotnet/DocumentParseOps.fs` `planApplyArtifact`
- `src/Shared/dotnet/ImportDocument.fs` `planParseFile` (appends `Op.SetDocumentState(fileId, Unparsed, Current)` when the File is `Unparsed`)
- `src/Server/DocumentPersistWrite.fs` `planParseFile`
- `src/Server/ParseThread.fs` `parseFile` then `postParseOps` then `finishParse`
- `src/Server/Core/CoreMailboxApi.fs` `postNotice` of `Notice.NodeParsed`
- `src/Server/Api.fs` `applyParseFile` then `postParseFile` posts that same Notice after `CoreDoor.postGraphOnly`

A graph-only Change that still holds the `Unparsed` to `Current` op also reaches:

- `src/Server/Core/FileAgent.fs` `postGraphOnlyNotices`
- `src/Shared/DocumentOpImpact.fs` `noticesBeside`
- `src/Server/Core/DbAgent.fs` `commitPostChange`
- `src/Server/DatabaseProjection.fs` `noticesBeside`

Paste and cold import package stop at the peeled op list. The stub root stays out of the live graph:

- `src/Shared/documents/DocumentColdParse.fs` `planPasteOps` then `peelDocumentRootOps`
- `src/Client/UpdatePaste.fs` `planColdPaste` then `applyAndPost`
- `src/Shared/dotnet/ImportDocument.fs` `packageFromCold`
- `src/Shared/documents/FocusChildrenReplace.fs` `peelFrom` and `parseAmb`

The live import Change adds its own `Op.SetDocumentState` in `ImportText.markDocumentCurrentBeforeParse`. `Api.postEvents` posts the matching Notice. That Look at row is already checked.

## 3. Each SetDocumentState

One site.

- Line 294, `targetsDocumentRoot`, any `Op.SetDocumentState` whose id is the document root: the arm returns true and `peelDocumentRootOps` omits the op. The arm ignores the old enum and the new enum. `DocumentColdParse` builds no `Op.SetDocumentState`.

The Notice for the File parse that uses this planner is `Notice.NodeParsed`.

`Ev.documentStateFromNotice` maps that Notice to `Current`.

`GraphMutate.setDocumentState` ignores the old enum and returns the same graph when the node already holds `Current`.

`ImportDocument.planParseFile` appends `Op.SetDocumentState(fileId, Unparsed, Current)` only for `Unparsed`. `PullingFile` returns `Error "no file on server"`.

## 4. Received path

`CoreMailboxApi.postNotice` enqueues `CoreQueue.Notice`.

`CoreMailboxLoop.dispatchItem` logs it with `CoreEventDispatch.logNotice`, then `CoreMailboxLoop.applyNotice` calls `CoreMailboxFilesAxis.applyNodeParsed`.

`applyNodeParsed` writes `ParseState.Parsed` through `GraphMutate.setParseState`. A node that already has `Parsed` with `pullingFile` clear stays as it is. A node with `pullingFile` set stays as it is.

The client applies the logged Notice through `SyncLogic.applyLoggedNotice` and `ResidentProjection.applyDocumentNotice`.

`BootCache` applies the same helper.

## 5. Change

Source stays as read.

`Op.SetDocumentState` stays on `type Op` and on the `ImportDocument.planParseFile` append. That append is a separate Look at row.

## 6. Tests

Tests stayed unrun. No source file changed.

Section 6. Tests of 122 — Document state moves by Notice names no new test.

Existing posts:

- `tests/Server.Tests/NodeEventExpandTests.fs` ``planParseFile logs NodeParsed beside SetDocumentState``
- `tests/Server.Tests/ParseThreadLoadTests.fs` ``ParseThread loop runs planParseFile for stacked File``
