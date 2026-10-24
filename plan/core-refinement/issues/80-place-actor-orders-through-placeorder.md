# 80 — Place Actor orders through PlaceOrder

**Status:** `defined`
**Type:** coding
**Blocked by:** [79 — Place git Save as Order.Save](79-place-git-save-as-order-save.md)
**Trackers:** architecture

> Step 3 (Cancel and ActorStopped) dropped by Alan (2026-10-23); 81 carries Cancel through Notice.ActorStopped and removes CancelActor and ActorStop from CoreMsg.

## Context

`CoreMsg` carries these Actor cases:

- `StartActor`
- `RecordSearchStart`
- `StartPeerActor`
- `StartLoadSaveCommand`
- `CancelActor`
- `ActorStop`

`CoreMailboxApi.startActor`, `recordSearchStart`, `startPeerActor`, `startLoadSaveCommand`, `cancelByFocus`, and `actorStop` post those cases, and `CoreMailboxLoad.runMsg` sends each one to its handler: `CoreMailboxActors.dispatchStartActor`, `CoreEventDispatch.actorStart` for a search start, `CoreMailboxActors.dispatchStartPeerActor`, `CoreMailboxLoad.dispatchStartLoadSaveCommand`, `CoreMailboxActors.dispatchCancelActor`, and `CoreMailboxActors.dispatchActorStop`. `dispatchActorStop` finds the focus from the pool, appends `EventBody.ActorStop` of the focus and the result, and finishes the pool entry. `dispatchCancelActor` appends `EventBody.ActorStop` of the focus and `ActorCancelled` and finishes the pool entry; with no running Actor for the focus it appends nothing. A start that runs no Actor replies `Ok ()` and appends nothing. A client `PostEvent` of `EventBody.ActorStart` or `EventBody.Cancel` is handled in `dispatchPostEvent`. `Order` in `src/Shared/MailboxLog.fs` has `StartActor`, `StartPeerActor`, `LoadSave`, and `Cancel`.

This ticket is decision 19 **One case each** in [Message shape](../cqrs-messages.md). Each Actor start case is an `Order` case placed through `Mailbox.PlaceOrder`. `CancelActor` and `ActorStop` stay on `CoreMsg` in this ticket; [81](81-log-actor-and-search-stops-as-notices.md) moves them. The mailbox assigns the `EventId` when the order reaches the head of the queue. Each handler is the dispatcher that case uses today.

## What to build

Do these steps in order. Finish each step before starting the next.

1. [ ] **Order cases** — Add `RecordSearchStart of ActorStart` to `Order`. `MailboxLog.ofEv` maps `EventBody.ActorStart` to `Order.StartActor`. `MailboxLog.toEv` maps `Order.StartActor` and `Order.RecordSearchStart` to `EventBody.ActorStart`. Add `Actor orders map through MailboxLog`. Files: `src/Shared/MailboxLog.fs` (where `Order` is defined), `tests/Shared.Tests/MailboxLogTests.fs`. Done when it compiles and `MailboxLogTests` pass.
2. [ ] **Place and handle starts** — `startActor` places `Order.StartActor`, `recordSearchStart` places `Order.RecordSearchStart`, `startPeerActor` places `Order.StartPeerActor`, and `startLoadSaveCommand` places `Order.LoadSave`, each through `Mailbox.PlaceOrder`. A client `EventBody.ActorStart` in `dispatchPostEvent` calls the `Order.StartActor` handler on the mailbox thread. The `PlaceOrder` arm of `CoreMailboxLoad.dispatchMailbox` sends each order to the handler its `CoreMsg` case uses today and checks authority as `PlaceOrder` does today. Every accepted order appends an `Ev`. A start that runs no Actor appends `ActorStart` of the request, then `ActorStop` of its focus and `ActorSucceeded`. `PlaceOrder` replies `Ok` of the `EventId` of the order's first appended `Ev`, and each `CoreMailboxApi` function keeps its return type. Add acceptance tests 1, 2, 3 and 4. Files: `src/Server/Core/CoreMailboxApi.fs`, `src/Server/Core/CoreMailboxLoad.fs`, `src/Server/Core/CoreMailboxActors.fs`, `src/Server/Core/CoreEventDispatch.fs`, `tests/Server.Tests/CoreMsgActorCasesTests.fs`, `tests/Server.Tests/LoadSaveCommandTests.fs`. Done when `startActor places Order.StartActor`, `recordSearchStart places Order.RecordSearchStart`, `startPeerActor and startLoadSaveCommand place their orders` and `each accepted Actor order appends an Ev` pass.
3. [ ] **Remove** — Remove `StartActor`, `RecordSearchStart`, `StartPeerActor` and `StartLoadSaveCommand` from `CoreMsg`, with their arms. `CancelActor` and `ActorStop` stay, because `cancelByFocus` and `actorStop` still post them. Files: `src/Server/Core/CoreMsg.fs` (where `CoreMsg` is defined), `src/Server/Core/CoreMailboxLoad.fs`, `src/Server/Core/CoreMailboxEvents.fs`, and each test that names those cases, such as `tests/Server.Tests/MailboxExpandTests.fs`. Done when it compiles, `CoreMsgActorCasesTests` pass, and no file under `src/` or `tests/` names `StartActor (`, `RecordSearchStart (`, `StartPeerActor (` or `StartLoadSaveCommand (` as a `CoreMsg` case.

### Acceptance Criteria

1. [ ] **Start** — `startActor places Order.StartActor`. The pool starts the Actor, and the log holds the `ActorStart` entry.
2. [ ] **Search start** — `recordSearchStart places Order.RecordSearchStart`. The start does not call `pool.startActor`. The log holds `ActorStart` of the request.
3. [ ] **Peer and load-save** — `startPeerActor and startLoadSaveCommand place their orders`. `Order.StartPeerActor` and `Order.LoadSave` carry the `PeerActorName` the caller passes.
4. [ ] **Every order logs** — `each accepted Actor order appends an Ev`. A start that runs no Actor logs its start and its stop.
5. [ ] **Still working** — `upload, import, parse, Load, undo, and persist complete`.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [60 — Add Mailbox beside CoreMsg](60-add-mailbox-beside-coremsg.md)
- [62 — Admit at the entry](62-admit-at-the-entry.md)
- [63 — Move queries to Ask](63-move-queries-to-ask.md)
- [79 — Place git Save as Order.Save](79-place-git-save-as-order-save.md)
- [81 — Log Actor and search stops as Notices](81-log-actor-and-search-stops-as-notices.md)
- [GLOSSARY](../../../GLOSSARY.md)
