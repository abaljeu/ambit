# 06 — Investigation: children missing on the first connect after idle

Status: candidate found, pending confirmation against a captured occurrence
Scope: [06 — Children missing on the first connect after idle](../06-children-missing-on-connect.md)

This note walks the path from server start to a client's first render. It builds on the investigation that landed in `f10817ac`. It confirms that investigation's recover-and-race mechanism, corrects two conclusions, and adds the FileAgent fallback and the client's want filter, which together change how a first `/state` paint can stay empty after the server heals.

It does not change any code path. A reproducing test for the recover seam lives in `tests/Shared.Tests/EventTests.fs`, skipped so the suite stays green.

## How a restart reaches a client's first render

- `RouteRegistration.CreateBoot` (`src/Server/RouteRegistration.fs`) calls `DatabaseSetup.resolveDbConnection` with `PersistenceMode.Db`. On any exception from `Database.initSchema`, that function writes `Gambol: DB connection failed - falling back to file store.` to stderr and returns `DbStatus.Absent`. `CoreRuntime.startHost` (`src/Server/Core/CoreRuntime.fs`) starts `DbAgent.createWithDataDir` only for `DbStatus.Ok`. Every other status, including `Absent`, starts `FileAgent.create` on `boot.DataDir`.
- On the DbAgent path, `DbAgent.createWithLiveSave` (`src/Server/Core/DbAgent.fs`) calls `Database.loadPersistedState`, which reads the projection through `tryLoadGraphFromProjection`. That read runs `GraphProjection.graphFromPersistence` (`src/Shared/GraphProjection.fs`), an all-or-nothing check: every parent's child ordinals must be dense `0..n-1`, or the whole read returns `Error`. On `Error`, `loadPersistedState` falls back silently to `Graph.create (), EventId.zero`. There is no `eprintfn` on that branch.
- `DbAgent.createLoaded` then calls the private `loadReconciled`, which fetches every row (`Database.getEvents` has no `WHERE` on event id) and decodes each one with `EventLogFile.decodeEvent`. `List.choose` keeps only the rows that decode. A row that fails to decode is dropped before `EventLog.recover` sees it.
- `EventLog.recover` (`src/Shared/EventLog.fs`) applies every event still ahead of the starting checkpoint, oldest first, through `Ev.apply`. `applyRecover` on `ApplyResult.Invalid` keeps the graph unchanged and still sets `eventId` to that event's id. A dropped populate leaves the empty `childMap` entry `Graph.addDetachedNode` created: Parsed, with an empty child list.
- This sequence runs synchronously, before the mailbox exists. `loaded.state.Value` holds the result from the first moment any query can reach the mailbox.
- The mailbox then starts with `CoreMailboxLoop.startWithPrelude`, running `DbAgent.liveStartupSweep` as its `until` prelude. While that prelude is not complete, `startupLoop` answers only the messages `CoreMailboxLoop.isStartupRead` accepts — `Query.GraphAtEvent`, `Query.LastEventId`, `Query.EventsSince` — straight off the queue.
- Once the sweep reports, `DbAgent.applyMaintenance` may read the projection fresh and replace `loaded.state.Value`. Only after this step does the prelude finish and `loaded.ready.TrySetResult()` run.
- On the FileAgent path there is no projection sweep and no `EventLog.recover`. `FileAgent.create` loads `DocumentLoader.tryLoadState` / `DocumentPersistChange.readAllDocuments` (Directory File outlines, with discovered File bodies as Unparsed stubs). `reconcileLoaded` either truncates the file event log when the checkpoint is ahead, or sets `eventId` to the log tip and leaves the graph as loaded. `FileAgent`'s accepted replies report ready as `true` immediately.
- `Api.getState` (`src/Server/Api.fs`) answers `/state` with `CoreDoor.graphAtEvent handle`, then for the default `BootstrapScope.RootClosure` trims the reply through `ResidentProjection.visibleClosureGraph`. The body includes `isReady = CoreDoor.isReady handle`.
- `Program.fs` `loadFromState` always fetches `/state` fresh, paints it through `finishPaint` (`StateLoaded`), then calls `runBootPoll`. `Want.compose` (`src/Shared/Want.fs`) builds that poll's `want`: only Header-absent ids, at most 100. A Node already present with `childMap = []` is not re-requested. Poll HTTP errors in `runBootPoll` are ignored. [HTTP contract section 2.8](doc/current/http-contract.md) is this Browser poll (`POST /ambit/poll` with `eventId` and `want`; success includes `nodes` and `childMap`).
- `ViewModelChildrenIndicator.rowChildrenIndicator` (`src/Shared/ViewModelChildrenIndicator.fs`), drawn by `RowView.nodeBullet` (`src/Client/RowView.fs`), shows a solid circle for a present Node whose `GraphChildren.tryGet` is `Some []` (Parsed, empty). An Absent Header (id in a parent's list, not in `graph.nodes`) also gets a solid circle, the placeholder bullet.

## Relationship to the earlier investigation

The earlier note in this file, landed with `f10817ac`, is the base. This section records what still holds and what this pass changes.

Confirmed:

- `EventLog.recover` can advance past a stale `Replace` and leave the parent Parsed with an empty child list. The skipped test ``recover silently drops a stale Replace and freezes the parent's children empty`` in `tests/Shared.Tests/EventTests.fs` fails on that last assertion when unskipped, and still stands as the recover-seam reproduction.
- `loadPersistedState` swallows a projection `Error` and forces a full replay from `Graph.create (), EventId.zero`.
- `loadReconciled` drops decode failures silently.
- `CoreMailboxLoop.isStartupRead` answers `Query.GraphAtEvent` during the prelude. The race is real.
- After a successful sweep, `applyMaintenance` can replace in-memory state from the projection tables, which stayed correct in Postgres. That matches project.md Findings: no event emptied LAPS or ambit, and the rows stayed intact.
- `BootCacheStore.readCache` / `BootCache.decideBootRead` are not on the live `loadFromState` path. First paint always comes from a fresh `/state`.
- A captured occurrence is still required before any path is the confirmed cause, per the ticket's Stop section.

Corrected:

- A lasting empty display does not need the client to connect in the few-second prelude window and then never refresh. `Want.compose` requests only Absent Headers. A Parsed-empty Node already in the `/state` graph is never re-wanted, so the first paint stays empty for the session even after the sweep heals server memory. Candidate 1 of the earlier note is therefore stronger as a client-visible bug, and weaker as a "race only" story.
- `visibleClosureGraph` is not inert without an Unloaded entry already on the server Graph. The trim itself can send a nested Workspace as a child id without a Header. That row uses the Absent-Header placeholder bullet, also a solid circle. `Want.compose` does request those Absent ids, so a later poll on a healthy server usually installs the Headers and their children. This is a poorer fit for a one-day, after-idle miss that lasted until the next day's connect, unless that poll also failed.

Extended:

- The FileAgent fallback after `resolveDbConnection` is a complete, logged startup error path the earlier note did not rank. Alan's 2026-11-25 decision that a startup process error masked the issues, while the database stayed intact, names this shape.
- The skipped recover test uses a synthetic `Replace` span that a clean, amended replay would not store. It proves the recover seam. A production full replay reaches that seam only after an earlier decode or apply drop, or another precondition break.
- The Browser poll has no implemented 200-line residency cap. That cap lives in the Agent poll spec only. `Want.compose` caps the Browser `want` at 100 Absent ids.
- [07 — Persistent server error log](../07-persistent-error-log.md) would record the connection fallback, the `loadPersistedState` fallback, each decode drop, and each `ApplyResult.Invalid` recover skip. Those entries would distinguish the ranked candidates on the next occurrence. This ticket does not implement that log.

## Ranked candidates

### 1. A startup exception falls back to FileAgent and serves the dataDir outline for the life of the process

Mechanism:

- `Database.initSchema` throws (a timeout, a transient Azure connection error, a lock, or any other exception). `resolveDbConnection` catches it, writes the fallback line to stderr, and returns `DbStatus.Absent`. The process stays up.
- `CoreRuntime.startHost` boots `FileAgent`. The in-memory graph is the Directory File outline on disk, not the Postgres projection. FileAgent does not replay the file event log onto that graph. Ready is `true` from the first `/state`.
- Postgres is untouched, which matches the intact store. The next start whose `initSchema` succeeds boots DbAgent and serves the projection, so the same Nodes can show their children again with no event in between.
- This produces a solid-circle Parsed-empty bullet only when the dataDir outline for that parent is itself empty. That disk condition is not yet confirmed.

Evidence:

- This is the only start path that logs a startup error and then continues on a different store. Stderr is process-local, and `HttpResponseLog.prepareFresh` wipes `http-responses.log` on every start, so the Nov 23 line would already be gone. 07 — Persistent server error log exists because of that gap.
- `isWritable` is true only for `DbStatus.Ok`, so a connect on this path cannot Save. That fits "before the user did anything."
- Azure idle commonly recycles the process, so the first connect after idle is a cold start.

Proposed fix direction: in `PersistenceMode.Db`, do not start FileAgent when `initSchema` fails. Fail closed, or retry the connection, and keep the stderr line plus the 07 — Persistent server error log file. Do not change FileAgent itself.

What would confirm or rule this out:

- The `db-status` label at the Nov 23 connect: `StatusView.renderDatabaseStatus` shows `Files only` when `DbStatus.Absent`, and `DB synced` when `Ok`.
- The `/state` body's `isReady` field: `true` immediately favours this path.
- Whether the Azure dataDir Directory outlines for LAPS and the ambit Workspace already listed those children on disk.

### 2. A cold DbAgent replay leaves a Node wrongly empty, and the first `/state` paint stays empty after the sweep heals the server

Mechanism:

- An ordinal-density violation anywhere in the projection sends `loadPersistedState` down its silent fallback. `loadReconciled` then replays every stored event from `EventId.zero`.
- Somewhere in that replay, an event that populated a parent's children fails `EventLogFile.decodeEvent` or comes back `ApplyResult.Invalid`. The parent stays Parsed with an empty child list in `loaded.state.Value` only. Nothing in this path writes those historical drops back to `node_children`.
- `isStartupRead` can serve that graph on `/state` before the prelude finishes. `loadFromState` paints it immediately.
- `liveStartupSweep` may then repair the projection and `applyMaintenance` may replace server memory with the correct graph. The client is not healed by that step: `Want.compose` will not re-request a Header that is already present with `childMap = []`, and the sweep emits no event for the poll to apply. The empty lists last for the session. A later connect to a healed, still-running server, or to a later start whose projection load succeeds, shows the children.

Evidence:

- The skipped EventLog recover test demonstrates the empty-list freeze at the recover seam.
- `DbAgent.liveStartupSweep`'s log line shows ordinal drift in production, already self-repairing, which is the trigger for a full replay.
- `src/Server/Core/CoreEventDispatch.fs` `persistNew` / `store` / `withConfirmedOps` show a live post stores amended ops, so a clean replay is normally deterministic. A genuine decode or precondition failure is still required to open the gap.
- `tests/Server.Tests/DbAgentTests.fs` `DbAgent missing ROOT fails closed while reads stay available` shows a harder sweep failure wedges mutations while reads stay available, which is a different, permanent failure, not this transient one.

Proposed fix direction: make `EventLog.recover` surface a dropped event instead of swallowing it, and have `loadReconciled` refuse readiness rather than publish a Graph with a shortened history. Make `loadPersistedState`'s `Error` branch a loud startup incident, not a silent fallback to an empty Graph. Independently, either delay `Query.GraphAtEvent` until the prelude finishes, or have the client re-fetch `/state` when `isReady` becomes true, so a Parsed-empty first paint is not sticky.

What would confirm or rule this out:

- The `DbAgent: projection repair …` log line's timestamp against the Nov 23 connect.
- `/state` `isReady` false on that first paint, then true later, with no new events.
- The browser console `decodeStateResponse` line, and whether LAPS and ambit were present Headers with empty lists rather than placeholder rows.

### 3. `isStartupRead` lets a connect through before the startup sweep finishes, on its own

`Query.GraphAtEvent` is answered from `loaded.state.Value` while the prelude is still running. This is confirmed by reading `CoreMailboxLoop.startWithPrelude`. On its own, without candidate 2's replay failure, this window should not produce an emptied child list: the state it serves is `loadReconciled`'s normal catch-up. Ranked third because the race is confirmed, but it needs candidate 2, or another mutation, to have anything wrong to serve.

Proposed fix direction: covered by candidate 2's readiness and re-fetch work.

What would confirm or rule this out: the same timing evidence as candidate 2.

### 4. `Api.getState`'s RootClosure trim uses `visibleClosureGraph`

`Api.getState`'s default scope calls `ResidentProjection.visibleClosureGraph`, not `ResidentProjection.bootstrapGraph` / `rootBootstrapGraph`. Without a saved `zoom` inside LAPS or ambit, a nested Workspace can be a child id of `workspaces` without a Header in the `/state` Graph. `tests/Server.Tests/ApiGetStateTests.fs` `getState without zoom keeps nested Workspace Unloaded` records that shape. The placeholder bullet is a solid circle, so it can look like this bug at first paint.

`Want.compose` requests those Absent Headers, and `ResidentProjection.wantAnswer` / `SyncLogic.applySyncResponse` install the real child lists when the server Graph is healthy. Ranked fourth: real, usually short-lived on a healthy boot poll, and a poorer fit for a miss that lasted until the next day's connect unless that poll failed (`runBootPoll` ignores HTTP errors).

Proposed fix direction: route `RootClosure` through the same complete-subtree function `BootCache.fs` already uses, so a genuinely Unloaded Node keeps its hollow-circle bullet, and a nested Workspace Header is present on first paint.

What would confirm or rule this out: whether the saved `zoom` on Nov 23 pointed at or inside LAPS or ambit, and whether those rows were dark placeholder bullets (Absent) or ordinary text Headers with solid dots (Parsed empty).

### 5. Client BootCacheStore paints a stale snapshot before the fresh `/state` response

`loadFromState` always fetches `/state` fresh and renders it through `finishPaint`. `BootCacheStore.persistAfterState` writes the cache only after that render. A repository-wide search for `BootCacheStore.readCache`, `BootCache.decideBootRead`, and `BootCache.decideBootReadWait` finds no call site outside `src/Shared/BootCache.fs` and `tests/Shared.Tests/BootCacheTests.fs`. This path does not participate in first paint.

### 6. A poll cap or truncation empties child lists

The Browser poll in HTTP contract section 2.8 has no implemented residency-line cap. The "at most 200 lines" figure is an Agent poll spec item only. `Want.compose` truncates the Browser `want` to 100 Absent Header ids. That cap can delay filling Absent Headers; it does not clear a child list already painted as Parsed empty. This is not a sufficient cause of the Nov 23 symptom.

## Tests

A later coding ticket, after a captured occurrence confirms the cause, should keep the existing skipped recover test and add the cases that match the confirmed candidate.

- Unskip ``recover silently drops a stale Replace and freezes the parent's children empty`` once `EventLog.recover` surfaces the drop, and assert the failure is visible to `loadReconciled`.
- A `loadPersistedState` test: `graphFromPersistence` returns `Error`, and startup does not publish `Graph.create (), EventId.zero` as a ready Graph.
- A prelude test: `Query.GraphAtEvent` during `liveStartupSweep` either waits, or a client that painted `isReady = false` re-fetches `/state` when ready becomes true and a Parsed-empty Header is replaced with the healed list.
- A `Want.compose` test that documents today's filter (present Parsed-empty ids are absent from `want`) so the sticky-paint fix does not regress.
- A `resolveDbConnection` test in `PersistenceMode.Db`: `initSchema` throws, the process does not boot FileAgent, and `DbStatus.Absent` is visible. A second start whose schema init succeeds serves the projection.

## Open questions for Alan

- What did the `db-status` label say on the Nov 23 connect (`DB synced`, `Files only`, `DB mismatch1`, or `DB mismatch2`)?
- In the Network tab's `/state` body, was `isReady` false on first paint?
- Did the saved `?zoom=` on that connect point at or inside LAPS or the ambit Workspace?
- Did those solid circles sit on ordinary named Headers, or on dark placeholder rows (Absent Headers)?
- Do the Azure dataDir Directory outlines for LAPS and ambit already list the children that Postgres holds?
- Once 07 — Persistent server error log lands, the next cold start's `<dataDir>/SYSTEM/errors.log` should be read to see which silent path, if any, fired: the connection fallback, the projection fallback, a decode drop, or an `ApplyResult.Invalid` recover skip.

## See also

[[../../project.md]], [[../../evidence/investigation-report.md]], 07 — Persistent server error log
