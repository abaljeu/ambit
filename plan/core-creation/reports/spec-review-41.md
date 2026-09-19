# Spec review: 41 — Migrate Core mailbox, CoreMsg, and Pool onto Event

The review uses tree SHA `fce22cf7f5ead1db27d0b5635c610c8c54d432f0` against [41 — Migrate Core mailbox, CoreMsg, and Pool onto Event](plan/core-creation/issues/41-migrate-core-mailbox-coremsg-and-pool-onto-event.md) What-to-build and [Core creation architecture](plan/core-creation/arch.md) Story **Caller, persist, and Poll** Migrate item 2 **Changes callers**, item 4 **GetEventHistory**, item 9 **mailbox appends ActorStart / ActorStop**, item 12 **every start request is ActorStart**, and item 13 **stamp authority**. Persist restore and HTTP/Browser migrate are out of scope.

## (a) Missing or partial

None.
- "Core Changes callers use `postEvent`." [`CoreMailbox.postEvent`](src/Server/Core/CoreMailbox.fs) and [`CoreChanges.postEvents`](src/Server/Core/CoreChanges.fs) send `PostEvent`. [`TestActor`](src/Server/TestActor.fs) posts Change Events on that door.
- "Dispatch `tryFind`s the target Ev, fills inverse Ops, and stores the completed Ev (same `submissionId`)." [`CoreEventDispatch.completeAction`](src/Server/Core/CoreEventDispatch.fs) uses `EventLog.tryFind` and `Ev.inverseOps` and keeps `submissionId`.
- "`eventHistory` / `GetEventHistory` returns the EventLog (the log or `since`), not a two-stack." [`GetEventHistory`](src/Server/Core/CoreMailboxBackend.fs) returns mailbox `EventLog`; `eventsSince` is `EventLog.since`.
- "Callers do not `postEvent` those bodies." Mailbox `dispatchStartActor` / `dispatchActorStop` append ActorStart / ActorStop through `CoreEventDispatch`. `postEvent` rejects those bodies. [`CoreActorPool`](src/Server/Core/CoreActorPool.fs) does not write EventLog.
- "every start request is ActorStart." `StartActor` doors take `ActorStart`.
- "stamp `authority` from the admitted Caller on every stored Ev." `prepare` and `lifecycleEvent` stamp from the admitted Caller.

Checklist tests: [Issue41CoreMailboxTests.fs](tests/Server.Tests/Issue41CoreMailboxTests.fs). Lifecycle append also in [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) and [TestActorHelloTests.fs](tests/Server.Tests/TestActorHelloTests.fs). Full CI was not run.

## (b) Scope creep

None in Core mailbox, CoreMsg, or Pool that belongs to this ticket. Later HTTP Adapter, Browser, persist, and contract files are ignored.

## (c) Implemented but wrong

None. Name-only Undo/Redo is empty Ops; dispatch fills inverse Ops. Ev mint at command builders is later migrate; the `postEvent` door still completes EventId, Authority, and inverse Ops.

**PASS** (all What-to-build outcomes present)
