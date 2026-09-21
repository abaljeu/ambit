# Code review — 19 — AI extract pack is XML with Focus cssClass

Range: `8349a803...HEAD` plus ticket Status `coded`. Spec: [19 — AI extract pack is XML with Focus cssClass](../issues/19-ai-xml-pack-focus-css.md).

Mechanical scan: `python3 .agents/skills/code-review/scripts/standards-scan.py --diff 8349a803556e09536eecf0f6767c2d27b342c064`. No LONG-line or 40-line hits. `writePresent` is 26 lines.

## Standards

- No documented-standard hits on [AiExtractPack.fs](../../../src/Server/AiExtractPack.fs). Functions stay under 40 lines; lines stay under 100.
- Surgical: Actor pack switches to `AiExtractPack.packExtract`; Amb extract-walk and FocusChildrenReplace stay.
- Write-only `System.Xml.Linq` on Server. No Fable.SimpleXml. No parse in Shared.
- Plan files use title links. Ticket Non-goals name tickets by title.

## Spec

- Pack string is XML `node` tree; Focus gets css class `focus` on the extract copy; original `cssClasses` unchanged.
- System prompt says XML and css class `focus`. Return rules stay outline text.
- Zoom is the XML root so Focus = Zoom is visible (Ask harness). Child walk matches SuppliedExtract (Owned+Ref, omit missing).
- Amb “no Focus sentinel” tests still pass. Reply apply unchanged.

## Summary

Standards 0 findings. Spec 0 findings. Worst: none.
