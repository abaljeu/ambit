# 02 — Core Actor pool

**Context:** Core owns Actor pool machinery. Actor definitions stay outside Core.

**What to build:** Launch long-running work off the apply queue, assign Core-owned job identity, cancel further output from a job, and finish work through Core Changes and inner apply. The apply queue must remain available while the Actor runs.

Intended shape (2026-09-11 rewind): exactly one Core mailbox — the Changes apply queue. Posts, cancel, delete-actor, launch, query, and admit-and-enqueue are fast messages on that mailbox. See [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]]. The runner is a TaskPool (or equivalent), not a mailbox. The registry (public number, lock-present, credential, handle to terminate) is state of that mailbox. There is no credential MailboxProcessor. Admit and enqueue are one mailbox message. Drop is registry remove plus async terminate if the task is still running. Detail: [[18-finish-and-drop.md]], [[../reports/actor-pool-rewind-review.md]].

Launch and query exist on a discarded second pool mailbox. Rebuild the shape. Do not wrap-patch that mailbox.

**Blocked by:** [[01-generalized-server-actor-produce-path.md]], [[12-define-actor-pool-shutdown-behavior.md|Define Actor-pool shutdown behavior]]

**See also:** [[plan/core-creation/project.md]], [[plan/core-creation/reports/kernel-fsproj.md]], [[plan/event-sourced-ops/details/actors-and-jobs.md]], [[plan/event-sourced-ops/issues/09-job-identity-with-advisory-soft-lock.md]]

**Status:** ready-for-agent
Actual: 15m

- [ ] Launch returns a Core-owned job identity and does not hold the apply queue while the Actor runs.
- [ ] Cancel prevents further Actor output without undoing Changes that already merged.
- [ ] A finishing Actor submits Change objects through Core Changes and inner apply.
- [x] The design specifies cancellation and cancel-after-enqueue behavior before implementation.
- [ ] Actor definitions, Browser chrome, and advisory soft-lock policy or indicators are not implemented in this issue.
- [ ] One apply mailbox holds registry and admit-and-enqueue; the runner is a TaskPool, not a mailbox.

## Comments

- 2026-09-11 — Rewind review named the mailbox/TaskPool/registry shape. Status was `needs-info`. Launch/query remain on the discarded second mailbox until rebuild.

## Time

- 2026-09-11 15m — tighten intended mailbox/TaskPool shape after rewind (from chat)
