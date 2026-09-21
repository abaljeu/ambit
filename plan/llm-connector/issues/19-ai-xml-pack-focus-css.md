# 19 — AI extract pack is XML with Focus cssClass

**Status:** defined
**Blocked by:** None — [11 — Pack extract with Amb (supplied-fragment walk)](11-simple-extract-format.md) Amb extract-walk stays; this ticket changes only the CloudAgents document.
**Type:** bug-fixing
Estimate: 2h

## Context

[08 — Agent ask from what I see](08-agent-ask-from-what-i-see.md) packs the Zoom extract with [11 — Pack extract with Amb (supplied-fragment walk)](11-simple-extract-format.md). `RunAgentActor.packExtract` sets `Graph.focus` then Amb-writes. Amb serialize never reads `graph.focus` (ticket 11 lock: no Focus sentinel). The system prompt says “with Focus marked.” The model cannot tell which Node is Focus. That is worse when Focus is not the `?` Command.

Amb `.amb` outline is opaque to models. The document the model sees must be XML so structure is clear. Focus is a css class on the Focus Node of the in-memory extract copy only. Persist nothing. Do not rewrite Focus `text` with a `[Focus]` prefix. Do not restore Fable.SimpleXml.

## What to build

### 1. XML pack of the Zoom extract

1. [ ] `packExtract` (or a small helper it calls) starts from the extract Graph with `withFocus`.
2. [ ] On that copy only, merge css class `focus` onto the Focus Node (`CssClass.toggle` / list merge; keep existing classes).
3. [ ] Serialize the Zoom-rooted extract to write-only XML for the CloudAgents prompt document. Server `System.Xml.Linq` (or equivalent write-only helper). No Shared parse dependency.
4. [ ] Tree mirrors the outline: one `node` element per present Node; text content is Node text (escaped); `class` is space-separated `CssClass` names (Focus has `focus`); Children nest under the parent.
5. [ ] Walk the same as `AmbWriteWalk.SuppliedExtract`: Owned and Ref into Nodes present in the extract; omit missing ids; no file persist; no owning-document partition.

### 2. System prompt

1. [ ] Say the next message is an **XML** extract. Focus is the Node with css class `focus`.
2. [ ] Do not say “mixed Amb / codec text with Focus marked.”
3. [ ] Return rules stay outline text that replaces Focus Children (reply path unchanged: Amb/Plain tidy via FocusChildrenReplace).

### 3. Proofs

1. [ ] Pack string is XML.
2. [ ] Focus Node element carries `focus` class.
3. [ ] Original Graph Node `cssClasses` unchanged.
4. [ ] [11 — Pack extract with Amb (supplied-fragment walk)](11-simple-extract-format.md) “no Focus sentinel” Amb extract-walk tests stay valid.

## Non-goals

1. Mixed-format owning-codec for all document types — still tabled.
2. Nested-tag Amb pack or Fable.SimpleXml parse — rejected; do not restore.
3. Changing FocusChildrenReplace / reply apply format ([12 — Replace Focus Children from reply](12-replace-focus-children-from-reply.md) stays cancelled).
4. Stream tickets [17 — CloudAgents Console stream](17-cloudagents-console-stream.md) and [18 — AI Actor stream](18-ai-actor-stream.md).

## See also

[llm-connector architecture](../arch.md), [08 — Agent ask from what I see](08-agent-ask-from-what-i-see.md), [11 — Pack extract with Amb (supplied-fragment walk)](11-simple-extract-format.md)

## Comments

- 2026-09-21 — Filed from Alan lock: XML AI pack; Focus = css class `focus` on the extract copy only.

## Time
