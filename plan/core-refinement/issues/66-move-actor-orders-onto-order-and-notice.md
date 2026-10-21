# 66 — Move Actor orders onto Order and Notice

**Status:** `defined`
**Type:** coding
**Blocked by:** 60 — Add Mailbox beside CoreMsg; 63 — Move queries to Ask; 62 — Admit at the entry
**Trackers:** architecture

## Context

`CoreMsg` carries these cases:

- `StartActor`
- `RecordSearchStart`
- `StartPeerActor`
- `StartLoadSaveCommand`
- `CancelActor`
- `ActorStop`
- `RecordSearchStop`

`CoreMailboxActors.dispatchStartActor` starts the pool. `dispatchRecordSearchStart` appends `ActorStart` and does not run the walk. `RouteRegistration.searchDoor` calls `recordSearchStart` and `recordSearchStop`. `dispatchRecordSearchStop` stores `ActorStop` of the root id and `ActorSucceeded`. `dispatchActorStop` and `dispatchCancelActor` reply `Ok ()` and append nothing when no Actor runs for the caller or the focus. A start that runs no Actor replies `Ok ()` and appends nothing. A client `PostEvent` of `EventBody.ActorStart` or `EventBody.Cancel` is handled in `dispatchPostEvent`. 60 — Add Mailbox beside CoreMsg moves `PeerActorName` onto Shared so `Order` can carry it.

## What to build

Decision 19 **One case each** in [Message shape](../cqrs-messages.md). Each case Context lists moves to its own case. `ActorStop` and `RecordSearchStop` become `Notice` cases. The others become `Order` cases. Each handler is the dispatcher that case uses today.

1. [ ] **Order cases** — `Order` in `src/Shared/MailboxLog.fs` gains `RecordSearchStart of ActorStart`. `MailboxLog.ofEv` and `MailboxLog.toEv` map `EventBody.ActorStart` and `EventBody.ActorStop` both ways.
2. [ ] **Place** — Each `CoreMailboxApi` function places its order through `Mailbox.PlaceOrder`:
   - `startActor` places `Order.StartActor`.
   - `recordSearchStart` places `Order.RecordSearchStart`.
   - `startPeerActor` places `Order.StartPeerActor`.
   - `startLoadSaveCommand` places `Order.LoadSave`.
   - `cancelByFocus` places `Order.Cancel`.
   - A client `EventBody.ActorStart` or `EventBody.Cancel` in `dispatchPostEvent` places `Order.StartActor` or `Order.Cancel`.
3. [ ] **Handle** — The `PlaceOrder` arm of `CoreMailboxLoad.dispatchMailbox` sends each order to the dispatcher Context names for its case. The handlers check authority as `PlaceOrder` does today.
4. [ ] **Stops** — Add `Notice.ActorStopped of focusId: NodeId * result: ActorResult` and `Notice.SearchStopped of rootId: NodeId`. `CoreMailboxApi.actorStop` posts `Notice.ActorStopped` with `postNotice`, and `recordSearchStop` posts `Notice.SearchStopped`. The logged notice is the stop entry. The `ActorStopped` handler finishes the pool entry, as `dispatchActorStop` does. `Order.Cancel` is the cancel request. Its handler appends `Notice.ActorStopped` of the focus and `ActorCancelled` as its stop entry and finishes the pool entry, as `dispatchCancelActor` does. The browser reads an Actor result from `Notice.ActorStopped` and a search stop from `Notice.SearchStopped`, where it reads `EventBody.ActorStop` today.
5. [ ] **Log** — Every accepted order appends an `Ev`. Where today's dispatcher appends one, that `Ev` is the order's entry. A start that runs no Actor appends `ActorStart` of the request, then `ActorStop` of its focus and `ActorSucceeded`. A cancel that finds no running Actor replies `Error` of "no running actor".
6. [ ] **Reply** — `PlaceOrder` replies `Ok` of the `EventId` of the order's first appended `Ev`. Each `CoreMailboxApi` function keeps its current return type.
7. [ ] **Remove** — Remove the cases Context lists from `CoreMsg`.

### Acceptance Criteria

1. [ ] **Start** — `startActor places Order.StartActor`. The pool starts the Actor, and the reply is `Ok` of the `ActorStart` entry's `EventId`.
2. [ ] **Search** — `recordSearchStart places its order and recordSearchStop posts SearchStopped`. The start does not call `pool.startActor`. The log holds the start, then `Notice.SearchStopped` of the root id.
3. [ ] **Peer and load-save** — `startPeerActor and startLoadSaveCommand place their orders`. `Order.StartPeerActor` and `Order.LoadSave` carry the `PeerActorName` the caller passes.
4. [ ] **Cancel** — `cancelByFocus places Order.Cancel`. The Actor for that focus finishes. The log holds `Notice.ActorStopped` of the focus and `ActorCancelled`. The reply is `Ok` of that entry's `EventId`.
5. [ ] **Stop** — `actorStop posts Notice.ActorStopped`. The log holds `Notice.ActorStopped` of the focus and the result, and the pool entry is finished.
6. [ ] **Every order logs** — `each accepted Actor order appends an Ev`. A start that runs no Actor logs its start and its stop.
7. [ ] **Still working** — `upload, import, parse, Load, undo, and persist complete`.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [60 — Add Mailbox beside CoreMsg](60-add-mailbox-beside-coremsg.md)
- [62 — Admit at the entry](62-admit-at-the-entry.md)
- [63 — Move queries to Ask](63-move-queries-to-ask.md)
- [64 — Merge PostEvent and PostGraphOnly into Change](64-merge-postevent-and-postgraphonly-into-change.md)
- [GLOSSARY](../../../GLOSSARY.md)
