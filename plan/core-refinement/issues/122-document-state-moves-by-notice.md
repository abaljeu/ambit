# 122 — Document state moves by Notice

## 1. Type

coding

## 2. Blockers

[ ] none

## 3. Look at

[ ] `CoreMailboxLoop.applyNotice` dispatches each Notice in [CoreMailboxLoop.fs](../../../src/Server/Core/CoreMailboxLoop.fs)
[ ] `CoreMailboxFilesAxis.writePullingFileUnpersisted`, `writeReceived`, `applyNodeParsed`, `writeUnpersisted`, and `writePersisted` write the axis in [CoreMailboxFilesAxis.fs](../../../src/Server/Core/CoreMailboxFilesAxis.fs)

## 4. Context

These send the Notice:

- Load
- Parse
- Persist

Document state changes by that pushed Notice. The effect is idempotent and matches `Op.SetDocumentState` for that same change.

## 5. Changes

[ ] [50 — Upload emits NodeUploading](50-upload-emits-nodeuploading.md) establishes the `Notice.NodeUploading` effect.
[ ] [53 — Arrival and reconcile emit NodeReceived](53-arrival-and-reconcile-emit-nodereceived.md) establishes the `Notice.NodeReceived` effect.
[ ] [54 — Parse and import stop the Parsed Set](54-parse-and-import-stop-the-parsed-set.md) establishes the `Notice.NodeParsed` effect.
[ ] [57 — Upload Actor posts Node notices](57-upload-actor-posts-document-state.md) posts `Notice.NodeUploading` and `Notice.NodeReceived`.
[ ] [73 — Add the NodeParsing and NodePersisting Notices](73-add-the-nodeparsing-and-nodepersisting-notices.md) establishes the `Notice.NodePersisting` effect.
[ ] [100 — Core loop gates Persisted on the written version](100-core-loop-gates-persisted-on-the-written-version.md) establishes the `Notice.NodePersisted` effect.

## 6. Tests

[ ] No new test is required.
[ ] The old tests should still work.

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
