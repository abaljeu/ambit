# 54 — Parse and import stop the Parsed Set

**Status:** `defined`
**Type:** coding
**Blocked by:** 44 — Apply document state from Node events
**Trackers:** architecture, project spec

## Context

`ParseThread.finishParse` posts `Notice.NodeParsed` through `CoreMailboxApi.postNotice` when the parse thread finishes a node. When PullingFile is clear, `Notice.NodeParsed` sets parse state Parsed. When PullingFile is set, parse state stays Unparsed. Persist state stays unchanged. When the node is Parsed and Unpersisted, the handler pushes.

## What to build

Import, file parse, and directory reconcile put the Parsed mark on `ParseThread.finishParse`.

### 1. Parsed mark

1. [ ] **Import** — `ImportText.markDocumentCurrentBeforeParse` is the import mark. `ImportText.buildImportChange` and `ImportText.buildDirectoryMergeChange` do not run on the parse thread. Route that import through the parse thread. `ParseThread.finishParse` posts `Notice.NodeParsed` for the focus node.
2. [ ] **File parse** — `ParseThread.parseFile` runs `ImportDocument.planParseFile` through `DocumentPersistWrite.planParseFile`. `ParseThread.finishParse` posts `Notice.NodeParsed`. `Api.postParseFile` also calls `DocumentPersistWrite.planParseFile` and posts with `CoreDoor.postGraphOnly`. Route that command through the parse thread so `ParseThread.finishParse` posts `Notice.NodeParsed`.
3. [ ] **Directory reconcile** — `DirectoryReconcile.planDirectoryReconcile` runs `DirectoryReconcile.markCurrent`. `ParseThread.parseDirectory` runs that plan. `ParseThread.finishParse` posts `Notice.NodeParsed`.

### Acceptance Criteria

1. [ ] **Parsed test** — import, file parse, and directory reconcile set document state through `ParseThread.finishParse` posting `Notice.NodeParsed`. When PullingFile is clear, the handler sets Parsed. When PullingFile is set, parse state stays Unparsed. Persist state stays unchanged.

## See also

- [Graph change parsed producers](../arch.md#graph-change-parsed)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [GLOSSARY](../../GLOSSARY.md)
