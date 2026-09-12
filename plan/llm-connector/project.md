# llm-connector

Stage: spec
Summary: Run an Agent from a Zoom-rooted mixed-format extract and replace Focus Children through ordinary Core Changes.
Updated: 2026-09-11
Actual: 6h55m

## Locked Restart

Draft PR #4 (cloud-agent Create slice / POST `/ambit/actors` / Md reply under Focus) was **closed unmerged** on 2026-09-11. [[plan/llm-connector/issues/05-create-cloud-agent-posts-reply-under-focus.md]] and the fat vertical slice approach are obsolete as the build frontier.

**Locked design points:** [[reports/agent-redesign-locked-2026-09.md]]

[[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]] locks the replacement Agent behavior, including Command-text dispatch and command text `?test hello`. [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] locks the typed boundaries, Event sequence, Authority identities, mailbox lifecycle, recovery, and test seams. The Run Agent Actor orchestrates Document, CloudAgents, and ordinary Core Changes.

The Phase 1 and Phase 1b gates are complete. The Project remains at `spec`; no Agent implementation issue or vertical-proof issue exists yet. The next serial executable work is the provider-neutral Core lifecycle in [[plan/core-creation/issues/Implementation Planning and Record.md]]. After that lifecycle is rebuilt, specify the smallest vendor-neutral CloudAgents implementation increment from the locked behavior and architecture.
