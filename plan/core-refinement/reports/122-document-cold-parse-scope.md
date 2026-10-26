# 122 — Document state moves by Notice — document cold parse

Ticket: [122 — Document state moves by Notice](../issues/122-document-state-moves-by-notice.md).

Slice: the Look at row for `src/Shared/documents/DocumentColdParse.fs` line 294. Live line 294 is the `Op.SetDocumentState` arm of `targetsDocumentRoot`. An equivalent Notice goes through the Mailbox. `Op.SetDocumentState` stays. Section 6. Tests names no new test.

Spec: [Document-state events](../spec.md) section 3. Sequence item 1. Expand.

## 1. Signatures

[ ] `Op.SetDocumentState of nodeId: NodeId * oldState: DocumentStateEnum * newState: DocumentStateEnum` on `type Op` in `src/Shared/History.fs`.
[ ] `Notice.NodeParsed of NodeId` on `type Notice` in `src/Shared/History.fs`.
[ ] `Ev.documentStateFromNotice : Notice -> (NodeId * DocumentStateEnum) option` in `src/Shared/History.fs`. `Notice.NodeParsed` maps to `(nodeId, Current)`.
[ ] `GraphMutate.setDocumentState : NodeId -> DocumentStateEnum -> DocumentStateEnum -> Graph -> Result<Graph, string>` in `src/Shared/GraphMutate.fs`. The same graph returns when the node already holds `newState`.
[ ] `GraphMutate.setParseState : NodeId -> ParseState -> Graph -> Result<Graph, string>` in `src/Shared/GraphMutate.fs`. Parsed while `pullingFile` is set returns the same graph. An equal stored parse state with `pullingFile` clear returns the same graph.
[ ] `DocumentOpImpact.noticesBeside : Op list -> Notice list` in `src/Shared/DocumentOpImpact.fs`. `Unparsed` to `Current` maps to `Notice.NodeParsed` when the batch has no `PullingFile` or `Unparsed` target.
[ ] `DocumentColdParse.targetsDocumentRoot` (private): `NodeId -> Op -> bool` in `src/Shared/documents/DocumentColdParse.fs`.
[ ] `DocumentColdParse.peelDocumentRootOps : NodeId -> Op list -> NodeId list * Op list` in `src/Shared/documents/DocumentColdParse.fs`.
[ ] `DocumentColdParse.planOpsFromGraphs : Graph -> NodeId -> Graph -> Op list` in `src/Shared/documents/DocumentColdParse.fs`.
[ ] `DocumentColdParse.planApplyCold : Graph -> NodeId -> string -> string -> Result<Op list, string>` in `src/Shared/documents/DocumentColdParse.fs`.
[ ] `DocumentColdParse.planPasteOps : string -> Result<NodeId list * Op list, string>` in `src/Shared/documents/DocumentColdParse.fs`.
[ ] `CoreMailboxApi.postNotice : MailboxHost -> Notice -> unit` (internal) in `src/Server/Core/CoreMailboxApi.fs`.
[ ] `ParseThreadDeps.finishParse : NodeId -> unit` in `src/Server/ParseThread.fs`. `bootCore` fills it with `CoreMailboxApi.postNotice` of `Notice.NodeParsed`.

## 2. Match sites

[ ] `Op.SetDocumentState(id, _, _) when id = documentRootId` in `DocumentColdParse.targetsDocumentRoot` returns true (`src/Shared/documents/DocumentColdParse.fs` line 294).
[ ] `peelDocumentRootOps` keeps ops for which `targetsDocumentRoot` is false. A root `Op.SetDocumentState` leaves the nested list (`src/Shared/documents/DocumentColdParse.fs`).
[ ] `planOpsFromGraphs` builds `NewNode`, `SetText`, `SetName`, `SetClasses`, `NewSpecialNode`, `Replace`, and delete ops (`src/Shared/documents/DocumentColdParse.fs`). The `Op.SetDocumentState` constructor is absent from that function.
[ ] Caller `planPasteOps` calls `planApplyCold` then `peelDocumentRootOps` (`src/Shared/documents/DocumentColdParse.fs`).
[ ] Caller `UpdatePaste.planColdPaste` calls `DocumentColdParse.planPasteOps`, then `applyAndPost` (`src/Client/UpdatePaste.fs`, `src/Client/UpdateHelpers.fs`).
[ ] Caller `ImportDocument.packageFromCold` calls `planApplyCold` then `peelDocumentRootOps` (`src/Shared/dotnet/ImportDocument.fs`).
[ ] Caller `FocusChildrenReplace.peelFrom` and `FocusChildrenReplace.parseAmb` call `peelDocumentRootOps` (`src/Shared/documents/FocusChildrenReplace.fs`).
[ ] Caller `DocumentParseOps.planApplyArtifact` calls `planApplyCold` on a cold read and `planOpsFromGraphs` on a warm read (`src/Shared/dotnet/DocumentParseOps.fs`). That caller skips `peelDocumentRootOps`.
[ ] Caller `ImportDocument.planParseFile` calls `DocumentParseOps.planApplyArtifact`, then appends `Op.SetDocumentState(fileId, Unparsed, Current)` when the File is `Unparsed` (`src/Shared/dotnet/ImportDocument.fs`). `PullingFile` returns `Error "no file on server"`.
[ ] Caller `DocumentPersistWrite.planParseFile` calls `ImportDocument.planParseFile` (`src/Server/DocumentPersistWrite.fs`).
[ ] Caller `ParseThread.parseFile` posts those ops, then `finishParse` posts `Notice.NodeParsed` (`src/Server/ParseThread.fs`).
[ ] Caller `Api.applyParseFile` posts the same ops with `CoreDoor.postGraphOnly`, then `CoreMailboxApi.postNotice` of `Notice.NodeParsed` (`src/Server/Api.fs`).
[ ] `FileAgent.postGraphOnlyNotices` posts `DocumentOpImpact.noticesBeside` for a graph-only Change (`src/Server/Core/FileAgent.fs`).
[ ] `DatabaseProjection.noticesBeside` posts the same list from `DbAgent.commitPostChange` for a graph-only Change (`src/Server/DatabaseProjection.fs`, `src/Server/Core/DbAgent.fs`).

## 3. Existing tests

[ ] ``planApplyCold paste path uses Plain indent nesting`` in `tests/Shared.Tests/DocumentColdParseTests.fs` calls `planApplyCold` then `peelDocumentRootOps` and checks Plain indent nesting.
[ ] ``planApplyCold on empty string yields no top-level peel`` in `tests/Shared.Tests/DocumentColdParseTests.fs` checks an empty peel.
[ ] ``live-parent planApplyCold peel is empty when siblings exist`` in `tests/Shared.Tests/DocumentColdParseTests.fs` checks peel against a live parent.
[ ] ``planApplyCold md heading emits SetClasses md-head`` in `tests/Shared.Tests/DocumentColdParseTests.fs` checks `planOpsFromGraphs` then peel for `SetClasses`.
[ ] ``paste of a heading document is one md-head`` in `tests/Shared.Tests/DocumentColdParseTests.fs` checks `planPasteOps`.
[ ] ``planParseFile rejects file with no server body`` in `tests/Shared.Tests/DocumentColdParseTests.fs` checks `ImportDocument.planParseFile` returns `Error "no file on server"` for `PullingFile`.
[ ] ``planParseFile logs NodeParsed beside SetDocumentState`` in `tests/Server.Tests/NodeEventExpandTests.fs` calls `Api.postParseFile` and checks a Current `Op.SetDocumentState` plus a logged `Notice.NodeParsed`.
[ ] ``ParseThread loop runs planParseFile for stacked File`` in `tests/Server.Tests/ParseThreadLoadTests.fs` runs `ParseThread.start` with `finishParse` posting `Notice.NodeParsed` through `CoreMailboxApi.postNotice`.

## 4. Copy-from

[ ] `ParseThread.bootCore` in `src/Server/ParseThread.fs` sets `finishParse` to `CoreMailboxApi.postNotice` of `Notice.NodeParsed`.
[ ] `Api.applyParseFile` in `src/Server/Api.fs` posts `Notice.NodeParsed` after `CoreDoor.postGraphOnly` accepts the parse Change.
[ ] `DocumentOpImpact.noticesBeside` in `src/Shared/DocumentOpImpact.fs` maps `Op.SetDocumentState(id, Unparsed, Current)` to `Notice.NodeParsed` when the Change is clear of upload and arrival targets.
[ ] `Api.postEvents` in `src/Server/Api.fs` posts that list through `inferredNodeEvents` and `CoreMailboxApi.postNotice` after `CoreDoor.postEvents` succeeds.
[ ] `GraphMutate.setDocumentState` in `src/Shared/GraphMutate.fs` is the write `Op.SetDocumentState` uses. `Ev.applyNotice` calls it with the enum from `documentStateFromNotice`.

## 5. Implement

[ ] Section 3. Look at names line 294 as a `SetDocumentState` send. The live arm returns true so `peelDocumentRootOps` removes that op from the nested list. `planOpsFromGraphs` builds content ops. The constructor `Op.SetDocumentState` is absent there.
[ ] Section 5. Changes says send a Notice through the Mailbox at that site and keep `Op.SetDocumentState`. `DocumentColdParse` is a shared Op-list planner. Document-state events section 3. Sequence item 1. Expand keeps `ImportDocument.planParseFile` unchanged. `planParseFile` runs on the parse thread. `ParseThread.finishParse` posts `Notice.NodeParsed`. `planApplyCold` and `planOpsFromGraphs` are the planners that call uses. `peelDocumentRootOps` is the paste, cold-import-package, and focus-reply path.
[ ] A Notice built inside `targetsDocumentRoot` would name the disposable stub root. Paste, `ImportDocument.packageFromCold`, and `FocusChildrenReplace` omit that root from the live graph. The live import Change carries `Op.SetDocumentState` from `ImportText.markDocumentCurrentBeforeParse`. `Api.postEvents` posts the matching Notice. That Look at row is already checked.
[ ] The File parse Change can still carry `Op.SetDocumentState(fileId, Unparsed, Current)` from `ImportDocument.planParseFile`. `Api.applyParseFile` and `ParseThread.finishParse` each post `Notice.NodeParsed` for that file through `CoreMailboxApi.postNotice`. `FileAgent.postGraphOnlyNotices` posts the same Notice from `noticesBeside` when that op is on the graph-only Change.
[ ] Section 4. Context requires a Notice identical to `SetDocumentState` and idempotent. `documentStateFromNotice` maps `Notice.NodeParsed` to `Current`. `GraphMutate.setDocumentState` returns the same graph when the node already holds `Current`. `GraphMutate.setParseState` of `Parsed` returns the same graph when the node is already `Parsed` with `pullingFile` clear, and when `pullingFile` is set. `ImportDocument.planParseFile` returns an error for `PullingFile` before it appends `Op.SetDocumentState`.
[ ] Section 6. Tests of 122 — Document state moves by Notice names no new test.
[ ] Section 6. Tests also says the default test set runs for each edited file. This slice edits no source file, so that run stays idle. No named new test observes a public seam.
