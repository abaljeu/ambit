# 17 — Cancel a job

**Status:** blocked
**Blocked by:** [[plan/core-creation/issues/18-finish-and-drop.md]]

## Context

A person stops further Actor work by Focus NodeId. At most one live Actor may target that Focus, so lookup is unique. Different Focus NodeIds may run concurrently regardless of overlapping or nested Zoom extracts. Changes that already merged stay. Cancel is not Undo.

## What to build

Core finds the live Actor by Focus NodeId and queues terminal Cancelled on the one Core mailbox. Processing Cancelled durably appends ActorFinished with no Error or Change, synchronously removes the registry and secret credential, then requests asynchronous task termination without waiting. Strict mailbox order means Change-before-Cancel applies and Cancel-before-Change rejects. A late duplicate completion is ignored. Cancellation preserves Focus Children except for earlier accepted Changes. Do not restore span membership or Graph lock-present.

- [ ] Cancel identifies the unique live job by Focus NodeId.
- [ ] Cancelled appends ActorFinished, removes the registry and secret, and requests termination without waiting.
- [ ] Change-before-Cancel applies and Cancel-before-Change rejects through normal Authority admission.
- [ ] Cancel does not Undo merged Changes.
- [ ] Cancellation emits no Error or Graph Change and preserves Focus Children.
- [ ] A late duplicate completion after cancellation is ignored.

## See also

[[plan/core-creation/issues/10-define-actor-cancellation-and-output-admission.md]], [[plan/core-creation/issues/02-core-actor-pool.md]], [[plan/core-creation/issues/23-close-core-object-seam.md]]

## Comments

- 2026-09-06 — Blocked by [[plan/core-creation/issues/23-close-core-object-seam.md]]. Cancel needs sender-at-Post; production Changes still post with no Credential.
- 2026-09-11 — Reconciled with [[plan/llm-connector/reports/agent-redesign-locked-2026-09.md]]. CancellationToken, normal credential admission, FIFO, and no Undo remain locked. Span membership was superseded and cancellation lookup remained pending.
- 2026-09-11 — [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] locked Cancelled as a durable terminal Event, synchronous registry and credential removal, non-blocking termination, strict Change/Cancel order, and duplicate-completion ignore. Status is `blocked` by the shared terminal path in [[plan/core-creation/issues/18-finish-and-drop.md]].

## Design note (2026-09-07)

Cancel is a fast Core mailbox message sharing the terminal and drop path ([[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]], [[plan/core-creation/issues/18-finish-and-drop.md]]). Update durable lifecycle and admission state synchronously, request termination without waiting, and do not invent a second drop or cancel-specific reject.
