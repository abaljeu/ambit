# Issue 41 — Core mailbox Event seam audit

Date: 2026-09-15. Ticket: [[../issues/41-migrate-core-mailbox-coremsg-and-pool-onto-event.md|41 — Migrate Core mailbox, CoreMsg, and Pool onto Event]]. Spec: [[../arch.md|Core creation architecture]] Story **Caller, persist, and Poll** migrate hop. Field shapes: [[../reports/event-abstraction.md|Event abstraction]]. Git: `scripts/gitstatus.sh` on branch **dev** (clean). Prerequisite: [[../issues/40-expand-postevent-eventlog-and-event-json.md|40 — Expand postEvent, EventLog store, and Event JSON persist]] is Status `coded`; [[../issues/41-migrate-core-mailbox-coremsg-and-pool-onto-event.md|41 — Migrate Core mailbox, CoreMsg, and Pool onto Event]] still lists blocked-by [[../issues/40-expand-postevent-eventlog-and-event-json.md|40 — Expand postEvent, EventLog store, and Event JSON persist]] until that lands on **dev**.

## 1. Ticket intent (what 41 must prove)

1. **postEvent callers and name-only Undo/Redo** — Core Changes paths on **CoreMailbox** and **CoreMsg / CoreMailboxBackend** use `postEvent`. Event is built at that door. Name-only Undo/Redo may carry only `target`; dispatch uses `EventLog.tryFind`, fills inverse Ops via `Event.inverseOps`, stores the completed Event (same `submissionId`).
2. **GetEventHistory** — `eventHistory` / `GetEventHistory` returns **EventLog** (full log or `since`), not the **History** two-stack (`past` / `future`).
3. **ActorStart and ActorStop on EventLog** — On the mailbox thread: append `EventBody.ActorStart` / `EventBody.ActorStop` to `eventLog`. Callers do not `postEvent` those bodies. Order for start: live row → ActorStart Event → `pool.schedule` ([[../arch.md|Core creation architecture]] Module **CoreActorPool** Interface: On the mailbox thread: live row → ActorStart Event on EventLog → pool.schedule).
4. **Authority stamp** — Core overwrites `Event.authority` from the admitted `Caller` on every stored Event; wire payload authority is ignored.
5. **Compatibility** — `PostChange`, `History`, `HistoryEvent`, `StartActorRequest`, and ChangeLog persist name stay compilable until [[../issues/45-contract-historyevent-clienthistory-pendingkind-and-changelog.md|45 — Contract HistoryEvent, mailbox History, PendingKind, StartActorRequest, and ChangeLog]]. HTTP Adapter, PersistHandlers Event restore, and Browser Poll stay on [[../issues/42-migrate-persisthandlers-restore-and-geteventssince.md|42 — Migrate PersistHandlers restore and getEventsSince]], [[../issues/43-migrate-http-adapter-onto-postevent-and-event-poll.md|43 — Migrate HTTP Adapter onto postEvent and Event Poll]], and [[../issues/44-migrate-browser-poll-history-pending-and-eventid.md|44 — Migrate Browser Poll, History, pending, and EventId cursor]].

## 2. Current implementation (gap vs destination)

### 2.1 Dual mailbox stores

**CoreMailboxBackend** `MailboxContext` holds both `eventHistory: History ref` and `eventLog: EventLog ref`. Only `dispatchPostEvent` writes `eventLog`. `dispatchPostChange`, `syncEventHistory`, `recordActorStarted`, and `recordActorFinished` write **History** only. The two stores are not unified.

### 2.2 postEvent door (expand from 40, incomplete for 41)

| Location | Today | 41 requirement |
| --- | --- | --- |
| `CoreMailbox.postEvent` | Admits Caller; posts `PostEvent` | Becomes the Changes path for migrated callers |
| `CoreMailboxBackend.dispatchPostEvent` | `EventLog.append` only; returns stored id | Also stamp authority; name-only Undo/Redo fill; tie to persist/Graph apply for Change bodies (see §4) |
| `CoreMailbox.postChange` / `PostChange` | `dispatchPostChange` → `persist.postChange` + `syncEventHistory` | Must route through postEvent semantics (Event built at door) while old msg may remain for compat |

`dispatchPostEvent` does not call `persist`, does not apply Graph, does not map `Gambol.Server.Authority` → `Gambol.Shared.Events.Authority`, and does not implement name-only Undo/Redo.

### 2.3 GetEventHistory

| Location | Today | 41 requirement |
| --- | --- | --- |
| `CoreMsg.GetEventHistory` | `AsyncReplyChannel<History>` | Reply with `EventLog` (or split: full log here, `EventsSince` for tail — ticket text allows “log or since”; `CoreMailbox.eventsSince` already exists) |
| `CoreMailbox.eventHistory` | `Async<History>` | `Async<EventLog>` (or equivalent) |
| `CoreMailboxBackend.runMsg` `GetEventHistory` | `syncEventHistory` then `eventHistory.Value` | Return `eventLog.Value` (and drop or stop relying on History sync for migrated facts) |
| `CoreMailboxBackend.replyFailure` `GetEventHistory` | `History.empty` | `EventLog.empty` |

### 2.4 Actor lifecycle

| Location | Today | 41 requirement |
| --- | --- | --- |
| `dispatchStartActor` | `pool.startActor` → `recordActorStarted` (History `ActorEvent`) → `schedule` | After `Ok secret`: append **ActorStart** Event to `eventLog` (with stamped authority, `commandName`, basis `EventId` from request), then `schedule` |
| `dispatchActorStop` | `recordActorFinished` (History) → `pool.finish` | Append **ActorStop** Event to `eventLog`, then drop live row via `finish` |
| `CoreActorPool.runStartActor` | Input `StartActorRequest` with `revision: Revision` | Still compiles; ActorStart body uses `revision: EventId` — mapping at mailbox when building Event |
| `CoreMsg.StartActor` / `CoreMailbox.startActor` | `StartActorRequest` | Keep name; payload fields align with `Gambol.Shared.Events.ActorStart` per [[../reports/event-abstraction.md|Event abstraction]] §6 — ActorStart replaces the name StartActorRequest |

### 2.5 Changes callers still on PostChange

Production and Core tests still use `PostChange`, not `postEvent`:

1. `CoreMailbox.coreChanges` → `postChange` → `PostChange` ([[src/Server/Core/CoreMailbox.fs]]).
2. `TestActor` hello path → `coreChanges.postChange` ([[tests/Server.Tests/TestActor.fs]]).
3. **CoreMsgActorCasesTests**, **CredentialedChangePostsTests**, **TestActorHelloTests**, **CoreMailboxDoorTests** (Graph-only and Change history assertions).

HTTP `Api.postChange` is out of scope for 41 ([[../issues/43-migrate-http-adapter-onto-postevent-and-event-poll.md|43]]); Core-layer migration is in scope.

## 3. Functions and types requiring change

### 3.1 [[src/Server/Core/CoreMsg.fs]]

1. **`GetEventHistory`** — Change reply type from `History` to `Gambol.Shared.Events.EventLog` (or add a parallel case; prefer one door per ticket).
2. **`PostChange`** — Keep for compat; implementation should delegate to postEvent pipeline or become a thin wrapper that builds `Event` and dispatches **`PostEvent`**.
3. **`StartActor`** — Optional: carry `ActorStart` or map `StartActorRequest` → `ActorStart` inside backend only (ticket: StartActorRequest name remains until 45).
4. **`PostEvent` / `EventsSince`** — Signatures already correct; behavior grows in backend.

### 3.2 [[src/Server/Core/CoreMailboxBackend.fs]]

1. **`MailboxContext`** — Stop treating `eventHistory` as the source of truth for migrated behavior; either mirror into EventLog during transition or read only `eventLog` for `GetEventHistory`.
2. **`recordActorStarted` / `recordActorFinished`** — Replace or supplement with **`appendActorStartEvent`** / **`appendActorStopEvent`** on `eventLog` (private helpers).
3. **`dispatchStartActor`** — Build `Event` with `EventBody.ActorStart`; stamp authority from `caller`; set `commandName` (from request or fixed lifecycle label); append before `schedule`.
4. **`dispatchActorStop`** — Build `EventBody.ActorStop(focusId, result)`; append to `eventLog`; then `pool.finish`.
5. **`dispatchPostEvent`** — Stamp `authority` from admitted `Caller`; handle name-only Undo/Redo; for `EventBody.Change`, run today’s persist/accept path (likely still `persist.postChange` with derived `Change list` until 42) and append the stored Event; preserve `submissionId` on Undo/Redo completion.
6. **`dispatchPostChange`** — Redirect to shared postEvent dispatch or duplicate logic removal.
7. **`syncEventHistory`** — Revisit: may remain for History compat only, or sync `eventLog` from persist via `EventLog.restore` when 42 lands; for 41, avoid duplicating Change facts only in History.
8. **`runMsg`** — `GetEventHistory` branch returns `eventLog`.
9. **`replyFailure`** — `GetEventHistory` → `EventLog.empty`.
10. **`operationContext`** — Unchanged names; logging still valid.

New private helpers likely needed (names illustrative): **`stampAuthority`**, **`completeNameOnlyUndoRedo`**, **`changeListToEvent`**, **`eventToChangeList`** (minimal, match existing batch semantics).

### 3.3 [[src/Server/Core/CoreMailbox.fs]]

1. **`eventHistory`** — Return type `Async<Gambol.Shared.Events.EventLog>`.
2. **`postChange`** — Build `Event`(s) from `Change list` + `commandName` (may need default or caller-supplied name) and call **`postEvent`**, or post **`PostEvent`** internally while keeping public signature for compat.
3. **`coreChanges.postChange` / `postGraphOnlyChange`** — Both use Event door semantics for EventLog append; graph-only skips file persistence only.
4. Doc comments — Update `eventHistory` and lifecycle lines to EventLog / ActorStart / ActorStop vocabulary.

### 3.4 [[src/Server/Core/CoreActorPool.fs]]

1. **`StartActorRequest`** — Keep type; document field correspondence to **`ActorStart`** (`revision` is `Revision` today, `EventId` in Event body).
2. **`runStartActor`** — No EventLog write (unchanged); mailbox owns ActorStart append.
3. **Tests fakes** — Return types unchanged; callers may assert on `ActorStart` Event in `eventLog` instead of History `ActorStarted`.

### 3.5 Shared Event modules (read-only for 41; no API delete)

1. **`Gambol.Shared.Events.Event`** — `inverseOps`, `tryFind` consumer in backend.
2. **`Gambol.Shared.Events.EventLog`** — `append`, `nextId`, `since`, `tryFind`, `restore`.
3. **`Gambol.Shared.Events.EventJson`** — Encode/decode for persisted Events (40); 41 does not move persist to Event JSON on File/Db ([[../issues/42-migrate-persisthandlers-restore-and-geteventssince.md|42]]).

### 3.6 Out of scope (must not break)

1. **`PersistHandlers`** in [[src/Server/Core/FileAgent.fs]], [[src/Server/Core/DbAgent.fs]] — Still `Change list` / `getChangesSince : Revision -> Change list` until 42.
2. **`Api.postChange`**, Poll, Client — [[../issues/43-migrate-http-adapter-onto-postevent-and-event-poll.md|43 — Migrate HTTP Adapter onto postEvent and Event Poll]] and [[../issues/44-migrate-browser-poll-history-pending-and-eventid.md|44 — Migrate Browser Poll, History, pending, and EventId cursor]].
3. **Delete** `History`, `HistoryEvent`, `StartActorRequest` — [[../issues/45-contract-historyevent-clienthistory-pendingkind-and-changelog.md|45 — Contract HistoryEvent, mailbox History, PendingKind, StartActorRequest, and ChangeLog]].

## 4. Compatibility constraints

1. **Two authority types** — `Gambol.Server.Authority` on `Caller` vs `Gambol.Shared.Events.Authority` on `Event`; stamping must convert explicitly (same string payload, distinct F# types).
2. **Revision vs EventId** — `StartActorRequest.revision` is `Revision`; `ActorStart.revision` is `EventId`. Mailbox must convert when appending ActorStart (likely `EventId` of basis log position at admit time).
3. **Change batch → single Event** — `postChange` accepts `Change list`; `postEvent` accepts one `Event`. Define one rule (e.g. one Event per batch with merged ops and `submissionId` from primary change, or one Event per `Change`) and keep `CoreChangesAccepted.changes` aligned with persist until HTTP migrates.
4. **Graph apply and revision** — Today persist owns Graph and `Revision`. EventLog append must not regress **CoreMsgActorCasesTests** persist/admit behavior. Until 42, Change Events likely still flow through `persist.postChange` while also appending to `eventLog`.
5. **postGraphOnlyChange** — Goes through same Event flow; skips file persistence only (not EventLog).
6. **History two-stack** — Type and `GetEventHistory`→`History` path may remain compiled but should not be the returned history door after 41; tests that read `history.past` need EventLog-shaped assertions (see §6).
7. **Dual store during migrate** — If History ref stays for compat, avoid divergent Actor/Change sequences between `eventHistory` and `eventLog`; prefer single writer logic appending EventLog and optionally mirroring to History until 45.

## 5. Focused tests needed (Server.Tests; do not duplicate Shared.Tests Event coverage)

Narrowest seam per [[../arch.md|Core creation architecture]] §1: **CoreMailbox `postEvent`** and **EventLog `since`**, extended for 41 behaviors.

### 5.1 [[tests/Server.Tests/CoreMailboxDoorTests.fs]]

1. **postEvent authority stamp** — Posted Event with wrong/missing authority stores admitted Caller authority.
2. **postEvent Change via migrated door** — Successful credentialed post appends Change Event to `eventLog`; `eventsSince` sees it; persist revision still advances (bridge test until 42).
3. **name-only Undo/Redo** — Post Undo/Redo with only `target`; stored Event has filled ops; same `submissionId`.
4. **GetEventHistory returns EventLog** — Replace or add facts on `eventLog.events` instead of `history.past` / `ChangeEvent` / `ActorEvent`.
5. **Actor lifecycle on EventLog** — Start + stop (or hello path) yields `ActorStart` and `ActorStop` bodies on same sequence as Change Events; no duplicate History-only assertions for new behavior.
6. **ActorStart ordering** — Live row exists before schedule; ActorStart Event id ordering before body runs (mailbox-thread sequencing).

### 5.2 [[tests/Server.Tests/CoreMsgActorCasesTests.fs]]

1. **StartActor** — Pool still receives `StartActorRequest`; add assertion that mailbox `eventLog` contains matching **`ActorStart`** body (fields + basis `EventId`).
2. **Actor PostChange** — After migrate, Actor change reaches persist and **`eventLog`** (not only History sync).

### 5.3 [[tests/Server.Tests/TestActorHelloTests.fs]]

1. Replace **`waitForActorFinished`** / `ActorFinished` History checks with **`ActorStop`** on `eventLog` (or `eventsSince` poll).
2. Full hello lifecycle: one ActorStart, Change Event(s) from Actor postChange, one ActorStop; live row drop unchanged.

### 5.4 [[tests/Server.Tests/CredentialedChangePostsTests.fs]] and [[tests/Server.Tests/ActorCoreChangesDoorTests.fs]]

1. Re-run admit/refuse matrix after `postChange` → internal `postEvent` routing; revision and persist errors unchanged.

### 5.5 Existing 40 tests to keep green

1. **`CoreMailbox.postEvent appends an Event that eventsSince returns`**
2. **`CoreMailbox.postEvent without admitted Caller is refused`**

## 6. Likely compile impacts

1. **`CoreMailbox.eventHistory`** return type `History` → `EventLog` breaks all callers of `.past` / `.future` / `.nextId` on the result (CoreMailboxDoorTests, TestActorHelloTests, any production caller of `eventHistory` — grep shows tests only today).
2. **`CoreMsg.GetEventHistory`** channel type change forces **CoreMailboxBackend** and any internal match arms; no public `CoreMsg` export (test **`CoreMsg is not a public type`** stays valid).
3. **`CoreMailbox.postChange` implementation** — If it calls `postEvent`, may need `commandName` parameter or default string on `Event` record (field required on `Event` today).
4. **Server.Tests** — Widespread test updates for history shape; production **Api** unchanged in 41.
5. **No Client / Shared fsproj change required** unless adding shared helpers; prefer backend-private conversion to limit blast radius.
6. **Fsproj** — No new files required if logic stays in CoreMailboxBackend; optional small Shared helper only if duplication is large.

## 7. Dependency and sequencing notes

1. Land **40** on **dev** before 41 if ticket blocked-by is enforced (Event JSON + `postEvent`/`eventLog` ref already present in tree; persist Event file may still be ChangeLog-only per 42).
2. Implement **backend dispatch** first (authority, Undo/Redo, ActorStart/Stop append), then **wire postChange → postEvent**, then **GetEventHistory** return type, then **test migrations**.
3. **42** before expecting restart restore of EventLog from disk; **41** tests can use in-memory mailbox only.
4. **45** deletes History door and `StartActorRequest` rename; do not delete in 41.

## 8. Summary risk table

| Risk | Mitigation |
| --- | --- |
| postEvent without persist bridge | Keep calling `persist.postChange` for Change bodies until 42; append Event in same dispatch |
| Dual History / EventLog divergence | Single append path; History mirror optional and tested |
| Revision / EventId mismatch on start | Document conversion at ActorStart append |
| Batch Change → Event shape | Decide and test one batch rule early in CoreMailboxBackend |
| Large test churn on `eventHistory` | Update CoreMailboxDoorTests and TestActorHelloTests in same PR as return type change |
