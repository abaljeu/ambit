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

## Not yet specified

- Convert [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]] and [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] into the smallest vendor-neutral CloudAgents implementation issue after the provider-neutral Core lifecycle is rebuilt.
- Define the replacement vertical proof after the Core lifecycle and Agent implementation contracts are executable.

## Out of scope

- Follow-up turns, LLM-authored Changes, Graph/file queries, CLI/MCP — later Chapters on the Epic.
- New durability rules, new Core merge policy, provider-specific domain behavior, and exact Focus sentinel spelling.
- An Epic Project folder.
