# Spec review: [41 — Migrate Core mailbox, CoreMsg, and Pool onto Event](../issues/41-migrate-core-mailbox-coremsg-and-pool-onto-event.md) and [42 — Migrate PersistHandlers restore and getEventsSince](../issues/42-migrate-persisthandlers-restore-and-geteventssince.md)

Tree: staging tip `fce22cf7f5ead1db27d0b5635c610c8c54d432f0`. Spec only. Aid: [Core creation architecture](../arch.md) Story **Caller, persist, and Poll** Migrate. Later [43 — Migrate HTTP Adapter onto postEvent and Event Poll](../issues/43-migrate-http-adapter-onto-postevent-and-event-poll.md), [44 — Migrate Browser Poll, History, pending, and EventId cursor](../issues/44-migrate-browser-poll-history-pending-and-eventid.md), and [45 — Contract HistoryEvent, mailbox History, PendingKind, StartActorRequest, and ChangeLog](../issues/45-contract-historyevent-clienthistory-pendingkind-and-changelog.md) already landed; migrate-phase leftovers those tickets contracted are not 41/42 gaps. [46 — Mailbox History durability](../issues/46-mailbox-history-durability.md) three-way drop/apply is not a 42 gap.

## Spec

### [41 — Migrate Core mailbox, CoreMsg, and Pool onto Event](../issues/41-migrate-core-mailbox-coremsg-and-pool-onto-event.md)

#### (a) Missing or partial

None.
- "Core Changes callers use `postEvent`." [`CoreMailbox.postEvent`](src/Server/Core/CoreMailbox.fs) and [`CoreChanges.postEvents`](src/Server/Core/CoreChanges.fs) send `PostEvent`. Ev is completed at [`CoreEventDispatch.postEvent`](src/Server/Core/CoreEventDispatch.fs).
- "Dispatch `tryFind`s the target Ev, fills inverse Ops, and stores the completed Ev (same `submissionId`)." `completeAction` uses `EventLog.tryFind` and `Ev.inverseOps` and keeps `submissionId`.
- "`eventHistory` / `GetEventHistory` returns the EventLog (the log or `since`), not a two-stack." [`GetEventHistory`](src/Server/Core/CoreMailboxBackend.fs) returns mailbox `EventLog`; `eventsSince` is `EventLog.since`.
- "Mailbox appends ActorStart Ev records" / "stores an ActorStop Ev on EventLog for `ActorResult`." `dispatchStartActor` / `dispatchActorStop` call `CoreEventDispatch.actorStart` / `actorStop`. `postEvent` rejects those bodies. [`CoreActorPool`](src/Server/Core/CoreActorPool.fs) does not write EventLog.
- "every start request is ActorStart." `StartActor` doors take `ActorStart`. The StartActorRequest name is gone because 45 contracted it.
- "stamp `authority` from the admitted Caller on every stored Ev." `prepare` and `lifecycleEvent` stamp from the admitted Caller; wire authority is overwritten.

Compiled proof: [Issue41CoreMailboxTests.fs](tests/Server.Tests/Issue41CoreMailboxTests.fs). Command-builder Ev mint and leftover-name delete belong to later migrate/contract.

#### (b) Scope creep

None in Core mailbox, CoreMsg, or Pool that belongs to this ticket.

#### (c) Implemented but wrong

None.

**PASS**

### [42 — Migrate PersistHandlers restore and getEventsSince](../issues/42-migrate-persisthandlers-restore-and-geteventssince.md)

#### (a) Missing or partial

None.
- "persist ActorStart / ActorStop. Persistence is this same EventLog on file/DB." `actorStart` / `actorStop` call `PersistHandlers.appendEvent`. [FileAgent](src/Server/Core/FileAgent.fs) writes `gambol.events`; [DbAgent](src/Server/Core/DbAgent.fs) writes the `events` table.
- "File/Db call `EventLog.restore`. Restore merges persisted Events and dedupes by `submissionId`." Load uses `EventLog.restorePersisted`; live append uses `EventLog.restore`. [`EventLog.restore`](src/Shared/EventLog.fs) skips a known `submissionId`.
- "`getEventsSince` returns an Ev tail." PersistHandlers, CoreMailbox, and CoreChanges return `Ev list` from `EventLog.since`.

Compiled proof: [PersistHandlersRestoreTests.fs](tests/Server.Tests/PersistHandlersRestoreTests.fs) (in the Server.Tests fsproj). [Issue42PersistHandlersTests.fs](tests/Server.Tests/Issue42PersistHandlersTests.fs) duplicates that module and is not compiled. `EventLog.recover` three-way drop/apply is 46; not required here and not a fail.

#### (b) Scope creep

None for this ticket. Recover, Poll consume, and ChangeLog name drop belong to later tickets.

#### (c) Implemented but wrong

None. Lifecycle Events have no Ops and persist on `appendEvent`, not Graph `applyEvent`.

**PASS**

## Summary

Spec 41: 0 findings. Spec 42: 0 findings. Worst issue within each axis: none. Both tickets meet What-to-build on this tip.
