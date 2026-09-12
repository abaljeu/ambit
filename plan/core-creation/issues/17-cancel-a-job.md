# 17 — Cancel a job

**Status:** blocked
**Blocked by:** [[plan/core-creation/issues/18-finish-and-drop.md]]

## Context

A person stops further Actor work by Focus NodeId. Lookup is unique because Focus exclusivity is [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]]. Changes that already merged stay. Cancel is not Undo.

## What to build

Cancel the unique live Actor by Focus NodeId. Terminal and drop are [[18-finish-and-drop.md]]. Preserve earlier accepted Changes, reject later output through normal Authority admission, and do not Undo. Do not restore span membership or Graph lock-present.

- [ ] Cancel identifies the unique live job by Focus NodeId.
- [ ] Cancel does not Undo merged Changes.
- [ ] Later output after Cancelled is refused through normal Authority admission.

## See also

[[plan/core-creation/issues/10-define-actor-cancellation-and-output-admission.md]], [[plan/core-creation/issues/02-core-actor-pool.md]], [[plan/core-creation/issues/23-close-core-object-seam.md]]

## Comments

- 2026-09-06 — Blocked by [[plan/core-creation/issues/23-close-core-object-seam.md]]. Cancel needs sender-at-Post; production Changes still post with no Credential.
- 2026-09-11 — Reconciled with [[plan/llm-connector/reports/agent-redesign-locked-2026-09.md]]. CancellationToken, normal credential admission, FIFO, and no Undo remain locked. Span membership was superseded and cancellation lookup remained pending.
- 2026-09-11 — [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] locked Cancelled as a durable terminal Event, synchronous registry and credential removal, non-blocking termination, strict Change/Cancel order, and duplicate-completion ignore. Status is `blocked` by the shared terminal path in [[plan/core-creation/issues/18-finish-and-drop.md]].

## Design note (2026-09-07)

Cancel is a fast Core mailbox message. It shares the terminal and drop path in [[plan/core-creation/issues/18-finish-and-drop.md]]. See [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]].

## Time

- 2026-09-11 5m — drop restated terminal and drop path; keep unique Focus cancel (from chat)
