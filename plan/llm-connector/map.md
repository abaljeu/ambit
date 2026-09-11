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
- [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]] — the Command Node's text is the dispatch (`?test echo` for TestActor echo; `?ai ...` later). The document codec formats Command Nodes as prompt-like content, but the provider request does not extract a separate Command instruction.
- [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] — the Browser sends exact included NodeIds plus Zoom, Focus, Command, and current event id. Core reads Command Node text to resolve ActorName and constructs the extract from its authoritative Graph. No Browser-selected ActorName crosses the boundary.
- One Core API returns `{ nodes; events; latestId }` for Browser Poll, Change, and Command and in-process Actor Change. Change, Undo, Redo, ActorStarted, and ActorFinished share one durable ordered Event sequence. Public Authority identity persists; secret credentials never persist.
- Run Agent Actor owns the system prompt and orchestrates generic Document mixed-format serialization, vendor-neutral CloudAgents completion, Reference-Paste-style replacement, and normal Core Change.
- Success replaces every Child under Focus. A failed structural response parse retries the complete response through the plain-text indentation outline parser. Existing Core Change merge and amendment own reconciliation.
- One live Actor is allowed per Focus NodeId; other overlap is allowed. Cancel uses Focus NodeId. ActorStarted exposes durable public actor identity; ActorFinished records Succeeded, safe Failed, Cancelled, or restart reconciliation as Interrupted. Failure and cancellation preserve Focus Children and emit no Graph text.
- CloudAgents remains the standalone vendor-neutral project and API. Cursor is an ordinary adapter. Provider selection is not a domain decision in this Project.
- [[plan/llm-connector/issues/05-create-cloud-agent-posts-reply-under-focus.md]] and its Create payload, Md paste-replace implementation, and vertical proof remain cancelled.

## Not yet specified

- Convert [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]] and [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] into the smallest vendor-neutral CloudAgents implementation issue after the provider-neutral Core lifecycle is rebuilt.
- Define the replacement vertical proof after the Core lifecycle and Agent implementation contracts are executable.

## Out of scope

- Follow-up turns, LLM-authored Changes, Graph/file queries, CLI/MCP — later Chapters on the Epic.
- New durability rules, new Core merge policy, provider-specific domain behavior, and exact Focus sentinel spelling.
- An Epic Project folder.
