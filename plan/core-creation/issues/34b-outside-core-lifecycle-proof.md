# 34b — Outside Core lifecycle proof

**Status:** ready-to-implement
**Blocked by:** None — can start immediately.
Actual: 20m

## Context

Core already accepts credentialed Browser Changes through its one mailbox. This ticket implements Story path **Outside Core lifecycle proof** (Story 2) in [[plan/core-creation/arch.md|Core creation architecture]]. It does not implement Story path **Browser Run hello**. The proof enters at CoreActorPool.startActor or at TestActor body input, runs TestActor `hello`, and observes the Graph and lifecycle from outside the Actor. There is no Browser HTTP and no Agent transport.

This ticket follows Seam **Test seam for this tracer**. The architecture Module map owns State, Interface, and Uses details.

## What to build

Prove one successful TestActor `hello` as a complete tracer through Core. The outer proof starts the Actor, observes ActorStarted before admitted output, observes one Owned child text `hello` under Focus, observes one ActorFinished, and confirms that the live row is gone and the secret is revoked. The durable public Actor identity stays on History. TestActor produces output but does not assert. The proof does not require Browser HTTP, an Agent transport, or HTTP Adapter universal-response encoding.

### 1. CoreMailbox door

Use the **CoreMailbox** door when the proof exercises the full public Core lifecycle.

1. [x] Start Actor — submit the architecture's StartActor request through CoreMailbox.
2. [x] Carry Actor output — route credentialed Actor Change and ActorStop messages through the same mailbox. There is no second Actor mailbox.
3. [x] Expose lifecycle facts — let the outer proof read the resulting Graph and lifecycle Events. Events (ActorStarted, ActorFinished) accessible via eventHistory.

### 2. CoreMsg lifecycle

Keep start, admitted output, and stop ordered on **CoreMsg / CoreMailboxBackend**.

1. [x] Start on the loop — validate the caller, call startActor synchronously, and reply with that result; do not wait for the Actor body. Bookkeeping (identities, live row, ActorStarted) is on the loop. ActorStarted uses in-loop persist/History, not PostAndAsyncReply to the same mailbox. Schedule of Actor body not yet implemented (§3).
2. [x] Admit live output — accept TestActor's Change only while its Authority and live row remain valid on the mailbox-owned table.
3. [x] Finish once — process `ActorStop ActorSucceeded` after earlier Actor output; drop the live row and secret (the mailbox uses pool drop); request terminate without waiting. The public identity stays on History.

### 3. CoreActorPool start

Use **CoreActorPool** as the live registry and start seam.

1. [x] Prepare Actor input — build Actor input Graph from client-provided graphIds (SiteMap Included context under Zoom, honoring Fold). Server does NOT expand from Zoom; client sends graphIds.
2. [x] Select TestActor — resolve the `test` Actor from the command Node. (Fixed: actor selection now via CSS class "actor-test" or fallback to text; TestActor interprets text for command only.)
3. [x] Establish lifecycle order — identities, live row, ActorStarted, and schedule run inside the synchronous startActor call so the live row and ActorStarted are observable before TestActor output can be admitted. ActorStarted uses in-loop persist/History; schedule is fire-and-forget of the body only. (Fixed: frozen Model bug; startActor now reads current mutable model via getModel().)
4. [x] Own the live table — mailbox-owned live table (no SynchronizedTable, no lock, no second registry copy in loop state). The mailbox is the only thread that reads or writes the table. `admit` / `drop` / `isLive` as the mailbox uses them.

### 4. History lifecycle

Record Actor lifecycle Events and successful Changes on **History**.

1. [x] Record ActorStarted — preserve the public Actor identity on History (process-lifetime for this cut).
2. [x] Record one ActorFinished — append exactly one successful terminal Event.
3. [x] Preserve Change-only Undo — do not make Actor lifecycle Events Undo targets.
4. [x] Mailbox owns one History sequence — successful PostChange and postGraphOnlyChange append ChangeEvent(s) for the accepted Change(s); ActorStarted and ActorFinished append Actor Events. All on mailboxHistory.past (process-lifetime until durability).

### 5. TestActor hello

Implement the `hello` behavior at the **ActorFn / TestActor input** seam.

1. [x] Interpret the command — select `hello` from the command Node.
2. [x] Post hello — submit one admitted `PostChange` that adds one Owned child text `hello` under Focus.
3. [x] Stop successfully — queue `ActorStop ActorSucceeded` after the Change.
4. [x] Keep assertions outside — return behavior only; do not assert inside TestActor.

### 6. CoreRuntime composition

Make TestActor available to the proof through **CoreRuntime** composition.

1. [x] Register TestActor — register the `test` Actor before the mailbox starts (register-then-start one host).

### 7. Outside proof

Verify Story path **Outside Core lifecycle proof** from outside the Actor.

1. [x] Enter at the named seam — start at CoreActorPool.startActor or at TestActor body input without Browser HTTP or a live external service. When at Pool: expand, select `test`, create, pass body input. When at Actor: TestActor interprets → `hello`.
2. [x] Exercise the full lifecycle — keep admit-before-`PostChange` and `ActorStop` on CoreMailbox and CoreMsg. There is no second Actor mailbox.
3. [x] Observe the Graph — find one Owned child text `hello` under Focus.
4. [x] Observe lifecycle order — find ActorStarted before output and exactly one ActorFinished after it.
5. [x] Observe cleanup — confirm the live row is gone and the secret no longer admits output. The public identity remains on History.
6. [x] Exclude transport encoding — do not require the HTTP Adapter universal response.

## See also

[[plan/core-creation/arch.md|Core creation architecture]], [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md|Core mailbox messages clear fast]]

## Comments

- 2026-09-14 — This ticket implements Story path 2 **Outside Core lifecycle proof**. Story path 1 **Browser Run hello** stays on [[35b-browser-run-hello.md|35b — Browser Run hello]].
- 2026-09-14 — Aligned to the 2026-09-14 arch correction: synchronous startActor (bookkeeping on the loop, body off-loop), mailbox-owned live table, register-then-start one host, `ActorStop ActorSucceeded` drop of live row and secret.

## Time

- 2026-09-14 20m — Align Story path 2 wording to current arch (from chat)
- 2026-09-14 25m — Implement §1 CoreMailbox door (startActor, actorStop, lifecycle facts)
- 2026-09-14 — §1.3 partial: Graph and `lockPresent` exposed via getState; lifecycle Events blocked on §4 History append (ActorStarted/Finished). Door functions startActor/actorStop remain in place.
- 2026-09-14 90m — Implement §2 CoreMsg lifecycle and §4 History append (ActorStarted/ActorFinished). dispatchStartActor appends ActorStarted via in-loop persist after pool.startActor; dispatchActorStop appends ActorFinished before pool.finish. History holds HistoryEvent (Change or Actor lifecycle) on one sequence. Actor authority placeholder until §3 selection. Events exist but not yet exposed (§1.3 remains partial).
- 2026-09-14 — Architectural fix: Actor lifecycle Events (ActorStarted/ActorFinished) now live in mailbox-owned in-memory History (Loop.mailboxHistory), not on the file/DB persist layer. PersistHandlers is six operations only (getState, getRevision, getChangesSince, postChange, postGraphOnlyChange, snapshotDone); no mapState. dispatchStartActor and dispatchActorStop append Events directly to the mailbox History ref. CoreMailboxBackend.runMsg merges mailbox History.past into the returned State.history so getState exposes lifecycle Events for tests. Actor Events are process-lifetime only and are not persisted to disk or database. Added tests proving ActorStarted and ActorFinished in history.past. Updated CoreMailbox.fs docs: lifecycle Events now accessible via getState (§1.3 door exposure complete). Clarified §2.1: schedule not yet implemented.
- 2026-09-14 — Alan lock fix: getState now returns Graph facts only per CONTEXT.md State = Graph data. Added separate CoreMailbox.eventHistory door to read Actor lifecycle Events from mailbox-owned History. CoreMailboxBackend.GetState no longer merges mailboxHistory into State.history. Updated CoreMailboxDoorTests to use eventHistory instead of getState for ActorStarted/ActorFinished assertions. Graph door ≠ Events door; State reserved for Graph.
- 2026-09-14 — Implement §3 CoreActorPool start: Created IncludedDescendantIds.expand (renamed from LoadedDescendantIds) for graph expansion honoring Fold state (not just residency); created TestActor module with actorFn that interprets command node text; updated ActorFn signature to accept ActorInput with named ids and secret; implemented CoreActorPool.startActor to use client-provided graphIds (not server expand), select actor from command node, append ActorStarted via callback, and schedule actor body as fire-and-forget; updated CoreMailboxBackend to pass getState, coreChanges, and appendActorStarted callback to pool.startActor; registered TestActor in CoreRuntime.create; added tests proving client-graphIds-driven subgraph, select/live-row-before-schedule order, and ActorStarted recording via eventHistory.
- 2026-09-14 — Alan lock: CoreActorPool.startActor now builds Actor input Graph from client-provided graphIds; server does NOT expand from Zoom or call IncludedDescendantIds. Client must walk SiteMap honoring Fold state (Included context per CONTEXT.md) and send graphIds. IncludedDescendantIds module renamed from LoadedDescendantIds to reflect Fold-based (not residency-based) walking; algorithm kept for future client use but notes it should walk SiteMap.expanded (Fold state) when Browser Command graphIds is implemented. Added validation: empty graphIds fails; commandId must be in provided graphIds. Updated test name to "uses client graphIds to build subgraph"; added tests for empty graphIds and missing commandId.
- 2026-09-14 — Implement §7 Outside proof: Added comprehensive test `34b section7 outside proof - full lifecycle via CoreMailbox` that consolidates all §7 requirements. Test enters at CoreMailbox.startActor (Pool seam), exercises full lifecycle via CoreMailbox/CoreMsg (no second mailbox), observes Graph (one Owned child text "hello"), observes order via eventHistory (ActorStarted before Change before ActorFinished, exactly one ActorFinished), observes cleanup (live row dropped, public Actor identity preserved on History), no HTTP Adapter. All §7 checkboxes proven.
- 2026-09-14 — Implement §5 TestActor hello: Extended CoreChanges with actorStop method; updated TestActor.run to call actorStop ActorSucceeded after successful PostChange; added CoreRuntime.readOnly wrapper for actorStop; created TestActorHelloTests.fs with five outside tests proving hello child under Focus, ActorFinished lifecycle event, dropped live row, ActorStarted before output order, and case-insensitive command interpretation. CoreRuntime already registers TestActor before mailbox starts (§6 verified). Tests assert outside TestActor module per spec. Uses CoreMailbox.eventHistory to read Actor lifecycle Events.
- 2026-09-14 — Refactor TestActor into generic dispatcher: Extracted hello behavior (interprets hello, posts Owned child, no actorStop or try/catch). Generic dispatcher owns exception handling, command dispatch, and always enqueues ActorStop after behavior. ActorFinished remains Actor-posted via dispatcher wrapper. TestActor.actorFn stays registered as "test" Actor.
