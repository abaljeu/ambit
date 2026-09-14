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
3. [ ] Expose lifecycle facts — let the outer proof read the resulting Graph and lifecycle Events.

### 2. CoreMsg lifecycle

Keep start, admitted output, and stop ordered on **CoreMsg / CoreMailboxBackend**.

1. [ ] Start on the loop — validate the caller, call startActor synchronously, and reply with that result; do not wait for the Actor body. Bookkeeping (identities, live row, ActorStarted, schedule) is on the loop. Schedule is fire-and-forget of the body only. ActorStarted uses in-loop persist/History, not PostAndAsyncReply to the same mailbox.
2. [ ] Admit live output — accept TestActor's Change only while its Authority and live row remain valid on the mailbox-owned table.
3. [ ] Finish once — process `ActorStop ActorSucceeded` after earlier Actor output; drop the live row and secret (the mailbox uses pool drop); request terminate without waiting. The public identity stays on History.

### 3. CoreActorPool start

Use **CoreActorPool** as the live registry and start seam.

1. [ ] Prepare Actor input — expand the requested Graph and pass the named ids and Actor secret to TestActor.
2. [ ] Select TestActor — resolve the `test` Actor from the command Node.
3. [ ] Establish lifecycle order — identities, live row, ActorStarted, and schedule run inside the synchronous startActor call so the live row and ActorStarted are observable before TestActor output can be admitted. ActorStarted uses in-loop persist/History; schedule is fire-and-forget of the body only.
4. [ ] Own the live table — mailbox-owned live table (no SynchronizedTable, no lock, no second registry copy in loop state). The mailbox is the only thread that reads or writes the table. `admit` / `drop` / `isLive` as the mailbox uses them.

### 4. History lifecycle

Record Actor lifecycle Events on **History** with Graph Actions.

1. [ ] Record ActorStarted — preserve the durable public Actor identity.
2. [ ] Record one ActorFinished — append exactly one successful terminal Event.
3. [ ] Preserve Change-only Undo — do not make Actor lifecycle Events Undo targets.

### 5. TestActor hello

Implement the `hello` behavior at the **ActorFn / TestActor input** seam.

1. [ ] Interpret the command — select `hello` from the command Node.
2. [ ] Post hello — submit one admitted `PostChange` that adds one Owned child text `hello` under Focus.
3. [ ] Stop successfully — queue `ActorStop ActorSucceeded` after the Change.
4. [ ] Keep assertions outside — return behavior only; do not assert inside TestActor.

### 6. CoreRuntime composition

Make TestActor available to the proof through **CoreRuntime** composition.

1. [ ] Register TestActor — register the `test` Actor before the mailbox starts (register-then-start one host).

### 7. Outside proof

Verify Story path **Outside Core lifecycle proof** from outside the Actor.

1. [ ] Enter at the named seam — start at CoreActorPool.startActor or at TestActor body input without Browser HTTP or a live external service. When at Pool: expand, select `test`, create, pass body input. When at Actor: TestActor interprets → `hello`.
2. [ ] Exercise the full lifecycle — keep admit-before-`PostChange` and `ActorStop` on CoreMailbox and CoreMsg. There is no second Actor mailbox.
3. [ ] Observe the Graph — find one Owned child text `hello` under Focus.
4. [ ] Observe lifecycle order — find ActorStarted before output and exactly one ActorFinished after it.
5. [ ] Observe cleanup — confirm the live row is gone and the secret no longer admits output. The public identity remains on History.
6. [ ] Exclude transport encoding — do not require the HTTP Adapter universal response.

## See also

[[plan/core-creation/arch.md|Core creation architecture]], [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md|Core mailbox messages clear fast]]

## Comments

- 2026-09-14 — This ticket implements Story path 2 **Outside Core lifecycle proof**. Story path 1 **Browser Run hello** stays on [[35b-browser-run-hello.md|35b — Browser Run hello]].
- 2026-09-14 — Aligned to the 2026-09-14 arch correction: synchronous startActor (bookkeeping on the loop, body off-loop), mailbox-owned live table, register-then-start one host, `ActorStop ActorSucceeded` drop of live row and secret.

## Time

- 2026-09-14 20m — Align Story path 2 wording to current arch (from chat)
- 2026-09-14 25m — Implement §1 CoreMailbox door (startActor, actorStop, lifecycle facts)
- 2026-09-14 — §1.3 partial: Graph and `lockPresent` exposed via getState; lifecycle Events blocked on §4 History append (ActorStarted/Finished). Door functions startActor/actorStop remain in place.
