# 143 — Browser accepts server data

**Status:** `defined`

One job: The page takes whatever the server sends and copies it in. New items are added. New text or settings for an item the page has are written. A new list of children for an item the page has replaces the old list. Anything about an item the page does not have is skipped. Taking data never fails, so the page never gets stuck. A section counts as loaded once the page has its list and every item in it ([[plan/core-refinement/reports/missing-changes-reload-findings.md]] §1 Model).

Type: coding

Blockers:

- 141 — Server stores whole child lists
- 142 — View shows a child that has no content yet
- 48 — Browser applies Node events (must be `done`)

## Inputs

### Working

- [[src/Shared/SyncLogic.fs]]
  - `foldProjectedEvents` (118–153): re-runs ops with their checks
  - `applyNoticeGraph` (92–101): returns `Error` when a notice is refused
  - `applySyncResponse`, `applyWantPreservingPending`
  - `applyLoadResponse` and its "raced Load answer" guard (256–273)
  - `rewindPendingBeforeWant`, `undoPendingGraph`, `reapplyLeftoverPending`
  - `consumeCatchUpPoll`, `reconcileExternalAck`
  - `reconcileAck` and its `projectSuffixes` step
  - `applyServerTail`
- [[src/Shared/ResidentProjection.fs]]
  - `applyOpAllowing` (8–34): skips a `Replace` on an Unloaded parent
  - `applyOpForSyncAllowing`, `applyOpsForSyncAllowing`, `casUserMessage`: the old-value compare and the undo-all-pending path
  - `installWantAnswer` (207–228): the "dangling edge" refusal
- [[src/Shared/Model.fs]]: `GraphChildren.isLoaded`, `GraphChildren.status` (265–269). Today they mean "key present".
- [[src/Shared/ViewModelSync.fs]]: `CatchUpBaseline`, `SyncInfo.catchUp`
- [[src/Shared/Want.fs]]: `compose` reads `isLoaded`
- [[src/Client/Update.fs]]: `PollDone`, `LoadDone`, `reconcileSubmit`, `applyIgnoredSubmitAnswer`, `finishAppliedSubmit`; the apply-`Error` arms left by 140
- [[src/Client/UpdateWorkspaceSync.fs]]: its `reconcileAck` and `reconcileExternalAck` calls
- [[src/Client/SyncAnswer.fs]]
- [[src/Client/Program.fs]]: `applyBootNovel`
- [[src/Client/UpdateActorLive.fs]] (~57)
- [[src/Client/UpdateHelpers.fs]]: `isAutoSyncBlocked`, `adjustModeAfterServerApply` (286–298)
- Tests:
  - [[tests/Shared.Tests/SyncLogicTests.fs]]
  - [[tests/Shared.Tests/AckReconcileTests.fs]]
  - [[tests/Shared.Tests/SplitOriginTraceTests.fs]]
  - [[tests/Shared.Tests/ClientHistoryRuntimeTests.fs]]
  - [[tests/Shared.Tests/BrowserNodeEventTests.fs]]
  - [[tests/Shared.Tests/BootCachePollTests.fs]]
  - [[tests/Server.Tests/BrowserResidencyClientTests.fs]]

### Reference

- [[plan/core-refinement/reports/missing-changes-reload-findings.md]] §1–§4 and Q1–Q5
- [[src/Shared/NoticeGraph.fs]]: the shared notice rule
- [[plan/core-refinement/spec.md]] §2

## Do not load

- the page's own edit path: `applyLocalEvent`, `applyLocalUndo`, `applyLocalRedo`, and their `GraphMutate` checks (these stay)
- server code
- view rendering (142)

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Expand `SyncLogic.acceptOp : Op -> Graph -> Graph`. It never fails:
   - `NewNode` and `NewSpecialNode`: add the node with an empty child list. An id the page already has is overwritten (Q4).
   - `SetText`, `SetClasses`, `SetName`, `SetUpdateTime`, `SetDocumentState`, when the node is present: write the new value and ignore the old one. A special node's text follows its name.
   - `Replace (parent, _, newList)`, when the parent is present: set `childMap[parent] = newList`, then rebuild the indexes.
   - Any op on an absent node: no change.
3. [ ] Expand `SyncLogic.acceptNotice : Notice -> Graph -> Graph`. Run `NoticeGraph.writeGraph`. An absent node or an `Error` leaves the graph unchanged (Q1).
4. [ ] Change `foldProjectedEvents`. For each event after the page's id, in id order:
   - ops through `acceptOp`
   - notices through `acceptNotice`
   - set `eventId`
   - keep the actor live focus
   - remove the `Result`
5. [ ] Change `installWantAnswer` to write both maps and return `Graph`. Contract the dangling-edge refusal.
6. [ ] Change `GraphChildren.isLoaded` and `status`: loaded when the key is present and every listed child id is in `nodes`.
7. [ ] Contract the pending rewind: `rewindPendingBeforeWant`, `undoPendingGraph`, `reapplyLeftoverPending`, `consumeCatchUpPoll`, `CatchUpBaseline`, and `SyncInfo.catchUp`. Server data goes onto the current graph (§4). An external-changes post reply starts an ordinary poll.
8. [ ] Contract `applyLoadResponse`'s guard. Load uses `applySyncResponse`.
9. [ ] Change `reconcileAck`. Keep the batch identity checks (they lead to `ServerRejected`). Replace `projectSuffixes` with accepting the reply's confirmed events.
10. [ ] Change the callers to the non-failing results: `Update.fs` arms, `SyncAnswer`, `applyBootNovel`, `UpdateActorLive`, `UpdateWorkspaceSync`. Contract the apply-`Error` arms that 140 left.
11. [ ] Change the dirty-edit rule:
    - contract `isAutoSyncBlocked` from `PollDone` and `LoadDone`, and accept as usual
    - change `adjustModeAfterServerApply` to stay in Editing and leave the edit field as it is
    - commit already builds `SetText` from the live text, so the person typing wins
12. [ ] Change or delete the tests that expect the removed errors ("raced Load answer", "dangling edge", suffix-projection `Rejected`). Expand the tests in Outputs.

## Outputs

- Changed files: each file under Working.
- Added tests:
  - `Shared`: ``Accept adds a created node``
  - `Shared`: ``Accept writes data for a node the page has``
  - `Shared`: ``Accept replaces the child list of a node the page has``
  - `Shared`: ``Accept ignores data and children for a node the page does not have``
  - `Shared`: ``Accept keeps a listed child with no content and the parent is not loaded``
  - `Shared`: ``Loaded needs the child list and content for each child``
  - `Shared`: ``Want asks for a parent whose listed child has no content``
  - `Shared`: ``A refused notice leaves the graph unchanged``
  - `Server`: ``Poll during a dirty edit applies the tail and keeps the edit field``
- Changed or deleted tests: the "raced Load answer" tests, the `installWantAnswer` dangling-edge tests, and the `AckReconcileTests` that expect a suffix `Rejected`. Each one is named in the check report.

## Stop when

[ ] Record the stop at `plan/core-refinement/reports/143-browser-accepts-server-data-stop.md`, then ask.

- the fix needs a file outside Inputs
- a server event type does not map to one of the four cases ([[plan/core-refinement/reports/missing-changes-reload-findings.md]] §3)
- Alan has not answered Q1, Q4, or Q5, and a test needs the answer
- the page's own edit checks would have to change

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `Shared`: ``Accept adds a created node``
  - `Shared`: ``Accept writes data for a node the page has``
  - `Shared`: ``Accept replaces the child list of a node the page has``
  - `Shared`: ``Accept ignores data and children for a node the page does not have``
  - `Shared`: ``Accept keeps a listed child with no content and the parent is not loaded``
  - `Shared`: ``Loaded needs the child list and content for each child``
  - `Shared`: ``Want asks for a parent whose listed child has no content``
  - `Shared`: ``A refused notice leaves the graph unchanged``
  - `Server`: ``Poll during a dirty edit applies the tail and keeps the edit field``
[ ] `git grep -n -e "raced Load answer" -e "dangling edge" -e consumeCatchUpPoll -- src` prints nothing.
[ ] All executed tests pass.
[ ] Write the verdict to `plan/core-refinement/reports/143-browser-accepts-server-data-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
