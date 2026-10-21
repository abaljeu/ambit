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

`CoreMailboxActors.dispatchStartActor` starts the pool. `dispatchRecordSearchStart` appends `ActorStart` and does not run the walk. `RouteRegistration.searchDoor` calls `recordSearchStart` and `recordSearchStop`. `dispatchRecordSearchStop` stores `ActorStop` of the root id and `ActorSucceeded`. A client `PostEvent` of `EventBody.ActorStart` or `EventBody.Cancel` is handled in `dispatchPostEvent`. 60 — Add Mailbox beside CoreMsg moves `PeerActorName` onto Shared so `Order` can carry it.

## What to build

1. [ ] **StartActor** — `CoreMailboxApi.startActor` and `recordSearchStart` place `Order.StartActor` of `ActorStart`. For `startActor`, `CoreMailboxActors` keeps the pool start Context describes for `dispatchStartActor`. `recordSearchStart` keeps the behaviour Context describes for `dispatchRecordSearchStart`. The search walk stays in the search route, outside the mailbox.
2. [ ] **StartPeerActor** — Decision 13 **PeerActorName** in Message shape. `CoreMailboxApi.startPeerActor` places `Order.StartPeerActor` of `PeerActorName * ActorStart`. `GithubTransportActor` still passes `PeerActorName` `"github-load"` and `"github-save"`. Use the `PeerActorName` from Context. This ticket does not declare that type again.
3. [ ] **LoadSave** — `CoreMailboxApi.startLoadSaveCommand` places `Order.LoadSave` of `LoadSavePath * PeerActorName * LoadSaveCommandRequest`. `RouteRegistration` and `DeskLoadPush` still pass the path, the `PeerActorName`, and the request.
4. [ ] **Cancel** — `CoreMailboxApi.cancelByFocus` and a client `EventBody.Cancel` in `dispatchPostEvent` place `Order.Cancel` of the focus `NodeId`. The handler finishes that actor, as `dispatchCancelActor` does.
5. [ ] **ActorStop** — Add `Notice.ActorStopped of focusId: NodeId * result: ActorResult`. `CoreMailboxApi.actorStop` and `recordSearchStop` call `postNotice`. `recordSearchStop` posts the stop Context describes for `dispatchRecordSearchStop`. `dispatchActorStop` still finishes the pool entry. `postNotice` returns when the notice is queued. A caller that needs the appended `Ev` awaits `Ask` of `Query.LastEventId` after the post.
6. [ ] **Remove** — Remove the cases Context lists from `CoreMsg`. Each `PlaceOrder` replies `Result<EventId, string>`. These handlers do not call `admitCaller`.

### Acceptance Criteria

1. [ ] **Start** — `startActor calls pool.startActor`. The log contains the start. `recordSearchStart` does not call `pool.startActor`, and the log contains the start.
2. [ ] **Peer and load-save** — `startPeerActor places Order.StartPeerActor`. The name is the `PeerActorName` item 2 names. `startLoadSaveCommand` places `Order.LoadSave` with that name, the path, and the request.
3. [ ] **Cancel** — `cancelByFocus places Order.Cancel`. The actor for that focus id finishes. The reply is `Ok` of the new `EventId`.
4. [ ] **Stop** — `actorStop and recordSearchStop post Notice.ActorStopped`. The search stop is the stop Context describes. `CoreMsg` has none of the cases Context lists.
5. [ ] **Still working** — `upload, import, parse, Load, undo, and persist complete`.

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
