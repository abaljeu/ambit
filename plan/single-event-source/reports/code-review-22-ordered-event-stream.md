# Code review — 22 ordered event stream

Range: uncommitted work vs `HEAD`. Spec: [22 — One ordered event stream](../issues/22-ordered-event-stream.md). Alan, 2026-10-08: typed match for an unregistered actor. Start and cancel stay on the existing mailbox handlers. `CoreEventDispatch.Context` is `{ admit; persist; eventLog }`.

## Standards

### (a) Violations

None. Bindings in the scan are under 40 lines. [CoreMailboxBackend.fs](../../../src/Server/Core/CoreMailboxBackend.fs) is 783 lines. [History.fs](../../../src/Shared/History.fs) is 800. No added line over 100 characters.

`commit` and `appendLifecycle` both call `appendNew`. The reply merge ors `externalChanges` and keeps a message from either reply. It conses events and reverses once.

### (b) Smells

None that still stand. The earlier string match for an unregistered actor is gone. `StartError.UnknownActor` is the match.

## Spec

### (a) Missing or partial

Contract item 3 is partial on the test. The routes return 404. No test names the deleted `runSubmitCommand` and `runSubmitCancel` functions. Those functions are absent from the tree.

### (b) Scope creep

None.

### (c) Implemented but wrong

None after the retry reply. A second post of the same unknown-actor ActorStart includes the ActorStop that was stored as the next event for that focus. The match is on the body case, not the message string.

The direct `startActor` door still returns an error for an unregistered actor and stores nothing. The events list is the path that stores ActorStart and the failed ActorStop.

### (d) Unnamed types

The ticket section Unnamed types names `MailboxHost.gate`, `StartError`, and `EventBody.Cancel`. `CoreEventDispatch.Context` has no added field. `PostedReply` in the backend is a private alias for the existing PostEvent reply channel.

## Summary

Standards: 0 hard. Spec: 1 partial (contract test does not name the deleted posters). No new `CoreEventDispatch` field.
