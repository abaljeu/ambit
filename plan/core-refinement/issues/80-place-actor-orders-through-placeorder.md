# 80 — Place Actor orders through PlaceOrder

**Status:** `defined`
**Type:** coding
**Blocked by:** [79 — Place git Save as Order.Save](79-place-git-save-as-order-save.md)
**Trackers:** architecture

## Context

`CoreMsg` carries these Actor cases:

- `StartActor`
- `RecordSearchStart`
- `StartPeerActor`
- `StartLoadSaveCommand`
- `CancelActor`
- `ActorStop`

`CoreMailboxApi.startActor`, `recordSearchStart`, `startPeerActor`, `startLoadSaveCommand`, `cancelByFocus`, and `actorStop` post those cases, and `CoreMailboxLoad.runMsg` sends each one to its handler: `CoreMailboxActors.dispatchStartActor`, `CoreEventDispatch.actorStart` for a search start, `CoreMailboxActors.dispatchStartPeerActor`, `CoreMailboxLoad.dispatchStartLoadSaveCommand`, `CoreMailboxActors.dispatchCancelActor`, and `CoreMailboxActors.dispatchActorStop`. `dispatchActorStop` finds the focus from the pool, appends `EventBody.ActorStop` of the focus and the result, and finishes the pool entry. `dispatchCancelActor` appends `EventBody.ActorStop` of the focus and `ActorCancelled` and finishes the pool entry; with no running Actor for the focus it appends nothing. A start that runs no Actor replies `Ok ()` and appends nothing. A client `PostEvent` of `EventBody.ActorStart` or `EventBody.Cancel` is handled in `dispatchPostEvent`. `Order` in `src/Shared/MailboxLog.fs` has `StartActor`, `StartPeerActor`, `LoadSave`, and `Cancel`.

This ticket is decision 19 **One case each** in [Message shape](../cqrs-messages.md). Each Actor case is an `Order` case placed through `Mailbox.PlaceOrder`. The mailbox assigns the `EventId` when the order reaches the head of the queue. Each handler is the dispatcher that case uses today.

## What to build

Do these steps in order. Finish each step before starting the next.

1. [ ] **Order cases** — Add `RecordSearchStart of ActorStart` to `Order`, and add `Notice.ActorStopped of focusId: NodeId * result: ActorResult` to `Notice`. `MailboxLog.ofEv` maps `EventBody.ActorStart` to `Order.StartActor`. `MailboxLog.toEv` maps `Order.StartActor` and `Order.RecordSearchStart` to `EventBody.ActorStart`. `EventJson` writes and reads `Notice.ActorStopped`. Add `Actor orders map through MailboxLog`. Files: `src/Shared/MailboxLog.fs` (where `Order` is defined), `src/Shared/History.fs` (where `Notice` is defined), `src/Shared/EventJson.fs`, `tests/Shared.Tests/MailboxLogTests.fs`. Done when it compiles and `MailboxLogTests` pass.
2. [ ] **Place and handle starts** — `startActor` places `Order.StartActor`, `recordSearchStart` places `Order.RecordSearchStart`, `startPeerActor` places `Order.StartPeerActor`, and `startLoadSaveCommand` places `Order.LoadSave`, each through `Mailbox.PlaceOrder`. A client `EventBody.ActorStart` in `dispatchPostEvent` calls the `Order.StartActor` handler on the mailbox thread. The `PlaceOrder` arm of `CoreMailboxLoad.dispatchMailbox` sends each order to the handler its `CoreMsg` case uses today and checks authority as `PlaceOrder` does today. Every accepted order appends an `Ev`. A start that runs no Actor appends `ActorStart` of the request, then `ActorStop` of its focus and `ActorSucceeded`. `PlaceOrder` replies `Ok` of the `EventId` of the order's first appended `Ev`, and each `CoreMailboxApi` function keeps its return type. Add acceptance tests 1, 2, 3 and 6. Files: `src/Server/Core/CoreMailboxApi.fs`, `src/Server/Core/CoreMailboxLoad.fs`, `src/Server/Core/CoreMailboxActors.fs`, `src/Server/Core/CoreEventDispatch.fs`, `tests/Server.Tests/CoreMsgActorCasesTests.fs`, `tests/Server.Tests/LoadSaveCommandTests.fs`. Done when `startActor places Order.StartActor`, `recordSearchStart places Order.RecordSearchStart`, `startPeerActor and startLoadSaveCommand place their orders` and `each accepted Actor order appends an Ev` pass.
3. [ ] **Place Cancel and post ActorStopped** — `cancelByFocus` places `Order.Cancel`, and a client `EventBody.Cancel` in `dispatchPostEvent` calls the `Order.Cancel` handler on the mailbox thread. The `Order.Cancel` handler appends `EventBody.ActorStop` of the focus and `ActorCancelled`, finishes the pool entry, and replies `Ok` of that `EventId`. A Cancel for an Actor run that is no longer active is ignored: it appends nothing and `cancelByFocus` replies `Ok ()`. `CoreMailboxApi.actorStop` and `CoreDoor.actorStop` take the Actor's focus id beside the result, and each Actor passes the focus it started on. When the Actor is done, `actorStop` posts `Notice.ActorStopped` of that focus id and the `ActorResult` with `postNotice`. The `Notice.ActorStopped` arm of `CoreMailboxLoad.applyNotice` appends one `Ev` of `EventBody.ActorStop` of the focus and the result, and finishes the pool entry for that focus. A focus with no running Actor gets `Error` of "no running actor", and the log gains no entry. Add acceptance tests 4 and 5. Files: `src/Server/Core/CoreMailboxApi.fs`, `src/Server/Core/CoreMailboxLoad.fs`, `src/Server/Core/CoreMailboxActors.fs`, `src/Server/Core/CoreDoor.fs`, `src/Server/GithubTransportActor.fs`, `src/Server/RunAgentActor.fs`, `src/Server/SearchActor.fs`, `src/Server/TestActor.fs`, `tests/Server.Tests/CoreMsgActorCasesTests.fs`, `tests/Server.Tests/CancelByFocusTests.fs`, `tests/Server.Tests/CoreMailboxDoorTests.fs`. Done when `cancelByFocus places Order.Cancel`, `second Cancel for the same run logs one stop` and `actorStop posts Notice.ActorStopped` pass.
4. [ ] **Remove** — Remove `StartActor`, `RecordSearchStart`, `StartPeerActor`, `StartLoadSaveCommand`, `CancelActor` and `ActorStop` from `CoreMsg`, with their arms. Files: `src/Server/Core/CoreMsg.fs` (where `CoreMsg` is defined), `src/Server/Core/CoreMailboxLoad.fs`, `src/Server/Core/CoreMailboxEvents.fs`, and each test that names those cases, such as `tests/Server.Tests/MailboxExpandTests.fs`. Done when it compiles, `CoreMsgActorCasesTests` pass, and no file under `src/` or `tests/` names `StartActor (`, `CancelActor (` or `ActorStop (` as a `CoreMsg` case.

### Acceptance Criteria

1. [ ] **Start** — `startActor places Order.StartActor`. The pool starts the Actor, and the log holds the `ActorStart` entry.
2. [ ] **Search start** — `recordSearchStart places Order.RecordSearchStart`. The start does not call `pool.startActor`. The log holds `ActorStart` of the request.
3. [ ] **Peer and load-save** — `startPeerActor and startLoadSaveCommand place their orders`. `Order.StartPeerActor` and `Order.LoadSave` carry the `PeerActorName` the caller passes.
4. [ ] **Cancel** — `cancelByFocus places Order.Cancel`. The Actor for that focus finishes, and the log holds `EventBody.ActorStop` of the focus and `ActorCancelled`. `second Cancel for the same run logs one stop`: a second Cancel for that focus replies `Ok ()`, and the log holds one cancel stop.
5. [ ] **Done** — `actorStop posts Notice.ActorStopped`. The log holds `EventBody.ActorStop` of the focus and the result, and the pool entry is finished.
6. [ ] **Every order logs** — `each accepted Actor order appends an Ev`. A start that runs no Actor logs its start and its stop.
7. [ ] **Still working** — `upload, import, parse, Load, undo, and persist complete`.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [60 — Add Mailbox beside CoreMsg](60-add-mailbox-beside-coremsg.md)
- [62 — Admit at the entry](62-admit-at-the-entry.md)
- [63 — Move queries to Ask](63-move-queries-to-ask.md)
- [79 — Place git Save as Order.Save](79-place-git-save-as-order-save.md)
- [81 — Log Actor and search stops as Notices](81-log-actor-and-search-stops-as-notices.md)
- [GLOSSARY](../../../GLOSSARY.md)
