# Issue 42 — PersistHandlers restore and getEventsSince seam map

Date: 2026-09-15. Ticket: [[../issues/42-migrate-persisthandlers-restore-and-geteventssince.md|42 — Migrate PersistHandlers restore and getEventsSince]]. Spec: [[../arch.md|Core creation architecture]] Story **Caller, persist, and Poll** persist migrate hop. Field shapes: [[event-abstraction.md|Event abstraction]]. Prerequisites Status `coded`: [[../issues/40-expand-postevent-eventlog-and-event-json.md|40 — Expand postEvent, EventLog store, and Event JSON persist]], [[../issues/41-migrate-core-mailbox-coremsg-and-pool-onto-event.md|41 — Migrate Core mailbox, CoreMsg, and Pool onto Event]]. HTTP Poll stays Change-shaped until [[../issues/43-migrate-http-adapter-onto-postevent-and-event-poll.md|43 — Migrate HTTP Adapter onto postEvent and Event Poll]].

## 1. PersistHandlers type fields and callers

### 1.1 Type fields

[[src/Server/Core/CoreMsg.fs]] defines `PersistHandlers` as:

1. **getState** — `unit -> Result<State, string>`
2. **getRevision** — `unit -> Result<Revision, string>`
3. **getChangesSince** — `Revision -> Result<Change list, string>` (Poll/Load Change tail today)
4. **postChange** — `Change list -> Result<CoreChangesAccepted, string>` (document/graph persist + ChangeLog / DB `changes` row)
5. **postGraphOnlyChange** — `Change list -> Result<CoreChangesAccepted, string>` (skips EventLog by arch)
6. **snapshotDone** — `Graph option -> unit` (Db projection handshake)

There is no `getEventsSince`, `postEvent`, or Event-shaped field on PersistHandlers today. [[../arch.md|Core creation architecture]] Module **PersistHandlers** Interface item **getEventsSince / persist EventLog** is still unchecked.

### 1.2 Callers by module

1. **CoreMsg** — Owns the type. `GetChangesSince` reply is still `Change list`. `PostEvent` / `EventsSince` are mailbox cases, not PersistHandlers fields. `EventsSince` replies with in-memory `EventLog` only.
2. **CoreMailbox** — [[src/Server/Core/CoreMailbox.fs]]. `host` takes `PersistFilling.handlers` into `CoreMailboxBackend.makeMailBox`. Public `getChangesSince` unwraps `GetChangesSince`. `coreChanges.getChangesSince` is that door. `eventsSince` posts `EventsSince` (mailbox `eventLog`, not persist). `postChange` loops one `PostEvent` per Change; graph apply still ends in `persist.postChange` via **CoreEventDispatch**.
3. **CoreMailboxBackend** — [[src/Server/Core/CoreMailboxBackend.fs]]. `MailboxContext.persist` is the seam. `GetState` / `GetRevision` / `GetChangesSince` / `SnapshotDone` call handlers directly. `PostGraphOnlyChange` → `postGraphOnlyChange`. `PostEvent` → **CoreEventDispatch.postEvent** (which may call `persist.postChange`). `StartActor` / `ActorStop` append lifecycle Events in memory only (see §4). Startup `failedPersist` keeps `getState` / `getRevision` / `getChangesSince` and closes `postChange` / `postGraphOnlyChange`.
4. **CoreEventDispatch** — [[src/Server/Core/CoreEventDispatch.fs]]. `Context.persist` used only when `Event.ops` is `Some`: builds a `Change` from Ops + `submissionId` and calls `persist.postChange [ change ]`. ActorStart / ActorStop bodies have `ops = None`, so they never reach PersistHandlers here.
5. **FileAgent** — [[src/Server/Core/FileAgent.fs]]. Builds the six-field handlers over in-memory `State ref` + `SYSTEM/gambol.log` via [[src/Server/ChangeLog.fs]]. `FileAgent.persist` wraps them as `PersistFilling`.
6. **DbAgent** — [[src/Server/Core/DbAgent.fs]]. Same six fields over projection state + PostgreSQL `changes` table (`Database.appendChangeWithTx` / `getChangesAfterCheckpointRevision`). `DbAgent.persist` → `PersistFilling`.
7. **Api** — [[src/Server/Api.fs]]. Never takes PersistHandlers. Uses `CoreChanges`: `getPoll` / `postLoad` call `handle.getChangesSince` for the Change list; `postChange` calls `handle.postChange`. That path stays Change until [43 — Migrate HTTP Adapter onto postEvent and Event Poll](../issues/43-migrate-http-adapter-onto-postevent-and-event-poll.md).
8. **CoreRuntime / CoreMailbox.createFile|createDb** — Compose File or Db `PersistFilling` into `CoreMailbox.host`. No EventLog seed from disk.

`PersistFilling` ([[src/Server/Core/MailboxHost.fs]]) carries `handlers: PersistHandlers` plus ready / flush / dispose / optional `until` / `bindSnapshot`. It does not carry a restored `EventLog`.

## 2. How FileAgent and DbAgent load, restore, and return change tails today

### 2.1 FileAgent (Change path)

1. **Load state** — `DocumentLoader.tryLoadState`: Graph from on-disk documents; `Revision` from `SYSTEM/gambol.meta` ([[src/Server/Bookkeeping.fs]]). Does not replay `gambol.log` into Graph.
2. **Open Change log** — `Bookkeeping.openLogStream` → `SYSTEM/gambol.log`. `ChangeLog.buildIndex` indexes line offsets. Stream seeks to end for appends.
3. **getChangesSince** — For each index slot after `after.Value`, `ChangeLog.readEntryAt` + `ChangeLog.decodeChange`. Soft-decode failures are skipped (`List.choose`).
4. **postChange** — Apply batch (dedupe by `changeId` via log scan), validate disk effects, optional document persist, append `ChangeLog.encodeChange` lines, update state / meta when clean.
5. **Event JSON** — No FileAgent call to `EventJson.encode` / `EventJson.decode`. No second events file beside `gambol.log`. [40 — Expand postEvent, EventLog store, and Event JSON persist](../issues/40-expand-postevent-eventlog-and-event-json.md) Status `coded` for Shared codecs and mailbox store only; File persist did not gain an Event write/read path.

### 2.2 DbAgent (Change path)

1. **Load state** — `Database.loadPersistedState`: Graph + revision from projection tables. The `decodeChange` callback is unused for Graph rebuild (signature kept). Change rows are not folded into mailbox EventLog.
2. **getChangesSince** — `Database.getChangesAfterCheckpointRevision` then `Serialization.decodeChange` on `payload` TEXT (Change JSON, same shape as ChangeLog).
3. **postChange** — Apply batch, live-save artifacts when configured, `ChangeLog.encodeChange` into `changes.payload` inside a transaction, update projection / snapshot flags.
4. **Event JSON** — No Event-shaped column or codec on the Db path. Same gap as File.

### 2.3 Mailbox restore gap

`CoreMailboxBackend.makeMailBox` always sets `eventLog = ref EventLog.empty`. Neither File nor Db returns persisted Events for `EventLog.restore`. After process restart, in-memory lifecycle and Change Events from [41 — Migrate Core mailbox, CoreMsg, and Pool onto Event](../issues/41-migrate-core-mailbox-coremsg-and-pool-onto-event.md) are gone; only Change tails remain via `getChangesSince`.

## 3. EventLog.restore / since / encode/read Event JSON APIs in Shared

### 3.1 EventLog ([[src/Shared/EventLog.fs]])

1. **empty** — `{ events = []; nextId = EventId.zero }`
2. **nextId** — Counter for the next append
3. **append** — Cons Event with assigned `id`, bump `nextId` (newest-head list; destination arch text still says oldest-head)
4. **since** — Filter `events` where `id > after`; keeps same `nextId`
5. **tryFind** — By `EventId` (name-only Undo/Redo in CoreEventDispatch)
6. **restore** — Fold persisted Events onto `log.events`, skip duplicate `submissionId`, **keep source `nextId` unchanged** (Shared.Tests `restore keeps source nextId`). Does not compute `nextId` from max persisted id.

### 3.2 Event JSON ([[src/Shared/EventJson.fs]])

1. **EventJson.encode** — Full Event (`id`, `submissionId`, `authority`, `commandName`, `body`) including ActorStart / ActorStop kinds
2. **EventJson.decode** — Matching decoder
3. Destination inventory ([[event-abstraction.md|Event abstraction]] §3.2) lists encode/read on **EventLog**; [40 — Expand postEvent, EventLog store, and Event JSON persist](../issues/40-expand-postevent-eventlog-and-event-json.md) comments once claimed a move onto EventLog and delete of EventJson. **Current tree:** codecs live in EventJson; EventLog has no encode/decode. Tests: [[tests/Shared.Tests/EventJsonTests.fs]].

### 3.3 Not on PersistHandlers yet

Nothing under Server calls `EventLog.restore` or `EventJson.*` for File/Db I/O. Shared.Tests cover restore dedupe ([[tests/Shared.Tests/EventTests.fs]]); History’s lagging twin is `History.restoreChanges` ([[src/Shared/History.fs]], [[tests/Shared.Tests/HistoryTests.fs]]).

## 4. ActorStart / ActorStop append today vs persist.postChange

### 4.1 In-memory path ([41 — Migrate Core mailbox, CoreMsg, and Pool onto Event](../issues/41-migrate-core-mailbox-coremsg-and-pool-onto-event.md))

1. **StartActor** — After `pool.startActor` Ok: `CoreEventDispatch.actorStart eventLog caller request` then `pool.schedule`.
2. **ActorStop** — For Authority Actor: `CoreEventDispatch.actorStop eventLog caller focusId result` then `pool.finish`.
3. Both helpers build a lifecycle `Event` (new `submissionId`, stamped authority, empty `commandName`) and call private `append` on the mailbox `EventLog ref` only.

### 4.2 Persist

1. **actorStart / actorStop** do not take `PersistHandlers` and do not call `postChange`.
2. **postEvent** persist branch requires `Event.ops = Some`; Actor bodies return `None` → `Ok None` with no File/Db write.
3. Therefore ActorStart / ActorStop **never hit** `persist.postChange` (or any other persist field). [42 — Migrate PersistHandlers restore and getEventsSince](../issues/42-migrate-persisthandlers-restore-and-geteventssince.md) item **persist ActorStart and ActorStop** must add a durable Event write and call it from the lifecycle append path (or a widened PersistHandlers Event door).

## 5. Existing Server.Tests that touch getChangesSince, restore, FileAgent, DbAgent persist

### 5.1 getChangesSince / Change Poll tail

1. **Actor getChangesSince surfaces persist error instead of empty list** — [[tests/Server.Tests/ActorCoreChangesDoorTests.fs]]
2. **ACK returns stamped complete Change equal to ChangeLog** — uses `CoreMailbox.getChangesSince` — [[tests/Server.Tests/FileAgentFailureTests.fs]]
3. **trailing duplicate keeps stamps on last new Change** — uses `CoreMailbox.getChangesSince` — [[tests/Server.Tests/FileAgentFailureTests.fs]]
4. Stub `CoreChanges.getChangesSince` — [[tests/Server.Tests/CoreCredentialsTests.fs]], [[tests/Server.Tests/ApiGetStateTests.fs]], [[tests/Server.Tests/CoreChangesTests.fs]], [[tests/Server.Tests/ApiPostLoadTests.fs]]
5. HTTP Poll asserting Change lists — [[tests/Server.Tests/StateEndpointTests.fs]] (`/ambit/poll?rev=…`)

### 5.2 eventsSince / EventLog (in-memory only; not persist restore)

1. **CoreMailbox.postEvent appends an Event that eventsSince returns** — [[tests/Server.Tests/CoreMailboxDoorTests.fs]]
2. **eventHistory returns full EventLog and eventsSince returns its tail** — [[tests/Server.Tests/Issue41CoreMailboxTests.fs]]
3. ActorStart / ActorStop in-memory assertions — [[tests/Server.Tests/CoreMailboxDoorTests.fs]], [[tests/Server.Tests/Issue41CoreMailboxTests.fs]] (`mailbox appends ActorStart and ActorStop in lifecycle order`)

### 5.3 FileAgent persist

1. Module [[tests/Server.Tests/FileAgentFailureTests.fs]] — exception log, timeout, soft-fail, restart soft-fail, ACK vs ChangeLog, trailing duplicate / multi-Change `handlers.postChange`
2. File host fixtures — [[tests/Server.Tests/TestBackend.fs]] `admittedHostFile`, [[tests/Server.Tests/ActorCoreChangesDoorTests.fs]], [[tests/Server.Tests/CoreMsgActorCasesTests.fs]], [[tests/Server.Tests/TestActorHelloTests.fs]], [[tests/Server.Tests/GraphOnlyChangePostTests.fs]], [[tests/Server.Tests/LazyLoadReconciliationServerTests.fs]], [[tests/Server.Tests/IgnoredDestinationValidationTests.fs]]

### 5.4 DbAgent persist / reload

1. Module [[tests/Server.Tests/DbAgentTests.fs]] — empty DB, startup sweep, FIFO buffer, sweep failure, **new process loads state from projection and changes after post**, reload updateTime, DB away, rebuildFromDocumentFiles, loadPersistedState name/kind, commit hang, live-save artifacts, missing ROOT, dual-owned repair
2. Module [[tests/Server.Tests/DbAgentFailureTests.fs]] — PostEvent exception survival
3. Db fixtures — [[tests/Server.Tests/TestBackend.fs]] `admittedHostDb`, [[tests/Server.Tests/DatabaseProjectionContractTests.fs]]

### 5.5 Shared restore (not Server persist)

1. **EventLog.restore** — [[tests/Shared.Tests/EventTests.fs]]
2. **History.restoreChanges / fromChanges** — [[tests/Shared.Tests/HistoryTests.fs]]
3. **EventJson round-trips** — [[tests/Shared.Tests/EventJsonTests.fs]]

No Server.Test today asserts File/Db → `EventLog.restore` on mailbox start, or durable ActorStart / ActorStop across process restart.

## 6. Concrete minimal code change list for [42 — Migrate PersistHandlers restore and getEventsSince](../issues/42-migrate-persisthandlers-restore-and-geteventssince.md)

Goal: ticket items **persist ActorStart and ActorStop**, **EventLog.restore**, **getEventsSince returns Events**, while **getChangesSince** (Change list) keeps compiling for HTTP Poll ([43 — Migrate HTTP Adapter onto postEvent and Event Poll](../issues/43-migrate-http-adapter-onto-postevent-and-event-poll.md)).

### 6.1 PersistHandlers and Core wiring

1. **Add getEventsSince (or postEvent persist field)** on `PersistHandlers` — e.g. `getEventsSince: EventId -> Result<Event list, string>` (or return `EventLog`) plus a durable append for Events that lack Change Ops (lifecycle). Keep existing `getChangesSince` / `postChange` signatures for Api Poll.
2. **Widen failedPersist** in CoreMailboxBackend to close or pass through the new Event fields consistently.
3. **CoreMsg / CoreMailbox** — Add a public door that reads persist Event tail (name per arch: `getEventsSince`) distinct from in-memory `eventsSince`, **or** make `EventsSince` prefer persist+memory merge once restore lands. Do not change Api Poll yet.
4. **CoreChanges** — Optional additive `getEventsSince` for non-HTTP callers; leave `getChangesSince: Revision -> Async<Change list>` for Api.
5. **Seed mailbox EventLog on host create** — PersistFilling or handlers must expose loaded Events; `makeMailBox` / `host` call `EventLog.restore persisted EventLog.empty` (and set `nextId` past max restored id — restore alone does not bump `nextId`).

### 6.2 CoreEventDispatch lifecycle persist

1. **actorStart / actorStop** — After in-memory append (or as part of one helper), write the stored Event through PersistHandlers (new Event append API). Do not route lifecycle through `postChange` Change lists.
2. **postEvent Change path** — Keep deriving `Change` + `persist.postChange` for Graph/docs until contract; additionally append Event JSON to the Event persist stream so restore sees Change Events (destination: one EventLog persist; expand may dual-write Event JSON while ChangeLog name remains). Minimal if restore can synthesize Events from Change rows for Actions only — then lifecycle still needs a real Event write.

### 6.3 FileAgent

1. **Event persist stream** — Append Event JSON (via `EventJson.encode`) beside or into the lagging ChangeLog name without dropping Change encode for Poll. Prefer one append API used by both lifecycle and Change confirmation.
2. **Load** — Read all durable Events at create; keep Change index for `getChangesSince`.
3. **getEventsSince** — Tail by `EventId` from the Event store (or restored list).
4. **getChangesSince** — Leave Change decode path intact for [43 — Migrate HTTP Adapter onto postEvent and Event Poll](../issues/43-migrate-http-adapter-onto-postevent-and-event-poll.md).

### 6.4 DbAgent

1. **Durable Event rows** — New table/column or Event JSON in payload with a discriminant; dual-write while Change Poll still decodes Change JSON. Lifecycle rows must not break `decodeChange` on the old Poll path (separate store or skip non-Change payloads in `getChangesSince`).
2. **Load** — Load Event list; `EventLog.restore` into mailbox seed; keep projection + Change `getChangesSince`.
3. **getEventsSince** — Query Event tail by EventId.

### 6.5 Shared / tests (narrow)

1. **Optional** — If arch wants encode on EventLog, thin wrappers calling EventJson; not required to ship 42 if File/Db import EventJson.
2. **Fix nextId after restore** — Host seed must set `nextId` to max(persisted ids)+1 (or extend `EventLog.restore`); otherwise post-restart appends collide at EventId 0.
3. **Tests** — File/Db: persist ActorStart/Stop survives create-dispose-create; restore dedupes `submissionId`; `getEventsSince` returns Events; `getChangesSince` still returns Changes for Poll fixtures; extend Issue41 lifecycle tests to assert durable side, not only `eventHistory`.

### 6.6 Explicit non-goals (stay on later tickets)

1. **Api.getPoll / postLoad Event tail** — [43 — Migrate HTTP Adapter onto postEvent and Event Poll](../issues/43-migrate-http-adapter-onto-postevent-and-event-poll.md)
2. **Browser EventId cursor / PendingChange** — [44 — Migrate Browser Poll, History, pending, and EventId cursor](../issues/44-migrate-browser-poll-history-pending-and-eventid.md)
3. **Drop ChangeLog name / History types** — [45 — Contract HistoryEvent, mailbox History, PendingKind, StartActorRequest, and ChangeLog](../issues/45-contract-historyevent-clienthistory-pendingkind-and-changelog.md)

## 7. Biggest surprises (summary)

1. [40 — Expand postEvent, EventLog store, and Event JSON persist](../issues/40-expand-postevent-eventlog-and-event-json.md) Status `coded` “Event JSON beside ChangeLog” did **not** wire FileAgent or DbAgent: only Shared `EventJson` + empty mailbox `EventLog` exist; there is nothing on disk/DB for `EventLog.restore` to consume yet.
2. ActorStart / ActorStop are mailbox-memory only; `Event.ops = None` intentionally skips `persist.postChange`, so [42 — Migrate PersistHandlers restore and getEventsSince](../issues/42-migrate-persisthandlers-restore-and-geteventssince.md) needs a **new** persist door, not a reuse of Change post.
3. `EventLog.restore` keeps `nextId` frozen; seeding the mailbox from persist without fixing `nextId` would reuse EventId 0 after restart.
