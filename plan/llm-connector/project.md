# llm-connector

Stage: slice
Summary: Run an Agent from a Zoom-rooted mixed-format extract and replace Focus Children through ordinary Core Changes.
Updated: 2026-09-19
Actual: 7h30m

## Notes

- 2026-09-19 — Split Story path **Agent ask from what I see**: [08 — Run Agent Actor calls CloudAgents](issues/08-agent-ask-from-what-i-see.md) (Focus text, no Graph write), [11 — Simple extract format](issues/11-simple-extract-format.md) (`<div>` / `<focus>`), [12 — Replace Focus Children from reply](issues/12-replace-focus-children-from-reply.md). Md pack cancelled. Mixed-format tabled.
- 2026-09-19 — Live CloudAgents / API key: until a key exists, only prove call reject; success stays on fake ([[issues/08-agent-ask-from-what-i-see.md|08 — Run Agent Actor calls CloudAgents]]).
- 2026-09-19 — [[issues/09-agent-failure-preserves-children.md|09 — Agent failure preserves children]] framework half `done` (TestActor preserve proof). AI-Actor erase still open.
- 2026-09-19 — Drop observation: live Focus ids, not secrets ([[issues/09-agent-failure-preserves-children.md|09 — Agent failure preserves children]]).
- 2026-09-19 — [[issues/09-agent-failure-preserves-children.md|09 — Agent failure preserves children]]: unblock for TestActor / `?test` framework proof; AI-Actor erase stays after [[issues/08-agent-ask-from-what-i-see.md|08]] and [[issues/12-replace-focus-children-from-reply.md|12]].
- 2026-09-19 — 09 lock: on failure Actor framework does not cause Changes; AI Actor does not erase data (future agentic extensions out of scope).
- 2026-09-19 — `/to-tickets` (tracer-cut): [[issues/08-agent-ask-from-what-i-see.md|08 — Agent ask from what I see]] (frontier), [[issues/09-agent-failure-preserves-children.md|09 — Agent failure preserves children]], [[issues/10-cancel-by-focus.md|10 — Cancel by Focus]]. Stage `slice`.
- 2026-09-19 — Arch grill closed: Agent Command `?ai` + args; vertical proof after first implement tickets `defined`; Focus mark spelling deferred to Document ticket; keep CloudAgents DLL; live-Actor chrome on core-creation 21/22 (first Agent vertical = Graph+Poll only).
- 2026-09-19 — Locked Agent Command spelling: `?ai` + optional args invokes Run Agent Actor; args ignored for now.
- 2026-09-19 — Published [[spec.md]] then [[arch.md]] from locked [[issues/06-define-command-run-agent-redesign.md|06]] / [[issues/07-lock-run-agent-architecture.md|07]] (checkboxes for delivered vs open). Stage `arch`.

## Implementation tickets

- [[issues/08-agent-ask-from-what-i-see.md|08 — Run Agent Actor calls CloudAgents]] — Status `defined`; frontier.
- [[issues/11-simple-extract-format.md|11 — Simple extract format]] — Status `defined`; frontier.
- [[issues/12-replace-focus-children-from-reply.md|12 — Replace Focus Children from reply]] — Status `defined`; blocked by 08 and 11.
- [[issues/09-agent-failure-preserves-children.md|09 — Agent failure preserves children]] — Status `done` (framework via TestActor / `?test`); AI-Actor erase deferred until 08 and 12. Report: [[reports/framework-failure-preserves-children.md]]; review: [[reports/code-review-framework-failure-preserves-children.md]].
- [[issues/10-cancel-by-focus.md|10 — Cancel by Focus]] — Status `defined`; blocked by 08.

## Locked Restart

Draft PR #4 (cloud-agent Create slice / POST `/ambit/actors` / Md reply under Focus) was **closed unmerged** on 2026-09-11. [[plan/llm-connector/issues/05-create-cloud-agent-posts-reply-under-focus.md]] and the fat vertical slice approach are obsolete as the build frontier.

**Locked design points:** [[reports/agent-redesign-locked-2026-09.md]]

[[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]] locks the replacement Agent behavior, including Command-text dispatch and command text `?test hello`. [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] locks the typed boundaries, Event sequence, Authority identities, mailbox lifecycle, recovery, and test seams. The Run Agent Actor orchestrates Document, CloudAgents, and ordinary Core Changes.

The Phase 1 and Phase 1b gates are complete. The Project is at `arch` after publishing spec.md and arch.md; no Agent implementation issue or vertical-proof issue exists yet. The next serial executable work is the provider-neutral Core lifecycle in [[plan/core-creation/issues/Implementation Planning and Record.md]]. After that lifecycle is rebuilt, specify the smallest vendor-neutral CloudAgents implementation increment from the locked behavior and architecture.
