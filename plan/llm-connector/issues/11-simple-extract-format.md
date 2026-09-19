# 11 — Pack extract with Amb (supplied-fragment walk)

**Status:** defined
**Blocked by:** None — can start immediately.
**Type:** task
Actual: 45m

## Context

Story path **Agent ask from what I see** needs a pack string for the supplied Zoom extract. The Graph fragment is already given. Reuse the Amb (Ambit `.amb` / `AmbDocument`) codec. Do not invent a nested `<div>` / `<focus>` format. Owning-codec mixed-format and Md extract serialize stay tabled. Existing Md artifact write (owned subgraph → file) does not change.

## What to build

A Document write option that serializes the supplied extract to an Amb string. Walk child lists as given: recurse through Owned and Ref appearances into Nodes present in the extract. Do not stop at nested document or File Node boundaries. Do not persist a file. Do not use owning-document partition. Focus lives on the extract Graph (`Graph.focus` / `withFocus`), not as an Amb-native mark. Parse stays default Amb parse.

### 1. Document

State / Interface / Uses: [[arch.md]] module **Document**.

1. [ ] Amb extract-walk write — serialize the supplied extract with `AmbDocument` using one walk option that follows the extract child lists as given.
2. [ ] Supplied child walk — recurse Owned and Ref appearances into Nodes present in the extract; omit a child id that is missing from the extract.
3. [ ] No document-file bound — do not stop at nested document or File Node boundaries; do not use owning-document partition.
4. [ ] No file write — do not persist; do not call Md or other artifact writers.
5. [ ] Focus on extract Graph — `Graph.withFocus` sets `Graph.focus` on the extract copy; JSON and History omit `focus`. Amb text has no Focus sentinel.

## Non-goals

- New Amb parse mode. Parse stays default `AmbDocument` / DocumentFormat Amb parse.
- Nested `<div>` / `<focus>` format. Abandoned.
- Fable.SimpleXml pack. Rejected: parse is JS/Parsimmon and throws on .NET Server.
- Mixed-format owning-codec serialize. Tabled.
- Existing Md artifact write. Unchanged.

## See also

[[arch.md|llm-connector architecture]], [[08-agent-ask-from-what-i-see.md|08 — Agent ask from what I see]], [[06-define-command-run-agent-redesign.md|06 — Define the revised Command + Run Agent seam]], [[07-lock-run-agent-architecture.md|07 — Lock the Run Agent architecture]]

## Comments

- 2026-09-19 — Replan: reuse Amb extract-walk. Nested-tag pack abandoned. Fable.SimpleXml rejected (parse is JS/Parsimmon; throws on .NET Server). GitHub PRs #53 and #54 close without land.
- 2026-09-19 — Focus seam: ephemeral `Graph.focus` / `withFocus` on the extract copy (already discussed on the abandoned nested-tag increment). Not an Amb-native Focus mark.

## Time

- 2026-09-19 45m — replan ticket 11 to Amb extract-walk (from chat)
