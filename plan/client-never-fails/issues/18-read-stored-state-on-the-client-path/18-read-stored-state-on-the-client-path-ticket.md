# 18 — Read stored state on the client path
**Status**: coded
One job: The Server test harness reads a stored Header the way the Browser does. A default GET of `/{file}/state` is followed by Poll, with `Want.compose` for Absent child ids, `ResidentProjection.installWantAnswer`, and another Poll until that Want is empty. GET state facts live in their own file. Change submission stays in `StateEndpointTests.fs`. Alan's decision of 2026-11-03 is the reason. The return in [06 — Server sends each Node with its child list](plan/client-never-fails/issues/06-server-sends-child-list/06-server-sends-child-list-ticket.md) is what the read must follow. This sweep changes more than four tests, past the usual count in [Size limit](plan/context/size-limit.md), and Alan approved that sweep on 2026-11-03.

Type: bug-fixing
Blockers: none

## Inputs

### Working

[x] `readState` in `tests/Server.Tests/StateEndpointTests.fs` is `readFullState`, so every caller reads `?scope=full`.
[x] `getStateJson` is the default GET `/ambit/state`. `readClosure` decodes that body to an event id and a Graph.
[x] `getStateJsonFull` and `readFullState` are the `?scope=full` GET.
[x] `postPoll` posts `/ambit/poll` with `want = []`.
[x] `decodePoll` decodes a `ChangeSuccessResponse`, including `nodes` and `childMap`.
[x] `Want.compose` in `src/Shared/Want.fs` keeps ids where `GraphChildren.isLoaded` is false.
[x] `ViewModel.buildSiteMapFrom` expands the zoom root and leaves its children folded.
[x] `ResidentProjection.installWantAnswer` merges a Poll answer's Headers and child-id lists into the Graph.
[x] `withClient` and `addNestedWorkspace` in `tests/Server.Tests/StateEndpointTests.fs` are the harness the GET facts use.
[x] ``GET state returns revision 0 for fresh server`` calls `readState`.
[x] ``GET state returns valid graph with root node`` calls `readState`.
[x] ``GET state default returns ROOT closure excluding nested workspace contents`` calls `readClosure`.
[x] ``GET state scope full returns canonical graph for tests`` calls `readFullState`.
[x] These four read-backs call `readFullState` and assert stored nested Headers, not the `scope` query:

- ``POST concurrent stale text Changes amend second as amb-conflict child``
- ``POST concurrent stale name Changes amend second as amb-conflict child``
- ``POST unrelated structural edits with stale revision both succeed``
- ``POST same-parent structural collision amends and succeeds``

[x] `tests/Server.Tests/Gambol.Server.Tests.fsproj` compiles `StateEndpointTests.fs`.
[x] `userRootChildren` and `userTreeShape` in `tests/Shared.Tests/SpecialNodeTestHelpers.fs` index `graph.nodes`. An Absent child throws.

### Reference

[x] Scope notes in [18 — Read stored state on the client path — scope](plan/client-never-fails/reports/18-read-stored-state-on-the-client-path-scope.md)

## Do not load

[x] Closed: `src/`. The Browser boot, Poll, `Want.compose`, and `installWantAnswer` already implement this path.
[x] Closed: `AmbitSession.getFullState` and `WorkspaceCloudUpload.fs`. The Desktop upload path stays on `?scope=full`.
[x] Closed: `GLOSSARY.md`, the spec, and the architecture.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Change `readState` so it uses `getStateJson`, then repeats Poll. Each round builds a SiteMap with `ViewModel.buildSiteMapFrom` at `graph.root`, takes `Want.compose` for that SiteMap, posts that Want, and installs the answer with `ResidentProjection.installWantAnswer`. Stop when the Want is empty. Stop after 5 rounds when the Want is still non-empty, and fail the test. Return the GET event id and the installed Graph. Leave `postPoll` on `want = []`.
3. [x] Change the four `readFullState` read-backs named in Inputs to `readState`. Each one asserts a nested stored Header. None asserts the `?scope=full` query.
4. [x] Move the four ``GET state …`` facts into `tests/Server.Tests/StateEndpointGetTests.fs`. Keep each assertion. ``GET state default returns ROOT closure excluding nested workspace contents`` stays on `readClosure`. ``GET state scope full returns canonical graph for tests`` stays on `readFullState`.
5. [x] Mark `readState`, `readClosure`, `readFullState`, `withClient`, and `addNestedWorkspace` internal so the new file can call them.
6. [x] Register `StateEndpointGetTests.fs` in `tests/Server.Tests/Gambol.Server.Tests.fsproj` after `StateEndpointTests.fs`.

## Outputs

[x] `tests/Server.Tests/StateEndpointTests.fs` — client-path `readState`, and the four nested read-backs on that path
[x] `tests/Server.Tests/StateEndpointGetTests.fs` — the four ``GET state …`` facts
[x] `tests/Server.Tests/Gambol.Server.Tests.fsproj` — compile the new file
[x] `plan/client-never-fails/issues/18-read-stored-state-on-the-client-path/18-build.md`

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/18-read-stored-state-on-the-client-path-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs, including any `src/` file.
[ ] Stop when the edit contradicts the return named in the One job line.
[ ] Stop when an assertion must change.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``GET state returns revision 0 for fresh server``
  - `tests/Server.Tests`: ``GET state returns valid graph with root node``
  - `tests/Server.Tests`: ``GET state default returns ROOT closure excluding nested workspace contents``
  - `tests/Server.Tests`: ``GET state scope full returns canonical graph for tests``
  - `tests/Server.Tests`: ``POST concurrent stale text Changes amend second as amb-conflict child``
  - `tests/Server.Tests`: ``POST concurrent stale name Changes amend second as amb-conflict child``
  - `tests/Server.Tests`: ``POST unrelated structural edits with stale revision both succeed``
  - `tests/Server.Tests`: ``POST same-parent structural collision amends and succeeds``
[x] Run `scripts/agent-test.sh --full`. Server and Shared both finish with 0 failures.
[x] All executed tests pass.
[x] Write the verdict to `plan/client-never-fails/reports/18-read-stored-state-on-the-client-path-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
