# 103 — Change reply is the event id

Ticket: [[../issues/103-change-reply-is-the-event-id.md]]
Spec: [[../spec.md]]
Arch: [[../arch.md]]

## 1. Standards

- 1. **Resubmission-report bare id** — Hard. `.agents/rules/refer-by-name.md`: never identify anything only by number; always include a name. `python scripts/standards-scan.py` (Windows current diff) exited 1. The printed `BARE_ID` line is `plan/core-refinement/reports/103-resubmission-id.md` line 15, `Item 2`, without the name `Resubmission id`. `python scripts/standards-scan.py --diff origin/staging` exited 0: that range is `HEAD` against staging, so the uncommitted ticket files are outside it. The scan also printed function sizes; `fromPosted` is 20 lines, `storedEvents` is 20 lines, `acceptedFromIds` is 14 lines. Each sits under the 40-line function limit in `.agents/rules/fsharp-source.md`.
- 2. **Duplicated accepted helper** — Judgement. Smell Duplicated Code. `FileAgent.accepted` and `DbAgent.accepted` both call `CoreChanges.accepted` with `eventId`, `isReady`, `confirmed`, and `externalChanges`.

```fsharp
let private accepted loaded confirmed externalChanges =
    CoreChanges.accepted
        loaded.state.Value.eventId
        ...
        confirmed
        externalChanges
```

## 2. Spec

- 1. **Resubmission laterEvents** — Wrong. [103 — Change reply is the event id](../issues/103-change-reply-is-the-event-id.md) says behavior does not change, and Same ack keeps `eventId`, `events`, and `externalChanges` as before. `CoreChanges.fromPosted` sets `eventId` to the stored Ev's id. On resubmission that id is the original Change. `Api.postEvents` then calls `eventsThroughSnapshot` after that id through the snapshot tip and concatenates the result onto `accepted.events`. An intervening Change therefore appears on the HTTP ack. Before this work, resubmission's `CoreChangesAccepted.eventId` came from `persist.getEventId()` (the tip), so that window was empty.
- 2. **Unstored PostEvent skip** — Extra. `CoreMailboxActors.postCancel` of `Ok None` and `postActorStart` of `Ok (None, _)` reply the draft Ev's id. `CoreMailboxApi.storedEvents` skips an id that is not accepted. `fromPosted` then returns `EventId.zero` and an empty `events` list. A Cancel or ActorStart that stores nothing used to ack the current tip and an empty list. The ticket leaves that skip unnamed.
- 3. **ActorStart expand** — Extra. `CoreMailboxApi.expandStored` appends a following `ActorStop` onto a stored `ActorStart`. The ticket names Change rebuild from the replied id. The pairing keeps a failed start's start-and-stop list on `postEvents`.

## 3. Arch

- No findings.

## 4. Tests

- 1. **Full suite** — `scripts/agent-test.sh --full` wrote `/tmp/ambit-full-suite-20261024-114401.log`. `scripts/check-test.sh 48438` printed `DONE` and that log path. Server.Tests passed 756, failed 1, skipped 0, total 757, duration 3 m 52 s. The failed test is `Gambol.Server.Tests.StateEndpointTests.POST duplicate submissionId with stale revision stays idempotent`. Shared.Tests did not run: the script stopped after the server failure.
- 2. **Same ack** — `postEvents returns the same CoreChangesAccepted from the event id` is in `tests/Server.Tests/CoreMailboxDoorTests.fs`. It posts a plain Change, an amended SetText after a later edit, and a batch of two. `assertAckFromLog` checks `eventId`, `events`, and `externalChanges` against the log. It does not post a resubmission after an intervening Change. It passed inside the server run.
- 3. **Resubmission** — `a resubmitted Change replies the stored id` is in the same file. A second `postEvents` of the same Ev equals the first `eventId` and `events`, and `externalChanges` is false. There is no later post between the two. It passed inside the server run.
- 4. **Still working** — The ticket names `upload, import, parse, Load, undo, and persist complete`. There is no test with that name. The full suite is the check, and the suite failed.
- 5. **HTTP idempotent** — `POST duplicate submissionId with stale revision stays idempotent` in `tests/Server.Tests/StateEndpointTests.fs` posts Change `cid`, then an intervening `other` Change, then `cid` again. It expects the second HTTP ack's Change submission ids to be `[ cid ]`. Actual was `[ cid, other ]`. `decodeAckChangeIds` reads `changesOnly` of the HTTP `events` list, which is `accepted.events @ laterEvents`.

## 5. Miss

- 1. **Resubmission-report bare id** — Hard. In `plan/core-refinement/reports/103-resubmission-id.md` line 15, write the name with the item: `Resubmission id`.
- 2. **Resubmission laterEvents** — Wrong. A resubmitted Change after a later post must keep the HTTP ack's Change list to that resubmitted Ev. `fromPosted` and `eventsThroughSnapshot` together currently add the later Changes.
- 3. **HTTP idempotent test** — Failing. `POST duplicate submissionId with stale revision stays idempotent` failed in the full suite.

## 6. Found elsewhere

The following issues were found beyond the ticket scope.  The fixes are suggested.

- 1. **Whole ticket coded** — Accept. [Implement issue 103](0fe5f913-9a2d-4c43-a40b-c102f0be299a) coded every What to build item after the user pointed at line 25 **Core reply**.
  - **Problem.** The user expected only `CoreMsg.PostEvent` and `CoreMsg.PostGraphOnly` to reply `Result<EventId, string>`. The coded tree also rebuilds `CoreChangesAccepted` at `CoreMailboxApi`, removes `message`, and adds the two named tests.
  - **Fix.** Leave that coded work. Do not revert. The user decides whether items 4 and 5 stay in this commit.
- 2. **Unrelated working-tree files** — Reject. These files stay as they are.
  - **Problem.** Beside [103 — Change reply is the event id](../issues/103-change-reply-is-the-event-id.md), the tree also carries edits in `AGENTS.md`, `.agents/rules/all-agents.md`, `.agents/rules/main-agent-behavior.md`, and untracked `scripts/_tmp_retro_parse.py`.
  - **Fix.** Fold those files into this ticket.
  - **Reason.** They sit outside the ticket. Desktop leaves the commit to the user. The retro parse script belongs to a separate retro.
