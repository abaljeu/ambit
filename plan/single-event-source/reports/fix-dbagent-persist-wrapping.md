# Fix DbAgent persist wrapping

Project: [[plan/single-event-source/project.md]]
Updated: 2026-09-16

## 1. Facts

Three red facts in [[tests/Server.Tests/DbAgentTests.fs]]. Production wrapping after the Ev door was wrong for timeout. Two facts used a new mailbox per `host agent` call, so they never exercised one persist filling after startup.

## 2. Cause per fact

1. **Commit hang timeout** — `DbAgent commit hang is rejected within timeout and mailbox survives`. After Ev, `CoreEventDispatch` applies through `postChange` (projection, already `runBounded`) then `appendEvent`. `LOCK TABLE events` blocks `Database.appendEvent`, which had no bound. Npgsql waited ~30s and wrapped as `Ev persist error: One or more errors occu…`. Production fix: bound Ev persist with `CoreMailboxBackend.runBounded` so the mailbox returns `change processing timed out` near 8s.
2. **Startup sweep never began** — `DbAgent serves reads while sweep buffers FIFO mutations then trims`. Sweep starts only in `CoreMailboxBackend.startWithPrelude` when a mailbox hosts `until`. The test waited on `entered` before any host, then called `admittedHostDb` again for each read and post. Each call is a new mailbox. The wait missed the sweep; FIFO was not one queue. Test fix: one mailbox, wait after host.
3. **DB gone after startup** — `DbAgent change fails and state is unchanged when DB goes away after startup`. `getState agent` hosted mailbox A (reads during sweep). Closing connections then `host agent` for `postChange` started mailbox B, which re-ran `until` against a dead DB and closed writes with `Startup projection sweep failed: Exceptio…`. Persist wrapping after a finished sweep is still `Database error:` from `persistGraphProjection`. Test fix: one mailbox, wait until ready, then close the DB. Kept the `Database error:` assert.

Nearby fact `DbAgent startup sweep failure preserves reads and fails mutations closed` was not rewritten. It still hosts per call; the failing sweep is fast enough that a second host still returns the closed-persist string.

## 3. Files

1. [[src/Server/Core/DbAgent.fs]] — `writePersistedEvent` / `appendPersistedEvent` wrap DB Ev append in `runBounded`.
2. [[tests/Server.Tests/DbAgentTests.fs]] — `getStateFrom`; pin one mailbox on the three facts; wait for ready before hang lock and before closing the DB.

Did not edit [[doc/api.md]]. Did not reintroduce preview. Did not change leftover Change/`postChange` assert strings except by hosting the mailbox the contract already assumes.

## 4. Commands and outcomes

1. `scripts/gitstatus.sh` — branch `dev`. Dirty: [[doc/api.md]], [[src/Server/Core/CoreEventDispatch.fs]], [[src/Server/Core/CoreMailbox.fs]], [[test-server.txt]], [[tests/Server.Tests/HttpResponseLogTests.fs]], [[tests/Server.Tests/StateEndpointTests.fs]], plus reports. This work added DbAgent source and tests only. Did not commit. Did not push.
2. Prior `scripts/test.sh` (parent cluster) — the three facts red as named above.
3. Focused `dotnet test tests/Server.Tests -c Debug --filter` on the three facts plus startup-sweep-failure-preserves — Passed 4, Failed 0, Duration 10 s.

## 5. Leftover failures (not this cluster)

From the earlier Server.Tests run, still out of scope:

1. [[tests/Server.Tests/HttpResponseLogTests.fs]] / [[tests/Server.Tests/StateEndpointTests.fs]] — cluster 4 (log body string, `00000000` prefix).
2. [[tests/Server.Tests/DatabaseProjectionContractTests.fs]] `db bootstrap duplicate returns stored Change and rejects no-op` — expected `"27428"`, actual `"27430"`.
