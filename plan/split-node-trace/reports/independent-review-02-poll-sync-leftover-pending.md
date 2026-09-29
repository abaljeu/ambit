# Independent review — leftover pending re-apply

Reviewer did not write the implementation. This report is not approval. [02 — Poll/sync leftover pending: re-apply for visibility](plan/split-node-trace/issues/02-poll-sync-leftover-pending-reapply.md) stays Status `coded`.

Re-review of tip `23ac7480`. The prior pass at `8f39c76d` was Needs work. This text replaces that verdict.

**Range:** `origin/staging...23ac7480`. Base `origin/staging` `01827839`. Command: `git diff origin/staging...23ac7480`. Diff is non-empty (11 files, +411 / −52). Commits: `6104c299` Chart Case B leftover pending re-apply after Poll/sync play. `8f39c76d` Re-apply leftover pending after Poll/sync rewind and play. `23ac7480` Keep event-stripped Want install off leftover re-apply.

**Spec:** [02 — Poll/sync leftover pending: re-apply for visibility](plan/split-node-trace/issues/02-poll-sync-leftover-pending-reapply.md). Locked Case B (Alan, 2026-09-29). [Chart Case B Poll/sync leftover pending re-apply](https://github.com/abaljeu/ambit/pull/173) is open and unmerged. [Re-apply leftover pending after Poll/sync play](https://github.com/abaljeu/ambit/pull/174) carries that plan.

Mechanical scan: `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` from a worktree at `23ac7480`. `applyPendingEvent` is 7 lines. `reapplyLeftoverPending` is 13 lines. `applyWantPreservingPending` is 8 lines. All three are under the 40-line limit in [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md). [SyncLogic.fs](src/Shared/SyncLogic.fs) stays under the 800-line file limit. No file-limit hit.

Focused facts at this tip: `dotnet test tests/Shared.Tests/Gambol.Shared.Tests.fsproj --filter "FullyQualifiedName~want install after catch-up|FullyQualifiedName~acknowledgement want install|FullyQualifiedName~keeps trailing pending|FullyQualifiedName~precondition undo|FullyQualifiedName~re-applies pending after Want|FullyQualifiedName~stale Want re-applies"`. Passed 6, failed 0.

## Standards

### 1. Bare issue id on the edited project line (hard)

[.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md) says every issue reference carries its number and its name. The edited sentence in [Event-sourced ops](plan/event-sourced-ops/project.md) still says `former issue 07` and links the destination file without that title beside the old number. The same sentence names [16 — Fix pending+merged-events: undo-then-apply instead of DataOutdated reload](plan/event-sourced-ops/issues/16-fix-pending-merged-events-undo-then-apply.md), [17 — Instrument apply-error → DataOutdated with op type and mismatch reason](plan/event-sourced-ops/issues/17-instrument-apply-error-dataoutdated.md), [01 — Poll/sync recoverable mismatch: undo all pending, then apply Server merge](plan/split-node-trace/issues/01-poll-sync-cas-undo-all-pending-apply-merge.md), and [02 — Poll/sync leftover pending: re-apply for visibility](plan/split-node-trace/issues/02-poll-sync-leftover-pending-reapply.md) with number and name. Commit `23ac7480` does not change this clause.

No smell to stand behind.

## Spec

No findings. The prior Must-fix is closed.

[SyncAnswer.apply](src/Client/SyncAnswer.fs) and [applyChangeSuccess](src/Client/SyncAnswer.fs) install the Want answer through [applyWantPreservingPending](src/Shared/SyncLogic.fs). That helper calls [graphAfterWant](src/Shared/SyncLogic.fs) on the current Graph. It does not rewind pending. It does not re-apply leftover. The PollDone catch-up arm still plays the Server list in [consumeCatchUpPoll](src/Shared/SyncLogic.fs), then installs Want through [SyncAnswer.apply](src/Client/SyncAnswer.fs). [finishAppliedSubmit](src/Client/Update.fs) and the workspace acknowledgement arms call [applyChangeSuccess](src/Client/SyncAnswer.fs). The acknowledgement stays a signal plus a baseline.

## Independent checks

**Prior Must-fix, catch-up plus Want.** After play, [applyWantPreservingPending](src/Shared/SyncLogic.fs) merges the Want answer onto the post-play Graph. The played submission stays out of the re-apply set inside [consumeCatchUpPoll](src/Shared/SyncLogic.fs). The new fact `want install after catch-up keeps the Server prefix off the Graph` plays a Server text Change, then installs a Want package. The trailing text is `b-local`. The prefix `NewNode` is absent. The queue still holds the prefix and the trailing Change.

**Prior Must-fix, acknowledgement.** `acknowledgement want install leaves leftover pending un-applied` keeps optimistic text `local`, leaves the pending `NewNode` out of the Graph, and leaves that Change in the queue. A rewind followed by re-apply would have inserted that Node.

**Catch-up, precondition undo, and direct Want rewind.** Those three paths still re-apply inside [consumeCatchUpPoll](src/Shared/SyncLogic.fs) and at the end of [applySyncResponse](src/Shared/SyncLogic.fs). Commit `23ac7480` does not change that gate. The empty-event catch-up arm in [Update.fs](src/Client/Update.fs) still calls [applySyncResponse](src/Shared/SyncLogic.fs). That is the direct Want-rewind path, not the acknowledgement.

**Queue, ordinary apply, ticket 01.** [consumeCatchUpPoll](src/Shared/SyncLogic.fs) does not assign `pending`. The F# diff does not call [ChangeAmendment](src/Shared/ChangeAmendment.fs). [01 — Poll/sync recoverable mismatch: undo all pending, then apply Server merge](plan/split-node-trace/issues/01-poll-sync-cas-undo-all-pending-apply-merge.md) stays Status `done`.

## Verdict

**Good.**

No residual Must-fix.

### Should-fix

1. On the edited line in [Event-sourced ops](plan/event-sourced-ops/project.md), name the moved issue: [01 — Generalized Server Actor produce path](plan/core-creation/issues/01-generalized-server-actor-produce-path.md).

## Summary

Standards: 1 finding. Worst: bare `issue 07` on the edited project line. Spec: 0 findings. The prior Must-fix is closed.
