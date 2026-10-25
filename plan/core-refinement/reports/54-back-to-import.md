# 54 — Back to import

Branch: `dev`.

## 1 Load function

[ ] The function that brings file data into the graph is `dispatchLoad` in `src/Server/Core/CoreMailboxLoad.fs`.

[ ] `replyLoad` places `Order.Load` and calls `dispatchLoad`.

[ ] `dispatchStartLoadSaveCommand` passes `dispatchLoad` into `DeskLoadPush.runLoadSave`.

## 2 Parse and document state

[ ] `dispatchLoad` calls `context.parsePush`.

[ ] `CoreMailboxLoop.makeMailBox` sets `parsePush` to `CoreMailboxFilesAxis.markUnparsedThenPush`.

[ ] `markUnparsedThenPush` writes `ParseState.Unparsed` with `GraphMutate.setParseState` on `Notice.MarkUnparsed`, then calls `rawPush`.

[ ] `ParseThread.finishParse` posts `Notice.NodeParsed`.

[ ] `CoreMailboxLoop.applyNotice` calls `CoreMailboxFilesAxis.applyNodeParsed`.

[ ] `applyNodeParsed` writes `ParseState.Parsed` with `GraphMutate.setParseState`.

[ ] `dispatchLoad` and `applyNodeParsed` emit no `Op.SetDocumentState`.

## 3 Who still emits SetDocumentState

[ ] Load and parse: `ImportDocument.planParseFile` prefixes `Op.SetDocumentState` from `Unparsed` to `Current`.

[ ] `DocumentPersistWrite.planParseFile` calls `ImportDocument.planParseFile`.

[ ] `ParseThread.parseFile` posts those ops, then `finishParse`.

[ ] `Api.applyParseFile` posts the same ops, then `Notice.NodeParsed`.

[ ] `DirectoryReconcile.markCurrent` emits the same op inside `planFromEntries`.

[ ] `ParseThread.parseDirectory` posts `planDirectoryReconcile` ops, which include `markCurrent`.

[ ] Paste and Change: `ImportText.markDocumentCurrentBeforeParse` emits `Op.SetDocumentState`.

[ ] `buildImportChange` and `buildDirectoryMergeChange` put that op on `EventBody.Change`.

[ ] `replyEdit` in `CoreMailboxLoop.fs` is `Order.Change` with `ChangeOrigin.Edit`.

## 4 Next step

[ ] The Load door is already correct.

[ ] The leftover on the parse consumer is `planParseFile` and `DirectoryReconcile.markCurrent`.

[ ] The paste builders are a separate Change.

[ ] Smallest next step: remove the `SetDocumentState` prefix from `ImportDocument.planParseFile` and from `DirectoryReconcile.markCurrent`, so Parse posts content ops only and document state stays on `GraphMutate.setParseState`.
