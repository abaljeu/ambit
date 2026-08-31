# Establish Undo slices 3a and 3b — work report

Status: superseded by the submitted-only Browser History decision in [[undo-implementation-plan.md]] and [[revise-undo-plan-submitted-history.md]]. The scheduling evidence below remains historical, but its same-record selection and History-amendment decisions are no longer planned behavior.

## Changed files

- [[undo-implementation-plan.md]] splits ordered implementation Slice 3 into narrow queue adaptation and batch selection in Slice 3a and runtime History wiring in Slice 3b.
- [[establish-undo-slices-3a-3b.md]] records this work.

## Scheduling evidence

- [[src/Shared/SyncPlanner.fs]] `applyAndEnqueueLocalAction` appends one action and immediately calls `tryStartSubmit`. From Idle, `tryStartSubmit` changes synchronization to `Sending` and emits the current pending list. Direct C then U therefore cannot enter one batch: C is selected before U is enqueued.
- `tryStartSubmit` emits nothing while `Sending`, but local actions still append to `pendingChanges`. If unrelated A is already in flight, rapid C and U can both accumulate behind A.
- On A's ACK, current `ackBatch` removes A and emits every remaining pending action. The sequence A in flight, then queued C and U therefore selects `[C; U]` into one HTTP batch.
- Current HTTP single-flight is necessary and handles the direct case, but it does not cover this accumulated-remainder case.

## Decisions captured

- Slice 3a preserves HTTP single-flight and adds only one planner selection invariant: stop an exact batch before a transition whose `recordId` already occurs in that candidate batch. This is not a general queue dependency subsystem.
- `InFlightBatch` remains as private encapsulation because pending Changes can accumulate while a different batch is in flight, and retry and confirmation must use exact selected membership.
- The relevant dependency is narrow: U was derived from optimistic C before C's complete enriched confirmation, so U waits for C confirmation and re-derivation while keeping U's `changeId`.
- Restored pending Changes keep the established filter, projected apply, save, and resubmit recovery sequence. Only their new queue representation and absence of PendingTransition and Browser History are new.
- Slice 3b owns normal local recording, optimistic Undo and Redo through ResidentProjection, the temporary legacy runtime compile bridge, and exact empty versus non-empty Poll and Load History behavior.
- Only ACK confirmations that match current in-flight transitions amend History. Complete ACK reconciliation remains in Slice 5 after Slice 4 provides complete confirmations. Poll and Load do not match or rebase History.
- Slice 3a remains independently verifiable because it includes queue type conversion, exact in-flight membership, restored-pending adaptation, and the proven accumulated-remainder edge. Later slices remain numbered 4 through 7.

## Verification

- Inspected current queue state, submit selection, ACK scheduling, local Change and Undo enqueueing, and dispatch ordering before revising the plan.
- Re-read the revised architecture, ownership, data-flow, and ordered-slice sections and inspected the focused diff for unrelated edits.
- No build or test was run because this was a Markdown-only planning change.
- [[WORK.md]] was not edited.

## Proposed WORK.md mutation

- `remove` [[undo-implementation-plan.md]] from Active because the requested Slice 3a/3b planning split is verified.
