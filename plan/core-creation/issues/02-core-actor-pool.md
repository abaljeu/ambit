# 02 — Core Actor pool

**Context:** Core owns Actor pool machinery. Actor definitions stay outside Core.

**What to build:** Rebuild the provider-neutral Actor pool locked by [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]. One Core mailbox owns registry and admission. TaskPool runs and terminates Actors without waiting. See [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]]. Rebuild the discarded second-pool-mailbox shape. Do not wrap-patch it.

Launch and query exist on a discarded second pool mailbox. Rebuild the shape.

**Blocked by:** [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]], [[plan/core-creation/issues/12-define-actor-pool-shutdown-behavior.md]], [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]

**See also:** [[plan/core-creation/project.md]], [[plan/core-creation/reports/kernel-fsproj.md]], [[plan/event-sourced-ops/details/actors-and-jobs.md]], [[plan/event-sourced-ops/issues/09-job-identity-with-advisory-soft-lock.md]]

**Status:** ready-for-agent
Actual: 25m

- [ ] Launch does not hold the mailbox while the Actor runs. Registration is [[15-launch-actor-and-hold-span.md]].
- [x] The design specifies cancellation and cancel-after-enqueue behavior before implementation.
- [ ] Actor definitions, Browser chrome, and advisory soft-lock policy or indicators are not implemented in this issue.
- [ ] Rebuild on the one apply mailbox; do not wrap-patch the discarded second pool mailbox.

Cancel, finish, and Interrupted restart are [[17-cancel-a-job.md]] and [[18-finish-and-drop.md]].

## Comments

- 2026-09-11 — Rewind review named the mailbox/TaskPool/registry shape. Status was `needs-info`. Launch/query remain on the discarded second mailbox until rebuild.
- 2026-09-11 — [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] superseded span locks, lock-present outside History, delete-only completion, and no terminal result. [[plan/core-creation/issues/27-prove-core-actor-lifecycle-with-testactor.md]] proves the provider-neutral machinery without an Agent transport.
- 2026-09-11 — First implementation increment is [[29-prove-testactor-hello.md]]. This ticket stays the program piece for the full pool.

## Time

- 2026-09-11 15m — tighten intended mailbox/TaskPool shape after rewind (from chat)
- 2026-09-11 10m — drop restated finish and Interrupted gates; keep unique pool rebuild (from chat)
