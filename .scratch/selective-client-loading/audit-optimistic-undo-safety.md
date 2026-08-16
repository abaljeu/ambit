# Optimistic Undo safety audit

## Verdict: Refuted

The claim is false under the implementation plan's current invariant that the complete
confirmed Change, including Server-appended Ops, becomes the invertible
`HistoryRecord.applied` payload.

It is provable with explicit conditions under a different invariant: submitted command
Ops alone are invertible; Server persistence extras are restricted to
`SetUpdateTime`, projected atomically on confirmation, and excluded from Browser
History.

The current Server does not append Nodes, edges, ownership corrections, or other
structural Ops to a Browser submission. That fact removes the feared structural ACK
counterexample, but it does not make the current complete-payload design independent:
`Change.inverse` deliberately inverts `SetUpdateTime`, and `ClientHistory.confirm`
deliberately re-derives an already-created inverse from the complete confirmation.

## Smallest counterexample to the claim

1. Browser applies and submits Change C. C dirties a Document.
2. Before C's ACK, Browser applies optimistic Undo U. U was derived from C's submitted
   Ops.
3. Server persists C and appends `SetUpdateTime` to C.
4. If complete C is the invertible History payload, U is now incomplete: it does not
   invert the appended stamp.
5. Without dependent re-derivation, the future record continues to contain the old U
   and Redo does not reconstruct complete C.

This is exactly the case implemented by `ClientHistory.confirm`:
`validateConfirmation` finds records in either `past` or `future`
(`src/Shared/ClientHistory.fs`, `tryFindRecord`, lines 134-144), then
`reviseDependent` re-inverts the confirmed Change while retaining U's `changeId`
(lines 202-222). The focused test asserts that behavior
(`tests/Shared.Tests/ClientHistoryTests.fs`,
`confirmation re-derives its direct dependent without changing identity`,
lines 286-316).

The appended Op is persistence metadata rather than a structural correction, so this
counterexample is a contradiction in the proposed History semantics, not evidence
that the resident Graph becomes structurally corrupt. Removing re-derivation requires
changing those semantics explicitly.

## Server confirmation contents

- `PersistStamp.opsBetween` can emit only
  `Op.SetUpdateTime(nodeId, oldTime, newTime)`
  (`src/Shared/History.fs`, lines 767-783).
- `PersistStamp.appendToLast` appends all such Ops to only the last newly logged
  Change (lines 791-798).
- File mode collects all newly logged submitted Ops, persists once, computes only
  stamp Ops, and enriches the last log entry
  (`src/Server/FileAgent.fs`, `handlePostChange`, lines 227-268).
- DB mode performs the same operation
  (`src/Server/DbAgent.fs`, `handlePostChange`, lines 233-275).
- Full-Graph and filesystem checks reject invalid submissions; no inspected path
  synthesizes `NewNode`, `NewSpecialNode`, `Replace`, ownership, placement, move, or
  delete Ops.
- Parse and directory reconciliation can generate structural Changes, but they are
  separate graph-only requests delivered later through Poll/Load, not ACK enrichment
  (`src/Server/Api.fs`, `postGraphOnlyChange`; and
  `src/Server/LazyLoadReconciliationServer.fs`, `postGraphOnlyChange` call).
- `SavePrep` reconstructs State with `History.empty` and performs no enrichment
  (`src/Server/SavePrep.fs`, lines 8-39).

Batch assignment is significant: stamps caused by any newly logged item are assigned
to the last newly logged Change, not the Change that dirtied each Document. A trailing
duplicate receives no new row, so the stamps remain attached to the last newly
persisted Change. The Change-only destination must return the stored complete payload
for duplicates; current source returns only IDs and aggregate stamps, so this is
planned, not current behavior.

## C and U in one batch

The Server fold applies batch items in request order in both agents
(`FileAgent.applyBatch`, lines 145-185; `DbAgent.applyBatch`, lines 89-130).
If C and U are both new and graph-changing, aggregate persistence stamps are appended
to U, because U is the last newly logged Change. They are not appended to C.

When C's ordered confirmation is processed after local U has moved the record to
`future`, current `ClientHistory.confirm` can find and amend by stable `recordId`;
it does not depend on stack position. Lineage order is enforced by
`pendingByRecord` (`src/Shared/ClientHistory.fs`, lines 168-190).

Under the complete-payload invariant, the current implementation still re-derives U
when C is confirmed. In the ordinary all-new `[C; U]` batch this is usually an
identity rewrite because C receives no extras. U's confirmation then amends the same
future record with U plus stamps. A later Redo inverts those stamps because
`Change.inverse` includes `SetUpdateTime`
(`src/Shared/History.fs`, lines 306-323).

The harder case is C alone in flight, U created locally while C waits, then enriched C
is confirmed. Complete-payload semantics require U re-derivation before submission.
Existing HTTP single-flight already keeps U out of C's in-flight batch in this case.
The additional same-record selection stop does not follow from this case; it only
affects C/U accumulated together behind another in-flight batch, where append-to-last
assigns extras to U and leaves C unchanged.

Under submitted-only History semantics, C and U may batch. Ordered confirmations need
only validate identity and the submitted Ops prefix, project allowed metadata extras,
and retire each transition by lineage. No inverse payload changes, so no dependent
rewrite is needed.

## Browser Graph and projection mutation categories

### Local commands, Undo, and Redo

Current normal commands use `SyncPlanner.applyAndEnqueueLocalAction`, which applies
through legacy `History.applyAction` before enqueue
(`src/Shared/SyncPlanner.fs`, lines 26-41;
`src/Client/UpdateHelpers.fs`, `applyAndPost`, lines 129-158).
Current Undo/Redo use the same path
(`src/Client/UpdateOps.fs`, lines 708-760).

Planned behavior moves all three to `ResidentProjection.applyChange` and records
normal command provenance. This is only planned behavior
(`undo-implementation-plan.md`, lines 65-73 and 133-143).

### Matching submit ACK

Current behavior applies aggregate `stampOps` directly to `model.graph` and does not
amend History (`src/Client/Update.fs`, `SubmitResponse`, lines 73-105).
`PersistStamp.applyToGraph` explicitly ignores History
(`src/Shared/History.fs`, lines 800-816).

The proposed complete-confirmation helper is not implemented. It can provide atomic
ordering because one MVU `update` computes one model and `App.dispatch` assigns that
model before running subsequent effects (`src/Client/App.fs`, lines 580-625).
The required order must nevertheless be specified: validate all ordered
confirmations, update/retire History lineage, project only allowed extras or
corrections, advance Revision, publish the model, then permit another Undo dispatch.

### Poll

Poll starts only when Idle with an empty pending queue
(`src/Shared/SyncPlanner.fs`, `tryStartPoll`, lines 91-96).
A response that races with a new local Change sees a non-empty pending queue and is
not auto-applied (`src/Client/UpdateHelpers.fs`, `isAutoSyncBlocked`, lines 196-215;
`src/Client/Update.fs`, `PollDone`, lines 174-242).

An applied non-empty tail clears History before folding Changes through
`ResidentProjection.applyChange`
(`src/Shared/SyncLogic.fs`, lines 35-71 and 84-90). The clear and projection occur in
one pure update, before another Browser message can run. Empty Poll preserves History.

Poll/Load tails are never matched to local transitions. A lost-ACK retry remains
pending and therefore blocks auto-application; after confirmation the Browser
Revision advances, so subsequent Poll starts after it. Treating a tail as remote and
clearing History is conservative, not an unsafe matching amendment.

### Load and authoritative packages

Load starts only when Idle with an empty pending queue
(`src/Shared/SyncPlanner.fs`, `tryStartLoad`, lines 98-108). A new local Change while
Load is in flight makes `isAutoSyncBlocked` true, so `LoadDone` refuses the payload and
enters `DataOutdated` rather than installing it
(`src/Client/Update.fs`, lines 244-297).

For an accepted response, non-empty Change tails clear History before projection;
packages install afterward and therefore win at the response Revision
(`src/Shared/SyncLogic.fs`, lines 52-71).

A package-only Load intentionally preserves History while changing residency:
`ResidentProjection.installPackages` replaces/merges authoritative Nodes and rebuilds
the Graph (`src/Shared/ResidentProjection.fs`, lines 53-63), and the existing focused
test locks preservation
(`tests/Shared.Tests/SyncLogicTests.fs`,
`empty Loaded child list marks Loaded without History clear`, lines 460-498).
This does not stale C under current scheduling: the Load was captured at the settled
client Revision, and any intervening local pending Change blocks installation.
The enforceable invariant must say “semantic Graph change,” not merely “any package
or Nodes/edges added”; residency expansion at the same settled Revision is permitted.

### Synchronous workspace posts and download alignment

`applyAndPostSync` applies a local Change, performs a blocking POST, then directly
applies ACK stamps (`src/Client/UpdateWorkspaceSync.fs`, lines 51-78). Because the
JavaScript call is synchronous, another Undo cannot dispatch between its local apply
and ACK handling.

Upload structure is different: it applies locally, then posts asynchronously outside
the normal pending/in-flight seam (`applyStructureLocally`, lines 80-93;
`completeUploadStructurePost`, lines 283-299). Undo is not gated by `Uploading`, so a
local inverse can exist before that ACK. The plan says this path will use the common
complete-confirmation helper, but it does not yet define the in-flight lineage object
that lets a bypass post qualify as “matching.” This is a specification gap.

Explicit download stamp alignment calls `applyAndPostSync`
(`src/Client/UpdateWorkspaceDownload.fs`, lines 45-70). Auto-download itself performs
desktop I/O only and does not mutate the Browser Graph (lines 127-134). Path-sync
snapshots update mapping/fact fields, not Graph (`src/Client/Update.fs`, lines 143-165).

### Restored pending, bootstrap, rejection, and reload

`StateLoaded` atomically replaces Graph and clears History
(`src/Client/Update.fs`, lines 43-68). Current restored pending then replays onto that
fresh state (`src/Client/App.fs`, `mergePendingAfterLoad`, lines 104-143). Planned
restored ordinary Changes have no transition and recreate no Browser History.

Submit rejection clears the persisted pending queue and enters `ServerRejected`, but
does not alter the Graph or clear current History
(`src/Client/Update.fs`, lines 107-122). This is not an authoritative projection
change; recovery is reload. Reload replaces Graph and History together.

### Direct application outside ResidentProjection

Current direct paths are ACK `PersistStamp.applyToGraph`, synchronous workspace ACK,
async structure ACK, and legacy `History.applyChange`/`applyAction` local paths.
The plan promises to remove those paths, but the invariant is not current behavior.
A source-level cutover check is required: all normal, inverse, ACK-extra, Poll, and
Load Change application must use one resident-projection/reconciliation seam; package
installation remains the separate authoritative residency seam.

## The two possible invariants

### A. Complete confirmed Change is invertible

“For every local transition, `HistoryRecord.applied` is the complete confirmed Change,
including every appended Op. Undo and Redo invert that complete payload.”

This is the current plan (`undo-implementation-plan.md`, lines 7-12, 90-97) and the
current `ClientHistory.confirm` implementation. It cannot eliminate dependent
re-derivation when Undo is created before separately submitted enriched C is
confirmed.

It can eliminate the additional same-record batch-selection stop if three existing
facts become explicit contract: exact in-flight retry membership; all persistence
extras attach only to the last newly persisted Change; and unchanged new submissions
reject the batch. In an all-new `[C; U]` batch, C then has no suffix, so the U already
in flight is still its exact inverse. This does not eliminate re-derivation for a
queued U behind separately submitted C.

### B. Submitted command Ops alone are invertible

This is the only invariant that can eliminate both same-record blocking and dependent
inverse re-derivation:

> A Browser History record contains exactly the Ops submitted for that local
> transition. A successful confirmation must preserve those Ops as an exact prefix.
> Any suffix is confirmation metadata, is projected atomically after prefix
> validation, and is never stored in or inverted by Browser History. The only allowed
> suffix Op is `SetUpdateTime`; any other suffix kind rejects the confirmation and
> requires reload. Poll/Load Changes remain complete durable Changes and are never
> recorded in Browser History. Any non-empty semantic remote Change tail clears
> Browser History before projection. Package-only residency expansion is allowed to
> preserve History only when captured and installed at the same settled Revision with
> no pending local transition; otherwise it is refused and requires reload.

Additional enforceability language is needed for bypass posts:

> Every normal and workspace submission records exact confirmation lineage before the
> request is issued. Its ACK is reconciled in one reducer step: validate ordered
> identity and submitted prefix; validate that suffix Ops are only `SetUpdateTime`;
> retire matching lineage; project suffix Ops through ResidentProjection; advance
> Revision; then publish the model. No subsequent Undo can observe an intermediate
> model. Unmatched confirmations reject and require reload.

This is a deliberate semantic change from the current plan and tests. It treats disk
mtime as authoritative projected status, not user-editable command state.

## Slice 3a/3b implications

If invariant A is retained:

- remove Slice 3a's same-record selection stop, conditional on locking
  append-to-last-new assignment, exact in-flight retry membership, and unchanged-new
  rejection in Server/planner tests;
- retain dependent re-derivation only for a same-record transition that is queued but
  is not part of the ordered ACK being reconciled;
- do not rewrite an already in-flight dependent; prove that its predecessor's
  confirmation has no suffix under the batch-assignment contract;
- retain correction projection and the C-alone-in-flight/U-queued test.

If invariant B is adopted:

- retain Slice 3a for queue conversion, private exact in-flight membership, retry, and
  restored-pending behavior;
- remove only the same-record selection rule;
- remove `pendingByRecord` dependent Change storage/re-derivation, while retaining
  ordered transition lineage needed to retire C/U/Redo confirmations;
- retain Slice 3b as a separate runtime/projection slice;
- make Slice 5 validate and project metadata suffixes without amending the invertible
  payload;
- specify and implement lineage for async upload-structure posts that bypass the
  normal planner.

There is no evidence-based reason to merge all of 3a and 3b. The queue representation
and exact in-flight retry boundary remain independently useful.

## Focused tests required

1. C confirmed with appended `SetUpdateTime` after local U: suffix projects, U and
   future History payload remain submitted-only, and no dependent rewrite is returned.
2. A in flight, then C/U queued: the next exact batch may contain `[C; U]`; ordered
   confirmations retire both while the record is in `future`.
3. `[C; U; Redo]` in one batch preserves lineage and final stack direction.
4. Any confirmation suffix containing `NewNode`, `NewSpecialNode`, `Replace`, or a
   Header Op other than `SetUpdateTime` rejects and requires reload.
5. FileAgent and DbAgent: aggregate stamps attach only to the last newly persisted
   Change; trailing duplicates do not receive them; duplicate lookup returns the
   original complete payload.
6. Async upload-structure C, local U before ACK, then ACK stamp: common lineage
   reconciliation projects the stamp and leaves submitted-only History valid.
7. Non-empty Poll and Load tails clear History before any projected Op; a raced local
   pending Change blocks response application.
8. Package-only Load preserves History only at the same settled Revision and with no
   pending transition; a raced local Change causes refusal/reload.
9. Absent Header and Unloaded Children consume allowed stamp suffixes as projected
   no-ops while still retiring confirmation lineage and advancing Revision.
10. Source/call-path assertion or review gate finds no direct ACK `applyToGraph` and no
    local/ACK Change application outside the designated reconciliation seam.

## Proposed WORK.md mutation

- `remove` [[.scratch/selective-client-loading/audit-optimistic-undo-safety.md]] from
  Active: the audit is complete. Add a new actionable plan item only if the root adopts
  submitted-only History semantics and needs the implementation plan revised.
