# 122 — Document state moves by Notice — DirectoryReconcile notice direction

Slice of [122 — Document state moves by Notice](plan/core-refinement/issues/122-document-state-moves-by-notice.md). Line 226 of `src/Shared/dotnet/DirectoryReconcile.fs` is `Op.SetDocumentState(directoryId, Unparsed, Current)` inside `DirectoryReconcile.markCurrent`.

## 1. Direction

[x] `DirectoryReconcile.planDirectoryReconcile` returns the Op list.
[x] `ParseThread.parseDirectory` posts that list, then `ParseThread.finishParse` posts `Notice.NodeParsed` for the same Directory Node or Workspace Node.
[x] The graph-only accept of that Change posts the same `Notice.NodeParsed` from `DocumentOpImpact.noticesBeside`.

## 2. Toward the Mailbox

[x] `src/Server/ParseThread.fs` `ParseThread.bootCore` sets `finishParse` to `CoreMailboxApi.postNotice` of `Notice.NodeParsed`.
[x] `src/Server/Core/CoreMailboxApi.fs` `CoreMailboxApi.postNotice` enqueues `CoreQueue.Notice`.
[x] `src/Server/Core/FileAgent.fs` `FileAgent.postGraphOnlyNotices` posts `DocumentOpImpact.noticesBeside` through the bound `postNotice`.
[x] `src/Server/Core/DbAgent.fs` `DbAgent.commitPostChange` posts `DatabaseProjection.noticesBeside` through the bound `postNotice`.
[x] `src/Server/Core/CoreMailbox.fs` `CoreMailbox` binds that post to `CoreMailboxApi.postNotice`.

## 3. SetDocumentState site and its Notice

[x] `DirectoryReconcile.markCurrent` at line 226 emits `Op.SetDocumentState(directoryId, Unparsed, Current)` when the Node is Unparsed.
[x] That Op writes Parsed.
[x] The equivalent Notice is `Notice.NodeParsed directoryId`.
[x] `ParseThread.afterPost` posts that Notice after the Op post returns Ok.
[x] `DocumentOpImpact.noticesBeside` returns that same Notice for this transition, and the graph-only accept posts it.

## 4. Received path

[x] `src/Server/Core/CoreMailboxLoop.fs` `CoreMailboxLoop.dispatchItem` matches `CoreQueue.Notice`.
[x] `src/Server/Core/CoreEventDispatch.fs` `CoreEventDispatch.logNotice` appends `EventBody.Notice`.
[x] `CoreMailboxLoop.applyNotice` matches `Notice.NodeParsed`.
[x] `src/Server/Core/CoreMailboxFilesAxis.fs` `CoreMailboxFilesAxis.applyNodeParsed` writes Parsed when `pullingFile` is clear.
[x] This site starts from Unparsed, so the Notice write matches the Op write, and a repeat leaves Parsed in place.

## 5. Change

[x] Source stays as read.
[x] The DirectoryReconcile look-at row in 122 — Document state moves by Notice is checked.

## 6. Tests

[x] Section 6 of 122 — Document state moves by Notice names no new test.
[x] No source file was edited, so the test suite stayed unrun.
