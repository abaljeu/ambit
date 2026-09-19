# 11 — Simple extract format

**Status:** coded
**Blocked by:** None — can start immediately.
**Type:** task
Actual: 4h45m

## Context

Story path **Agent ask from what I see** needs a pack string for the supplied Zoom extract. Owning-codec mixed-format and Md serialize are tabled. This ticket adds a temporary nested-tag format so the Actor can send the extract later without a document codec. Existing Md artifact write (owned subgraph → file) does not change.

## What to build

A Document call that writes the supplied subgraph to a string. Each Node is a `<div>` whose body is the Node text, then nested child tags, then the close tag. A Node with no children is a single line `<div>Text</div>`. The Focus Node uses `<focus>` in place of `<div>`. Walk the extract as given — not Owner-only, not an owning-document partition, not a file write.

### 1. Document

State / Interface / Uses: [[arch.md]] module **Document**.

1. [x] Leaf form — a Node with no children serializes as `<div>Text</div>` (or `<focus>Text</focus>` when it is Focus).
2. [x] Nested form — a Node with children serializes as `<div>Text` then the child strings then `</div>` (Focus uses `<focus>` / `</focus>`).
3. [x] Supplied walk — walk the given extract child lists as supplied; do not bound by Owner edges or document-root ownership.
4. [x] Round-trip parse — parse this format back to a child tree; reject a partial parse (no residue).
5. [x] No codec / file write — do not call Md or other artifact writers; do not persist.

Escaping Node text that contains these tags is out of scope. Stronger serialization stays tabled.

## See also

[[arch.md]], [[08-agent-ask-from-what-i-see.md]], [[12-replace-focus-children-from-reply.md]], [[06-define-command-run-agent-redesign.md]], [[07-lock-run-agent-architecture.md]]

## Comments

- 2026-09-19 — Temporary pack: nested `<div>` / `<focus>` strings. Md and mixed-format owning-codec serialize tabled.
- 2026-09-19 — Shared `DocumentNestedTag.writeExtract` / `parseExtract` writes and parses the supplied extract. Status `coded`.
- 2026-09-19 — Reshape: pack Focus lives on the extract Graph (`Graph.withFocus`); `writeExtract` takes only that Graph. Tag name is `tagName` of the Node (today `div`); Focus id on the copy writes `<focus>`. Parse carries the tag string, not an `isFocus` bool. Status stays `coded`.
- 2026-09-19 — Pack lives on Shared.DotNet and uses `System.Xml.Linq` (`XElement`). Not Fable. Status stays `coded`.
- 2026-09-19 — Spike [Fable.SimpleXml](../reports/spike-fable-simplexml.md): Generator write works on .NET and Fable; parse is a Fable JS binding and throws on .NET. Recommendation: reject for Shared pack. Status stays `coded`.

## Time

- 2026-09-19 1h45m — Shared nested-tag write/parse and tests (from chat)
- 2026-09-19 45m — Graph.focus + tagName reshape (from chat)
- 2026-09-19 45m — Move pack to Shared.DotNet; XElement write/parse (from chat)
- 2026-09-19 1h30m — Fable.SimpleXml spike (from chat)
