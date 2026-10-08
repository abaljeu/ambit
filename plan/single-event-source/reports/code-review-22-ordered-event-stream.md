# Code review — 22 ordered event stream

Range: uncommitted work vs `HEAD` (`git diff HEAD`). Spec: [22 — One ordered event stream](../issues/22-ordered-event-stream.md). Alan, 2026-10-08: push the list serially onto the queue; an unregistered actor is not a rejection.

## Standards

### (a) Violations

None.

The earlier list-merge used `@` inside the per-reply fold. The merge now conses each reply's events and reverses once.

### (b) Smells

1. **Primitive Obsession** — an unregistered start is still recognized from the `startActor` error string (`actor '…' not registered`). The pool does not return a typed error. Peer-actor text does not match this predicate.

## Spec

### (a) Missing or partial

None. `postEvents` pushes each `PostEvent` under one gate, then merges the replies. Client ActorStart and Cancel run on that path. The Actor body stays off the mailbox. One submission scan. An edit then an unknown ActorStart returns HTTP success and stores ActorStart plus ActorStop `unknown actor`.

### (b) Scope creep

None. `CoreMsg.PostEvents` and `CorePostedList` are deleted. No new `CoreMsg` case.

### (c) Implemented but wrong

None against Alan's decision. A launch error other than an unregistered actor still returns that error, and earlier events in the list stay stored. [http-contract.md](../../../doc/current/http-contract.md) names that remaining case.

### (d) Unnamed types

`CoreMsg.PostEvents` is only deleted. `CorePostedList.Host` is only deleted. `MailboxHost` gains a private `gate`. `CoreEventDispatch.Context` gains `pool`, `changes`, and `isReady`. The spec did not name those fields.

## Summary

Standards: 0 hard, 1 judgement (string match for an unregistered actor). Spec: 0 partial, 0 wrong. Three implementation fields the spec did not name.
