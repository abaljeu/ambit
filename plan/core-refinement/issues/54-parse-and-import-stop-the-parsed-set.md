# 54 — Parse and import stop the Parsed Set

**Status:** `defined`
**Type:** coding
**Blocked by:** 44 — Apply document state from Node events
**Trackers:** architecture, project spec

## Context

`ParseThread.finishParse` posts `Notice.NodeParsed` through `CoreMailboxApi.postNotice` when the parse thread finishes a node. The handler sets Parsed. Import, file parse, and directory reconcile still emit `Op.SetDocumentState` for that mark.

## What to build

Those three stop that op. The Parsed mark is `ParseThread.finishParse`.

### 1. Parsed mark

1. [ ] **Import** — `ImportText.markDocumentCurrentBeforeParse` stops `Op.SetDocumentState(focusId, Unparsed, Current)`. `ImportText.buildImportChange` and `ImportText.buildDirectoryMergeChange` do not run on the parse thread. Route that import through the parse thread. `ParseThread.finishParse` posts `Notice.NodeParsed` for the focus node.
2. [ ] **File parse** — `ImportDocument.planParseFile` stops `Op.SetDocumentState(fileId, Unparsed, Current)`. `ParseThread.parseFile` runs it through `DocumentPersistWrite.planParseFile`. `ParseThread.finishParse` posts `Notice.NodeParsed`. `Api.postParseFile` also calls `DocumentPersistWrite.planParseFile` and posts with `CoreDoor.postGraphOnly`. Route that command through the parse thread so `ParseThread.finishParse` posts `Notice.NodeParsed`.
3. [ ] **Directory reconcile** — `DirectoryReconcile.planDirectoryReconcile` stops `DirectoryReconcile.markCurrent`'s `Op.SetDocumentState(directoryId, Unparsed, Current)`. `ParseThread.parseDirectory` runs it. `ParseThread.finishParse` posts `Notice.NodeParsed`.

### Acceptance Criteria

1. [ ] **Parsed test** — `parsed producers stop SetDocumentState`. `ImportText.markDocumentCurrentBeforeParse`, `ImportDocument.planParseFile`, and `DirectoryReconcile.planDirectoryReconcile` do not emit `Op.SetDocumentState` for the Parsed mark. `ParseThread.finishParse` posts `Notice.NodeParsed`. The handler sets Parsed.

## See also

- [Graph change parsed producers](../arch.md#graph-change-parsed)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [GLOSSARY](../../GLOSSARY.md)
