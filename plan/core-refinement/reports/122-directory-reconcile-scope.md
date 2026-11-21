# 122 — Document state moves by Notice — DirectoryReconcile scope

Slice of [122 — Document state moves by Notice](plan/core-refinement/issues/122-document-state-moves-by-notice.md). The look-at row is `src/Shared/dotnet/DirectoryReconcile.fs` line 226. [Document-state events](plan/core-refinement/spec.md) keeps `DirectoryReconcile.planDirectoryReconcile` unchanged. `ParseThread.finishParse` posts `Notice.NodeParsed`.

## 1. Signatures

[x] `DirectoryReconcile.Input` is `{ dataDir: string; graph: Graph; directoryId: NodeId; discover: string -> Result<string list, string> }`.
[x] `DirectoryReconcile.Output` is `{ ops: Op list; push: NodeId list }`.
[x] `DirectoryReconcile.planDirectoryReconcile` is `(input: Input) : Result<Output, string>`.
[x] `DirectoryReconcile.markCurrent` is `(graph: Graph) (directoryId: NodeId)` and returns `Op list`.
[x] Line 226 is `Op.SetDocumentState(directoryId, Unparsed, Current)`.
[x] `ParseThreadDeps.finishParse` is `NodeId -> unit`.
[x] `CoreMailboxApi.postNotice` is `(host: MailboxHost) (msg: Notice)`.
[x] `DocumentOpImpact.noticesBeside` is `(ops: Op list) : Notice list`.
[x] `Notice.NodeParsed` carries `nodeId: NodeId`.

## 2. Match sites

[x] `DirectoryReconcile.markCurrent` builds `Op.SetDocumentState(directoryId, Unparsed, Current)` when `Node.documentStateEnum` is Unparsed.
[x] `DirectoryReconcile.planFromEntries` places `markCurrent` first in `Output.ops`.
[x] `DirectoryReconcile.planDirectoryReconcile` calls `planFromEntries` after the disk scan.
[x] `ParseThread.parseDirectory` is the production caller of `planDirectoryReconcile`.
[x] `ParseThread.parseOne` calls `parseDirectory` for a Directory Node and a Workspace Node.
[x] `ParseThread.postContent` posts `planned.ops` through `ParseThreadDeps.postOps`.
[x] `ParseThread.postParseOps` posts that list as a graph-only Change with command name `Parse`.
[x] `ParseThread.afterPost` calls `finishParse` with the same `directoryId` when that post returns Ok.
[x] `ParseThread.bootCore` sets `finishParse` to `CoreMailboxApi.postNotice` of `Notice.NodeParsed`.
[x] `DocumentOpImpact.noticesBeside` maps `id, Unparsed, Current` to `Notice.NodeParsed id`.
[x] `FileAgent.postGraphOnlyNotices` posts `DocumentOpImpact.noticesBeside` on a graph-only Change.
[x] `DbAgent.commitPostChange` posts `DatabaseProjection.noticesBeside` on a graph-only Change.
[x] `DatabaseProjection.noticesBeside` calls `DocumentOpImpact.noticesBeside` for a Change.
[x] `CoreMailbox.bindPersistFinished` binds that accept post to `CoreMailboxApi.postNotice`.
[x] `tests/Shared.Tests/DirectoryReconcileTests.fs` calls `planDirectoryReconcile` and applies the Op list in the test.
[x] `tests/Server.Tests/NodeEventExpandTests.fs` starts `ParseThread` and pushes the Directory Node.

## 3. Existing tests

[x] `planDirectoryReconcile logs NodeParsed beside SetDocumentState` in `tests/Server.Tests/NodeEventExpandTests.fs` waits until the log holds `Notice.NodeParsed` for the Directory Node, the parse state is Parsed, and the log holds `Op.SetDocumentState` to Parsed.
[x] `disk-newer reconcile ops leave the file Current` in `tests/Shared.Tests/DirectoryReconcileTests.fs` applies the planner Ops and checks the Directory Node is Parsed.
[x] `missing directory attaches while the reconciled node is Unparsed` in `tests/Shared.Tests/DirectoryReconcileTests.fs` applies the planner Ops and checks the reconciled Directory Node is Parsed.
[x] `disk-newer directory stays Current and is not pushed` in `tests/Shared.Tests/DirectoryReconcileTests.fs` checks a Parsed Directory Node yields an Op list with no `Op.SetDocumentState`.
[x] The shared planner tests apply Ops in the test process and leave the Mailbox post to the parse thread.

## 4. Copy-from

[x] `DocumentOpImpact.noticesBeside` is the helper that chooses `Notice.NodeParsed` for `Op.SetDocumentState(id, Unparsed, Current)`.
[x] `ParseThread.bootCore` is the helper that posts that Notice through `CoreMailboxApi.postNotice` from `finishParse`.

## 5. Implement

[x] Section 5 of 122 — Document state moves by Notice tells the implementer to send the Notice from the look-at file.
[x] Document-state events keeps `DirectoryReconcile.planDirectoryReconcile` unchanged.
[x] The code matches Document-state events: `markCurrent` returns the Op, and `ParseThread.finishParse` posts `Notice.NodeParsed` for that same Node id.
[x] The graph-only accept posts the same `Notice.NodeParsed` through `DocumentOpImpact.noticesBeside` when the Op list holds this transition.
[x] `CoreMailboxLoop.dispatchItem` receives `CoreQueue.Notice`, `CoreEventDispatch.logNotice` stores it, and `CoreMailboxFilesAxis.applyNodeParsed` writes Parsed when `pullingFile` is clear.
[x] For this Op the Node is Unparsed, so `pullingFile` is clear, and the Notice write is Parsed, the same parse write as `Op.SetDocumentState(directoryId, Unparsed, Current)`.
[x] A second `Notice.NodeParsed` leaves a Node that is already Parsed unchanged.
[x] Section 6 of 122 — Document state moves by Notice names no new test.
[x] Source stays as read.
[x] The DirectoryReconcile look-at row passes.
