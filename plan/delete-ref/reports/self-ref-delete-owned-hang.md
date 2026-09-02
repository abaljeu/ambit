# Worker report: self-Ref Delete of Owned Node hangs

QA filing plus diagnosis. No commit. [[WORK.md]] not edited.

## Issue

- Path: [[plan/delete-ref/issues/02-delete-owned-self-ref-hangs.md]]
- Title: Delete of an Owned Node that has a self-Ref hangs
- Status: ready-for-agent
- Project: [[plan/delete-ref/project.md]] Stage set to `tickets` (was `done`); [[plan/index.md]] row updated.

## What the user meant

Primary reading, confirmed in code: Node `x` has a Ref child to `x`. User Deletes the **Owned** appearance of `x` (the structural placement), not the self-Ref row.

Second reading (Delete of some other Owned child while `x` also has a self-Ref): classifier does not promote `x`; no self-owner. Not the hang.

## Diagnosis

1. `ViewModelDeleteOps.classifyDeleteForSelection` counts every graph occurrence. A self-Ref is `otherOccurrences`, so the action is `LocalDeleteWithPromotion`.
2. Promote turns that Ref into Owned, then span-remove drops the original Owned row. `x` now owns `x`.
3. `History.applyChange` then calls `invalidOwnedFileDirectoryPlacement` on the **final** Graph for the promote op. That uses `GraphQuery.enclosing` / `enclosingContainer` with parent `x`. Owner of `x` is `x` → unbounded recursion. That is the hang (Server apply / POST never returns; Browser waits).
4. `ResidentProjection.applyChange` (Browser local apply) does **not** run that History check. It finishes and leaves `ownerParentByChild[x] = x`.

Proved: classify test (fast, promote). `History.applyChange` on the planned ops hung until `enclosing` got a visited set. After that guard, History returns `Invalid` (owner chain). Client apply still `Changed` with self-owner.

## Code pointers

- [[src/Shared/ViewModelDeleteOps.fs]] classify `LocalDeleteWithPromotion` / `buildPromoteOps`
- [[src/Shared/ViewModelOccurrence.fs]] `getAllOccurrences` (finite scan; not the hang)
- [[src/Shared/History.fs]] `validateOwnershipForChange` after apply
- [[src/Shared/GraphQuery.fs]] `enclosing` (now cycle-stops; was the infinite walk)
- [[src/Shared/ResidentProjection.fs]] `applyChange` (no post-apply ownership validate)
- [[src/Client/UpdateOps.fs]] `deleteSelectionOp` → `applyAndPost`
- Repro: [[tests/Shared.Tests/DeleteOpsTests.fs]] `self-Ref` facts

## What was changed vs not

**Did (hang stop only):** visited set on `GraphQuery.enclosing`. Focused tests + Client compile gate.

**Did not:** product Delete (still promotes a self-Ref). Browser still applies a self-owner locally. No Sync/rejection UX. `isOwnerUnderTrash` still has an unguarded owner walk (not this repro). No [[WORK.md]] edit.

## Suggested [[WORK.md]] mutations

- `add` Pending: [[plan/delete-ref/issues/02-delete-owned-self-ref-hangs.md]] — Delete of Owned Node with a self-Ref finishes (do not promote the self-Ref; Move to TRASH or equivalent). Reopen [[plan/delete-ref/project.md]] is already `tickets`.
- Do not `remove` [[plan/owner-edge-db-repair/spec.md]] Active — different surface (startup DB Owned tree).
