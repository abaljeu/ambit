# 54 — Where import parse routing

Subject: [54 — Parse and import stop the Parsed Set](../issues/54-parse-and-import-stop-the-parsed-set.md).

Branch: `dev`.

## Answer

The smallest Import start is `CoreMailboxLoad.replyEdit` in `src/Server/Core/CoreMailboxLoad.fs`.

After `CoreEventDispatch.postEvent` stores the import Change, call `context.parsePush` with the import focus. That function is `CoreMailboxLoad.markUnparsedThenPush`. `CoreMailboxLoad.dispatchLoad` already calls it for a Load subject. It writes Unparsed, then pushes the id on `ParseStack`. `ParseThread.loop` consumes the id. `ParseThread.finishParse` posts `Notice.NodeParsed`. `CoreMailboxLoad.applyNodeParsed` calls `GraphMutate.setParseState` with `ParseState.Parsed`.

## What that call needs

- A label. `ImportText.buildImportChange` and `ImportText.buildDirectoryMergeChange` set `commandName` to `""`. `MailboxLog.placedOrder` stores that as `PlacedOrder.commandName = None`. `MailboxLog.orderOfBody` maps every `EventBody.Change` to `ChangeOrigin.Edit`, and `CoreMailboxLoad.placeOrder` sends every such order to `replyEdit`.
- The focus id. `buildImportChange` puts it on `ChildListWire.replace`. `buildDirectoryMergeChange` puts it on `ChildListWire.append`.

The queue, the consumer, `finishParse`, and `applyNodeParsed` already exist. The import label and the `parsePush` call are new.

## Candidates

### 1 — replyEdit

`src/Server/Core/CoreMailboxLoad.fs` `replyEdit`. `context.parsePush` is already in scope. This is the smallest start that reaches the parse thread and can stay on the import Change.

### 2 — postEvents

`Api.inferredNodeEvents` in `src/Server/Api.fs` posts `Notice.NodeParsed` when a Change contains `Op.SetDocumentState(id, Unparsed, Current)` and that Change is outside the upload and arrival arms. `Api.postEvents` runs that after `CoreDoor.postEvents`. The test `import logs NodeParsed beside SetDocumentState` posts import ops through `Api.postEvents`. `CoreChangesDoor` holds `host`, `caller`, and `writable`. The parse push is on `MailboxContext`, not on that door.

On disk, `ImportText.markDocumentCurrentBeforeParse` returns `[]` for an Unparsed document root. HEAD still returns `Op.SetDocumentState(focusId, Unparsed, Current)`. With the working copy, `inferredNodeEvents` has no import op to read.

### 3 — ImportText builders

`ImportText.buildImportChange` and `ImportText.buildDirectoryMergeChange` in `src/Shared/ImportText.fs` are the Import Change. Authority is `Browser`. Callers under `src`: none. Callers: tests, including `import logs NodeParsed beside SetDocumentState`.

`LocalProxy.handleImport` serves GET `/_desktop/file` without `content=1` and returns a `DesktopImportPackage`. A directory uses `ImportText.buildPackage`. A file uses `ImportDocument.buildFilePackage`. `Api.getImportFile` serves GET `/ambit/file` through `DocumentPersistPath.importPackageForReference`, which calls `buildFilePackage`. `UpdateCodec.decodeDesktopImportPackage` has no caller.

## finishParse runs when

`ParseThread.parseOne` reaches `finishParse` only from `afterPost`, after the content post returns Ok.

- `getGraph` returns the graph.
- `parseState` is not `ParseState.Parsed`.
- Kind is `Special File`, `Special Directory`, or `Special Workspace`.
- `DocumentPersistWrite.planParseFile` or `DirectoryReconcile.planDirectoryReconcile` returns Ok.
- `postOps` returns Ok. An empty op list counts as Ok.

## Later

- `ImportDocument.planParseFile` still prepends `Op.SetDocumentState(fileId, Unparsed, Current)` for an Unparsed file. `ParseThread.parseFile` posts those ops, then calls `finishParse`.
- `Api.applyParseFile` posts `Notice.NodeParsed` after `CoreDoor.postGraphOnly`. `App.runParseFile` calls POST `/ambit/file/parse`.
- `ParseThread.requeueOnUnparsed` stays false.
- FileAgent stays the persist host.
