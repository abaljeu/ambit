# 02 — Core Actor pool

**Context:** Core owns Actor pool machinery. Actor definitions stay outside Core.

**What to build:** Rebuild the provider-neutral Actor lifecycle locked by [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]. Launch long-running work off the Core mailbox, assign durable public Actor identity and an ephemeral secret credential, order Actor output and terminal messages with Browser requests, and append lifecycle Events to the one global Event sequence. The mailbox must remain available while the Actor runs.

Intended shape (2026-09-11 architecture lock): exactly one Core mailbox orders launch, Change, cancel, Succeeded, Failed, Cancelled, and drop. See [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]]. The runner is a TaskPool, not a mailbox. Mailbox state owns the live registry: public Actor identity, secret credential, termination handle, and Focus NodeId. Launch appends ActorStarted before output admission. The first terminal message appends ActorFinished, synchronously removes the registry and credential, then requests termination only if still running and never waits. There is no credential MailboxProcessor.

Launch and query exist on a discarded second pool mailbox. Rebuild the shape. Do not wrap-patch that mailbox.

**Blocked by:** [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]], [[plan/core-creation/issues/12-define-actor-pool-shutdown-behavior.md]], [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]

**See also:** [[plan/core-creation/project.md]], [[plan/core-creation/reports/kernel-fsproj.md]], [[plan/event-sourced-ops/details/actors-and-jobs.md]], [[plan/event-sourced-ops/issues/09-job-identity-with-advisory-soft-lock.md]]

**Status:** ready-for-agent
Actual: 15m

- [ ] Launch validates Authority, registers a live Actor, durably appends ActorStarted, and does not hold the Core mailbox while the Actor runs.
- [ ] Cancel prevents further Actor output without undoing Changes that already merged.
- [ ] Actor Changes use the public Core request and universal response behavior.
- [ ] Exactly one terminal message appends ActorFinished before synchronous registry removal and non-blocking termination.
- [ ] Restart appends ActorFinished Interrupted for unmatched ActorStarted Events.
- [x] The design specifies cancellation and cancel-after-enqueue behavior before implementation.
- [ ] Actor definitions, Browser chrome, and advisory soft-lock policy or indicators are not implemented in this issue.
- [ ] One apply mailbox holds registry and admit-and-enqueue; the runner is a TaskPool, not a mailbox.

## Comments

- 2026-09-11 — Rewind review named the mailbox/TaskPool/registry shape. Status was `needs-info`. Launch/query remain on the discarded second mailbox until rebuild.
- 2026-09-11 — [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] superseded span locks, lock-present outside History, delete-only completion, and no terminal result. [[plan/core-creation/issues/27-prove-core-actor-lifecycle-with-testactor.md]] proves the provider-neutral machinery without an Agent transport.
- 2026-09-11 — First implementation increment is [[29-prove-testactor-echo.md]] (TestActor echo). This ticket stays the program piece for the full pool.

## Time

- 2026-09-11 15m — tighten intended mailbox/TaskPool shape after rewind (from chat)
