# 34b — Outside Core lifecycle proof

**Status:** needs-info
**Blocked by:** None — can start immediately.

## Context

Core already accepts credentialed Browser Changes through its one mailbox. The next slice must prove the Actor lifecycle without Browser HTTP or an Agent transport. The proof enters at CoreActorPool or TestActor, runs TestActor `hello`, and observes the Graph and lifecycle from outside the Actor.

This ticket follows Story path **Outside Core lifecycle proof** and Seam **Test seam for this tracer** in [[plan/core-creation/arch.md|Core creation architecture]]. The architecture Module map owns State, Interface, and Uses details.

## What to build

Prove one successful TestActor `hello` as a complete tracer through Core. The outer proof starts the Actor, observes ActorStarted before admitted output, observes one Owned child text `hello` under Focus, observes one ActorFinished, and confirms that the live row and secret are gone. TestActor produces output but does not assert. The proof does not require Browser HTTP, an Agent transport, or universal-response encoding.

### 1. CoreMailbox door

Use the **CoreMailbox** door when the proof exercises the full public Core lifecycle.

1. [ ] Start Actor — submit the architecture's StartActor request through CoreMailbox.
2. [ ] Carry Actor output — route credentialed Actor Change and ActorStop messages through the same mailbox.
3. [ ] Expose lifecycle facts — let the outer proof read the resulting Graph and lifecycle Events.

### 2. CoreMsg lifecycle

Keep start, admitted output, and stop ordered on **CoreMsg / CoreMailboxBackend**.

1. [ ] Start before output — hand the accepted start to CoreActorPool without holding the mailbox.
2. [ ] Admit live output — accept TestActor's Change only while its Authority and live row remain valid.
3. [ ] Finish once — process ActorSucceeded after earlier Actor output and revoke the live identity.

### 3. CoreActorPool start

Use **CoreActorPool** as the live registry and start seam.

1. [ ] Prepare Actor input — expand the requested Graph and pass the named ids and Actor secret to TestActor.
2. [ ] Select TestActor — resolve the `test` Actor from the command Node.
3. [ ] Establish lifecycle order — make the live row and ActorStarted observable before TestActor output can be admitted.
4. [ ] Drop live state — remove the live row when the terminal message runs.

### 4. History lifecycle

Record Actor lifecycle Events on **History** with Graph Actions.

1. [ ] Record ActorStarted — preserve the durable public Actor identity.
2. [ ] Record one ActorFinished — append exactly one successful terminal Event.
3. [ ] Preserve Change-only Undo — do not make Actor lifecycle Events Undo targets.

### 5. TestActor hello

Implement the `hello` behavior at the **ActorFn / TestActor input** seam.

1. [ ] Interpret the command — select `hello` from the command Node.
2. [ ] Post hello — submit one Change that adds one Owned child text `hello` under Focus.
3. [ ] Stop successfully — queue ActorSucceeded after the Change.
4. [ ] Keep assertions outside — return behavior only; do not assert inside TestActor.

### 6. CoreRuntime composition

Make TestActor available to the proof through **CoreRuntime** composition.

1. [ ] Register TestActor — register the `test` Actor before the start request.

### 7. Outside proof

Verify Story path **Outside Core lifecycle proof** from outside the Actor.

1. [ ] Enter at the named seam — start at CoreActorPool or TestActor body input without Browser HTTP or a live external service.
2. [ ] Exercise the full lifecycle — keep admitted Change and ActorStop on CoreMailbox and CoreMsg.
3. [ ] Observe the Graph — find one Owned child text `hello` under Focus.
4. [ ] Observe lifecycle order — find ActorStarted before output and exactly one ActorFinished after it.
5. [ ] Observe cleanup — confirm the live row is gone and the secret no longer admits output.
6. [ ] Exclude transport encoding — do not require the HTTP Adapter universal response.

## See also

[[plan/core-creation/arch.md|Core creation architecture]], [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md|Core mailbox messages clear fast]]
