# 122 — Import document scope

This scope is the ImportDocument row of [122 — Document state moves by Notice](../issues/122-document-state-moves-by-notice.md).

## 1 Signatures

[ ] `ImportDocument.planParseFile` takes a Graph, a File Node id, and text, and returns `Result<Op list, string>`.
[ ] `DocumentPersistWrite.planParseFile` takes a data directory, a Graph, a File Node id, and optional text, and returns `Result<Op list, string>`.
[ ] `DocumentPersistWrite.parseOpsWithMtimeStamp` calls `ImportDocument.planParseFile` and can append `Op.SetUpdateTime`.
[ ] `ParseThreadDeps.finishParse` is `NodeId -> unit`.
[ ] `Api.applyParseFile` takes a Core changes door, a data directory, a File Node id, optional text, and a State, and returns `Async<IResult>`.
[ ] `Api.postParseFile` takes a Core changes door, a data directory, and a body, and returns `Async<IResult>`.
[ ] `CoreMailboxApi.postNotice` takes a Mailbox host and a Notice.
[ ] `DocumentOpImpact.noticesBeside` takes an Op list and returns a Notice list.
[ ] `CoreMailboxLoop.applyNotice` takes persist handlers and a Notice and returns `Result<unit, string>`.
[ ] `CoreMailboxFilesAxis.applyNodeParsed` takes persist handlers and a Node id and returns `Result<unit, string>`.
[ ] `GraphMutate.setDocumentState` writes the new document-state enum.
[ ] `GraphMutate.setParseState` writes `ParseState`.

## 2 Match sites

### Constructor

[ ] `ImportDocument.planParseFile` at `src/Shared/dotnet/ImportDocument.fs` line 130 builds `Op.SetDocumentState(fileId, Unparsed, Current)` when `DocumentState.toLegacy` is Unparsed.
[ ] That constructor is the Parsed mark. The code enum `Current` is Parsed.
[ ] A File Node whose legacy state is Parsed gets an empty mark list.
[ ] A File Node whose legacy state is PullingFile returns the error `no file on server` before the constructor.

### Callers

[ ] `DocumentPersistWrite.parseOpsWithMtimeStamp` calls `ImportDocument.planParseFile`.
[ ] `DocumentPersistWrite.planParseFile` calls `parseOpsWithMtimeStamp`.
[ ] `ParseThread.parseFile` calls `DocumentPersistWrite.planParseFile` with no text override, then `ParseThread.postParseOps` (`CoreDoor.postGraphOnly`, command name `Parse`).
[ ] `Api.applyParseFile` calls `DocumentPersistWrite.planParseFile`, then `CoreDoor.postGraphOnly` with command name `Parse`.
[ ] `Api.postParseFile` calls `Api.applyParseFile`.
[ ] `ImportDocument.buildReconcilePackage` calls `ImportDocument.planParseFile` only for a Parsed File Node, so that call omits this constructor.
[ ] `ImportDocument.buildReconcilePackage` returns the error `file is unparsed; use cold import` for an Unparsed File Node.

### Notice posts

[ ] `DocumentOpImpact.noticesBeside` maps `Op.SetDocumentState(id, Unparsed, Current)` to `Notice.NodeParsed id` when the same Op list has no transition to PullingFile or Unparsed.
[ ] `FileAgent.postGraphOnlyNotices` posts that list through the bound persist-finished callback on an accepted graph-only Change.
[ ] `DbAgent.commitPostChange` posts `DatabaseProjection.noticesBeside` through the same callback on an accepted graph-only Change.
[ ] `DatabaseProjection.noticesBeside` calls `DocumentOpImpact.noticesBeside`.
[ ] `ParseThread.bootCore` sets `finishParse` to `CoreMailboxApi.postNotice` of `Notice.NodeParsed`.
[ ] `ParseThread.afterPost` calls `finishParse` after a successful content post.
[ ] `Api.applyParseFile` calls `CoreMailboxApi.postNotice` of `Notice.NodeParsed fileId` after `CoreDoor.postGraphOnly` accepts a non-empty Op list.
[ ] An Unparsed File Node makes that Op list non-empty because the constructor is in the list.
[ ] `Api.postEvents` posts `Api.inferredNodeEvents`, which calls `DocumentOpImpact.noticesBeside`. This constructor's production apply is `CoreDoor.postGraphOnly`.

## 3 Existing tests

[ ] `planParseFile unparsed marks Current` in `tests/Shared.Tests/ImportDocumentTests.fs` checks that an Unparsed File Node yields `Op.SetDocumentState(fileId, Unparsed, Current)` and that apply leaves the node Parsed.
[ ] `planParseFile blank input marks Unparsed file Current` in `tests/Shared.Tests/ImportDocumentTests.fs` checks the same constructor for blank text.
[ ] `planParseFile Unparsed with prior children warms and keeps line ids` in `tests/Shared.Tests/ImportDocumentTests.fs` checks that the constructor leads the warm-reconcile batch.
[ ] `buildReconcilePackage rejects unparsed file` in `tests/Shared.Tests/ImportDocumentTests.fs` checks the error `file is unparsed; use cold import`.
[ ] `planParseFile logs NodeParsed beside SetDocumentState` in `tests/Server.Tests/NodeEventExpandTests.fs` checks that a stacked File parse logs the Set op and `Notice.NodeParsed`, and that `Api.postParseFile` does the same for a second File Node.
[ ] `ParseThread loop runs planParseFile for stacked File` in `tests/Server.Tests/ParseThreadLoadTests.fs` checks that the parse loop applies file text, with `finishParse` posting `Notice.NodeParsed`.

## 4 Copy-from

[ ] `DocumentOpImpact.noticesBeside` is the helper that maps this `Op.SetDocumentState` to `Notice.NodeParsed`.
[ ] `ParseThread.finishParse` is the helper that posts `Notice.NodeParsed` after `ImportDocument.planParseFile` runs on the parse thread.
[ ] `Api.applyParseFile` is the helper that posts `Notice.NodeParsed` on the `Api.postParseFile` accept path.
[ ] `DirectoryReconcile.planDirectoryReconcile` uses the same shape: the planner appends `Op.SetDocumentState(id, Unparsed, Current)`, and `ParseThread.finishParse` posts `Notice.NodeParsed`.

## 5 Implement

[ ] Section 3 — Look at of 122 — Document state moves by Notice left `src\Shared\dotnet\ImportDocument.fs` line 130 open while the Mailbox path already posts `Notice.NodeParsed` for that `Op.SetDocumentState`.
[ ] Section 5 — Changes of 122 — Document state moves by Notice asks for a send at each `SetDocumentState`. [Document-state events](../spec.md) keeps `ImportDocument.planParseFile` unchanged. The send is `ParseThread.finishParse` and `Api.applyParseFile`.
[ ] Section 4 — Context of 122 — Document state moves by Notice asks for a Notice identical to `SetDocumentState`. `Notice.NodeParsed` writes Parsed through `GraphMutate.setParseState`. On a File Node that write matches `Node.withDocumentState` for the code enum `Current`. Persist state stays unchanged. `CoreMailboxFilesAxis.applyNodeParsed` also requests persist when the node is Parsed and Unpersisted.
[ ] Section 6 — Tests of 122 — Document state moves by Notice names no new test.
[ ] This slice adds no test, so no new test observes a public seam.
[ ] The source stays as read.
[ ] The checked row is `src\Shared\dotnet\ImportDocument.fs` line 130 in section 3 — Look at.
