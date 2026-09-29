# 02 — Poll/sync leftover pending: re-apply for visibility

**Type:** coding
**Status:** done
**Actual:** 2h
**Blocked by:** None — can start immediately

## Context

Case B (Alan, 2026-09-29). The Client posts N Changes. While those posts are in flight, the person makes M more Changes locally. Another Actor lands work. The Server merge list arrives on the queue-empty Poll catch-up. The post acknowledgement stays a signal plus a baseline. It does not carry the list.

Correct resolution:

1. Rewind the in-flight posted optimistic prefix, and the catch-up baseline, as today.
2. Play the Server merge list.
3. The leftover trailing pending (the M) stay in the pending queue. The next post sends them as constructed. The Server amends them with [ChangeAmendment](src/Shared/ChangeAmendment.fs). The Client does not replan or amend that leftover.
4. Also re-apply that leftover pending onto the Client Graph after the rewind and the play, so the person still sees those edits.

[01 — Poll/sync recoverable mismatch: undo all pending, then apply Server merge](01-poll-sync-cas-undo-all-pending-apply-merge.md) said undo all pending, then apply the Server merge, and that only an edit-box draft stays local. That overspoke. Undo is how the Graph converges on the Server list. It does not drop leftover pending from the queue. This ticket is the visibility re-apply.

Accepted consume is [Client correction — rewind and replay](plan/event-sourced-ops/details/client-consume.md): leftover pending stays unamended, and after rewind and replay the Client re-applies it with ordinary apply. [Messaging — post, poll, and what a Reject means](plan/event-sourced-ops/details/messaging.md) keeps the two channels: the post acknowledgement signals and notes the baseline; Poll plays the list.

Today [consumeCatchUpPoll](src/Shared/SyncLogic.fs) sets the Graph to the baseline and plays the Server list. The pending queue is not cleared there, and the play does not put the trailing pending back on the Graph. [reconcileExternalAck](src/Shared/SyncLogic.fs) builds a missing baseline with [undoPendingGraph](src/Shared/SyncLogic.fs) over the whole pending list, then [retireSubmittedPrefix](src/Shared/SyncPlanner.fs) removes the posted prefix. The live Graph stays optimistic until catch-up play. Do not re-apply on that acknowledgement. The person still sees the edits until play replaces the Graph.

[finishAppliedSubmit](src/Client/Update.fs) starts that catch-up Poll only when the pending queue is empty. [16 — Fix pending+merged-events: undo-then-apply instead of DataOutdated reload](plan/event-sourced-ops/issues/16-fix-pending-merged-events-undo-then-apply.md) is the gate that lets catch-up run while leftover pending remains. This ticket does not wait on that gate. The play seam can be proved directly, and other apply paths already take pending effects off the Graph.

## What to build

After rewind and play, leftover trailing pending are still in the pending queue, and the Graph shows their effects again. Re-apply uses ordinary apply. The Client does not amend.

### 1. Re-apply after catch-up play

The pending queue is [SyncInfo.pending](src/Shared/ViewModelSync.fs). Apply copies it onto [ClientSyncState.pending](src/Shared/ActorLive.fs). Re-apply the trailing pending that remain after the posted prefix is retired. Do not re-apply that prefix on top of a Server list that already contains it.

1. [x] 1.1 After rewind and play — When [consumeCatchUpPoll](src/Shared/SyncLogic.fs) finishes, and on the PollDone arm in [Update.fs](src/Client/Update.fs) that uses it, re-apply leftover pending in queue order onto the post-play Graph.
2. [x] 1.2 Queue stays — Leave that leftover in the pending queue. The next post sends those Changes as constructed.

### 2. Other paths that undo pending off the Graph

Re-apply at the end of any sync apply that takes pending effects off the live Graph and leaves those pending in the queue. Do this once, on the post-play Graph, not between ops inside the fold.

1. [x] 2.1 End of [applySyncResponse](src/Shared/SyncLogic.fs) — [applyOpForSync](src/Shared/ResidentProjection.fs) calls [undoAllPending](src/Shared/ResidentProjection.fs) when a Poll/sync apply precondition fails, then applies the Server payload. [rewindPendingBeforeWant](src/Shared/SyncLogic.fs) calls [undoPendingGraph](src/Shared/SyncLogic.fs) before Want install. After that Server list or Want install is on the Graph, re-apply the leftover pending in queue order.
2. [x] 2.2 Post ack stays a signal — [SubmitResponse](src/Client/Update.fs) and [finishAppliedSubmit](src/Client/Update.fs) note the baseline. They do not play the Server list. Do not re-apply on that acknowledgement.

### 3. Ordinary apply on the Client, amend on the Server

1. [x] 3.1 Ordinary apply — Re-apply with [applyOp](src/Shared/ResidentProjection.fs) / [applyOps](src/Shared/ResidentProjection.fs). Do not call [ChangeAmendment](src/Shared/ChangeAmendment.fs) on the Client for this leftover.
2. [x] 3.2 Server amend later — The next post sends the leftover as constructed. The Server amends with [ChangeAmendment](src/Shared/ChangeAmendment.fs).

### 4. Tests

1. [x] 4.1 Case B — In [SyncLogicTests](tests/Shared.Tests/SyncLogicTests.fs), seed a submitted pending prefix plus trailing pending. After catch-up play, the trailing pending is still in the queue, and the Graph shows the trailing effects again.

## Open

If ordinary apply of one leftover op fails, leave that op in the pending queue for the Server on the next post. The Graph may omit that op until the Server answer returns. That visibility gap is accepted for this ticket. Do not add a Client [ChangeAmendment](src/Shared/ChangeAmendment.fs) here. A Client amend is open only if a later proof shows ordinary apply cannot leave the gap for the Server to close.

## See also

[Client correction — rewind and replay](plan/event-sourced-ops/details/client-consume.md), [Messaging — post, poll, and what a Reject means](plan/event-sourced-ops/details/messaging.md), [01 — Poll/sync recoverable mismatch: undo all pending, then apply Server merge](01-poll-sync-cas-undo-all-pending-apply-merge.md), [16 — Fix pending+merged-events: undo-then-apply instead of DataOutdated reload](plan/event-sourced-ops/issues/16-fix-pending-merged-events-undo-then-apply.md), [applySyncResponse](src/Shared/SyncLogic.fs), [consumeCatchUpPoll](src/Shared/SyncLogic.fs), [undoPendingGraph](src/Shared/SyncLogic.fs), [SubmitResponse](src/Client/Update.fs), [finishAppliedSubmit](src/Client/Update.fs), [applyOpForSync](src/Shared/ResidentProjection.fs), [undoAllPending](src/Shared/ResidentProjection.fs), [ChangeAmendment](src/Shared/ChangeAmendment.fs)

## Comments

- 2026-09-29 — Charted from Alan’s locked Case B policy. Undo converges the Graph on the Server list. Leftover trailing pending stay in the queue and post as constructed. Re-apply them with ordinary apply after rewind and play so the person still sees those edits. The Client does not amend. Status `defined`.
- 2026-09-29 — Alan accepted the chart. Implemented re-apply in [consumeCatchUpPoll](src/Shared/SyncLogic.fs) and at the end of [applySyncResponse](src/Shared/SyncLogic.fs) after a precondition undo or Want rewind. Ordinary apply skips an op that does not fit. The queue is unchanged. The post acknowledgement does not play the list. Status `coded`.
- 2026-09-29 — Must-fix from the independent review of this ticket. Event-stripped [SyncAnswer.apply](src/Client/SyncAnswer.fs) and the post acknowledgement install a Want answer through [applyWantPreservingPending](src/Shared/SyncLogic.fs). They do not rewind pending and do not re-apply it. After catch-up play, the posted prefix stays off the Graph. The acknowledgement leaves the Graph optimistic. Status stays `coded`.
- 2026-09-29 — Independent re-review at `23ac7480` is Good. Alan accepted the staging land. Status `done`.

## Time

- 2026-09-29 30m — charted coding ticket from locked Case B policy (from chat)
- 2026-09-29 1h — re-apply leftover pending after catch-up play and sync undo (from chat)
- 2026-09-29 30m — keep event-stripped Want install off leftover re-apply (from chat)
