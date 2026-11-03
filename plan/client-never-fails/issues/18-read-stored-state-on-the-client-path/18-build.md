# 18 — Read stored state on the client path — build

`readState` in `tests/Server.Tests/StateEndpointTests.fs` does a default GET `/ambit/state`, then Poll. Each round builds a SiteMap with `ViewModel.buildSiteMapFrom` at `graph.root`, takes `Want.compose`, posts that Want, and installs the answer with `ResidentProjection.installWantAnswer`. The loop stops when the Want is empty. A sixth round with a Want still present fails the test. The returned event id is the GET event id. `postPoll` stays on `want = []`.

This build edits test files only. `AmbitSession.getFullState` stays on `?scope=full`.

## Tests moved

These four facts moved from `tests/Server.Tests/StateEndpointTests.fs` to `tests/Server.Tests/StateEndpointGetTests.fs`. Assertions are unchanged.

[ ] ``GET state returns revision 0 for fresh server`` — client pull through `readState`
[ ] ``GET state returns valid graph with root node`` — client pull through `readState`
[ ] ``GET state default returns ROOT closure excluding nested workspace contents`` — `readClosure`, the default GET, with no Poll. A pull would install the nested Workspace Header this fact asserts is Absent.
[ ] ``GET state scope full returns canonical graph for tests`` — `readFullState`, `GET /ambit/state?scope=full`. This fact tests that scope.

`tests/Server.Tests/Gambol.Server.Tests.fsproj` compiles `StateEndpointGetTests.fs` after `StateEndpointTests.fs`.

## Older `?scope=full` read-backs

Each of these asserted a nested stored Header. None asserted the `scope` query. Each now calls `readState`.

[ ] ``POST concurrent stale text Changes amend second as amb-conflict child`` — client pull. Round 1 loads the ROOT child Header. Round 2 loads the amb-conflict child Header.
[ ] ``POST concurrent stale name Changes amend second as amb-conflict child`` — client pull, for the same nested Header.
[ ] ``POST unrelated structural edits with stale revision both succeed`` — client pull. A later round loads the grandchild Headers.
[ ] ``POST same-parent structural collision amends and succeeds`` — client pull. A later round loads both child Headers under the one parent.

## `readState` callers

`stateRoot` calls `readState`, so a `stateRoot` fact is on the client pull too.

### Client pull through `readState`

[ ] ``GET state returns revision 0 for fresh server``
[ ] ``GET state returns valid graph with root node``
[ ] ``POST Change and inverse Changes return complete confirmations in request order`` — `stateRoot` and `readState`
[ ] ``Edit reply with only a persist Notice has no external changes`` — `stateRoot`
[ ] ``POST explicit Undo JSON is rejected``
[ ] ``POST unchanged submission is rejected``
[ ] ``POST changes accepts X-Gambol-Client header`` — `stateRoot`
[ ] ``POST changes SetText changes child text and bumps revision`` — `stateRoot` and `readState`
[ ] ``POST changes NewNode+Replace adds child to root`` — `stateRoot` and `readState`
[ ] ``POST changes twice bumps revision to 2`` — `stateRoot` and `readState`
[ ] ``POST changes batch with two changes bumps revision to 2`` — `stateRoot` and `readState`
[ ] ``POST changes batch with bad second change keeps earlier items`` — `stateRoot` and `readState`
[ ] ``POST changes persists in GET state`` — `stateRoot` and `readState`
[ ] ``POST same submissionId twice is idempotent`` — `stateRoot` and `readState`
[ ] ``POST with stale base revision and valid SetText succeeds`` — `stateRoot` and `readState`
[ ] ``POST unrelated attribute edits with stale revision both succeed`` — `stateRoot` and `readState` twice
[ ] ``POST concurrent stale text Changes amend second as amb-conflict child`` — `stateRoot` and `readState`
[ ] ``POST concurrent stale name Changes amend second as amb-conflict child`` — `stateRoot` and `readState`
[ ] ``POST concurrent stale class Changes merge set delta and succeed`` — `stateRoot` and `readState`
[ ] ``POST unrelated structural edits with stale revision both succeed`` — `stateRoot` and `readState`
[ ] ``POST same-parent structural collision amends and succeeds`` — `stateRoot` and `readState`
[ ] ``POST duplicate submissionId with stale revision stays idempotent`` — `stateRoot` and `readState`
[ ] ``DB is present in db mode`` — `stateRoot`
[ ] ``DB rows persist after agent cache reset`` — `stateRoot`
[ ] ``DB rows survive second server startup without DB reset`` — `stateRoot`
[ ] ``DB authority does not import files when database is empty``
[ ] ``legacy Persistence:Mode file still uses Database persistence``
[ ] ``DB restart preserves NodeIds`` — `stateRoot` and `readState`
[ ] ``DB restart replays log when projection is cleared`` — `stateRoot` and `readState`
[ ] ``DB restart keeps duplicate submissionId idempotent`` — `stateRoot` and `readState`
[ ] ``DB restart keeps inverse Change in EventLog`` — `stateRoot` and `readState`

### Other reads in the same file

[ ] ``GET state default returns ROOT closure excluding nested workspace contents`` — `readClosure`
[ ] ``GET state scope full returns canonical graph for tests`` — `readFullState`
[ ] ``POST changes with invalid JSON returns 400`` — `getStateJson`. The body is discarded. The fact asserts the POST status.
[ ] ``POST changes with bad op returns 400`` — `getStateJson`. The body is discarded. The fact asserts the POST status.
[ ] ``unavailable Database serves read-only file fallback and ignores Persistence:Mode`` — its own default GET, then a read-only POST

## Counts

[ ] Tests moved: 4
[ ] Direct `readState` call sites on the client pull: 27. 25 stay in `StateEndpointTests.fs`. 2 moved with the GET facts.
[ ] Call sites switched from `readFullState` to `readState`: 4
[ ] `readFullState` call sites that stay: 1, the scope-full GET fact
[ ] Facts whose read path is the client pull, including `stateRoot`-only facts: 31

## Check

[ ] `scripts/agent-test.sh --filter` for the named GET and nested facts, then the two state-endpoint modules: Passed 42, Failed 0
[ ] `scripts/agent-test.sh` with no arguments: Passed 42, Failed 0, `tests/Server.Tests`
[ ] `scripts/agent-test.sh --full` Server: Passed 733, Failed 0, Skipped 1, Total 734
[ ] `scripts/agent-test.sh --full` Shared: Passed 1929, Failed 0, Skipped 3, Total 1932
[ ] Log: `/tmp/ambit-full-suite-20261103-161943.log`
