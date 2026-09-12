# 29 — Prove TestActor hello

**Status:** done
**Blocked by:** None — can start immediately.
Actual: 3h20m

## Context

Core can already apply a Change from a test Actor through the produce path. [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]], [[plan/core-creation/issues/12-define-actor-pool-shutdown-behavior.md]], and [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] are done. Launch and query still sit on a discarded second pool mailbox. The eight Phase 2 items name pieces of that program; they are not this increment. A first proof must launch TestActor through the public Core path and show one successful hello from outside.

## What to build

This increment draws only those bits from [[02-core-actor-pool.md]], [[14-server-tracks-credentials.md]], [[15-launch-actor-and-hold-span.md]], [[18-finish-and-drop.md]], and [[27-prove-core-actor-lifecycle-with-testactor.md]]. It does not include live query, cancel, host-stop, fail, post-twice, duplicate terminal, Interrupted restart, Browser chrome, or Actor definitions other than TestActor.

- [x] A first proof must launch TestActor through the public Core path and show one successful hello from outside.
- [x] Launch TestActor through the public Core request members.
- [x] The universal response is `{ nodes; events; latestId }`.
- [x] Register TestActor through normal composition.
- [x] The Command Node's text is the dispatch. Dispatch is [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]].
- [x] Command text `?test hello` launches TestActor hello.
- [x] Do not select the Actor from Command role, Kind, CSS class, or a separate Focus Header case id.
- [x] Core validates public Authority plus secret on launch and on the Actor post.
- [x] Core registers that live Actor against Focus, appends ActorStarted, then schedules the Actor so the Core mailbox is free.
- [x] ActorStarted and the registry exist before Actor output can be admitted.
- [x] TestActor posts one hello Change of Owned children under Focus, then queues Succeeded.
- [x] Succeeded is a Core-only terminal message, not a Change.
- [x] Core appends exactly one ActorFinished, then removes the live registry and secret at once, then requests terminate only if the task still runs and never waits.
- [x] Rebuild that shape on the one Core mailbox. Put the registry (public Actor identity, secret credential, termination handle, and Focus NodeId) in mailbox state. The runner is not a mailbox. Do not wrap-patch the discarded second pool mailbox.
- [x] This increment does not include live query, cancel, host-stop, fail, post-twice, duplicate terminal, Interrupted restart, Browser chrome, or Actor definitions other than TestActor.
- [ ] Tests launch TestActor through the public Core Actor path (Command text `?test hello`).
- [x] Tests observe Graph and registry outcomes from outside; TestActor does not assert.
- [ ] After launch, the outer fact sees ActorStarted with durable public Actor identity before Actor output is admitted.
- [ ] After one hello Change under Focus and Succeeded, the outer fact sees the hello Graph (one Owned child text `hello`).
- [ ] The outer fact sees exactly one ActorFinished.
- [ ] After ActorFinished, the live registry is gone; the secret no longer admits a post; the public identity remains on the Events.
- [ ] Tests use no live service, key, or Agent transport.

## See also

[[02-core-actor-pool.md]], [[27-prove-core-actor-lifecycle-with-testactor.md]], [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]], [[Implementation Planning and Record.md]], [[plan/core-creation/reports/actor-pool-rewind-review.md]], [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]], [[plan/core-creation/reports/implement-issue-29-testactor-hello.md]]

## Comments

- 2026-09-11 — Dispatch itself is [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]]. That ticket owns command text `?test hello`.
- 2026-09-11 — Hello uses the universal `{ nodes; events; latestId }` response. It does not use `CoreChangesAccepted`. TestActor receives the secret credential; after ActorFinished the outer fact proves that secret no longer admits a post.
- 2026-09-11 — Marked locked What-to-build spec details complete. Proof boxes stay open. Observe-from-outside stays complete.

## Time

- 2026-09-11 30m — first increment ticket and record cut (from chat)
- 2026-09-11 10m — record Command-text dispatch for TestActor echo (from chat)
- 2026-09-11 10m — hello command and test name; drop restated dispatch (from chat)
- 2026-09-11 10m — hello filename; drop restated pool, admission, launch, and finish (from chat)
- 2026-09-11 2h — implement public Core hello path, FileAgent mailbox registry, TestActor hello, and Server facts (from chat)
- 2026-09-11 20m — mark locked spec details complete; leave undelivered proof boxes open (from chat)
