# 29 — Prove TestActor hello

**Status:** ready-for-agent
**Blocked by:** none — [[32-move-persist-agents-under-coremailbox.md]] is done. Remaining hello sections still open.
Actual: 4h15m

## Context

Core can already apply a Change from a test Actor through the produce path. [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]], [[plan/core-creation/issues/12-define-actor-pool-shutdown-behavior.md]], and [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] are done. The eight Phase 2 items name pieces of that program; they are not this increment. A first proof must use the existing Browser Run path, launch TestActor through the public Core path (`StartActor`), and show one successful hello from outside.

## What to build

Build the hello slice of the one-mailbox Actor program locked by [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]. This increment takes mailbox, Authority, StartActor, and Succeeded-drop pieces from [[02-core-actor-pool.md]], [[14-server-tracks-credentials.md]], [[15-launch-actor-and-hold-span.md]], [[18-finish-and-drop.md]], and [[27-prove-core-actor-lifecycle-with-testactor.md]]. After [[32-move-persist-agents-under-coremailbox.md]], callers use the CoreMailbox door. A first proof uses the existing Browser Run path, launches TestActor through the public Core path (`StartActor`), and shows one successful hello from outside.

### What this increment avoids

Live query, cancel, host-stop, fail, post-twice, duplicate terminal, Interrupted restart, new Browser chrome or controls, and Actor definitions other than TestActor.

### 1. Shared Core mailbox foundation

The foundation is one Core mailbox: the `CoreMsg` union, the `CoreMailboxBackend` loop, and the CoreMailbox door on MailboxHost after [[32-move-persist-agents-under-coremailbox.md]]. Sources: [[src/Server/Core/CoreMailbox.fs]], [[src/Server/Core/CoreMailboxBackend.fs]]. Persist stays the PersistHandlers parameter from [[31-one-coremsg-loop-parameterized-persist.md]]. `StartActor` and `ActorStop of ActorResult` are new `CoreMsg` cases on that same loop. `type ActorResult = | ActorSucceeded`. The loop handles `StartActor`: mailbox validates the caller's public Authority and secret, then hands off async to startActor in CoreActorPool. The loop handles `ActorStop`. `ActorStop` mutates the CoreActorPool table. The loop admits the Actor against that table before `PostChange`. There is no second mailbox and no second live registry beside that table. This foundation is a prerequisite for the hello proof, not another mailbox refactor.

- [x] The live registry is CoreActorPool's data: public Actor identity, secret credential, termination handle, and Focus NodeId. That data is the pool's synchronized table. Mailbox-loop state does not hold a second copy.
- [x] Add the `StartActor` launch case to `CoreMsg`. The loop handles `StartActor`. Mailbox validates the caller's public Authority and secret, then hands off async to startActor in CoreActorPool to do the work.
- [x] startActor creates the Actor public identity and secret, writes the live row on that table against Focus, appends ActorStarted, then asks CoreActorPool to run the Actor body. The mailbox stays free. ActorStarted and the live row exist before any Actor Change can be admitted.
- [x] After the same loop admits the Actor's public Authority and secret against that table, Actor `test` (TestActor) switches to run `hello` and posts that Change through the existing `PostChange` persist case.
- [x] Add `ActorStop of ActorResult` to `CoreMsg`. `type ActorResult = | ActorSucceeded`. The loop handles `ActorStop`. After earlier queued Changes, `ActorStop ActorSucceeded` appends ActorFinished, removes the live row from the CoreActorPool table and the secret together, and requests terminate only if the body still runs. `ActorStop` is not a Change. The mailbox never waits.
- [x] CoreActorPool is the synchronized live table and thread-pool runner ([[src/Server/Core/CoreActorPool.fs]]). The live registry is that table. `StartActor` and `ActorStop` are `CoreMsg` cases. startActor writes the live row on that table. `ActorStop` mutates that table. The loop admits the Actor against that table before `PostChange`. The pool runs the Actor body (`ActorFn`) on the thread pool. The mailbox does not wait on that body or wrap-patch the discarded pool queue.
- [x] Callers reach `StartActor`, `PostChange`, and `ActorStop` through the public CoreMailbox door (MailboxHost after [[32-move-persist-agents-under-coremailbox.md]]), not FileAgent or DbAgent wrappers.

### 2. Register the TestActor definition

- [ ] Register TestActor through CoreActorPool.register (normal composition).

### 3. Admit and invoke the named Actor from Command text

- [ ] Launch TestActor through the public Core request members (`StartActor`).
- [ ] The universal response is `{ nodes; events; latestId }`.
- [ ] The Command Node's text is the dispatch. Dispatch is [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]].
- [ ] The Server interprets the Command text to resolve and invoke the named Actor. The Command text `?test hello` selects Actor `test` (TestActor), then that Actor switches to run the `hello` case. `hello` is the only case on TestActor in this increment.
- [ ] Do not select the Actor from Command role, Kind, CSS class, or a separate Focus Header case id.

### 4. Run the one-Node Command and admit hello

This is the first user-visible functional augmentation after the foundation and its internal registration and launch prerequisites.

- [ ] Trigger this path only when the existing Browser command/action is Run and the current Node's text starts with the literal `?`. Do not trim or normalize text, inspect case, role, Kind, or CSS, or add another trigger.
- [ ] The Browser sends a Command request to the Server.
- [ ] The request's complete Graph extract/context contains only the current Node. That one Node is the named Command Node, the Zoom root, and the one Focus. Do not include ancestors, siblings, or Children.
- [ ] The Server uses that Node's text as the dispatch.
- [ ] For `?test hello`, TestActor receives the Command Node with that complete text and posts `hello` as one Owned child of the current Focus through the normal Core Change path, then queues `ActorStop ActorSucceeded`.
- [ ] Existing Run behavior for a current Node whose text does not start with `?` stays unchanged.
- [ ] Reuse the existing Run UI. Add no new Browser chrome or control.

### 5. Finish and drop

- [ ] `ActorStop` is a Core-only terminal message, not a Change.
- [ ] Core appends exactly one ActorFinished, then removes the live row from the CoreActorPool table and the secret together, then requests terminate only if the task still runs and never waits.

### 6. Prove from outside

- [ ] A first proof must launch TestActor through the public Core path (`StartActor`) and show one successful hello from outside.
- [ ] Tests launch TestActor through the public Core Actor path (Command text `?test hello`).
- [ ] Tests observe Graph and registry outcomes from outside; TestActor does not assert.
- [ ] After `StartActor`, the outer fact sees ActorStarted with durable public Actor identity before Actor output is admitted.
- [ ] After one hello Change under Focus and `ActorStop ActorSucceeded`, the outer fact sees the hello Graph (one Owned child text `hello`).
- [ ] The outer fact sees exactly one ActorFinished.
- [ ] After ActorFinished, the live row is gone from the CoreActorPool table; the secret no longer admits a post; the public identity remains on the Events.
- [ ] Tests use no live service, key, or Agent transport.
- [ ] For the final manual proof, run the Server and Browser, make the current Node's text `?test hello`, and execute the existing Run.
- [ ] In that manual proof, the Browser sends the one-Node Command request and the final visible outcome is one Owned child with text `hello`.
- [ ] The manual proof uses no live external Agent service or key.

## See also

[[02-core-actor-pool.md]], [[14-server-tracks-credentials.md]], [[15-launch-actor-and-hold-span.md]], [[18-finish-and-drop.md]], [[27-prove-core-actor-lifecycle-with-testactor.md]], [[30-reshape-coreactorpool-synchronized-table.md]], [[31-one-coremsg-loop-parameterized-persist.md]], [[32-move-persist-agents-under-coremailbox.md]], [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]], [[Implementation Planning and Record.md]], [[plan/core-creation/reports/actor-pool-rewind-review.md]], [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]], [[plan/core-creation/reports/implement-issue-29-testactor-hello.md]]

## Comments

- 2026-09-11 — Dispatch itself is [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]]. That ticket owns command text `?test hello`.
- 2026-09-11 — Hello uses the universal `{ nodes; events; latestId }` response. It does not use `CoreChangesAccepted`. TestActor receives the secret credential; after ActorFinished the outer fact proves that secret no longer admits a post.
- 2026-09-12 — Alan locked the first user-visible augmentation as existing Browser Run through one-Node Command transport, named Actor dispatch, and TestActor hello. The same current Node is Command, Zoom root, and Focus.
- 2026-09-12 — Blocked on 30. CoreActorPool is not discarded; its queue design is.
- 2026-09-12 — Point 0 continues with 31 (one CoreMsg loop, parameterized persist) before hello.
- 2026-09-13 — Point 0 continues with 32 (persist agents under Core; generic CoreMailbox door) before hello.
- 2026-09-13 — Alan asked for positive mailbox-foundation specs.
- 2026-09-13 — Assumption: hello adds two `CoreMsg` cases, `StartActor` and `ActorStop of ActorResult`, on the existing `CoreMsg` union. `type ActorResult = | ActorSucceeded`. The loop handles both. Mailbox validates `StartActor` then hands off async to startActor in CoreActorPool. Actor hello Changes reuse `PostChange` after that loop admits the live Authority pair against the CoreActorPool table. Definition register stays `CoreActorPool.register` (section 2). This increment does not add a nested `ActorMsg` pump, Poll, Seed, Failed, or Cancelled. No other ActorResult cases yet.
- 2026-09-13 — Clarity: the actor is `test`. Test switches to run `hello`. No other cases exist yet.
- 2026-09-13 — Alan: the live registry IS CoreActorPool's data. After 30, CoreActorPool is a synchronized table + thread-pool runner. The mailbox still owns the fast messages (`StartActor`, `ActorStop`, admit-before-`PostChange`). Mailbox validates `StartActor` then hands off async to startActor; `ActorStop` mutates the pool table. Do not invent a second registry beside the pool.
- 2026-09-13 — Alan: StartActor opener unclear; name the CoreMsg case, not mailbox-state.
- 2026-09-13 — Alan: `StartActor` is the CoreMsg case; mailbox validates then async-hands to CoreActorPool.startActor.
- 2026-09-13 — Alan: generalize; CoreMsg gets `ActorStop of ActorResult`; `type ActorResult = | ActorSucceeded`. No other ActorResult cases yet.
- 2026-09-13 — Section 1 implemented: one Core mailbox owns `StartActor` / admit-before-`PostChange` / `ActorStop`; live rows live only on the CoreActorPool table; TestActor hello posts through that `PostChange` path. Later sections still open.

## Time

- 2026-09-11 30m — first increment ticket and record cut (from chat)
- 2026-09-11 10m — record Command-text dispatch for TestActor echo (from chat)
- 2026-09-11 10m — hello command and test name; drop restated dispatch (from chat)
- 2026-09-11 10m — hello filename; drop restated pool, admission, launch, and finish (from chat)
- 2026-09-12 10m — lock minimal Browser Run transport, named TestActor invocation, and manual proof (from chat)
- 2026-09-13 20m — rewrite mailbox foundation as a positive spec (from chat)
- 2026-09-13 10m — lock TestActor `hello` switch (from chat)
- 2026-09-13 10m — lock live registry as CoreActorPool table (from chat)
- 2026-09-13 15m — reconcile StartActor / startActor shape after Alan's four bullets (from chat)
- 2026-09-13 10m — lock ActorStop of ActorResult; ActorSucceeded only (from chat)
- 2026-09-13 2h — Shared Core mailbox foundation (section 1) (from chat)
