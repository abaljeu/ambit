# Commit-gated Agent — Epic union

Recommendation, not a Committed Decision. This report is effort-local **scope** ([[doc/agents/scope-vs-commitment.md]]). Glossary: [[CONTEXT.md]] — **Agent**, not Grok; **Ambit** is the SaaS.

Precedent: a loop that already sat on two Epics stayed two Epics ([[cursor-repo-to-ambit-mobile-grok.md]], [[plan/roadmap/issues/13-grill-cursor-repo-to-ambit-llm-use.md]]). Hub work is a `plan` pattern, not one User Epic ([[hub-epic-framing.md]], [[plan/transport-layer/map.md]]).

## 1. Recommendation

**Need a new User Epic.** Every honest union and every widening fails a one-sentence frame.

Frame: *A person reviews an Agent's proposed mail actions in Ambit and commits them.*

The Agent writes into Ambit (limited API). Mail work stays in the Graph and/or files until the person adjusts and commits. Then Ambit talks to the mail server. First slice (choose, gather, put in files and/or the Graph) is inbound on this Epic, not a reason to merge another Epic.

Do not swallow [[plan/roadmap/epics/agent-chat-managed-context.md]], [[plan/roadmap/epics/work-with-text-files-from-anywhere.md]], or [[plan/roadmap/epics/operate-a-pkm.md]].

## 2. Epics considered

| Epic | Current job |
| --- | --- |
| [[plan/roadmap/epics/agent-chat-managed-context.md]] | A person chats with an Agent that provides managed context. |
| [[plan/roadmap/epics/work-with-text-files-from-anywhere.md]] | A person works with documents from any connected device. |
| [[plan/roadmap/epics/operate-a-pkm.md]] | A person operates a PKM; Find; import is a dependency, not this Epic's build. |
| [[plan/roadmap/epics/manage-a-project.md]] | A person organizes work (status, date). |
| [[plan/roadmap/epics/build-or-explore-a-wiki.md]] | A person builds and walks a wiki (`.md`). |
| [[plan/roadmap/epics/create-and-publish-web-pages.md]] | A person creates HTML and publishes a public URL. |
| [[plan/roadmap/epics/organize-huge-outlines.md]] | Developer Epic: scale huge outlines. |
| [[plan/roadmap/epics/robust-outliner.md]] | Developer Epic: outline integrity and inner core. |

## 3. Candidate unions

**Agent-chat ∪ documents-from-anywhere.** Frame: *A person works with documents and an Agent from anywhere.* Fails: two jobs glued. Mail is not "from anywhere." Issue 13 already rejected this mega-Epic.

**Agent-chat ∪ PKM.** Frame: *A person operates knowledge with an Agent.* Fails: PKM is Find and navigate. Round-trip and generate-from-data are out of PKM ([[hub-epic-framing.md]]). "Without a seam" fights the wanted review-then-commit seam.

**PKM ∪ documents-from-anywhere.** Frame: *A person brings outside documents into knowledge they can Find.* Fails: no Agent, no commit to a mail server.

**Triple / "connect my tools".** Frame: *A person connects outside tools to the Graph.* Fails: program thesis, not one marketable end-goal. Would swallow files, publish, and chat. Same reject as [[hub-epic-framing.md]].

**Widen agent-chat.** Frame: *A person reviews Agent work in Ambit and commits it.* Fails: Ask from what I see lands replies now; that is not a second gate to the outside. Ingest is transport. The Epic's one-liner is chat with managed context. A new Chapter there would make that one-liner false. Desktop-repo already stretched this Epic without adding a mail server.

**Widen documents-from-anywhere.** Frame: keep *documents from any device*; add email as a document class (Google is already a future inbound). First slice fits. Fails the full scenario: Agent proposals and commit to the mail server are not device access. Putting the Agent here repeats issue 13 option B.

**Widen PKM.** Frame: *imports external data without a seam* includes mail. Fails: hub text already says PKM does not own round-trip. The seam is the product.

**Developer Epic (information hub).** Fails: this is an end-user pattern. Wrong kind ([[cursor-repo-to-ambit-mobile-grok.md]] option E).

## 4. Chosen frame (user terms)

Ambit is the review desk. An Agent (limited API key) ingests chosen mail (id, headers the person cares about, content) into files and/or the Graph. The Agent may mark sort, delete, highlight, and reply **in Ambit only**. The person opens Ambit, adjusts, and commits. Then Ambit deletes, replies, sorts, and archives against the mail server.

That is not a product commitment. Chat (`?`, Talk again) stays on agent-chat. Files-from-any-device stay on documents-from-anywhere. Find stays on PKM. Transport-layer stays the inbound / outbound / examine-before-commit **Project** ([[plan/transport-layer/overview.md]]), not the User Epic.

## 5. If the user accepts (names only)

- New Epic file (not written in this pass): opening line as in §1. First Chapter: ingest (choose, gather, files and/or Graph). Later Chapter: review and commit to the mail server.
- [[plan/roadmap/epics/agent-chat-managed-context.md]] Notes: cite the new Epic; do not merge. Limited API, Change the Graph, and MCP stay in-Graph Agent work.
- [[plan/roadmap/epics/work-with-text-files-from-anywhere.md]] Notes: ingest may reuse files/Graph; do not add an email Chapter here; do not move the commit gate here.
- [[plan/roadmap/epics/operate-a-pkm.md]] Notes: ingested mail is material to Find; this Epic does not own ingest or outbound mail.
- [[plan/transport-layer/map.md]]: mail connector on the future checklist; Epic home = the new Epic; staging = examine-before-commit.
- [[plan/roadmap/map.md]]: list the new Epic under charting.

Do not rewrite those files until the user accepts. Do not create the Epic file in this pass.
