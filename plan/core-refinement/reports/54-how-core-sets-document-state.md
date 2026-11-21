# 54 — How core sets document state

Branch: `dev`.

Question: how document state changes when a caller does not emit `Op.SetDocumentState`.

## Code today

Core sets Parsed by posting `Notice.NodeParsed`. That path does not build `Op.SetDocumentState`.

- `ParseThread.afterPost` calls `finishParse` after the content post succeeds.
- `ParseThread.bootCore` wires `finishParse` to `CoreMailboxApi.postNotice` of `Notice.NodeParsed`.
- `CoreMailboxLoad.dispatchItem` appends `EventBody.Notice`, then `applyNotice`.
- `applyNodeParsed` calls `writeParseState` with `ParseState.Parsed`.
- `GraphMutate.setParseState` writes the node through `Node.withParseState`. That sets `parseState` and clears `pullingFile`. `DocumentState.toLegacy` then reads that node as Current.
- When `pullingFile` is set, `setParseState` returns the same graph. The File Node stays Unparsed.
- `FileAgent.replaceGraph` stores that graph in memory. The notice argument is unused. `FileAgent.applyEvent` accepts an event only when `Ev.ops` returns ops.
- `DbAgent.replaceGraph` stores the same graph and calls `Database.writeNoticeRow`. For `Notice.NodeParsed` that row write uses the graph already mutated above.

`History.Op.apply` still applies `Op.SetDocumentState` when a Change contains it. Apply calls `Graph.setDocumentState`, which calls `GraphMutate.setDocumentState`, which calls `Node.withDocumentState`. `Op.invert` swaps the old and new states. Decode rebuilds the same op. That is the caller-op path. `finishParse` does not use it.

`Api.postEvents` runs `inferredNodeEvents` after the Change is stored. A `SetDocumentState` from Unparsed to Current becomes another `Notice.NodeParsed`, unless that Change also sets PullingFile or Unparsed. The inference reads the op. It does not create the op.

`Api.postParseFile` posts the ops from `DocumentPersistWrite.planParseFile`, then posts `Notice.NodeParsed` itself.

## Import on this tree

`ImportText.markDocumentCurrentBeforeParse` still returns `Op.SetDocumentState(focusId, Unparsed, Current)` for an Unparsed document root. `buildImportChange` and `buildDirectoryMergeChange` prepend that list. The event authority is Browser. `ParseThread.parseFile` and `ParseThread.parseDirectory` are the loops that call `finishParse`. Import is not one of those loops.

`ImportDocument.planParseFile` still prepends `Op.SetDocumentState(fileId, Unparsed, Current)` when the legacy state is Unparsed. `ParseThread.parseFile` posts those ops, then `finishParse` posts `Notice.NodeParsed`. The file parse therefore sets the axis twice: the op, then the notice.

## [54 — Parse and import stop the Parsed Set](plan/core-refinement/issues/54-parse-and-import-stop-the-parsed-set.md)

The ticket says the three Parsed producers stop the op, and the Parsed mark is `ParseThread.finishParse`. The handler that sets Parsed is the mailbox path above. That handler is present.

Import on this tree still emits the op, and it does not run on the parse thread. Removing the op from `buildImportChange` also removes the `inferredNodeEvents` source for `Notice.NodeParsed`. No other Import step posts that notice.

`SyncLogic.applyLoggedNotice` writes the client graph for `Notice.NodePersisted`. For `Notice.NodeParsed` it stores the event id. A Poll client learns Parsed today from the op inside the Change.

## Import instead of the op

Import posts the paste ops and the child replace. After that work runs on the parse thread, `ParseThread.finishParse` posts `Notice.NodeParsed`, and `applyNodeParsed` sets Parsed.

Still open for that Import path: put Import on the parse thread, and teach Poll to apply `Notice.NodeParsed` onto the client graph. `ImportDocument.planParseFile` still emits the op for file parse.
