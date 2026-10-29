# 02 — Survive a Server crash scope

Slice: The in-memory apply runs first, so the stored Event carries confirmed ops; `CoreEventDispatch.persistNew` appends that Event, then the filler writes the durable Graph; `DbAgent` writes the projection after the append; `FileAgent` writes its durable Graph after the append; a Graph write that returns `Error` of a string leaves the stored Event in the log; each reported event id has Events in the log; the Browser applies those Events.

## Signatures

[ ] `CoreEventDispatch.Context` holds `persist: PersistHandlers`, `eventLog: EventLog ref`, and `restartTip: EventId` in [[src/Server/Core/CoreEventDispatch.fs]].

[ ] `CoreEventDispatch.postEvent` has type `Context -> Caller -> Ev -> bool -> Result<Ev * CoreChangesAccepted option, string>` in [[src/Server/Core/CoreEventDispatch.fs]].

[ ] `CoreEventDispatch.persistNew` has type `Context -> Caller -> Ev -> bool -> Result<Ev * CoreChangesAccepted option, string>` in [[src/Server/Core/CoreEventDispatch.fs]].

[ ] `CoreEventDispatch.persist` calls `context.persist.applyEvent completed graphOnly` and returns `Result<CoreChangesAccepted option, string>` in [[src/Server/Core/CoreEventDispatch.fs]].

[ ] `CoreEventDispatch.store` calls `commit` on the `withConfirmedOps` Event and returns `Result<Ev, string>` in [[src/Server/Core/CoreEventDispatch.fs]].

[ ] `CoreEventDispatch.appendNew` calls `context.persist.appendEvent stored` then `EventLog.append` on the mailbox ref in [[src/Server/Core/CoreEventDispatch.fs]].

[ ] `CoreEventDispatch.writeAxis` has type `PersistHandlers -> Notice -> (Graph -> Result<Graph, string>) -> Result<unit, string>` in [[src/Server/Core/CoreEventDispatch.fs]].

[ ] `PersistHandlers` defines `appendEvent: Ev -> Result<unit, string>`, `applyEvent: Ev -> bool -> Result<CoreChangesAccepted, string>`, and `replaceGraph: Notice -> Graph -> unit` in [[src/Server/Core/CoreMsg.fs]].

[ ] `CoreChangesAccepted` holds `eventId`, `events`, `externalChanges`, and `isReady` in [[src/Server/Core/CoreChanges.fs]].

[ ] `ChangeAmendment.applyPosted` has type `bool -> EventId -> Ev -> Op list -> State -> ApplyResult * bool * Op list` in [[src/Shared/ChangeAmendment.fs]].

[ ] `FileAgent` `processPostEvents` has type `LoadedFile -> Ev list -> bool -> Result<CoreChangesAccepted, string>` in [[src/Server/Core/FileAgent.fs]].

[ ] `DbAgent` `processPostEvents`, `finishAppliedPostChange`, `commitPostChange`, and `persistGraphProjection` live in [[src/Server/Core/DbAgent.fs]].

[ ] `DbAgent.appendPersistedEvent` and `writePersistedEvent` call `Database.appendEvent` in [[src/Server/Core/DbAgent.fs]].

[ ] `EventLogFile.appendEvent` has type `FileStream -> int64 ResizeArray -> Ev -> Result<unit, string>` in [[src/Server/EventLogFile.fs]].

[ ] `SyncLogic.applySyncResponse` folds `response.events` through private `foldProjectedEvents` in [[src/Shared/SyncLogic.fs]].

[ ] `BootCache.foldLog` replays delta Events through private `applyLoggedEvent` in [[src/Shared/BootCache.fs]].

## Match sites

[ ] `CoreMailboxActors.dispatchPostEvent` matches client Events and calls `CoreEventDispatch.postEvent` with `graphOnly = false` in [[src/Server/Core/CoreMailboxActors.fs]].

[ ] `CoreMailboxEvents.dispatchPostGraphOnly` calls `CoreEventDispatch.postEvent` with `graphOnly = true` in [[src/Server/Core/CoreMailboxEvents.fs]].

[ ] `CoreEventDispatch.persistNew` runs `prepare`, then `persist`, then `store`, then `markAcceptedSpecials` in [[src/Server/Core/CoreEventDispatch.fs]].

[ ] `CoreEventDispatch.markAcceptedSpecials` calls `pushChangeSpecials` when `graphOnly` is false in [[src/Server/Core/CoreEventDispatch.fs]].

[ ] `FileAgent.persistHandlers` wires `applyEvent` to `processPostEvents` and `appendEvent` to `EventLogFile.appendEvent` in [[src/Server/Core/FileAgent.fs]].

[ ] `FileAgent.processPostEvents` calls `applyBatch`, then `validatePostChange`, then sets `loaded.state.Value <- newState` before any `appendEvent` from `CoreEventDispatch.store` in [[src/Server/Core/FileAgent.fs]].

[ ] `DbAgent.persistHandlers` wires `applyEvent` to `processPostEvents` and `appendEvent` to `appendPersistedEvent` in [[src/Server/Core/DbAgent.fs]].

[ ] `DbAgent.finishAppliedPostChange` calls `commitPostChange`, which runs `persistGraphProjection` before `loaded.state.Value <- stateToStore` inside `applyEvent` in [[src/Server/Core/DbAgent.fs]].

[ ] `CoreEventDispatch.pushChangeSpecials` may call `markForPersist` and `logNotice` after the Event append in `persistNew` in [[src/Server/Core/CoreEventDispatch.fs]].

[ ] `CoreEventDispatch.writeAxis` calls `persist.replaceGraph` on success in [[src/Server/Core/CoreEventDispatch.fs]].

[ ] `CoreMailboxApi.postEventAccepted` posts `PostEvent` on the mailbox in [[src/Server/Core/CoreMailboxApi.fs]].

[ ] The Browser applies server Event tails through `SyncLogic.applySyncResponse`; this slice needs no Browser edit if the log always holds Events for each reported id.

## Existing tests

[ ] ``FileAgent applyEvent applies Ev Ops without leftover Change`` in [[tests/Server.Tests/PersistApplyTests.fs]] uses private `applyThenAppend`, which calls `applyEvent` then `appendEvent` on `PersistHandlers`.

[ ] ``DbAgent applyEvent applies Ev Ops without leftover Change`` in [[tests/Server.Tests/PersistApplyTests.fs]] uses the same `applyThenAppend` helper.

[ ] ``CoreEventDispatch persist apply does not copy Ev to leftover Change`` in [[tests/Server.Tests/PersistApplyTests.fs]] posts through `CoreMailboxApi.postEvent` and reads `getEventsSince`.

[ ] ``File startup does not replay the event log`` in [[tests/Server.Tests/MailboxHistoryDurabilityTests.fs]] appends Events with `appendOnly` and expects the Graph without the Change child after restart.

[ ] ``Db recover applies Ops Ev ahead of projection revision`` in [[tests/Server.Tests/MailboxHistoryDurabilityTests.fs]] appends Events with `appendOnly` and expects the Change child in the Graph after Db restart.

[ ] ``File mixed hello survives dispose create`` and ``Db mixed hello survives dispose create`` in [[tests/Server.Tests/MailboxHistoryDurabilityTests.fs]] post through `CoreMailboxApi.postEvents` and assert log and tip after restart.

[ ] ``ActorStart persist Error does not keep Ev in mailbox log`` in [[tests/Server.Tests/Issue42PersistHandlersTests.fs]] stubs `appendEvent` to `Error "event persist failed"` and expects an empty mailbox history.

[ ] ``getEventsSince returns Ev after postChange`` in [[tests/Server.Tests/Issue42PersistHandlersTests.fs]] posts a Change and reads `getEventsSince`.

[ ] ``applySyncResponse installs Want answer after Event tail`` in [[tests/Shared.Tests/SyncLogicTests.fs]] exercises `SyncLogic.applySyncResponse` on a `SyncResponse` with Events.

## Copy-from

[ ] Private `applyThenAppend` in [[tests/Server.Tests/PersistApplyTests.fs]] already separates in-memory `applyEvent` from `appendEvent` at the `PersistHandlers` seam.

[ ] Private `appendOnly` in [[tests/Server.Tests/MailboxHistoryDurabilityTests.fs]] appends Events through `persist.handlers.appendEvent` without running `applyEvent`.

[ ] `DbAgent.createForTest` and `DbAgent.createForTestWithDependencies` in [[src/Server/Core/DbAgent.fs]] inject startup sweep and `persistNode` for failure tests.

[ ] `Issue42PersistHandlersTests.withFilling` wraps `CoreMailbox.host` with stubbed `PersistHandlers` fields for door-level persist errors.

## write-ticket

### Source files

[ ] [[src/Server/Core/CoreEventDispatch.fs]] — reorder or split `persistNew` so `store` / `appendNew` runs before durable Graph work; keep `withConfirmedOps` on the appended Event.

[ ] [[src/Server/Core/DbAgent.fs]] — move `persistGraphProjection` and in-memory state commit to after `appendPersistedEvent` on the post path.

[ ] [[src/Server/Core/FileAgent.fs]] — defer durable Graph / in-memory commit until after `appendEvent` on the post path.

[ ] [[src/Server/Core/CoreMsg.fs]] — extend `PersistHandlers` only if a Graph write must surface `Result<unit, string>` after append (today `replaceGraph` is `unit`).

### Tests

[ ] Add ``CoreEventDispatch postEvent appends Event before Db projection revision moves`` in [[tests/Server.Tests/PersistApplyTests.fs]] or [[tests/Server.Tests/DbAgentTests.fs]] using a real test database connection.

[ ] Add ``DbAgent graph projection Error leaves appended Event in events table`` in [[tests/Server.Tests/DbAgentTests.fs]] with a stubbed projection step that returns `Error` of a string.

[ ] Change ``File startup does not replay the event log`` in [[tests/Server.Tests/MailboxHistoryDurabilityTests.fs]] to match append-then-Graph recovery (Graph catches up from the log or durable write follows append).

[ ] Change ``Db recover applies Ops Ev ahead of projection revision`` in [[tests/Server.Tests/MailboxHistoryDurabilityTests.fs]] so projection never leads the Event log after append-only seeding.

## Over size

[ ] Split ``DB rows survive second server startup without DB reset`` in [[tests/Server.Tests/StateEndpointTests.fs]] and File restart cases from [[tests/Server.Tests/Issue42PersistHandlersTests.fs]] into a follow-on ticket if the four-test cap blocks full restart coverage.

## Size

[ ] Three core server modules plus an optional `PersistHandlers` signature change fit one implement ticket.

[ ] Four listed tests fit the arch batch cap; restart HTTP coverage moves to Over size if needed.

[ ] No [[src/Client]] or [[src/Shared/SyncLogic.fs]] edit is required for Browser apply; `SyncLogic.applySyncResponse` and `BootCache.foldLog` already replay Events when the log is complete.
