# 29 — Prove TestActor hello

**Status:** ready-for-agent
**Blocked by:** none — Point 0 ([[32-move-persist-agents-under-coremailbox.md]] and prior) is done. Sections 1ff are not implemented.
Actual: 5h

## Context

Core can already apply a Change from a test Actor through the produce path. [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]], [[plan/core-creation/issues/12-define-actor-pool-shutdown-behavior.md]], and [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] are done. The eight Phase 2 items name pieces of that program; they are not this increment. A first proof must use the existing Browser Run path, launch TestActor through the public Core path (`StartActor`), and show one successful hello from outside.

Architecture (Story paths, Module map, Seams): [[plan/core-creation/arch.md|Core creation architecture]]. This ticket holds acceptance for the hello tracer; do not restate Module map Interface here.

## What to build

Build the hello slice of the one-mailbox Actor program locked by [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]. This increment takes mailbox, Authority, StartActor, and Succeeded-drop pieces from [[02-core-actor-pool.md]], [[14-server-tracks-credentials.md]], [[15-launch-actor-and-hold-span.md]], [[18-finish-and-drop.md]], and [[27-prove-core-actor-lifecycle-with-testactor.md]]. After [[32-move-persist-agents-under-coremailbox.md]], callers use the CoreMailbox door. Follow arch Story paths **Browser Run hello** and **Outside Core lifecycle proof**; Story path **Browser Change posts** is out of this ticket’s acceptance except as shared credentialed `PostChange` context on the arch.

### What this increment avoids

Live query, cancel, host-stop, fail, post-twice, duplicate terminal, Interrupted restart, new Browser chrome or controls, and Actor definitions other than TestActor.

### 1. Shared Core mailbox foundation

Prerequisite for the hello proof. Land Actor cases on the one Core mailbox per arch Story paths 1–2 and Module map (**CoreMsg / CoreMailboxBackend**, **CoreMailbox**, **CoreActorPool**, **History**). Sources: [[src/Server/Core/CoreMailbox.fs]], [[src/Server/Core/CoreMailboxBackend.fs]], [[src/Server/Core/CoreActorPool.fs]]. Persist stays PersistHandlers (Point 0). `type ActorResult = | ActorSucceeded`. No second mailbox, nested `ActorMsg` pump, or second live registry.

1. [ ] Live registry is only CoreActorPool’s synchronized table — arch **CoreActorPool**
2. [ ] `StartActor` on CoreMsg: validate credentials, async handoff to CoreActorPool.startActor — arch **CoreMsg / CoreMailboxBackend**
3. [ ] startActor writes live row, appends ActorStarted, schedules body; mailbox stays free — arch **CoreActorPool**
4. [ ] Admit before Actor `PostChange`; TestActor (Pool-selected `test`) runs `hello` through existing `PostChange` — arch **CoreMsg / CoreMailboxBackend**, **TestActor**
5. [ ] `ActorStop of ActorResult` (`ActorSucceeded` only): append ActorFinished, drop live row and secret, request terminate without waiting — arch **CoreMsg / CoreMailboxBackend**
6. [ ] Callers use CoreMailbox door only (not FileAgent / DbAgent wrappers) — arch **CoreMailbox**

### 2. Register the TestActor definition

1. [ ] Register TestActor through CoreActorPool.register (normal composition) — arch **CoreRuntime**, **TestActor**

### 3. Admit and invoke the named Actor from Command text

Payload and Pool/TestActor split: arch **CoreMailbox**, **CoreActorPool**, **TestActor**, **HTTP Adapter**.

1. [ ] Launch through public Core `StartActor` (id payload shape on arch Module map)
2. [ ] Universal response `{ nodes; events; latestId }` when that path is exercised
3. [ ] Command Node text is the dispatch ([[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]])
4. [ ] Pool selects Actor kind `test` from the command Node; TestActor interprets and runs `hello` only — command text `?test hello`
5. [ ] Do not select the Actor from Command role, Kind, CSS class, or a separate Focus Header case id

### 4. Run the one-Node Command and admit hello

First user-visible functional augmentation. Browser trigger rules live here; module contracts on arch **Browser Run**, **HTTP Adapter**, **TestActor**.

1. [ ] Trigger only when existing Browser Run and current Node text starts with literal `?` (no trim/normalize; no role/Kind/CSS trigger)
2. [ ] Browser sends a Command request; current Node is Command, Zoom root, and Focus; `graphIds` from arch **Loaded descendant id list** at that Zoom root (flat ids; Loaded children only; no Unloaded descent; ownership ignored)
3. [ ] Server uses that Node’s text as dispatch
4. [ ] For `?test hello`, TestActor posts one Owned child text `hello` under Focus via admitted `PostChange`, then queues `ActorStop ActorSucceeded`
5. [ ] Existing Run for text not starting with `?` stays unchanged
6. [ ] Reuse existing Run UI; add no new Browser chrome or control

### 5. Finish and drop

1. [ ] `ActorStop` is Core-only terminal message, not a Change — arch **CoreMsg / CoreMailboxBackend**
2. [ ] Exactly one ActorFinished, then drop live row and secret together; request terminate only if still running; never wait — arch **History**, **CoreActorPool**

### 6. Prove from outside

Entry shapes and test seam: arch Story path **Outside Core lifecycle proof** and Seam **Test seam for this tracer**.

1. [ ] First proof shows one successful hello from outside; harness may call CoreActorPool or TestActor at arch body-input / start shapes (not HTTP-only)
2. [ ] Command text remains `?test hello` (Pool selects `test`; TestActor runs `hello`)
3. [ ] Outer facts assert Graph and lifecycle; TestActor does not assert
4. [ ] After `StartActor`, outer fact sees ActorStarted with durable public Actor identity before Actor output is admitted
5. [ ] After hello Change under Focus and `ActorStop ActorSucceeded`, outer fact sees hello Graph (one Owned child text `hello`)
6. [ ] Outer fact sees exactly one ActorFinished
7. [ ] After ActorFinished: live row gone; secret no longer admits; public identity remains on Events
8. [ ] Tests use no live service, key, or Agent transport
9. [ ] Manual proof: Server + Browser, current Node `?test hello`, existing Run → one Owned child text `hello`
10. [ ] Manual proof uses no live external Agent service or key

## See also

[[plan/core-creation/arch.md|Core creation architecture]], [[02-core-actor-pool.md]], [[14-server-tracks-credentials.md]], [[15-launch-actor-and-hold-span.md]], [[18-finish-and-drop.md]], [[27-prove-core-actor-lifecycle-with-testactor.md]], [[30-reshape-coreactorpool-synchronized-table.md]], [[31-one-coremsg-loop-parameterized-persist.md]], [[32-move-persist-agents-under-coremailbox.md]], [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]], [[Implementation Planning and Record.md]], [[plan/core-creation/reports/actor-pool-rewind-review.md]], [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]], [[plan/core-creation/reports/implement-issue-29-testactor-hello.md]]

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
- 2026-09-13 — Earlier note that section 1 was implemented is withdrawn: code was reverted; sections 1ff remain open. Point 0 only is implemented. See [[plan/core-creation/arch.md]] and [[Implementation Planning and Record.md]].
- 2026-09-13 — Alan: CoreActorPool selects Actor kind `test` from the command Node and creates TestActor; TestActor interprets the command Node and runs `hello`. StartActor / HTTP / Core fields are `zoomId`, `focusId`, `commandId`, `graphIds` only; Pool expands to Graph and passes named ids + Actor secret. Outside tests call Pool or Actor at those shapes — not an HTTP-only gap. Story path 3 is Browser Change posts only.
- 2026-09-13 — DRY reshape: ticket acceptance points at arch Module map; checklists use numbered `N. [ ]` (from chat).
- 2026-09-13 — Alan: Browser Command `graphIds` are the Zoom-rooted Loaded descendant flat id list (Shared reusable function); not a one-Node-only extract. Unloaded children omitted; ownership ignored.

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
- 2026-09-13 2h — Shared Core mailbox foundation attempt (section 1); not retained in tree (from chat)
- 2026-09-13 15m — Align to arch: Pool selects `test` from command Node; TestActor interprets → hello; StartActor fields zoomId/focusId/commandId/graphIds; Pool→Graph + named ids + secret; outside proof calls Pool or Actor (from chat)
- 2026-09-13 20m — DRY reshape vs arch; numbered checklist items (from chat)
- 2026-09-13 10m — lock Browser `graphIds` as Shared Loaded descendant id list (from chat)
