# Code review — 22 One ordered event stream

Range: uncommitted work vs `HEAD` (`git diff HEAD` plus untracked `CorePostedList.fs`, `RunLaunch.fs`, and `OrderedEventStreamTests.fs`). Spec: [22 — One ordered event stream](../issues/22-ordered-event-stream.md).

Ticket **Status** stays `coded`. This report is not approval.

Scan: `python3 .agents/skills/code-review/scripts/standards-scan.py`. Printed `MUTABLE` lines are in tests. [fsharp-source.md](../../../.agents/rules/fsharp-source.md) does not apply that rule to tests, so those lines are not findings. Measured functions are under 40 lines. No line over 100 characters in this diff. [History.fs](../../../src/Shared/History.fs) stays at 800 lines.

Follow-up before this report: `CorePostedList.Host` takes an `Ev` instead of a one-off tuple. Unused command-test helpers are gone. Plan lines name [11 — Server evaluates](../../online-search/issues/11-server-evaluates.md). `POST /ambit/events` tests cover an edit then ActorStart, and an edit then Cancel.

## Standards

No hard violation.

### Judgement

Possible Duplicated Code. `storedSubmission` scans the event log by `submissionId`, then `commitClientBody` scans that log again. `currentGraph` repeats the `Graph.create ()` fallback already in `dispatchActorStartResult`.

## Spec

### (a) Partial

The contract checklist asks a test to show `runSubmitCommand` and `runSubmitCancel` are absent. Those functions are deleted. `POST /ambit/command` and `POST /ambit/cancel` return 404. The Fable client build compiles without the posters. No test asserts those two names.

The events-door checks that the checklist names are present: one list with an edit then ActorStart, and one list with an edit then Cancel, both on `POST /ambit/events`.

### (b) Scope creep

None.

### (c) Wrong implementation

None on the success path. An unregistered actor name still returns HTTP 400 after earlier events in that list are stored. That is the existing `startActor` bookkeeping failure. Credential refusal and a client ActorStop still apply nothing.

### (d) Types the spec did not name

`CoreMsg.PostEvents` is the one mailbox message the spec asked for. The spec did not name that case. `CorePostedList.Host` is the apply seam. The spec did not name that record. `EventBody.Cancel` is the client Cancel the spec named.

Standards: 0 hard, 1 judgement (duplicated submission scan). Spec: 1 partial (no test names the deleted posters), 2 unnamed types (`PostEvents`, `Host`).
