# 29 — Prove TestActor hello

**Status:** blocked
**Blocked by:** [[31-one-coremsg-loop-parameterized-persist.md]] — One CoreMsg loop, parameterized persist (note: 30 is done)
Actual: 1h10m

## Context

Core can already apply a Change from a test Actor through the produce path. [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]], [[plan/core-creation/issues/12-define-actor-pool-shutdown-behavior.md]], and [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] are done. The eight Phase 2 items name pieces of that program; they are not this increment. A first proof must use the existing Browser Run path, launch TestActor through the public Core path, and show one successful hello from outside.

## What to build

This increment draws only those bits from [[02-core-actor-pool.md]], [[14-server-tracks-credentials.md]], [[15-launch-actor-and-hold-span.md]], [[18-finish-and-drop.md]], and [[27-prove-core-actor-lifecycle-with-testactor.md]]. It does not include live query, cancel, host-stop, fail, post-twice, duplicate terminal, Interrupted restart, new Browser chrome or controls, or Actor definitions other than TestActor.

### 1. Shared Core mailbox foundation

Build this augmentation on the shared `CoreMailbox` callable interface in [[src/Server/Core/CoreMailbox.fs]] and the internal `CoreMailboxBackend` implementation used by the FileAgent and DbAgent persistence twins in [[src/Server/Core/CoreMailboxBackend.fs]]. This foundation is a prerequisite, not another mailbox refactor.

- [ ] Rebuild that shape on the one Core mailbox. Put the registry (public Actor identity, secret credential, termination handle, and Focus NodeId) in mailbox state. CoreActorPool shall be used as the thread-pool runner after 30; do not wrap-patch its old mailbox-queue internals.
- [ ] This increment does not include live query, cancel, host-stop, fail, post-twice, duplicate terminal, Interrupted restart, new Browser chrome or controls, or Actor definitions other than TestActor.

### 2. Register the TestActor definition

- [ ] Register TestActor through normal composition.

### 3. Admit and invoke the named Actor from Command text

- [ ] Launch TestActor through the public Core request members.
- [ ] The universal response is `{ nodes; events; latestId }`.
- [ ] The Command Node's text is the dispatch. Dispatch is [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]].
- [ ] The Server interprets the Command text to resolve and invoke the named Actor. For `?test hello`, the named Actor is `test`, which invokes TestActor.
- [ ] Do not select the Actor from Command role, Kind, CSS class, or a separate Focus Header case id.
- [ ] Core validates public Authority plus secret on launch and on the Actor post.
- [ ] Core registers that live Actor against Focus, appends ActorStarted, then schedules the Actor so the Core mailbox is free.
- [ ] ActorStarted and the registry exist before Actor output can be admitted.

### 4. Run the one-Node Command and admit hello

This is the first user-visible functional augmentation after the foundation and its internal registration and launch prerequisites.

- [ ] Trigger this path only when the existing Browser command/action is Run and the current Node's text starts with the literal `?`. Do not trim or normalize text, inspect case, role, Kind, or CSS, or add another trigger.
- [ ] The Browser sends a Command request to the Server.
- [ ] The request's complete Graph extract/context contains only the current Node. That one Node is the named Command Node, the Zoom root, and the one Focus. Do not include ancestors, siblings, or Children.
- [ ] The Server uses that Node's text as the dispatch.
- [ ] For `?test hello`, TestActor receives the Command Node with that complete text and posts `hello` as one Owned child of the current Focus through the normal Core Change path, then queues Succeeded.
- [ ] Existing Run behavior for a current Node whose text does not start with `?` stays unchanged.
- [ ] Reuse the existing Run UI. Add no new Browser chrome or control.

### 5. Finish and drop

- [ ] Succeeded is a Core-only terminal message, not a Change.
- [ ] Core appends exactly one ActorFinished, then removes the live registry and secret at once, then requests terminate only if the task still runs and never waits.

### 6. Prove from outside

- [ ] A first proof must launch TestActor through the public Core path and show one successful hello from outside.
- [ ] Tests launch TestActor through the public Core Actor path (Command text `?test hello`).
- [ ] Tests observe Graph and registry outcomes from outside; TestActor does not assert.
- [ ] After launch, the outer fact sees ActorStarted with durable public Actor identity before Actor output is admitted.
- [ ] After one hello Change under Focus and Succeeded, the outer fact sees the hello Graph (one Owned child text `hello`).
- [ ] The outer fact sees exactly one ActorFinished.
- [ ] After ActorFinished, the live registry is gone; the secret no longer admits a post; the public identity remains on the Events.
- [ ] Tests use no live service, key, or Agent transport.
- [ ] For the final manual proof, run the Server and Browser, make the current Node's text `?test hello`, and execute the existing Run.
- [ ] In that manual proof, the Browser sends the one-Node Command request and the final visible outcome is one Owned child with text `hello`.
- [ ] The manual proof uses no live external Agent service or key.

## See also

[[02-core-actor-pool.md]], [[27-prove-core-actor-lifecycle-with-testactor.md]], [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]], [[Implementation Planning and Record.md]], [[plan/core-creation/reports/actor-pool-rewind-review.md]], [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]], [[plan/core-creation/reports/implement-issue-29-testactor-hello.md]]

## Comments

- 2026-09-11 — Dispatch itself is [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]]. That ticket owns command text `?test hello`.
- 2026-09-11 — Hello uses the universal `{ nodes; events; latestId }` response. It does not use `CoreChangesAccepted`. TestActor receives the secret credential; after ActorFinished the outer fact proves that secret no longer admits a post.
- 2026-09-12 — Alan locked the first user-visible augmentation as existing Browser Run through one-Node Command transport, named Actor dispatch, and TestActor hello. The same current Node is Command, Zoom root, and Focus.
- 2026-09-12 — Blocked on 30. CoreActorPool is not discarded; its queue design is.
- 2026-09-12 — Point 0 continues with 31 (one CoreMsg loop, parameterized persist) before hello.

## Time

- 2026-09-11 30m — first increment ticket and record cut (from chat)
- 2026-09-11 10m — record Command-text dispatch for TestActor echo (from chat)
- 2026-09-11 10m — hello command and test name; drop restated dispatch (from chat)
- 2026-09-11 10m — hello filename; drop restated pool, admission, launch, and finish (from chat)
- 2026-09-12 10m — lock minimal Browser Run transport, named TestActor invocation, and manual proof (from chat)
