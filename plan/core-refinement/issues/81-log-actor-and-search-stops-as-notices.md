# 81 — Log Actor and search stops as Notices

**Status:** `defined`
**Type:** coding
**Blocked by:** [80 — Place Actor orders through PlaceOrder](80-place-actor-orders-through-placeorder.md)
**Trackers:** architecture

## Context

`RouteRegistration.searchDoor` calls `CoreMailboxApi.recordSearchStart` and `CoreMailboxApi.recordSearchStop`. `recordSearchStop` posts `CoreMsg.RecordSearchStop`, and `CoreMailboxLoad.runMsg` calls `CoreEventDispatch.actorStop`, which stores `EventBody.ActorStop` of the root id and `ActorSucceeded`. The `Order.Cancel` handler appends `EventBody.ActorStop` of the focus and `ActorCancelled`. `Notice` is in `src/Shared/History.fs`. `CoreMailboxLoad.applyNotice` handles each `Notice`, and `Database.writeNoticeRow` writes the graph node row a Notice changes. `appendEvent` writes each logged `Ev` to the `events` table. `ActorLive` reads the live focus set and the Actor result from `EventBody.ActorStart` and `EventBody.ActorStop`. `SyncLogic` sends a Notice body to `applyLoggedNotice`.

This ticket is decision 19 **One case each** in [Message shape](../cqrs-messages.md). `Notice.ActorStopped` of the focus id and the result is the one Actor stop: an Actor that is done posts it, and a Cancel posts it with `ActorCancelled`. The search stop is `Notice.SearchCancelled`.

## What to build

Do these steps in order. Finish each step before starting the next.

1. [ ] **Notice cases** — Add `Notice.SearchCancelled of rootId: NodeId`. `EventJson` writes and reads it. `MailboxLog.toEv` maps `MailboxEventBody.Notice` to History `EventBody.Notice`. `Database.writeNoticeRow` writes no graph node row for it. Its `applyNotice` arm changes no graph and returns `Ok ()`. Add `SearchCancelled keeps its fields through EventJson and MailboxLog`. Files: `src/Shared/History.fs` (where `Notice` is defined), `src/Shared/EventJson.fs`, `src/Shared/MailboxLog.fs`, `src/Server/Database.fs`, `src/Server/Core/CoreMailboxLoad.fs`, `tests/Shared.Tests/EventJsonTests.fs`, `tests/Shared.Tests/MailboxLogTests.fs`. Done when it compiles and `SearchCancelled keeps its fields through EventJson and MailboxLog` passes.
2. [ ] **Browser reads the stops** — `ActorLive` removes the root from the live set on `Notice.SearchCancelled`. `SyncLogic` applies `Notice.SearchCancelled` to the live focus set. Add acceptance test 4. Files: `src/Shared/ActorLive.fs`, `src/Shared/SyncLogic.fs`, `tests/Shared.Tests/ActorLiveTests.fs`. Done when `ActorLive reads SearchCancelled` passes.
3. [ ] **Cancel stop** — The `Order.Cancel` handler posts `Notice.ActorStopped` of the focus and `ActorCancelled`, and `cancelByFocus` replies `Ok ()`. The `Notice.ActorStopped` arm logs `EventBody.ActorStop` of the focus and `ActorCancelled`, and finishes the pool entry, the same as for an Actor that is done. A Cancel for an Actor run that is no longer active stays ignored and logs nothing. The tests that call `expectActorCancelled` keep reading `EventBody.ActorStop`. Add acceptance test 1. Files: `src/Server/Core/CoreMailboxActors.fs`, `tests/Server.Tests/CoreMsgActorCasesTests.fs`, `tests/Server.Tests/CancelByFocusTests.fs`, `tests/Server.Tests/AskCancelHarness.fs`. Done when `cancelByFocus posts Notice.ActorStopped` and `second Cancel for the same run logs one stop` pass.
4. [ ] **Search stop** — Add `Mailbox.SearchCancel of caller: Caller * rootId: NodeId * reply`. `recordSearchStop` posts it. Its arm in `CoreMailboxLoad.dispatchMailbox` checks caller authority as `PlaceOrder` does, logs `Notice.SearchCancelled` of the root id, and replies `Ok ()`. A caller without authority gets `Error`, and a log error is returned as `Error`. Remove `CoreMsg.RecordSearchStop` with its arms. Add acceptance tests 2 and 3. Files: `src/Server/Core/CoreMsg.fs` (where `Mailbox` and `CoreMsg` are defined), `src/Server/Core/CoreMailboxApi.fs`, `src/Server/Core/CoreMailboxLoad.fs`, `src/Server/Core/CoreMailboxEvents.fs`, `tests/Server.Tests/CoreMsgActorCasesTests.fs`, `tests/Server.Tests/SearchActorTests.fs`. Done when `recordSearchStop logs SearchCancelled` and `recordSearchStop without authority returns Error` pass, and no file under `src/` or `tests/` names `RecordSearchStop (`.
5. [ ] **Stored** — Each logged `Notice.SearchCancelled` has its row in the `events` table through `appendEvent` and changes no graph node row. Add acceptance test 5. Files: `tests/Server.Tests/DbAgentTests.fs`. Done when `SearchCancelled is stored in the events table` passes.

### Acceptance Criteria

1. [ ] **Cancel** — `cancelByFocus posts Notice.ActorStopped`. The Actor for that focus finishes. The log holds `EventBody.ActorStop` of the focus and `ActorCancelled`, and the reply is `Ok`. A second Cancel for that focus logs nothing.
2. [ ] **Search** — `recordSearchStop logs SearchCancelled`. The log holds the search start, then `Notice.SearchCancelled` of the root id.
3. [ ] **Search authority** — `recordSearchStop without authority returns Error`. The log gains no entry.
4. [ ] **Browser** — `ActorLive reads SearchCancelled`. `Notice.SearchCancelled` removes the root from the live set.
5. [ ] **Stored** — `SearchCancelled is stored in the events table`. Each `Notice.SearchCancelled` has an `events` row and reads back unchanged. No graph node row changes.
6. [ ] **Still working** — `upload, import, parse, Load, undo, and persist complete`.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [76 — Log Notices on the event log](76-log-notices-on-the-event-log.md)
- [79 — Place git Save as Order.Save](79-place-git-save-as-order-save.md)
- [80 — Place Actor orders through PlaceOrder](80-place-actor-orders-through-placeorder.md)
- [GLOSSARY](../../../GLOSSARY.md)
