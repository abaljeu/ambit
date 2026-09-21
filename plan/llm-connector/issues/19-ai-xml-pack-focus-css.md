# 19 — AI extract pack is XML with Focus cssClass

**Status:** done
**Blocked by:** None — [11 — Pack extract with Amb (supplied-fragment walk)](11-simple-extract-format.md) Amb extract-walk stays; this ticket changes only the CloudAgents document.
**Type:** bug-fixing
Estimate: 2h
Actual: 2h

## Context

[08 — Agent ask from what I see](08-agent-ask-from-what-i-see.md) packs the Zoom extract with [11 — Pack extract with Amb (supplied-fragment walk)](11-simple-extract-format.md). `RunAgentActor.packExtract` sets `Graph.focus` then Amb-writes. Amb serialize never reads `graph.focus` (ticket 11 lock: no Focus sentinel). The system prompt says “with Focus marked.” The model cannot tell which Node is Focus. That is worse when Focus is not the `?` Command.

Amb `.amb` outline is opaque to models. The document the model sees must be XML so structure is clear. Focus is css class `prompt` on the Focus Node of the in-memory extract copy only. Persist nothing. Do not rewrite Focus `text` with a `[Focus]` prefix. Do not restore Fable.SimpleXml.

## What to build

### 1. XML pack of the Zoom extract

1. [x] `packExtract` (or a small helper it calls) starts from the extract Graph with `withFocus`.
2. [x] On that copy only, merge css class `prompt` onto the Focus Node (`CssClass.toggle` / list merge; keep existing classes).
3. [x] Serialize the Zoom-rooted extract to write-only XML for the CloudAgents prompt document. Server `System.Xml.Linq` (or equivalent write-only helper). No Shared parse dependency.
4. [x] Tree mirrors the outline: one `node` element per present Node; text content is Node text (escaped); `class` is space-separated `CssClass` names (Focus has `prompt`); Children nest under the parent.
5. [x] Walk the same as `AmbWriteWalk.SuppliedExtract`: Owned and Ref into Nodes present in the extract; omit missing ids; no file persist; no owning-document partition.

### 2. System prompt

1. [x] Say the next message is the **XML** Zoom-rooted extract. The Focus / prompt node is the one with css class `prompt`.
2. [x] Do not say “mixed Amb / codec text with Focus marked.” Do not claim Amb marking or mixed-format owning-codecs.
3. [x] Return rules stay outline text that replaces Focus Children (reply path unchanged: Amb/Plain tidy via FocusChildrenReplace).

### 3. Proofs

1. [x] Pack string is XML.
2. [x] Focus Node element carries `prompt` class.
3. [x] Original Graph Node `cssClasses` unchanged.
4. [x] [11 — Pack extract with Amb (supplied-fragment walk)](11-simple-extract-format.md) “no Focus sentinel” Amb extract-walk tests stay valid.

## Non-goals

1. Mixed-format owning-codec for all document types — still tabled.
2. Nested-tag Amb pack or Fable.SimpleXml parse — rejected; do not restore.
3. Changing FocusChildrenReplace / reply apply format ([12 — Replace Focus Children from reply](12-replace-focus-children-from-reply.md) stays cancelled).
4. Stream tickets [17 — CloudAgents Console stream](17-cloudagents-console-stream.md) and [18 — AI Actor stream](18-ai-actor-stream.md).

## See also

[llm-connector architecture](../arch.md), [08 — Agent ask from what I see](08-agent-ask-from-what-i-see.md), [11 — Pack extract with Amb (supplied-fragment walk)](11-simple-extract-format.md)

## Comments

- 2026-09-21 — Alan accepted. Squash-landed on staging. Status `done`.
- 2026-09-21 — Alan: `systemPrompt` must match the XML pack (Zoom-rooted extract; css class `prompt`; no Amb/mixed-format claims). Status stays `coded`.
- 2026-09-21 — Alan: Focus mark css class token is `prompt` (not `focus`). Status stays `coded`.
- 2026-09-21 — Coded: Server write-only XML pack; Focus css class `prompt` on extract copy; system prompt says XML; proofs green. Status `coded`.
- 2026-09-21 — Filed from Alan lock: XML AI pack; Focus = css class on the extract copy only.

## Time

- 2026-09-21 2h — Ticket, XML pack, Focus cssClass, proofs (from chat)
