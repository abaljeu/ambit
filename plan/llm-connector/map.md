# llm-connector

Labels: wayfinder:map

## Destination

Run an Agent from a Zoom-rooted mixed-format Graph extract, mark Focus in the outbound document, and replace Focus Children from returned text through ordinary Core Changes.

## Notes

- Enables [[plan/roadmap/epics/agent-chat-managed-context.md]] Chapter **Ask from what I see**.
- This Project owns document extraction, the vendor-neutral CloudAgents call, and response write-back. [[plan/expression-language/issues/33-recognize-ask-run-statement.md]] only recognizes `?` as a Run statement.
- Spoken name is Run Agent. Glossary: [[CONTEXT.md]] Run Agent, Included context, Agent. Do not say Agent for the Actor.
- Long-running Actor foundations: [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]] and [[plan/core-creation/issues/02-core-actor-pool.md]]. ESO background: [[plan/event-sourced-ops/details/actors-and-jobs.md]].

## Decisions so far

- 2026-09-11 redesign: Ambit is an info hub. Actor data formats and protocols vary; the adaptive-update process is common. [[reports/agent-redesign-locked-2026-09.md]]
- [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]] — Command-text dispatch, command text `?test hello`, Zoom-rooted extract, Focus replacement, Focus exclusivity, preserve-children, ordinary merge.
- [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] — typed boundaries, launch membership, Event sequence, mailbox lifecycle, recovery, test seams.
- CloudAgents remains the standalone vendor-neutral project and API. Cursor is an ordinary adapter. Provider selection is not a domain decision in this Project.
- [[plan/llm-connector/issues/05-create-cloud-agent-posts-reply-under-focus.md]] and its Create payload, Md paste-replace implementation, and vertical proof remain cancelled.
- 2026-09-19 — First pack is Amb extract-walk of the supplied Zoom extract. Nested-tag `<div>` / `<focus>` abandoned. Fable.SimpleXml rejected. Mixed-format owning-codec stays tabled. [[issues/11-simple-extract-format.md|11 — Pack extract with Amb (supplied-fragment walk)]].
- 2026-09-19 — Follow-up [14 — Provider-named AI errors](issues/14-provider-named-ai-errors.md): auth/start failure names the provider on Client Error (not Ask); no Graph Error dumps.

## Implementation

1. [14 — Provider-named AI errors](issues/14-provider-named-ai-errors.md) — Status `defined`.
2. [13 — Vertical proof: Browser Ask from what I see](issues/13-vertical-proof-browser-ask.md) — Status `done`.
3. [11 — Pack extract with Amb (supplied-fragment walk)](issues/11-simple-extract-format.md) — Status `done`.
4. [08 — Agent ask from what I see](issues/08-agent-ask-from-what-i-see.md) — Status `done`.
5. [09 — Agent failure preserves children](issues/09-agent-failure-preserves-children.md) — Status `done`.
6. [10 — Cancel by Focus](issues/10-cancel-by-focus.md) — Status `done`.

## Not yet specified

None — arch grill closed. Mixed-format owning-codec pack stays tabled under Decisions. Live-Actor chrome is owned by core-creation, not this map.

## Out of scope

- Follow-up turns, LLM-authored Changes, Graph/file queries, CLI/MCP — later Chapters on the Epic.
- New durability rules, new Core merge policy, provider-specific domain behavior, and exact Focus sentinel spelling.
- An Epic Project folder.
