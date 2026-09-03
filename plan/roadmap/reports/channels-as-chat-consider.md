# Channels as Chat — consider only

Hypothesis (user): the connected-channel scheme may evolve into Chat with an Agent — type in Ambit, send to an agent computer, put the response in the view, edit and send again; then pull/send/receive email the same way. This report is consideration only. No Epic or map edits. Not a Committed Decision. Scope: [[doc/agents/scope-vs-commitment.md]].

Sources: [[plan/roadmap/epics/operate-connected-channels.md]], [[plan/roadmap/epics/agent-chat-managed-context.md]], [[plan/roadmap/map.md]], [[commit-gated-agent-epic-union.md]], [[operate-connected-channels-epic.md]], [[new-epic-definition-choices.md]], [[hub-epic-framing.md]], [[CONTEXT.md]].

## 1. Does Chat with an Agent already exist?

Yes. Do not invent a new Chat Epic.

| Kind | Path | One-liner |
| --- | --- | --- |
| User Epic | [[plan/roadmap/epics/agent-chat-managed-context.md]] | A person chats with an Agent that provides managed context. |
| Map | [[plan/roadmap/map.md]] charting | Agent chat with managed context (current: Ask from what I see) |
| Chapters | [[plan/roadmap/epics/chapters/ask-from-what-i-see.md]], [[talk-again.md]], [[change-the-graph.md]], [[query-the-graph-or-the-files.md]], [[act-through-cli.md]], [[act-through-mcp.md]], [[ambit-keeps-consistency-with-desktop-repo-for-agentic-work.md]] | Named chat beats |
| Feature Project | [[plan/llm-connector/project.md]] | Ask from what I see |

Glossary **Agent** ([[CONTEXT.md]]): an LLM-empowered worker; Ambit will have one. Operate connected channels already says: Chat stays on agent-chat; do not swallow that Epic.

There is no separate Epic titled exactly "Chat with an Agent." The standing name is **Agent chat with managed context**.

## 2. How the loop maps

| User step | Homes today |
| --- | --- |
| Type in Ambit, send to an agent computer, response in the view | Agent chat (Ask from what I see; replies as Owned children / Actor inbound) |
| Edit, send again | Agent chat ([[talk-again.md]]) |
| Pull email, send and receive | Operate connected channels (mail first in framing, not the title) |

Same surface loop (compose → send → receive → view). Different product jobs: managed-context chat in-Graph vs connect-and-operate an outside channel (review/commit to the outside may be a later Chapter on channels).

## 3. Judgment

**Keep Chat as its own Epic.** Do not treat Chat as "channel zero" of Operate connected channels.

- **Risk of hub mega-Epic:** Collapsing chat into channels (or "chat is just another channel") revives rejected frames: *Connect my tools to my Graph* ([[hub-epic-framing.md]]), option 3 *Connect tools with a review seam* ([[new-epic-definition-choices.md]]), and Agent-chat ∪ documents / triple unions ([[commit-gated-agent-epic-union.md]]). Channels Notes already forbid swallowing agent-chat, documents-from-anywhere, and PKM.
- **Risk of two Epics that feel like one loop:** The UX rhyme is real. Leaving both standing without a shared *pattern* note can look like duplicate product. Precedent still favors two Epics when one sentence cannot hold both jobs (issue 13; commit-gated union). Shared scheme belongs in transport / Actor language, not one User Epic title.

## 4. Too early vs a one-line note

**Too early on the channels Epic:** Chapters that rename chat beats; "agent computer is the first channel"; merge Talk again / Ask into channels; any claim that channels *is* Chat; Chapter charting that steals llm-connector or agent Actor work.

**Reasonable one-liner (Notes only, if wanted later):** the compose–send–receive–view loop may *feel* like Chat; Chat remains [[agent-chat-managed-context.md]]. Do not write "may become Chat" — Chat already exists; channels must not become it.

## 5. Bottom line

Chat is already named. Channels is outside connect-and-operate (mail first). Same scheme as UX/transport rhyme; not the same Epic. Do not merge.
