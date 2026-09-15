# Issue 41 — Full-suite failure diagnosis

Date: 2026-09-15. Ticket: [[../issues/41-migrate-core-mailbox-coremsg-and-pool-onto-event.md|41 — Migrate Core mailbox, CoreMsg, and Pool onto Event]]. Reported result: Full suite failed, 428 passed, 6 failed. Sources: leftover `./scripts/test.sh all` output (428/6), then focused re-runs of the six named tests on the dirty **dev** workspace. Git at diagnosis: `scripts/gitstatus.sh` showed uncommitted Core Event mailbox migration under `src/Server/Core/` plus related Server tests. No commit.

## 1. Verdict

Issue-41 code needs a fix before resume. `CoreMailbox.coreChanges.postChange` now builds Events and routes through `CoreEventDispatch`, which skips empty Ops and returns `Ok` instead of the prior File/Db `Unchanged submission is rejected` error. That breaks HTTP and CoreChanges unchanged-submission compatibility. Ticket 41 requires CI green.

## 2. Six failing tests

From the matching full-suite log (428 passed, 6 failed), reproduced in focused filters:

1. `Gambol.Server.Tests.DbAgentTests.DbAgent serves reads while sweep buffers FIFO mutations then trims` — [[../../tests/Server.Tests/DbAgentTests.fs|DbAgentTests.fs]]
2. `Gambol.Server.Tests.DbAgentTests.DbAgent startup sweep failure preserves reads and fails mutations closed` — [[../../tests/Server.Tests/DbAgentTests.fs|DbAgentTests.fs]]
3. `Gambol.Server.Tests.DbAgentTests.DbAgent change fails and state is unchanged when DB goes away after startup` — [[../../tests/Server.Tests/DbAgentTests.fs|DbAgentTests.fs]]
4. `Gambol.Server.Tests.DatabaseProjectionContractTests.db bootstrap duplicate returns stored Change and rejects no-op` — [[../../tests/Server.Tests/DatabaseProjectionContractTests.fs|DatabaseProjectionContractTests.fs]]
5. `Gambol.Server.Tests.StateEndpointTests.POST unchanged submission is rejected(backend: File)` — [[../../tests/Server.Tests/StateEndpointTests.fs|StateEndpointTests.fs]]
6. `Gambol.Server.Tests.StateEndpointTests.POST unchanged submission is rejected(backend: Db)` — [[../../tests/Server.Tests/StateEndpointTests.fs|StateEndpointTests.fs]]

## 3. Classification

Categories: (a) caused by issue-41 migration changes; (b) pre-existing / environment / DB flakiness; (c) unrelated local state or pre-existing test structure.

1. **DbAgent serves reads while sweep buffers FIFO mutations then trims** — **(c)**, with latent **(a)** if the Wait were fixed. Error: `Expected startup sweep to begin.` The test waits on the sweep event before it calls `admittedHostDb` / `CoreMailbox.host`, and the prelude that runs the sweep starts only in `startWithPrelude`. `DbAgent.fs` has no migration diff. Same Wait-before-host order exists on HEAD. Later asserts expect `postChange []` to error with `changes must not be empty`; under current migration that empty post would become `Ok` via `CoreEventDispatch` (latent (a)).
2. **DbAgent startup sweep failure preserves reads and fails mutations closed** — **(a)**. Error: `Expected mutation rejection after startup sweep failure.` Uses `createForTest` (no live DB). After failed prelude, `postChange []` used to hit `failedPersist` through `PostChange`. Now `coreChanges.postChange` sends `PostEvents` with an empty list; `CoreEventDispatch.persist` treats empty pending as `Ok None` and never calls `persist.postChange`, so the reply is `Ok`.
3. **DbAgent change fails and state is unchanged when DB goes away after startup** — **(b)**. Error: expected substring `Database error:`, actual message starts `Startup projection sweep failed: Exception…`. Real `DbAgent.create` path; failure shows startup sweep did not succeed against the DB before the test closed connections. Not the empty-Ops shortcut (this post has Graph Ops).
4. **db bootstrap duplicate returns stored Change and rejects no-op** — **(a)**. Assertion `unchanged submission must be rejected` after `core.postChange` of a Change with `ops = []`. Duplicate ACK path ran; the no-op path returned `Ok` under the new Event door. Needs DB to run, but the fail is the migration behavior, not connectivity.
5. **POST unchanged submission is rejected (File)** — **(a)**. Expected `BadRequest`, actual `OK`.
6. **POST unchanged submission is rejected (Db)** — **(a)**. Same as File.

Correction vs the handoff phrase “four DB/environment tests and two unchanged-submission compatibility tests”: count is **four migration-caused** (tests 2, 4, 5, 6), **one DB/environment** (test 3), **one pre-existing test-structure** (test 1). The two StateEndpoint cases are the named unchanged-submission compatibility pair; DatabaseProjection’s no-op leg is the same root cause.

## 4. Unchanged-submission compatibility — root cause

### 4.1 Call path

1. HTTP `/ambit/changes` binds `CoreMailbox.coreChanges` in [[../../src/Server/RouteRegistration.fs|RouteRegistration.fs]] (`boundChanges`), then `Api.postChange` → `handle.postChange`.
2. Issue-41 rewired `CoreMailbox.coreChanges.postChange` to map each `Change` to `EventBody.Change` and post `PostEvents` (see [[../../src/Server/Core/CoreMailbox.fs|CoreMailbox.fs]]).
3. `DatabaseProjectionContractTests` uses the same `coreChanges.postChange` door.

Ticket 43 (HTTP Adapter migrate) is out of scope for wiring Api onto `postEvent` by name. HTTP already uses the CoreChanges door, so migrating that door changes HTTP behavior now.

### 4.2 Behavior change

Previous path: `PostChange` → `persist.postChange` → FileAgent / DbAgent `ChangeAmendment.applyChange`. Empty Ops yield `ApplyResult.Unchanged` → `Error "Unchanged submission is rejected."`

New path in [[../../src/Server/Core/CoreEventDispatch.fs|CoreEventDispatch.fs]] `persist`:

1. `Event.ops` of `Some []` is dropped (`None` in the choose).
2. When `pending` is empty, return `Ok None` (no persist call).
3. `CoreMailbox.coreChanges` maps `Ok (_, None)` to a fabricated `CoreChangesAccepted` with empty changes → HTTP 200.

Empty Change list (`postChange []`) takes the same shortcut and can bypass startup `failedPersist`.

### 4.3 Expected under ticket 41?

No. Ticket 41 says CI stays green and keeps Change-era compatibility until later contract tickets. Rejecting unchanged submissions remains File/Db agent contract; the Event door must preserve that reject when it replaces `PostChange` for CoreChanges callers. This needs a code fix in the issue-41 migration (reject empty / Unchanged at `CoreEventDispatch` or always run `persist.postChange` for Change-bodied Events), not a test waiver and not deferral to ticket 43.

## 5. Workspace notes (diagnosis only)

1. Dirty tree: Core mailbox Event migration (`CoreMailbox`, `CoreMailboxBackend`, `CoreMsg`, `CoreActorPool`, new `CoreEventDispatch`), plus Server test updates and `Issue41CoreMailboxTests.fs`.
2. `FileAgent.fs` / `DbAgent.fs` production code unchanged in the diff; failure-test string updates expect `PostEvent` / `eventCount` in logged operation names.
3. Earlier suite runs in this workspace also showed broader reds (FileAgentFailure, CoreChanges, more StateEndpoint) and DLL lock conflicts from overlapping `testhost` builds; the 428/6 result is the cleanest full-suite snapshot matching the reported numbers.

## 6. Resume guidance

1. Fix unchanged / empty-Ops rejection on the `postEvent` / `PostEvents` persist path before more ticket work.
2. Re-run StateEndpoint unchanged (File+Db) and DatabaseProjection no-op; also re-check DbAgent startup-failure empty post.
3. Treat the Wait-before-host DbAgent sweep test as a separate pre-existing test bug unless a green HEAD baseline proves otherwise.
4. Treat the “DB goes away” substring miss as environment/DB unless it remains after a healthy startup sweep.

## 7. Fix (2026-09-15)

Surgical fix landed: [[issue-41-unchanged-submission-fix.md]]. `CoreEventDispatch.persist` forwards empty Ops / empty batches to `persist.postChange` again. Focused re-run: StateEndpoint unchanged (File+Db), DatabaseProjection no-op, DbAgent startup-sweep-failure empty post, Issue41 mailbox tests, and related CoreMailbox `postEvent` door tests — all passed.
