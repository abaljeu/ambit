# 126 — planParseFile drops its SetDocumentState

**Status:** `defined`

## 1. Type

coding

## 2. Blockers

[ ] [135 — Backend infers document-state Notices from ops](135-backend-infers-document-state-notices-from-ops.md)

## 3. Problem

`ImportDocument.planParseFile` emits `Op.SetDocumentState` (`Unparsed` to `Current`) ahead of the parse ops.

## 4. Change

- `ImportDocument.planParseFile` emits the parse ops only.
- `ParseThread` posts `Notice.NodeParsed` after the Change.
- The parse Change is graph-only. `guardUnparsed` is false. The ops apply on an Unparsed File.
- `Notice.NodeParsed` sets Current.
- This path builds no Notice from ops.

Source: `ImportDocument.fs`.

## 5. Tests

[ ] `ImportDocument.planParseFile` ops contain no `Op.SetDocumentState`.
[ ] Graph-only apply of those ops edits an Unparsed File.
[ ] `Notice.NodeParsed` after that apply sets Current.
[ ] `planParseFile posts NodeParsed and sets Current` checks the `ParseThread` log and legacy Current.

## 6. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
