# DocumentState record contract

Sequence on [core-refinement architecture](../arch.md) is `expand-contract`. `Model.fs` was not edited. No commit.

## Contract

`DocumentState` stays and becomes the record `{ parseState: ParseState; persistState: PersistState; documentState: DocumentStateEnum }`. `ParseState` and `PersistState` move into that record. `DocumentStateEnum` keeps `Current` | `Unparsed` | `NoServerFile`, and `NoServerFile` stays its own case. This expand does not rename `Current`. Do not delete `DocumentState`. Until migrate, the Node field `documentState` stays `DocumentStateEnum`.

Yes. The Node field is described as `DocumentStateEnum` until migrate. That sentence is on §3 step 1 **Migrate** and **Contract**, on §9 story path 1 **State axes on special nodes**, and on §4 **Approach (Mikado)**. [Model](../../../src/Shared/Model.fs) line 119 is still `documentState : DocumentStateEnum`.

## Files

1. [core-refinement architecture](../arch.md) — §3 step 1 **State axes on special nodes**, §4 **Approach (Mikado)**, §9 story path 1 **State axes on special nodes**, and the stage and ticket-source lines.
2. [core-refinement map](../map.md) — decision 11 **Tickets from the sequence**.
3. [core-refinement](../project.md) — `Stage: slice`, `Updated: 2026-10-15`, and the new issue lines.
4. Issues 19 through 27 under [issues](../issues/).

[18 — Core Persist stack](../issues/18-core-persist-stack.md) still says removal of `DocumentState`. That file was not rewritten.

## Tickets

1. [19 — Migrate special-node state onto the DocumentState record](../issues/19-migrate-special-node-state-onto-the-document-state-record.md) — Blocked by: None — can start immediately.
2. [20 — Contract special-node state inside DocumentState](../issues/20-contract-special-node-state-inside-document-state.md) — Blocked by: 19 — Migrate special-node state onto the DocumentState record.
3. [21 — Contract Load onto the Parse stack](../issues/21-contract-load-onto-the-parse-stack.md) — Blocked by: 26 — Revisit WebDAV parse.
4. [22 — Expand Core path control](../issues/22-expand-core-path-control.md) — Blocked by: None — can start immediately.
5. [23 — Migrate callers onto Core path control](../issues/23-migrate-callers-onto-core-path-control.md) — Blocked by: 22 — Expand Core path control.
6. [24 — Contract absolute paths out of callers](../issues/24-contract-absolute-paths-out-of-callers.md) — Blocked by: 23 — Migrate callers onto Core path control.
7. [25 — Enable mailbox git requests](../issues/25-enable-mailbox-git-requests.md) — Blocked by: None — can start immediately.
8. [26 — Call mailbox git requests](../issues/26-call-mailbox-git-requests.md) — Blocked by: 25 — Enable mailbox git requests.
9. [27 — Stop the old git path](../issues/27-stop-the-old-git-path.md) — Blocked by: 26 — Call mailbox git requests.

Fully checked beats were skipped. Story path 3 **Persist stack** is fully checked. Beats that already have a ticket were left: [01 — Persist/git work-tree gate](../issues/01-persist-git-work-tree-gate.md), [02 — Git Load: Unparsed then Parse stack](../issues/02-git-load-unparsed-then-parse-stack.md), and [18 — Core Persist stack](../issues/18-core-persist-stack.md).

## Frontier

New tickets that can start now:

1. [19 — Migrate special-node state onto the DocumentState record](../issues/19-migrate-special-node-state-onto-the-document-state-record.md)
2. [22 — Expand Core path control](../issues/22-expand-core-path-control.md)
3. [25 — Enable mailbox git requests](../issues/25-enable-mailbox-git-requests.md)

These existing `defined` tickets also have no blocker: [07 — Git changed list sets Unparsed](../issues/07-git-changed-list-unparsed.md), [08 — Pointer: Core Actor pool](../issues/08-pointer-core-actor-pool.md), [09 — Pointer: Core Files contract](../issues/09-pointer-core-files-contract.md), [10 — Pointer: Core Query contract](../issues/10-pointer-core-query-contract.md), [11 — Pointer: Server tracks credentials](../issues/11-pointer-server-tracks-credentials.md), [13 — Pointer: Finish and drop](../issues/13-pointer-finish-and-drop.md), [14 — Pointer: Database down and probe](../issues/14-pointer-database-down-and-host-stop.md), [15 — Pointer: Prove Core Actor lifecycle with TestActor](../issues/15-pointer-prove-core-actor-lifecycle-testactor.md), and [16 — Pointer: Drain Actor lifecycle on host stop](../issues/16-pointer-drain-actor-lifecycle-on-host-stop.md).
