# 54 — Parse and import stop the Parsed Set — scope

Scope for [54 — Parse and import stop the Parsed Set](../issues/54-parse-and-import-stop-the-parsed-set.md). Mode: implement.

## Signatures

[ ] `ParseThreadDeps`: `{ dataDir; consumer; push; getGraph; postOps; markUnparsed; finishParse: NodeId -> unit }` in `d:\dev\amble\gambol\src\Server\ParseThread.fs`.

[ ] `ParseThread.afterPost`: calls `deps.finishParse nodeId` on successful `postContent` in `d:\dev\amble\gambol\src\Server\ParseThread.fs`.

[ ] `ParseThread.bootCore`: `finishParse` posts `CoreMailboxApi.postNotice core.host (Notice.NodeParsed nodeId)` in `d:\dev\amble\gambol\src\Server\ParseThread.fs`.

[ ] `ParseThread.parseFile`: `DocumentPersistWrite.planParseFile deps.dataDir graph fileId None` then `afterPost` in `d:\dev\amble\gambol\src\Server\ParseThread.fs`.

[ ] `ParseThread.parseDirectory`: `DirectoryReconcile.planDirectoryReconcile` then `afterPost` in `d:\dev\amble\gambol\src\Server\ParseThread.fs`.

[ ] `ParseThread.postParseOps`: `GraphOnlyChangePost.mint "Parse"` then `CoreDoor.postGraphOnly` in `d:\dev\amble\gambol\src\Server\ParseThread.fs`.

[ ] `DocumentPersistWrite.planParseFile`: `dataDir -> graph -> fileId -> textOpt: string option -> Result<Op list, string>`; calls `ImportDocument.planParseFile` in `d:\dev\amble\gambol\src\Server\DocumentPersistWrite.fs`.

[ ] `ImportDocument.planParseFile`: `graph -> fileId -> text -> Result<Op list, string>` in `d:\dev\amble\gambol\src\Shared\dotnet\ImportDocument.fs`.

[ ] `ImportText.markDocumentCurrentBeforeParse` (private): `graph -> focusId -> Op list`; emits `Op.SetDocumentState(focusId, Unparsed, Current)` when focus is document root and Unparsed in `d:\dev\amble\gambol\src\Shared\ImportText.fs`.

[ ] `ImportText.buildImportChange`: `graph -> focusId -> existingChildren -> package -> submissionId -> Ev` in `d:\dev\amble\gambol\src\Shared\ImportText.fs`.

[ ] `ImportText.buildDirectoryMergeChange`: same shape as `buildImportChange` in `d:\dev\amble\gambol\src\Shared\ImportText.fs`.

[ ] `DirectoryReconcile.markCurrent` (private): `graph -> directoryId -> Op list` in `d:\dev\amble\gambol\src\Shared\dotnet\DirectoryReconcile.fs`.

[ ] `DirectoryReconcile.planDirectoryReconcile`: `Input -> Result<Output, string>`; `Output` is `{ ops; push }` in `d:\dev\amble\gambol\src\Shared\dotnet\DirectoryReconcile.fs`.

[ ] `Notice.NodeParsed`: `nodeId: NodeId` case in `d:\dev\amble\gambol\src\Shared\History.fs`.

[ ] `CoreMailboxApi.postNotice`: `MailboxHost -> Notice -> unit` in `d:\dev\amble\gambol\src\Server\Core\CoreMailboxApi.fs`.

[ ] `CoreMailboxLoop.applyNotice`: `Notice.NodeParsed` calls `CoreMailboxFilesAxis.applyNodeParsed` in `d:\dev\amble\gambol\src\Server\Core\CoreMailboxLoop.fs`.

[ ] `CoreMailboxFilesAxis.applyNodeParsed`: `writeParseState` with `ParseState.Parsed`, then optional persist push in `d:\dev\amble\gambol\src\Server\Core\CoreMailboxFilesAxis.fs`.

[ ] `GraphMutate.setParseState`: no-op graph when `parseState = Parsed` and `pullingFile` is set in `d:\dev\amble\gambol\src\Shared\GraphMutate.fs`.

[ ] `Api.inferredNodeEvents`: on `EventBody.Change`, emits `Notice.NodeParsed id` for `SetDocumentState(id, Unparsed, Current)` when no upload or arrival ops in the same Change in `d:\dev\amble\gambol\src\Server\Api.fs`.

[ ] `Api.postEvents`: after `CoreDoor.postEvents`, runs `inferredNodeEvents` and `CoreMailboxApi.postNotice` per notice in `d:\dev\amble\gambol\src\Server\Api.fs`.

[ ] `Api.applyParseFile` / `Api.postParseFile`: `DocumentPersistWrite.planParseFile`, `CoreDoor.postGraphOnly`, then direct `postNotice (Notice.NodeParsed fileId)` in `d:\dev\amble\gambol\src\Server\Api.fs`.

[ ] `CoreMailboxApi.mbLoadNode`: mailbox `Order.Load subject` → `CoreMailboxLoad.dispatchLoad` → `context.parsePush subject` in `d:\dev\amble\gambol\src\Server\Core\CoreMailboxApi.fs` and `CoreMailboxLoad.fs`.

## Match sites

[ ] `ParseThread.finishParse`: wired in `ParseThread.bootCore`; test harness copies in `NodeEventExpandTests.startParse`, `ParseThreadLoadTests`, `MailboxExpandTests`, `LoadSaveCommandTests`.

[ ] `ParseThread.afterPost`: caller of `finishParse` from `parseFile` and `parseDirectory` in `ParseThread.fs`.

[ ] `ParseThread.parseOne`: matches `Special File` → `parseFile`; `Special (Directory | Workspace)` → `parseDirectory` in `ParseThread.fs`.

[ ] `ParseThread.alreadyParsed`: skips work when `node.documentState.parseState = Parsed` (no `finishParse`) in `ParseThread.fs`.

[ ] `ImportText.buildImportChange`: calls `markDocumentCurrentBeforeParse`; match on `DocumentPartition.isDocumentRootNode` and `Unparsed` in `ImportText.fs`.

[ ] `ImportText.buildDirectoryMergeChange`: same `markCurrent` prefix as `buildImportChange` in `ImportText.fs`.

[ ] `ImportDocument.planParseFile`: match on File kind and `DocumentState.toLegacy`; `markCurrent` when legacy `Unparsed` in `ImportDocument.fs`.

[ ] `DirectoryReconcile.planFromEntries`: prepends `markCurrent graph directoryId` to reconcile ops in `DirectoryReconcile.fs`.

[ ] `Api.inferredNodeEvents`: `List.choose` on `Op.SetDocumentState` inside `EventBody.Change` in `Api.fs`.

[ ] `Api.postEvents`: `List.collect inferredNodeEvents` then `List.iter postNotice` in `Api.fs`.

[ ] `Api.applyParseFile`: `postGraphOnly` success branch posts `Notice.NodeParsed fileId` in `Api.fs`.

[ ] `RouteRegistration.postParseRequest`: HTTP handler calls `Api.postParseFile` in `RouteRegistration.fs`.

[ ] `RouteRegistration.createPersistenceContext`: `ParseThread.bootCore boot` starts parse consumer in `RouteRegistration.fs`.

[ ] `CoreMailboxLoad.dispatchLoad`: `context.parsePush subject` after Load subject check in `CoreMailboxLoad.fs`.

[ ] `CoreMailboxLoop.applyNotice`: `Notice.NodeParsed` → `applyNodeParsed` in `CoreMailboxLoop.fs`.

[ ] Production callers of `buildImportChange` / `buildDirectoryMergeChange`: tests and `NodeEventExpandTests` only; no `src/Client` caller today. Import Changes reach the server through `Api.postEvents` when the client posts them.

## Existing tests

[ ] ``import logs NodeParsed beside SetDocumentState`` — `d:\dev\amble\gambol\tests\Server.Tests\NodeEventExpandTests.fs`; posts `ImportText.buildImportChange` ops via `Api.postEvents`; expects `Notice.NodeParsed` and `SetDocumentState` to Current without `ParseThread`.

[ ] ``planParseFile logs NodeParsed beside SetDocumentState`` — `NodeEventExpandTests.fs`; calls `Api.postParseFile`; expects `NodeParsed` beside Current Set on the file, not via parse stack alone.

[ ] ``planDirectoryReconcile logs NodeParsed beside SetDocumentState`` — `NodeEventExpandTests.fs`; `startParse` + push directory id; waits for `NodeParsed` and Current via parse thread.

[ ] ``ParseFinished is NodeParsed`` — `NodeEventExpandTests.fs`; direct `postNotice (Notice.NodeParsed)` logs beside Current.

[ ] ``ParseFinished sets Parsed and leaves PersistState`` — `d:\dev\amble\gambol\tests\Server.Tests\CoreLoopNoticeTests.fs`; `CoreMailboxLoop.applyNotice` with `Notice.NodeParsed`; Parsed axis and persist unchanged.

[ ] ``ParseFinished leaves a pullingFile File Node Unparsed`` — `CoreLoopNoticeTests.fs`; `Notice.NodeParsed` with `pullingFile` true leaves parse Unparsed.

[ ] ``NodeParsed pushes a persist request`` — `CoreLoopNoticeTests.fs`; `applyNodeParsed` push when Parsed and Unpersisted, not when PullingFile.

[ ] ``NodeParsed writes parse state to the database`` — `d:\dev\amble\gambol\tests\Server.Tests\DbAgentNoticeTests.fs`; notice apply writes Parsed to DB when not pulling.

[ ] ``ParseThread loop runs planParseFile for stacked File`` — `d:\dev\amble\gambol\tests\Server.Tests\ParseThreadLoadTests.fs`; Load pushes file; parse thread applies disk content (does not assert `NodeParsed` source).

[ ] ``build import marks unparsed file current before tree operations`` — `d:\dev\amble\gambol\tests\Shared.Tests\ImportTextTests.fs`; first op of `buildImportChange` is `SetDocumentState(Unparsed, Current)`.

[ ] ``planParseFile unparsed marks Current`` / ``planParseFile blank input marks Unparsed file Current`` — `ImportDocumentTests.fs`; ops list contains `SetDocumentState` to Current for Unparsed file parse.

[ ] ``buildImportChange single line attaches to focus`` — `ImportTextTests.fs`; structure of import Change ops, not server notices.

[ ] Directory reconcile planner tests — `DirectoryReconcileTests.fs`; several assert `Assert.False(setsDocumentState planned.ops)` on Parsed directories; unparsed directory plans may include `markCurrent` ops today.

## Copy-from

[ ] `ParseThread.bootCore` `finishParse` — file `d:\dev\amble\gambol\src\Server\ParseThread.fs`, function `bootCore` inner `finishParse`: posts `Notice.NodeParsed` after parse work on the stack.

[ ] `CoreMailboxLoad.dispatchLoad` — file `d:\dev\amble\gambol\src\Server\Core\CoreMailboxLoad.fs`, function `dispatchLoad`: pushes a node id onto the parse stack after mailbox Load (same push `ParseThread.bootCore` registers).

[ ] `CoreMailboxApi.mbLoadNode` — file `d:\dev\amble\gambol\src\Server\Core\CoreMailboxApi.fs`, function `mbLoadNode`: public Load order that reaches `parsePush` (candidate seam for routing `postParseFile` instead of inline `applyParseFile`).

## implement

[ ] Ticket: import routes through the parse thread and `finishParse` posts `NodeParsed` for the focus node. Code: `buildImportChange` / `buildDirectoryMergeChange` embed `markDocumentCurrentBeforeParse`; server applies import via `Api.postEvents`; `inferredNodeEvents` posts `NodeParsed` beside the Change, not from `ParseThread.finishParse`.

[ ] Ticket: stop planners emitting the Parsed mark as `Op.SetDocumentState(Unparsed, Current)`. Code: `markDocumentCurrentBeforeParse`, `ImportDocument.planParseFile` `markCurrent`, and `DirectoryReconcile.markCurrent` still emit that op ([Graph change parsed producers](../arch.md#graph-change-parsed)).

[ ] Ticket: `Api.postParseFile` routes through the parse thread so `finishParse` posts `NodeParsed`. Code: `applyParseFile` plans ops, `CoreDoor.postGraphOnly`, then `CoreMailboxApi.postNotice (Notice.NodeParsed fileId)` on the HTTP thread; `ParseThread.parseFile` is not used for this command.

[ ] Ticket: file parse on the parse thread posts `NodeParsed` from `finishParse`. Code: `ParseThread.parseFile` → `afterPost` → `bootCore` `finishParse` already posts `NodeParsed` when Load or push runs the consumer (matches ticket for stack path only).

[ ] Ticket: directory reconcile on the parse thread posts `NodeParsed` from `finishParse`. Code: `ParseThread.parseDirectory` → `afterPost` → `finishParse` already posts `NodeParsed` when reconcile ops post successfully (matches ticket for stack path).

[ ] Ticket acceptance: when PullingFile is clear, handler sets Parsed; when PullingFile is set, parse stays Unparsed; persist unchanged. Code: `CoreMailboxFilesAxis.applyNodeParsed` + `GraphMutate.setParseState` already implement this; tests ``ParseFinished sets Parsed and leaves PersistState`` and ``ParseFinished leaves a pullingFile File Node Unparsed`` cover the handler.

[ ] Named acceptance tests (ticket **Parsed test**): treat ``import logs NodeParsed beside SetDocumentState``, ``planParseFile logs NodeParsed beside SetDocumentState``, and ``planDirectoryReconcile logs NodeParsed beside SetDocumentState`` in `NodeEventExpandTests.fs` as the public seams. After implement, each should show `Notice.NodeParsed` in the event log from parse-thread `finishParse`, without `Op.SetDocumentState(Unparsed, Current)` in the same Change for the Parsed mark; import and `postParseFile` cases should use parse stack push (or Load) like the directory test, observable via `parsedMsg` / `waitParsed` and `CoreMailboxLoop.applyNotice` behavior under PullingFile.

[ ] ``import logs NodeParsed beside SetDocumentState``: observe via `Api.postEvents` + event log notices; assert no Current Set op for Parsed mark; assert `NodeParsed` after parse thread completes on focus id.

[ ] ``planParseFile logs NodeParsed beside SetDocumentState``: observe via `Api.postParseFile` HTTP seam; assert graph update via parse thread and `NodeParsed` from `finishParse`, not inline `postNotice` in `applyParseFile`.

[ ] ``planDirectoryReconcile logs NodeParsed beside SetDocumentState``: already uses `startParse`; tighten assertions so Parsed mark is not in reconcile ops and `NodeParsed` still arrives from `finishParse` only.

## Files this change touches

Count: **14** files (7 source, 7 test).

### Source

[ ] `d:\dev\amble\gambol\src\Shared\ImportText.fs` — remove or stop using `markDocumentCurrentBeforeParse` in `buildImportChange` and `buildDirectoryMergeChange`.

[ ] `d:\dev\amble\gambol\src\Shared\dotnet\ImportDocument.fs` — remove `markCurrent` `SetDocumentState` prefix from `planParseFile`.

[ ] `d:\dev\amble\gambol\src\Shared\dotnet\DirectoryReconcile.fs` — remove `markCurrent` from `planFromEntries` / `planDirectoryReconcile` output ops.

[ ] `d:\dev\amble\gambol\src\Server\Api.fs` — drop or narrow `inferredNodeEvents` `NodeParsed` for import Current transition; route `postParseFile` / import follow-up through parse push (`mbLoadNode` or exposed push) instead of `applyParseFile` posting `NodeParsed` directly.

[ ] `d:\dev\amble\gambol\src\Server\ParseThread.fs` — optional: accept optional body text on stack path for desktop `postParseFile`; ensure import focus id enqueue after import Change applies.

[ ] `d:\dev\amble\gambol\src\Server\DocumentPersistWrite.fs` — adjust only if parse-thread file path must mirror `textOpt` handling from `applyParseFile`.

[ ] `d:\dev\amble\gambol\src\Server\RouteRegistration.fs` — wire parse push or Load into `postParseRequest` if `Api.postParseFile` no longer applies ops inline.

### Tests

[ ] `d:\dev\amble\gambol\tests\Server.Tests\NodeEventExpandTests.fs` — three ``… NodeParsed beside SetDocumentState`` tests and helpers (`startParse`, `postChange`) for parse-thread-only Parsed marking.

[ ] `d:\dev\amble\gambol\tests\Shared.Tests\ImportTextTests.fs` — ``build import marks unparsed file current before tree operations`` and related import Change shape tests.

[ ] `d:\dev\amble\gambol\tests\Shared.Tests\ImportDocumentTests.fs` — `planParseFile` tests that expect `SetDocumentState(Unparsed, Current)` in ops.

[ ] `d:\dev\amble\gambol\tests\Shared.Tests\DirectoryReconcileTests.fs` — planner tests where unparsed directories currently receive `markCurrent` in `planned.ops`.

[ ] `d:\dev\amble\gambol\tests\Server.Tests\ParseThreadLoadTests.fs` — extend coverage if `postParseFile` or import enqueue shares the stack with Load.

[ ] `d:\dev\amble\gambol\tests\Server.Tests\DocumentPersistenceTests.fs` — `DocumentPersistWrite.planParseFile` tests if returned ops no longer include Current Set.

[ ] `d:\dev\amble\gambol\tests\Shared.Tests\SpecialNodeStateAxesTests.fs` — only if shared apply paths for Current Set on parse are narrowed (likely unchanged if tests set state explicitly).
