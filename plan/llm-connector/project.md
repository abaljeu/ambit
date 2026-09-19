# llm-connector

Stage: done
Summary: Run an Agent from a Zoom-rooted mixed-format extract and replace Focus Children through ordinary Core Changes.
Updated: 2026-09-19
Started: 2026-09-19
Finished: 2026-09-19
Actual: 17h

## Notes

- 2026-09-19 — Stage `done`: first Agent vertical (08–11, 13) delivered on staging.
- 2026-09-19 — Landed [13 — Vertical proof: Browser Ask from what I see](issues/13-vertical-proof-browser-ask.md) on staging (Good). Status `done`.
- 2026-09-19 — Coded [13 — Vertical proof: Browser Ask from what I see](issues/13-vertical-proof-browser-ask.md): Browser-shaped `?ai` harness, `setFake` Finished, Poll Focus Children, lifecycle drop. Status `coded`.
- 2026-09-19 — Filed [13 — Vertical proof: Browser Ask from what I see](issues/13-vertical-proof-browser-ask.md) (Status `defined`; frontier). Client encode already shared with `?test`.
- 2026-09-19 — Landed AI-Actor Failed preserve ([09](issues/09-agent-failure-preserves-children.md)) on staging (Good). Status `done`.
- 2026-09-19 — Coded [09 — Agent failure preserves children](issues/09-agent-failure-preserves-children.md) AI-Actor erase proof: `setFake` yields Failed; Focus Children preserved; no Error Graph text. Status `coded`.
- 2026-09-19 — CloudAgents `setFake` handler yields `AgentStatus` (`Finished` or `Failed`).
- 2026-09-19 — Cancelled [12 — Replace Focus Children from reply](issues/12-replace-focus-children-from-reply.md): replace landed on 08; nested-tag path abandoned.
- 2026-09-19 — Landed [10 — Cancel by Focus](issues/10-cancel-by-focus.md) on staging (Good). Status `done`.
- 2026-09-19 — Coded [10 — Cancel by Focus](issues/10-cancel-by-focus.md): CoreMailbox `cancelByFocus`, hanging `setFake`, Run Agent cancel token. Status `coded`.
- 2026-09-19 — Landed [08 — Agent ask from what I see](issues/08-agent-ask-from-what-i-see.md) on staging (Good). Status `done`.
- 2026-09-19 — Coded [08 — Agent ask from what I see](issues/08-agent-ask-from-what-i-see.md): `setFake`, Run Agent Actor, Amb pack, Focus-child replace. Status `done`.
- 2026-09-19 — Locked CloudAgents `setFake` on the DLL (`(StartArgs -> AgentStatus) option -> bool`); success tests use fake; live call-reject until API key.
- 2026-09-19 — Landed [11 — Pack extract with Amb (supplied-fragment walk)](issues/11-simple-extract-format.md) on staging (Good). Status `done`.
- 2026-09-19 — Live CloudAgents / API key: until a key exists, only prove call reject; success stays on fake ([[issues/08-agent-ask-from-what-i-see.md|08 — Agent ask from what I see]]).
- 2026-09-19 — [09 — Agent failure preserves children](issues/09-agent-failure-preserves-children.md) framework half `done` (TestActor preserve proof). AI-Actor erase proof coded.
- 2026-09-19 — Drop observation: live Focus ids, not secrets ([[issues/09-agent-failure-preserves-children.md|09 — Agent failure preserves children]]).
- 2026-09-19 — Coded [11 — Pack extract with Amb (supplied-fragment walk)](issues/11-simple-extract-format.md): Amb extract-walk write and `Graph.focus`. Status `done`. Stage `build`.
- 2026-09-19 — Replan [11 — Pack extract with Amb (supplied-fragment walk)](issues/11-simple-extract-format.md): Amb extract-walk write; nested-tag abandoned; Fable.SimpleXml rejected. Status `defined`.
- 2026-09-19 — 09 lock: on failure Actor framework does not cause Changes; AI Actor does not erase data (future agentic extensions out of scope).
- 2026-09-19 — `/to-tickets` (tracer-cut): [[issues/08-agent-ask-from-what-i-see.md|08 — Agent ask from what I see]] (frontier), [[issues/09-agent-failure-preserves-children.md|09 — Agent failure preserves children]], [[issues/10-cancel-by-focus.md|10 — Cancel by Focus]]. Stage `slice`.
- 2026-09-19 — Arch grill closed: Agent Command `?ai` + args; vertical proof after first implement tickets `defined`; Focus mark spelling deferred to Document ticket; keep CloudAgents DLL; live-Actor chrome on core-creation 21/22 (first Agent vertical = Graph+Poll only).
- 2026-09-19 — Locked Agent Command spelling: `?ai` + optional args invokes Run Agent Actor; args ignored for now.
- 2026-09-19 — Published [[spec.md]] then [[arch.md]] from locked [[issues/06-define-command-run-agent-redesign.md|06]] / [[issues/07-lock-run-agent-architecture.md|07]] (checkboxes for delivered vs open). Stage `arch`.

## Implementation tickets

- [13 — Vertical proof: Browser Ask from what I see](issues/13-vertical-proof-browser-ask.md) — Status `done`.
- [[issues/11-simple-extract-format.md|11 — Pack extract with Amb (supplied-fragment walk)]] — Status `done`.
- [[issues/08-agent-ask-from-what-i-see.md|08 — Agent ask from what I see]] — Status `done`.
- [[issues/12-replace-focus-children-from-reply.md|12 — Replace Focus Children from reply]] — Status `cancelled` (Amb replace on 08; nested-tag abandoned).
- [09 — Agent failure preserves children](issues/09-agent-failure-preserves-children.md) — Status `done`. Report: [framework-failure-preserves-children](reports/framework-failure-preserves-children.md); review: [code-review-framework-failure-preserves-children](reports/code-review-framework-failure-preserves-children.md).
- [[issues/10-cancel-by-focus.md|10 — Cancel by Focus]] — Status `done`.

## Locked Restart

Draft PR #4 (cloud-agent Create slice / POST `/ambit/actors` / Md reply under Focus) was **closed unmerged** on 2026-09-11. [[plan/llm-connector/issues/05-create-cloud-agent-posts-reply-under-focus.md]] and the fat vertical slice approach are obsolete as the build frontier.

**Locked design points:** [[reports/agent-redesign-locked-2026-09.md]]

[[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]] locks the replacement Agent behavior, including Command-text dispatch and command text `?test hello`. [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] locks the typed boundaries, Event sequence, Authority identities, mailbox lifecycle, recovery, and test seams. The Run Agent Actor orchestrates Document, CloudAgents, and ordinary Core Changes.

The Phase 1 and Phase 1b gates are complete. The Project is at `arch` after publishing spec.md and arch.md; no Agent implementation issue or vertical-proof issue exists yet. The next serial executable work is the provider-neutral Core lifecycle in [[plan/core-creation/issues/Implementation Planning and Record.md]]. After that lifecycle is rebuilt, specify the smallest vendor-neutral CloudAgents implementation increment from the locked behavior and architecture.
