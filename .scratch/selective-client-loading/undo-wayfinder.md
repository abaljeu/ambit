# Change-only Undo destination

See also: [[undo-spec.md]], [[spec.md]], [[doc/current/sync-mvp.md]], [[doc/arch.md]]

## Destination

Every local graph action sends an ordinary `Change`. A normal command sends its planned Change. Undo sends a new inverse Change. Redo sends a new inverse of the last applied Undo Change. The Browser applies each Change optimistically through the resident-projection rules, and the Server applies the same Change to its full Graph.

The Server confirms each accepted request with the complete persisted Change that has the same `changeId`. The confirmed Change keeps the submitted Ops as an unchanged prefix and appends any server Ops. The Browser projects only the appended Ops when the submitted direction is still current, then replaces the matching client History record's Change with the complete confirmed Change. Poll and Load continue to carry the same complete Changes from ChangeLog.

The wire batch, pending queue, Server apply path, ChangeLog, Poll, and Load therefore have one modification unit: `Change`. `ChangeRequest.Undo`, `ChangeRequest.Redo`, and process-local Server History leave this path. A Server restart cannot make a later Undo invalid because the Server no longer interprets an Undo intent.

## Verified current state

These points are proven by current source, not inferred:

- [[src/Shared/History.fs]] `ChangeRequest` has `Change`, `Undo`, and `Redo`, while `History` stores parallel `past` and `future` Change lists. `History.applyAction` needs those lists to materialize explicit Undo and Redo requests.
- [[src/Client/UpdateOps.fs]] `undoOp` and `redoOp` call `SyncPlanner.applyAndEnqueueLocalAction`, so both actions already change the Browser Graph and History before the Server response.
- [[src/Shared/SyncPlanner.fs]] `applyAndEnqueueLocalAction` stores the explicit action in `pendingChanges`; `ackBatch` removes entries by `changeId` and starts the next batch from the acknowledged Revision.
- [[src/Client/Update.fs]] `SubmitResponse` applies aggregate `stampOps` to the Graph and advances `history.nextId`, but it does not add those Ops to the matching History Change.
- [[src/Shared/Serialization.fs]] `ChangeBatchAck` returns `ackedChangeIds`, aggregate `stampOps`, and an optional message. It does not return complete confirmed Changes.
- [[src/Server/DbAgent.fs]] and [[src/Server/FileAgent.fs]] append persistence stamp Ops to the final ChangeLog Change, but store `newState.history` without the appended Ops. ChangeLog is enriched; process-local Server History is not.
- [[src/Server/Database.fs]] loads State with `History.empty`, and [[src/Server/FileAgent.fs]] also resets loaded State to `History.empty`. [[tests/Server.Tests/StateEndpointTests.fs]] proves explicit Change/Undo/Redo in one running process and separately proves DB restart and duplicate identity, but it does not prove Undo after restart.
- [[src/Shared/History.fs]] `Op.undo` removes each `NewNode` and `NewSpecialNode` from the node map and calls `Graph.fromNodes`. A paste with K created Nodes therefore performs K whole-Graph rebuilds during Undo.
- [[src/Shared/GraphBuild.fs]] `addDetachedNode` now avoids a whole-Graph rebuild for each fresh forward `NewNode`, and [[tests/Shared.Tests/LargeChangeApplyTests.fs]] protects forward bulk apply and nested Replace under 300 ms. There is no equivalent large Undo budget.
- [[src/Shared/ResidentProjection.fs]] already applies Header Ops only to Resident Nodes and Replace only to Loaded Children. [[src/Shared/SyncLogic.fs]] already consumes projected Changes atomically and clears local History for a non-empty upstream tail.
- [[src/Shared/Paste.fs]] and [[src/Client/UpdatePaste.fs]] build a paste from `NewNode` Ops followed by structural Replace Ops. This shape allows inverse Replace Ops to detach the created Nodes without removing their map entries.
- [[src/Shared/CommandEntry.fs]] owns `CommandId` and display names. [[src/Client/Controller.fs]] `withDiagnostic` receives the resolved name and [[src/Shared/ViewModel.fs]] renders `CmdLastResult`.
- [[src/Client/CommandDock.fs]] reads the same display name for title and accessibility text, but dispatches the raw updater instead of `withDiagnostic`. Clipboard paste and cut also use `withDiagnostic None`.
- [[src/Shared/Gambol.Shared.fsproj]] compiles [[src/Shared/History.fs]] and [[src/Shared/ViewModel.fs]] before [[src/Shared/CommandEntry.fs]]. A History type that stores `CommandId` would force an unrelated compile-order or module move.

The reported severe large-paste Undo delay is direct user evidence. The per-created-Node rebuild is a proven algorithmic cause and is the primary target. Its share of the observed wall time is not yet measured.

## Local Change and invocation audit

A Change can contain many Ops. That is not a multi-Change invocation:

- [[src/Client/UpdateHelpers.fs]] `splitNode` sends one Change containing `NewNode`, Replace, and optional SetText Ops.
- [[src/Client/UpdatePaste.fs]] sends one Change for every paste form. Edit-mode link and multiline paste put the dirty-text SetText and all paste Ops in that same Change. [[src/Client/Controller.fs]] sends cut as one Replace Change.
- [[src/Client/UpdateMove.fs]] `tryMoveNodeFromTo` puts a dirty-text SetText and one or two structural Replace Ops in one Change. Move Up/Down, Indent/Outdent, move-to-edge, and Move Selected all use this path.
- [[src/Client/UpdateOps.fs]] Duplicate, Delete, and Edit Classes each send one Change, even when that Change has several Ops. [[src/Client/UpdateRename.fs]], [[src/Client/UpdateFileSearch.fs]], and [[src/Client/UpdateAmbleRun.fs]] also send at most one Change per completed invocation.

Two current user paths can produce several local requests or History transitions from one UI invocation:

1. **Undo while text is dirty.** [[src/Client/UpdateOps.fs]] `undoOp` first calls `commitIfEditing`, which can enqueue one text Change, then immediately applies and enqueues Undo. The new text Change becomes the History head, so that same invocation undoes the just-committed Edit. In the Change-only destination this is two ordered Changes in one record lineage: the Edit Change, then its inverse. `redoOp` is not a proven two-Change path: a changed text commit clears `future`, so Redo then has no second action.
2. **Desktop Load.** [[src/Client/UpdateWorkspaceSync.fs]] applies each non-empty phase through History. Loading an existing mapped scope can create one stub-structure Change in `completeUploadInventory` and one server-file-present Change in `completeWorkspacePush`. Create-Workspace-from-folder can first add the Workspace Change in `uploadCreateWorkspaceOp`, so one Load can create up to three local History records. Each phase can be absent when its planned Ops are empty.

The global pending batch is a transport batch, not an invocation group. [[src/Shared/SyncPlanner.fs]] appends each local request to `pendingChanges` and submits the whole ready list; [[src/Client/App.fs]] rewrites that list into one Revision chain. The list can therefore contain Changes from several separate UI invocations that occurred while a prior request or Sync operation was busy.

Explicit Download can create one local stamp-alignment Change when its polled job completes. Automatic workspace behavior does not: `RequestWorkspacePathSyncSnapshot` only refreshes VM mapping/status facts, and `runAutoDownloadTick` performs a fire-and-forget file download with no job polling or stamp-align Change. There is no automatic `Workspace refresh` History record to name.

Server Parse and directory reconciliation can create one or several canonical Changes during the same Load workflow, but these are remote Changes. [[src/Shared/SyncLogic.fs]] clears client History before applying a non-empty Poll or Load tail through `ResidentProjection.applyChange`; it does not add those Changes to local Undo History.

## Invariants

1. The Browser and Server send, apply, persist, confirm, Poll, and Load only ordinary Changes.
2. One History-worthy normal local Change creates one logical client History record. One UI invocation may create several records. Undo and Redo move and amend their target record; ACK handling never pushes a duplicate record.
3. A History record always contains the most complete confirmed Change for its last applied direction, or an optimistic descendant that is linked to an earlier in-flight direction.
4. A confirmed Change has the submitted Ops as an exact prefix. Server enrichment is append-only and uses the submitted `changeId`.
5. Every inverse Change has a fresh `changeId` and the current base Revision when it is sent.
6. Undo and Redo project the complete Change onto the current resident Graph. Effects for Absent Headers and Unloaded Children are consumed without widening residency.
7. A dependent inverse is not released to the Server until confirmation of its predecessor has been folded into it. Unrelated rapid Undo requests can still advance optimistically and queue in order.
8. An ACK is reconciled in request order against the exact in-flight prefix. A missing, reordered, duplicate-with-different-content, or unknown confirmation is a data-outdated condition, not a best-effort merge.
9. A non-empty remote Poll or Load tail still clears local History. Own ACK confirmations amend local History and do not clear it.
10. Rejection keeps the current fail-safe rule: discard the persisted pending queue, mark Sync as rejected, and require reload. Do not try to reverse an optimistic chain after canonical rejection.

## Minimal client History seam

Use client-only metadata near the Browser VM, not fields in wire `Change` or ChangeLog:

- `HistoryRecord = { recordId; commandName: string; applied: Change }`
- `ClientHistory = { past: HistoryRecord list; future: HistoryRecord list }`
- A client-only pending transition identifies `recordId`, submitted `changeId`, and `Normal | Undo | Redo`. The kind and command name never cross the wire.

Keep the pure interface small:

1. `record commandName change` applies a normal Change and adds one record.
2. `undo newIdentity` returns the projected inverse Change, target command name, moved History, and pending transition.
3. `redo newIdentity` does the same in the opposite direction.
4. `confirm transition confirmedChange` validates identity and submitted-prefix invariants, amends the existing record, and re-derives any dependent inverse before it can be sent.
5. `clear` handles a non-empty upstream tail.

This interface hides stack orientation, dependent-confirmation lineage, and Emacs-style “undo the undo” behavior. When a normal Change arrives while `future` is non-empty, move the already-applied future records back into Undo order without creating new logical records.

Names are resolved at the command/event source and passed as strings. This keeps CommandEntry as the source of user-facing names without making History depend on its later compile position. A stable `recordId` is client-only and lets an ACK amend the record even after an optimistic stack move changed the current Change identity.

## Ordinary Change inversion

Build an inverse by reversing the source Ops, swapping old/new fields for Set and Replace Ops, omitting `NewNode` and `NewSpecialNode`, and assigning a fresh request identity. Apply that inverse with ordinary projected Change application. Do not call `Op.undo` or `History.undo`.

For create and paste, inverse Replace Ops detach the created Nodes from Children. The Nodes remain in the Graph map. Redo inverts the applied Undo Change and reattaches those same Nodes; it does not need another `NewNode`. This matches Gambol deletion as edge removal and removes the K calls to `Graph.fromNodes`.

This rule also prevents Redo from replacing an existing Node through `Graph.addDetachedNode`. Retaining detached Nodes is intentional for History. Garbage collection of permanently unreachable Nodes is a separate storage policy.

The pure inversion tests must cover Set Ops, nested Replace order, a split, a large paste, NewSpecialNode, Undo then Redo equality on reachable Graph structure, and projection when some affected Nodes became Absent or Unloaded.

## Ordered ACK reconciliation

The ACK should return `confirmedChanges` in request order. Each item is the exact ChangeLog payload for that accepted `changeId`, including appended persistence or full-Graph Ops. `ackedChangeIds` and aggregate `stampOps` then become redundant.

- Normal: the record already exists from optimistic apply. Confirm amends it in place and projects only appended Ops.
- Undo: the record already moved from `past` to `future` and contains the optimistic inverse. Confirm amends that moved record; it does not add the inverse as a second record.
- Redo: the record already moved from `future` to `past`. Confirm amends it there; it does not restore the old forward record as a duplicate.
- Dependent action: if an unconfirmed record was inverted again, keep that inverse queued locally. Fold the predecessor's complete confirmation into the queued inverse, preserve the inverse's own `changeId`, and only then release it in the next POST.

The Server must return the durable confirmed Change for an idempotent retry after a lost response. DB mode can read the stored payload by `change_uuid`; file mode can read the matching indexed ChangeLog entry. Returning only an ID on the duplicate path is insufficient because the Browser may have missed enrichment.

Batch revision chaining remains ordered. The planner may batch independent ready Changes, but it must stop before a Change that depends on an unconfirmed predecessor. Poll and Load remain blocked while pending Changes exist.

## Command provenance and feedback

Capture the generating command name when the normal History record is created. Undo and Redo retain that name across every inverse. Set `CmdLastResult` when the optimistic stack transition succeeds, not when the ACK arrives, so repeated requests show the next target immediately.

The accepted display is exactly `Undo: <command name>` and `Redo: <command name>`. Empty History displays exactly `Undo: nothing to undo` or `Redo: nothing to redo`. Set these through the current `CmdLastResult.Detail(Some actionName, targetName)` formatter.

Use the accepted names `Edit node` for a text commit, `Paste` for paste, and `Load` for each user-started local Load phase Change. Use the existing `Download` name for an explicit Download stamp-alignment Change. The audit also found the anonymous clipboard cut Change; name it `Cut`. Do not create a label for path-sync refresh or auto-download because neither creates a History record.

Keep one History record per local Change rather than adding invocation grouping in this redesign. A multi-phase Load can therefore produce several records named `Load`. Dirty-text Undo creates an `Edit node` Change and then immediately moves that same record with `Undo: Edit node`. This preserves current History granularity and avoids a new grouping identity across asynchronous phases.

All Change-producing surfaces must provide provenance at the Change seam. Keyboard and command palette use `withDiagnostic`, but that wrapper currently updates only `CmdLastResult`; CommandDock dispatches raw updaters, clipboard paste/cut are anonymous, and CSS/Rename prompt Enter handlers dispatch the final Change updater as command-bar-only. Search selection retains its invoking name, and file-search Enter/buttons already wrap the final updater with `Insert…`. Do not infer a History name later from current mode or ACK order.

## Retry, rejection, and concurrency

- Preserve each Change body and `changeId` across network retries. Rewrite only the base Revision when the planner releases a ready Change.
- A duplicate ACK must return the same confirmed Change value as the first successful ACK. Different content for one `changeId` is corruption and requires reload.
- A server rejection leaves the optimistic Browser Graph and History untrusted. Keep the existing blocked-risk and reload flow; do not add rollback.
- A remote Revision cannot race an own ACK because Poll, Load, and submit stay single-flight. A remote tail after the queue drains still clears client History.
- Browser refresh may continue to clear client History. Pending Changes may still retry from local storage, but restored pending entries do not recreate Undo records.
- Server process restart no longer affects Undo capability. The existing deployment-stamp stale-page rule remains useful, but it is not required for History correctness.

## Performance claims and measurements

Proven: current create Undo performs one full `Graph.fromNodes` rebuild per `NewNode` or `NewSpecialNode`; ordinary inverse Changes omit those Ops and therefore remove that cost.

Proven: forward large Change apply has a focused 2,000-Node budget, and current append Replace has a fast path. This does not prove inverse, SiteMap, persistence, encoding, or network performance.

Measure before and after with the same large external paste: pure inverse planning, projected Browser apply, SiteMap reconciliation, request encoding, Server full-Graph apply, persistence, ACK encoding, and total keypress-to-render time. Add a focused large-paste inverse budget that checks reachable structure and proves no per-created-Node rebuild. Treat Replace validation, non-append index rebuilds, SiteMap reconciliation, and persistence as secondary hypotheses until timings identify one.

## Incremental slices

1. Add characterization and timing tests for current large-paste Undo and reachable-Graph Undo/Redo semantics. Checkpoint: the test exposes the rebuild cost without changing behavior.
2. Add ordinary append-only inversion and the pure client History interface. Checkpoint: normal/Undo/Redo, create/paste detach and reuse, command-name retention, rapid stack movement, and no duplicate records pass through this interface.
3. Change Shared transport codecs to Change-only batches and ordered complete confirmations. Checkpoint: round trips prove identity, exact submitted prefix, enrichment, and ordered mixed batches.
4. Convert one Server adapter at a time, first file and then DB, to apply only Changes and return durable confirmations for new and duplicate submissions. Checkpoint: focused endpoint tests prove restart-safe Undo Changes, retry after lost ACK, ChangeLog equality, and atomic rejection.
5. Convert the Browser planner and update flow to optimistic ordinary Changes plus ordered confirmation. Checkpoint: projection tests cover Normal/Undo/Redo ACKs, dependent inverses, absent/unloaded effects, retries, rejection, and remote-tail History clearing.
6. Wire command provenance through keyboard, palette, dock, prompts, clipboard, text commit, Load phases, and explicit Download. Checkpoint: each local Change records its selected name, automatic refresh creates no record, and rapid Undo/Redo updates `CmdLastResult` immediately.
7. Run focused Shared, Browser planner, codec, and Server endpoint tests, compile Fable, then perform the same manual large-paste Undo measurement. Checkpoint: the primary rebuild is absent and any remaining delay has phase timings.

## Explicit deferrals

- Do not add durable Browser History, cross-session Undo, server Undo endpoints, a second action codec, or ChangeLog labels.
- Do not add detached-Node garbage collection in this redesign.
- Do not change remote conflict policy, Revision semantics, Poll/Load projection rules, residency scope, or Server partial residency.
- Do not optimize secondary paths until the phase measurements show a budget failure.
- Do not preserve compatibility for explicit Undo/Redo request JSON; coordinated Browser and Server deployment is the established project rule.

## Resolved user decisions

1. Undo and Redo use the exact accepted result wording stated above, including the accepted empty-stack text.
2. Non-registry local Change sources use their audited generating action: `Edit node`, `Paste`, `Cut`, `Load`, or explicit `Download`. Automatic workspace refresh and auto-download have no History label because they create no local Change.
3. Preserve current one-record-per-Change granularity. Composite commands that build one Change keep one record; dirty-text Undo uses one record lineage across two ordered Changes; a multi-phase Load may leave several `Load` records. Do not add speculative invocation grouping.

No user-owned decision remains for this wayfinder destination.
