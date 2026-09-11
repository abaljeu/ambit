# 29 — Prove TestActor echo

**Status:** ready-for-agent
**Blocked by:** None — can start immediately.
Actual: 40m

## Context

Core can already apply a Change from a test Actor through the produce path. [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]], [[plan/core-creation/issues/12-define-actor-pool-shutdown-behavior.md]], and [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] are done. Launch and query still sit on a discarded second pool mailbox. The eight Phase 2 items name pieces of that program; they are not this increment. A first proof must launch TestActor through the public Core path and show one successful echo from outside.

## What to build

Launch TestActor through the public Core request members. The universal response is `{ nodes; events; latestId }`. Register TestActor through normal composition. The Command Node's text is the dispatch. Command text `?test echo` launches TestActor echo. Do not select the Actor from Command role, Kind, CSS class, or a separate Focus Header case id. Core validates public Authority plus secret on launch and on the Actor post. It registers that live Actor against Focus, appends ActorStarted, then schedules the Actor on a TaskPool so the Core mailbox is free. ActorStarted and the registry exist before Actor output can be admitted.

TestActor posts one echo Change of Owned children under Focus, then queues Succeeded. Succeeded is a Core-only terminal message, not a Change. Core appends exactly one ActorFinished, then removes the live registry and secret at once, then requests terminate only if the task still runs and never waits.

Rebuild that shape on the one Core mailbox. Put the registry (public Actor identity, secret credential, termination handle, and Focus NodeId) in mailbox state. The runner is a TaskPool, not a mailbox. Do not wrap-patch the discarded second pool mailbox.

This increment draws only those bits from [[02-core-actor-pool.md]], [[14-server-tracks-credentials.md]], [[15-launch-actor-and-hold-span.md]], [[18-finish-and-drop.md]], and [[27-prove-core-actor-lifecycle-with-testactor.md]]. It does not include live query, cancel, host-stop, fail, post-twice, duplicate terminal, Interrupted restart, Browser chrome, or Actor definitions other than TestActor.

- [ ] Tests launch TestActor through the public Core Actor path (Command text `?test echo`).
- [ ] Tests observe Graph and registry outcomes from outside; TestActor does not assert.
- [ ] After launch, the outer fact sees ActorStarted with durable public Actor identity before Actor output is admitted.
- [ ] After one echo Change under Focus and Succeeded, the outer fact sees the echo Graph.
- [ ] The outer fact sees exactly one ActorFinished.
- [ ] After ActorFinished, the live registry is gone; the secret no longer admits a post; the public identity remains on the Events.
- [ ] Tests use no live service, key, or Agent transport.

## See also

[[02-core-actor-pool.md]], [[27-prove-core-actor-lifecycle-with-testactor.md]], [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]], [[Implementation Planning and Record.md]], [[plan/core-creation/reports/actor-pool-rewind-review.md]], [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]]

## Comments

- 2026-09-11 — Alan: Command Node text is the dispatch. `?test echo` launches TestActor echo. `?ai ...` later. This replaces Command-role, Kind, and CSS Actor selection. Echo Graph under Focus remains the assertion.

## Time

- 2026-09-11 30m — first increment ticket and record cut (from chat)
- 2026-09-11 10m — record Command-text dispatch for TestActor echo (from chat)
