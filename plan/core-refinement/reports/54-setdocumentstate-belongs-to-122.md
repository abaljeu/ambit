# 54 — SetDocumentState belongs to 122

## Branch

dev

## 54 — Parse and import stop the Parsed Set

[54 — Parse and import stop the Parsed Set](../issues/54-parse-and-import-stop-the-parsed-set.md) no longer tells import, file parse, or directory reconcile to stop `Op.SetDocumentState`.

Reworded:

- Context names the three emitters and points at [122 — Document state moves by Notice](../issues/122-document-state-moves-by-notice.md) for the drop.
- What to build says those three put the Parsed mark on `ParseThread.finishParse`.
- Each Parsed-mark step keeps the current emit site and the route onto the parse thread so `ParseThread.finishParse` posts `Notice.NodeParsed`.
- Acceptance checks that route and the handler. It no longer checks that producers stop the op.

## 122 — Document state moves by Notice

[122 — Document state moves by Notice](../issues/122-document-state-moves-by-notice.md) said to call a Notice and keep `SetDocumentState`.

Changes now says: drop `SetDocumentState` after the Notice matches that op.

No other section of 122 changed.
