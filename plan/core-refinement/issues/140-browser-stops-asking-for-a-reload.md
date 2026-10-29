# 140 — Browser stops asking for a reload

**Status:** `defined`

One job: The page stops telling you "Data changed on server — click to reload" or "New version available", and it never stops saving or checking for changes because of those messages. The server sends every change, and the page takes every change ([[plan/core-refinement/spec.md]] §2 item 2), so there is nothing to reload for.

Type: bug-fixing

Blockers: none

## Inputs

### Working

- [[src/Shared/ViewModelSync.fs]]: `SyncState.DataOutdated`, `SyncState.CodeOutdated`, and `SyncInfo.withSyncState` (its `inRisk` match)
- [[src/Shared/SyncLogic.fs]]: `SyncLogic.getPollOutcome`, the only producer of `DataOutdated`. It returns `Some DataOutdated` when the server id is ahead. `CodeOutdated` has no producer.
- [[src/Shared/SyncPlanner.fs]]: `isBlocked`, `retryWaiting`
- [[src/Shared/ViewModel.fs]]: `SystemMsg.PollDone` and `SystemMsg.LoadDone`; their first field carries the `getPollOutcome` result
- [[src/Client/App.fs]]: `runPollServer`, `runLoadServer`, the `sync-status` click handler
- [[src/Client/Program.fs]]: `handleBootPoll` builds `PollDone`
- [[src/Client/Update.fs]]: `applyIgnoredSubmitAnswer`, `finishAppliedSubmit`, `applySubmitResponse`, the `PollDone` arm, the `LoadDone` arm
- [[src/Client/UpdateHelpers.fs]]: `isAutoSyncBlocked`; the comment above `clientSyncState`
- [[src/Client/StatusView.fs]]: the `DataOutdated` and `CodeOutdated` texts
- [[src/Client/Overlays.fs]]: `renderSyncRiskAlert`
- [[doc/current/http-contract.md]] §2.2 items 7–11
- [[tests/Shared.Tests/SyncLogicTests.fs]]: the `getPollOutcome` tests and the `SyncInfo withSyncState …` tests
- [[tests/Server.Tests/BrowserResidencyClientTests.fs]]: `Poll answer chains the next Included residency Want` builds `PollDone`; the new tests go here

### Reference

- [[plan/core-refinement/reports/missing-changes-reload-findings.md]] in this draft folder, §5
- [[.agents/rules/fsharp-source.md]]

## Do not load

- `ServerRejected` and its producers
- [[src/Shared/BootCache.fs]]
- the client apply functions in `SyncLogic` and `ResidentProjection` (ticket 143 owns them)
- server code

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Contract `PollDone` to `SyncResponse * isReady: bool option * responseEventId: EventId option` and `LoadDone` to `SyncResponse * responseEventId: EventId * isReady: bool option`. Change each constructor to match.
3. [ ] Contract `SyncLogic.getPollOutcome` and its two call sites.
4. [ ] Change the `PollDone` and `LoadDone` arms:
   - apply the response
   - an empty tail with a higher server id applies as a no-op
   - a dirty edit field with a tail that is not empty leaves the graph and goes to `Idle`
   - an apply `Error msg` leaves the graph and the event id, goes to `Idle`, and sets `lastCmdResult` to `CmdLastResult.Error (None, "sync apply: " + msg)`
   - ticket 143 later removes these `Error` arms
5. [ ] Change `applyIgnoredSubmitAnswer` and `finishAppliedSubmit` in the same way for an `applyChangeSuccess` `Error`.
6. [ ] Contract `SyncState.DataOutdated` and `SyncState.CodeOutdated`, and each match arm that names them: `withSyncState`, `isBlocked`, `retryWaiting`, `applySubmitResponse`, `StatusView`, `renderSyncRiskAlert`, the `App.fs` click handler, and the `UpdateHelpers` comment.
7. [ ] Contract [[doc/current/http-contract.md]] §2.2 items 7–11 (`baseSequence`, `409`, client rewind and retry). Renumber the items after them.
8. [ ] Change [[tests/Shared.Tests/SyncLogicTests.fs]]:
   - delete the `getPollOutcome` tests
   - remove the `getPollOutcome` assertion from `serverProcessRestarted detects DeployEpochSec change without CodeOutdated`, and rename it ``serverProcessRestarted detects DeployEpochSec change``
   - in the `SyncInfo withSyncState …` tests, use `ServerRejected` as the only risk state
9. [ ] Expand [[tests/Server.Tests/BrowserResidencyClientTests.fs]] with the tests in Outputs.

## Outputs

- Changed files: each file under Working.
- Added tests in [[tests/Server.Tests/BrowserResidencyClientTests.fs]]:
  - `Server`: ``Poll ahead of the page applies the tail and stays Idle``
  - `Server`: ``Poll ahead with an empty tail stays Idle``
  - `Server`: ``Poll tail that does not apply shows the error and stays Idle``
- Changed tests:
  - `Server`: ``Poll answer chains the next Included residency Want``
  - `Shared`: ``serverProcessRestarted detects DeployEpochSec change``
- Report: `plan/core-refinement/reports/140-browser-stops-asking-for-a-reload-check.md`

## Stop when

[ ] Record the stop at `plan/core-refinement/reports/140-browser-stops-asking-for-a-reload-stop.md`, then ask.

- the fix needs a file outside Inputs
- the fix contradicts the Spec
- removing the two states would change `ServerRejected` behavior

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `Server`: ``Poll ahead of the page applies the tail and stays Idle``
  - `Server`: ``Poll ahead with an empty tail stays Idle``
  - `Server`: ``Poll tail that does not apply shows the error and stays Idle``
  - `Server`: ``Poll answer chains the next Included residency Want``
  - `Shared`: ``serverProcessRestarted detects DeployEpochSec change``
[ ] `git grep -n -e DataOutdated -e CodeOutdated -- src tests` prints nothing.
[ ] All executed tests pass.
[ ] Write the verdict to `plan/core-refinement/reports/140-browser-stops-asking-for-a-reload-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
