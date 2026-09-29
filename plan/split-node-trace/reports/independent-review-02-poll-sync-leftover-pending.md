# Independent review — leftover pending re-apply

Reviewer did not write the implementation. This report is not approval. No ticket Status changes.

**Range:** `origin/staging...8f39c76d`. Base `origin/staging` `01827839`. Command: `git diff origin/staging...8f39c76d`. Diff is non-empty (10 files, +285 / −50). Commits: `6104c299` Chart Case B leftover pending re-apply after Poll/sync play. `8f39c76d` Re-apply leftover pending after Poll/sync rewind and play.

**Spec:** [02 — Poll/sync leftover pending: re-apply for visibility](plan/split-node-trace/issues/02-poll-sync-leftover-pending-reapply.md). Locked Case B (Alan, 2026-09-29). [Chart Case B Poll/sync leftover pending re-apply](https://github.com/abaljeu/ambit/pull/173) is open and unmerged. [Re-apply leftover pending after Poll/sync play](https://github.com/abaljeu/ambit/pull/174) carries that plan. The plan delta from the chart tip is Status `coded`, the time log, and the coded comment.

Mechanical scan: `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` from a worktree at `8f39c76d`. `applyPendingEvent` is 7 lines. `reapplyLeftoverPending` is 13 lines. Both are under the 40-line limit in [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md). No file-limit hit.

## Standards

### 1. Bare issue id on the edited project line (hard)

[.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md) says every issue reference carries its number and its name. The edited sentence in [Event-sourced ops](plan/event-sourced-ops/project.md) still says `former issue 07` and links [01 — Generalized Server Actor produce path](plan/core-creation/issues/01-generalized-server-actor-produce-path.md) without that title beside the old number. The same sentence names [16 — Fix pending+merged-events: undo-then-apply instead of DataOutdated reload](plan/event-sourced-ops/issues/16-fix-pending-merged-events-undo-then-apply.md), [17 — Instrument apply-error → DataOutdated with op type and mismatch reason](plan/event-sourced-ops/issues/17-instrument-apply-error-dataoutdated.md), [01 — Poll/sync recoverable mismatch: undo all pending, then apply Server merge](plan/split-node-trace/issues/01-poll-sync-cas-undo-all-pending-apply-merge.md), and [02 — Poll/sync leftover pending: re-apply for visibility](plan/split-node-trace/issues/02-poll-sync-leftover-pending-reapply.md) with number and name.

No smell to stand behind.

## Spec

### 1. PollDone catch-up re-applies the posted prefix when the poll has a Want answer (wrong)

Spec: "Do not re-apply that prefix on top of a Server list that already contains it."

[consumeCatchUpPoll](src/Shared/SyncLogic.fs) re-applies leftover pending with ordinary apply and skips a pending Change whose submission id is in the played Server list. The PollDone catch-up arm in [Update.fs](src/Client/Update.fs) then calls [SyncAnswer.apply](src/Client/SyncAnswer.fs). That helper strips `events` and calls [applySyncResponse](src/Shared/SyncLogic.fs). The Want `nodes` and `childMap` stay. [rewindPendingBeforeWant](src/Shared/SyncLogic.fs) undoes the whole pending list when that payload is present and pending is non-empty. The new gate (`undidPending || didRewind`) re-applies with an empty played list, so the posted prefix is eligible again.

Poll and post responses carry that Want answer from [Api.fs](src/Server/Api.fs). [Want.compose](src/Shared/Want.fs) fills the request whenever the included walk still has an unloaded Node. An empty Want answer leaves the [consumeCatchUpPoll](src/Shared/SyncLogic.fs) graph in place. The Shared tests use empty `nodes` and empty `childMap`, so they stay on that empty path.

### 2. SubmitResponse re-applies when the acknowledgement has a Want answer (wrong)

Spec: "Do not re-apply on that acknowledgement." Also: "The live Graph stays optimistic until catch-up play. Do not re-apply on that acknowledgement."

[finishAppliedSubmit](src/Client/Update.fs) calls [applyChangeSuccess](src/Client/SyncAnswer.fs). That is the same event-stripped [applySyncResponse](src/Shared/SyncLogic.fs). A non-empty Want answer plus leftover pending sets `didRewind`. The gate re-applies on the acknowledgement. The acknowledgement still does not play the Server event list.

## Independent checks

These were asked for on this review. They confirm or extend the Spec findings.

**Catch-up play.** [consumeCatchUpPoll](src/Shared/SyncLogic.fs) rewinds to the baseline, plays the Server list, then re-applies leftover pending with [applyOps](src/Shared/ResidentProjection.fs). It leaves `pending` as it found it. A played submission id is skipped. That matches Case B at this Shared seam.

**Sync apply after undo.** [applySyncResponse](src/Shared/SyncLogic.fs) re-applies only when the fold note is present (the [undoAllPending](src/Shared/ResidentProjection.fs) path) or when [rewindPendingBeforeWant](src/Shared/SyncLogic.fs) ran. A direct poll arm that passes the Server list keeps those played ids. That part matches the ticket.

**Queue.** [consumeCatchUpPoll](src/Shared/SyncLogic.fs) does not assign `pending`. PollDone writes the model from `syncInfo`, which still holds the queue.

**Client amend.** The F# diff does not call [ChangeAmendment](src/Shared/ChangeAmendment.fs). Re-apply uses ordinary apply. The Server amends on a later post. That matches the lock.

**Acknowledgement.** [SubmitResponse](src/Client/Update.fs) notes the baseline through [reconcileExternalAck](src/Shared/SyncLogic.fs) and does not play the Server list. It re-applies when the Want answer is present. That is Spec finding 2.

**Tests and the ticket 01 correction.** [SyncLogicTests](tests/Shared.Tests/SyncLogicTests.fs) has the Case B catch-up fact, the precondition-undo fact, and the Want-rewind fact. The split Want fact in [SplitOriginTraceTests](tests/Shared.Tests/SplitOriginTraceTests.fs) now expects the pending split to show again. [01 — Poll/sync recoverable mismatch: undo all pending, then apply Server merge](plan/split-node-trace/issues/01-poll-sync-cas-undo-all-pending-apply-merge.md) corrects the edit-box overspeak and stays Status `done`.

The Case B SetText facts stay green if the prefix is re-applied. SetText from the old text fails on the Server text, so the prefix filter is not what those facts lock.

## Verdict

**Needs work.**

### Must-fix

1. Keep event-stripped [applySyncResponse](src/Shared/SyncLogic.fs) off this re-apply. After [consumeCatchUpPoll](src/Shared/SyncLogic.fs), the PollDone arm must keep that graph when it installs a Want answer, and the posted prefix must stay off the Graph. [SubmitResponse](src/Client/Update.fs) and [finishAppliedSubmit](src/Client/Update.fs) stay a signal plus a baseline. They must leave leftover pending un-applied on that acknowledgement.

### Should-fix

2. Add a fact that runs catch-up play and then [SyncAnswer.apply](src/Client/SyncAnswer.fs) with a non-empty `childMap`. Add a fact that an acknowledgement with a Want answer and leftover pending leaves the Graph without this re-apply.
3. On the edited line in [Event-sourced ops](plan/event-sourced-ops/project.md), name the moved issue: [01 — Generalized Server Actor produce path](plan/core-creation/issues/01-generalized-server-actor-produce-path.md).

## Summary

Standards: 1 finding. Worst: bare `issue 07` on the edited project line. Spec: 2 findings. Worst: the acknowledgement re-applies leftover pending when the Want answer is present.
