# 56 — Remove ParseThread markUnparsed

**Status:** `defined`
**Type:** coding
**Blocked by:** 44 — Apply document state from Node events
**Trackers:** architecture, project spec

## Context

`ParseThread.markUnparsed` posts `Notice.MarkUnparsed` through `CoreMailboxApi.postNotice` after a directory reconcile. Unparsed is set by the mailbox before a node is on the parse thread. Directory reconcile does not mark existing members Unparsed.

## What to build

Remove `ParseThread.markUnparsed` as a producer of Unparsed marks.

### 1. Remove markUnparsed

1. [ ] **Remove markUnparsed** — Remove `ParseThread.markUnparsed`. It does not post `Notice.MarkUnparsed` and it does not emit NodeParsing. Unparsed is set by the mailbox before a node is on the parse thread. Directory reconcile does not mark existing members Unparsed.

### Acceptance Criteria

1. [ ] **Unparsed test** — `parse thread does not mark Unparsed`. `ParseThread.markUnparsed` is gone. Directory reconcile does not mark an existing member Unparsed. The mailbox sets Unparsed and pushes before the node is on the parse thread.

## See also

- [Graph change remove markUnparsed](../arch.md#graph-change-remove-markunparsed)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [GLOSSARY](../../GLOSSARY.md)
