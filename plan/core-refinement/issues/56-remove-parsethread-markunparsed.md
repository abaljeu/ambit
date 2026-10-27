# 56 — Remove ParseThread markUnparsed

**Status:** `defined`
**Type:** coding
**Blocked by:** 44 — Apply document state from Node events
**Note:** 2026-10-25 — Likely absorbed by 45 — SetDocumentState leaves; NodeUnpersisted replaces it, which deletes `Notice.MarkUnparsed`. Not closed.
**Trackers:** architecture, project spec

## Context

`ParseThreadDeps.markUnparsed` remains on the record that `ParseThread.start` takes. The parse thread does not call it. Unparsed is set by the mailbox before a node is on the parse thread. Directory reconcile does not mark existing members Unparsed.

## What to build

The parse thread stops producing Unparsed marks.

### 1. Remove markUnparsed

1. [ ] **Remove markUnparsed** — `ParseThread.start` keeps `ParseThreadDeps.markUnparsed` for its callers and runs `startInner`, whose deps have no `markUnparsed`. The parse thread does not call it and posts no NodeUnparsed. It does not emit NodeParsing. Unparsed is set by the mailbox before a node is on the parse thread. Directory reconcile does not mark existing members Unparsed.

## See also

- [Graph change remove markUnparsed](../arch.md#graph-change-remove-markunparsed)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [GLOSSARY](../../GLOSSARY.md)
