# 30 — Reshape CoreActorPool to a synchronized table

**Status:** ready-for-agent
**Blocked by:** None — can start immediately.

## Context
CoreActorPool was designed as a mailbox queue. That is wrong. Issue 29 (Prove TestActor hello) needs a runner that is a thread pool plus a table of live Actors. The discarded hello implementation must not be restored. Registry/admission stay on the one Core mailbox (later, on 29); this ticket only fixes the pool.

## What to build
Preemptively strip the queue aspect of [[src/Server/Core/CoreActorPool.fs]]. The pool shall be used: it manages a thread pool and a table of live Actors. Manipulators of the table are synchronous and synchronized. The underlying thread pool and messages to Actors stay async. Work sent to an Actor follows normal expectations (ActorFn / ordinary messages — not a second CoreMsg pump).

- [ ] CoreActorPool has no MailboxProcessor and is not a mailbox queue.
- [ ] Live Actors live in a table; register, lookup, add-live, and drop-live are synchronous and serialized.
- [ ] Actor bodies still run on the underlying thread pool; messages to Actors stay ordinary.
- [ ] CoreRuntime.command still uses this pool (no wrap-patch of the old queue; no second mailbox).
- [ ] Existing CoreActorPool tests follow the synchronous table API.
- [ ] No TestActor hello, no new CoreMsg Actor cases, no Browser Run, no Actor definitions other than what tests already need.

## See also
[[02-core-actor-pool.md]], [[29-prove-testactor-hello.md]], [[Implementation Planning and Record.md]], [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]]

## Comments

- 2026-09-12 — Alan locked: strip the mailbox-queue design first as its own ticket; table mutators sync; pool + messages to Actors remain async.
