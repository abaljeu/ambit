# How the pack is encoded for the LLM

**Type:** grilling
**Status:** done
Blocked by:
Actual: 55m

## Question

How is included context packed and encoded for the LLM when Run Agent executes? Decide the pack shape. Do not implement the connector in this ticket.

## Answer

Run Agent is the spoken name. The person types `?` plus a message on Focus, then Run. Glossary: [[CONTEXT.md]] Run Agent.

The pack is SiteMap-visible Nodes below `rootnode`. First Run Agent passes Zoom as `rootnode`. Later calls may pass another root. The Browser sends that NodeId list. Core extracts that subgraph and includes `rootnode` so Md `write` has a document root. The Actor Md-writes that graph (nodes to text, not to a file). Codec is Md for now.

The LLM message is the `?` remainder on Focus Header (example: `? what do you think of this email`). Other pack Nodes are that email. The Actor converts the reply Md to a Graph and adds Owned children under Focus.

## Comments

- 2026-09-02: Filed unclaimed from WORK.md. Map: [[../map.md]]. Glossary: [[CONTEXT.md]] Included context.
- 2026-09-06: Q1: The `?` pack is Included context (SiteMap under Zoom, honoring Fold). Issue 02's subgraph-first default does not spec this pack. Encoding (who, what text) is still open.
- 2026-09-06: Q4: Browser supplies the pack as a simple list of NodeIds. Not paste `serializeSubtree` text. How those ids become Grok prompt text is still open.
- 2026-09-06: Q10: Actor calls a persist write — nodes to text, not to a file. Codec maybe markdown, maybe markup. Not flat `node.text` lines.
- 2026-09-06: Q12: The `?` remainder is the LLM message (`? what do you think of this email`). Other nodes are that email. Q13: Md for now. Q14: persist from a subgraph; Zoom as write root still open pending extract facts.
- 2026-09-06: Create extract is SiteMap visible Nodes below `rootnode` (not necessarily Zoom). Same subgraph is what the Actor receives. Md write of that graph; reply Md→graph onto Focus.
- 2026-09-06: Q21: First `?` `rootnode` is Zoom.
- 2026-09-06: Q25: Issue 20 cookie is the token standard. Ready to lock Answer.
- 2026-09-06: Q26: Lock. Spoken name is Run Agent (`?` then Run). Answer written.
- 2026-09-06: Alan confirmed lock.

## Time

- 2026-09-06 5m — Q1: pack is Included context (from chat)
- 2026-09-06 5m — Q4: Browser NodeId list (from chat)
- 2026-09-06 5m — Q10: persist nodes-to-text, codec open (from chat)
- 2026-09-06 10m — Q12 message; Q13 Md; Q14 Zoom/subgraph (from chat)
- 2026-09-06 10m — extract = visible below rootnode; Md reply onto Focus (from chat)
- 2026-09-06 5m — Q21 first `?` root is Zoom (from chat)
- 2026-09-06 5m — Q25 issue 20 is the token standard (from chat)
- 2026-09-06 10m — Q26 lock; Run Agent name; wrote Answer (from chat)
