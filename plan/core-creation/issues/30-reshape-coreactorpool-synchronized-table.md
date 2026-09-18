# 30 — Reshape CoreActorPool to a synchronized table

**Status:** done
**Blocked by:** None — can start immediately.

## Context
CoreActorPool was designed as a mailbox queue. That is wrong. Issue 29 (Prove TestActor hello) needs a runner that is a thread pool plus a table of live Actors. The discarded hello implementation must not be restored. Registry/admission stay on the one Core mailbox (later, on 29); this ticket only fixes the pool.

## What to build
Preemptively strip the queue aspect of [[src/Server/Core/CoreActorPool.fs]]. The pool shall be used: it manages a thread pool and a table of live Actors. Manipulators of the table are synchronous and synchronized. The underlying thread pool and messages to Actors stay async. Work sent to an Actor follows normal expectations (ActorFn / ordinary messages — not a second CoreMsg pump).

- [x] CoreActorPool has no MailboxProcessor and is not a mailbox queue.
- [x] Live Actors live in a table; register, lookup, add-live, and drop-live are synchronous and serialized.
- [x] Actor bodies still run on the underlying thread pool; messages to Actors stay ordinary.
- [x] CoreRuntime.command still uses this pool (no wrap-patch of the old queue; no second mailbox).
- [x] Existing CoreActorPool tests follow the synchronous table API.
- [x] No TestActor hello, no new CoreMsg Actor cases, no Browser Run, no Actor definitions other than what tests already need.

## See also
[[02-core-actor-pool.md]], [[29-prove-testactor-hello.md]], [[Implementation Planning and Record.md]], [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]]

## Comments

- 2026-09-12 — Alan locked: strip the mailbox-queue design first as its own ticket; table mutators sync; pool + messages to Actors remain async.

## Time

**Actual:** 20m

- 2026-09-12 — 20m implementation and testing (cloud agent)
