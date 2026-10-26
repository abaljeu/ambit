# 124 — Remove SetDocumentState one site at a time

Ticket: [124 — Remove SetDocumentState one site at a time](../issues/124-remove-setdocumentstate-one-site-at-a-time.md)

Spec: [Document-state events](../spec.md)

Arch: [core-refinement architecture](../arch.md)

## 1. Standards

- 1. **Directory merge length** — Hard. `.agents/rules/fsharp-source.md` limits a function to 40 lines. `ImportText.buildDirectoryMergeChange` in `src/Shared/ImportText.fs` is 54 lines.
- 2. **Import apply helpers** — Judgement. Smell Duplicated Code. `tests/Shared.Tests/SpecialNodeTestHelpers.fs` `applyChange` and `tests/Shared.Tests/ImportDocumentTests.fs` `applyChange` both call `ImportText.stateForImportOps` and then apply the Change ops. `SpecialNodeTestHelpers.applyChange` returns `ApplyResult`. `ImportDocumentTests.applyChange` raises on `ApplyResult.Invalid`.

## 2. Spec

- 1. **Import command name** — Unnamed. `ImportText.commandForUnparsedDocument` sets `commandName` to `Import:` plus the focus id when the focus is an Unparsed document root. `ImportText.importCommand` builds that text. `ImportText.tryImportFocus` reads it. The ticket names the producer `ImportText.markDocumentCurrentBeforeParse`. Spec section 4 maps a non-empty `commandName` to a logged command. The import form is a separate `commandName` value.
- 2. **Pending restore** — Wrong. `SyncPlanner.restorePending` applies the saved ops with `ChangeValidation.applyOps`. The fold keeps an event when that apply returns `ApplyResult.Changed`. On an Unparsed File Node the import `Op.Replace` returns `ApplyResult.Invalid`. The fold omits that event. `App.mergePendingAfterLoad` saves the shortened queue. The File Node stays Unparsed. The ticket requires the graph enum after the pass to match `Ev.documentStateFromNotice` for `Notice.NodeParsed`.
- 3. **Leftover pending** — Wrong. `SyncLogic.applyPendingEvent` applies `Ev.ops` with `ResidentProjection.applyOps`. `SyncLogic.reapplyLeftoverPending` folds that function. `SyncLogic.consumeCatchUpPoll` sets the graph to the catch-up baseline and then calls `reapplyLeftoverPending`. `SyncLogic.applySyncResponse` calls it after a Want install. On an Unparsed baseline the import tree stays off the graph.
- 4. **Empty directory merge** — Wrong. `ImportText.buildDirectoryMergeChange` emits `EventBody.Change []` with the import `commandName` when every entry name is already a child and the focus is an Unparsed document root. `ImportText.stateForImportOps` previews `Notice.NodeParsed` through `Ev.apply` before the ops. `ImportText.commitImportApply` returns `ApplyResult.Changed` when the document enum changes. `FileAgent` and `DbAgent` commit that result. The File Node becomes Parsed. `SyncLogic.applyLocalEvent` uses the same preview and then returns the error `Change did not change state`. The local File Node stays Unparsed.
- 5. **Undo command name** — Wrong. `ClientHistory.invertAs` copies `commandName` onto Undo and Redo. `Api.importParsedNotices` posts `Notice.NodeParsed` for every accepted event whose `commandName` is the import form. `FileAgent` and `DbAgent` run `ImportText.commitImportApply` on that event, so the preview runs again. Spec section 2 item 6 says Undo and Redo leave document-state messages unapplied. An accepted Undo of an import posts `Notice.NodeParsed`.
- 6. **Undo tree** — Judgement. `Ev.inverseOps` returns the inverse of the Change ops. A Notice stays outside that list. Undo restores the tree ops. The File Node stays Parsed.

## 3. Arch

- No findings.

## 4. Tests

- 1. **Pass 1 names** — Pass 1 names `build import marks unparsed file current before tree operations` and `import logs NodeParsed beside SetDocumentState`. Both facts exist. `rejected import leaves the file Unparsed` exists in `tests/Server.Tests/NodeEventExpandTests.fs`.
- 2. **Shared shape** — `build import marks unparsed file current before tree operations` in `tests/Shared.Tests/ImportTextTests.fs` asserts the import `commandName`, `ImportText.tryImportFocus`, and the op list. The op list is the package ops plus the attach op.
- 3. **Server accept** — `import logs NodeParsed beside SetDocumentState` posts `ImportText.buildImportChange` through `Api.postEvents`. The fact reads Parsed, `Notice.NodeParsed`, and the child list. A second `Notice.NodeParsed` leaves the File Node Parsed. The Change ops contain the package ops and the attach op. `Api.importParsedNotices` posts `Notice.NodeParsed` after `CoreDoor.postEvents` returns Ok. For an Unparsed File Node the preview enum is `Current`. `Ev.documentStateFromNotice` for `Notice.NodeParsed` returns `Current`.
- 4. **Server reject** — `rejected import leaves the file Unparsed` posts an import `commandName` whose body is `Op.SetText` on a missing node. The response is a rejection. The File Node stays Unparsed. The log has no `Notice.NodeParsed` for that node. `FileAgent.processPostEvents` writes `loaded.state` after the batch returns Ok.
- 5. **Shared enum** — The shared fact name says the Unparsed File Node is Parsed before the tree operations. The fact reads the event fields.

## 5. Miss

- 1. **Directory merge length** — Hard. Split `ImportText.buildDirectoryMergeChange` so each function has 40 lines or fewer.
- 2. **Pending import** — Wrong. In `SyncPlanner.restorePending`, apply `ImportText.stateForImportOps` before the saved ops, and keep the event when that apply returns `ApplyResult.Changed`.
- 3. **Catch-up import** — Wrong. In `SyncLogic.applyPendingEvent`, apply `ImportText.stateForImportOps` before `ResidentProjection.applyOps`.
- 4. **Empty directory merge** — Wrong. Make `SyncLogic.applyLocalEvent` return `ApplyResult.Changed` when the import preview changes the document enum, the same promotion as `ImportText.commitImportApply`.
- 5. **Undo command name** — Wrong. Give Undo and Redo a `commandName` outside the import form. Post `Notice.NodeParsed` from `Api.importParsedNotices` for `EventBody.Change` only.
- 6. **Shared fact** — Extend `build import marks unparsed file current before tree operations` so the fact applies the import Change on an Unparsed File Node and reads Parsed on the focus.

## 6. Found elsewhere

The following issues were found beyond the ticket scope. The fixes are suggested.

- 1. **Parse thread** — Arch section 9 Parsed producers routes `ImportText.buildImportChange` and `ImportText.buildDirectoryMergeChange` through the parse thread so `ParseThread.finishParse` posts `Notice.NodeParsed`. This pass posts that Notice from `Api.importParsedNotices`. Route the import Change through the parse thread. That work is [54 — Parse and import stop the Parsed Set](../issues/54-parse-and-import-stop-the-parsed-set.md).
- 2. **Browser caller** — Under `src`, the call sites of `ImportText.buildImportChange` and `ImportText.buildDirectoryMergeChange` are the two definitions. `Desktop.LocalProxy.handleImport` returns a `DesktopImportPackage`. Suggested fix: the browser calls `ImportText.buildImportChange` or `ImportText.buildDirectoryMergeChange` and posts that event.
