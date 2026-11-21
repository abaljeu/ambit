# 122 — Import document notice direction

This direction is the ImportDocument row of [122 — Document state moves by Notice](../issues/122-document-state-moves-by-notice.md).

## 1 Direction

[ ] `ImportDocument.planParseFile` keeps `Op.SetDocumentState(fileId, Unparsed, Current)` when the File Node is Unparsed.
[ ] The code enum `Current` is Parsed.
[ ] The equivalent Notice is `Notice.NodeParsed fileId`.
[ ] The planner returns the Op list. The Mailbox post is on the accept path.
[ ] A second `Notice.NodeParsed` leaves a Parsed File Node with `pullingFile` clear unchanged in `GraphMutate.setParseState`.

## 2 Toward the mailbox

[ ] `src/Server/ParseThread.fs` `ParseThread.parseFile` posts the Op list with `ParseThread.postParseOps`.
[ ] `src/Server/ParseThread.fs` `ParseThread.bootCore` `finishParse` calls `CoreMailboxApi.postNotice` with `Notice.NodeParsed`.
[ ] `src/Server/ParseThread.fs` `ParseThread.afterPost` calls `finishParse` after the content post succeeds.
[ ] `src/Server/Api.fs` `Api.postParseFile` calls `Api.applyParseFile`.
[ ] `src/Server/Api.fs` `Api.applyParseFile` calls `CoreMailboxApi.postNotice` with `Notice.NodeParsed fileId` after `CoreDoor.postGraphOnly` accepts a non-empty Op list.
[ ] `src/Server/Core/FileAgent.fs` `FileAgent.postGraphOnlyNotices` posts `DocumentOpImpact.noticesBeside` on an accepted graph-only Change when persist-finished is bound.
[ ] `src/Server/Core/DbAgent.fs` `DbAgent.commitPostChange` posts `DatabaseProjection.noticesBeside` on an accepted graph-only Change when persist-finished is bound.
[ ] `src/Server/Core/CoreMailboxApi.fs` `CoreMailboxApi.postNotice` enqueues `CoreQueue.Notice`.

## 3 SetDocumentState and its Notice

[ ] The site is `ImportDocument.planParseFile` at `src/Shared/dotnet/ImportDocument.fs` line 130.
[ ] The Op is `Op.SetDocumentState(fileId, Unparsed, Current)`.
[ ] The Notice is `Notice.NodeParsed fileId`.
[ ] `DocumentOpImpact.noticesBeside` selects that Notice for this transition when the Op list has no transition to PullingFile or Unparsed.
[ ] `ParseThread.finishParse` posts that Notice for the parse-thread apply of this Op.
[ ] `Api.applyParseFile` posts that Notice for the `Api.postParseFile` apply of this Op.
[ ] An Unparsed File Node puts this Op in the list, so the `Api.applyParseFile` list is non-empty and the Notice is posted.

## 4 Received path

[ ] `CoreMailboxLoop.dispatchItem` on `CoreQueue.Notice` calls `CoreEventDispatch.logNotice`.
[ ] `CoreMailboxLoop.applyNotice` matches `Notice.NodeParsed` and calls `CoreMailboxFilesAxis.applyNodeParsed`.
[ ] `CoreMailboxFilesAxis.applyNodeParsed` writes `ParseState.Parsed` through `GraphMutate.setParseState`.
[ ] `GraphMutate.setParseState` leaves the graph unchanged when the node is already Parsed and `pullingFile` is clear.
[ ] `GraphMutate.setParseState` leaves a PullingFile node unchanged when the requested state is Parsed.
[ ] Persist state stays unchanged.
[ ] `CoreMailboxFilesAxis.applyNodeParsed` requests persist when the node is Parsed and Unpersisted.
[ ] `Ev.documentStateFromNotice` maps `Notice.NodeParsed` to the code enum `Current`.
[ ] `ResidentProjection.applyDocumentNotice` applies that enum with `Graph.setDocumentState`.

## 5 Change

[ ] Source stays as read.
[ ] The look-at row for `src\Shared\dotnet\ImportDocument.fs` line 130 in section 3 — Look at of 122 — Document state moves by Notice is checked.

## 6 Tests

[ ] Tests stayed unrun.
[ ] Section 6 — Tests of 122 — Document state moves by Notice names no new test.
