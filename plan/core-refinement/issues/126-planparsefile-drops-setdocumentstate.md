# 126 — planParseFile drops its SetDocumentState

**Status:** `defined`

## 1. Type

coding

## 2. Blockers

[ ] [135 — Backend infers document-state Notices from ops](135-backend-infers-document-state-notices-from-ops.md)

## 3. Problem

`ParseThread.parseFile` parses a File. The File id is known before the parse Change.

## 4. Change

- `ImportDocument.planParseFile` emits the parse ops only.
- `ParseThread.postParseOps` posts `Notice.NodeParsing` for the File id before the Change.
- `ParseThread` posts `Notice.NodeParsed` for that File id after the Change.
- The parse Change is graph-only. `guardUnparsed` is false. The ops apply on an Unparsed File.
- `Notice.NodeParsing` sets nothing. `Notice.NodeParsed` sets Current.
- This path builds each Notice from the File id. It builds no Notice from ops.

Source: `ImportDocument.fs`, `ParseThread.fs`.

## 5. Tests

[ ] `ImportDocument.planParseFile` ops contain no `Op.SetDocumentState`.
[ ] Graph-only apply of those ops edits an Unparsed File.
[ ] `Notice.NodeParsed` after that apply sets Current.
[ ] `planParseFile posts NodeParsing, the Change, then NodeParsed` checks that order, the outline text, and legacy Current.

## 6. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
