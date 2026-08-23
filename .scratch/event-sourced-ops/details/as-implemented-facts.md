# As-implemented facts

How the software behaves today. These are **facts**, gathered so the standard does not contradict reality. Several of them are **behavior to beat** — they are named here so nobody mistakes them for the standard.

Do not copy a specification out of this file.

## Apply

- An Op applies through [[src/Shared/GraphMutate.fs]]. Attribute Ops compare and swap the old value; `Replace` compares and swaps a span of one parent's children and refuses on a mismatch. `SetUpdateTime` ignores a mismatch.
- `History.applyChange` checks ownership after apply. A static ownership check exists — owner edges, File and Directory placement, artifact names — and partial Graphs already skip a missing owner when the claimed owner is Unloaded or Absent.
- The Browser applies local edits, undo, and poll tails through the resident projection, **without** the ownership validation. Absent headers and Unloaded `Replace` become no-ops, silently.
- Distinct parents do not interact. Same-parent structural overlap is the remaining collision class.

**Behavior to beat:** whole-field `SetText` compare-and-swap, whole-set `SetClasses` replace, and span compare-and-swap `Replace` that refuses. The standard says set delta for classes ([[conflict-resolution.md]]) and full-list Replace with Accept Both for child lists ([[replace-amendment.md]]).

## Server apply path

- The request path reads the body, decodes it, and calls apply inside the agent. Apply deduplicates by change identity, passes a **global revision gate**, applies, then bumps the revision, validates and persists to disk, adds stamp Ops, and logs.
- The **global revision gate** refuses every concurrent Change, related or not. Dropping it is the first slice of [[.scratch/relaxed-concurrency/]].
- A file agent is a single mailbox that serialises all reads and writes for one file: one message at a time, one consumer loop. Posting from many request tasks is safe. Applying a Change runs **on** that loop; only the disk write is pushed to the pool, with a timeout so a stuck write does not wedge the loop.
- Parse plans **off** the mailbox on a snapshot, then sends a message for apply. That is already the shape a long job needs ([[actors-and-jobs.md]]). It still encodes to text and then decodes, and it discards the acknowledgement.

**Behavior to beat:** the revision gate, and the confirmation-echo acknowledgement.

## Conveyance

| Path | Carries | Applied how |
| --- | --- | --- |
| **Poll** | Revision, build stamps, readiness, and a Change list | Each Op applies. No graph merge. |
| **Load** | The same envelope **plus** packages — complete Workspace Nodes at that revision | Ops for the tail, then the packages are merged into the map |
| **State** | Revision and a full graph | No Change list at all |

Poll is Ops only. Load is mixed: Ops for the tail, Graph transfer for residency. Packages are sliced out of the Server Graph, not replayed from Ops. Making them Ops would be either genesis replay, which is rejected, or a verbose encoding of the same Nodes — so Load stays mixed. **Architecture pin:** Load packages as Graph / state transfer is **accepted**; only finer residency packaging (job emit vs Browser Load) stays proposed/parked detail.

**Behavior to beat:** a poll with a non-empty tail **clears** the Browser History. The standard says neither channel clears it ([[messaging.md]]).

## Acknowledgement contract

Today's acknowledgement is a confirmation echo: one confirmation per submitted Change, the submitted Ops as an exact prefix, and only stamp Ops allowed as a suffix. Client-side reconciliation requires equal length and the same order of change identities, and a changed prefix, a forbidden suffix, or an unmatched identity forces a reload. Tests lock that behavior. Stamp suffixes project onto the Graph and do **not** enter History.

An amended Change, or extra Changes from other Actors, **break** that contract on purpose. The submit path and the poll path are also two different apply functions today, and one receive path would end the submit path's use of reconciliation for success.

These are contract and migration problems. They are expected, and they do not make the design unsafe ([[messaging.md]]).

## Other facts worth keeping

- The user removes an item from the outline with a move to TRASH, which stays Owned. A hard delete happens only for a subtree already under TRASH with no references from outside.
- When other occurrences are Resident, the Browser already plans promote-then-remove in **one** Change, so that fill-in is on the undo stack because the Browser sent it ([[completing-ops.md]]).
- Startup repair can promote a Ref with no Change and no poll — [[.scratch/owner-edge-db-repair/]].
- Startup can replace or trim the in-memory Graph without Ops. A snapshot load builds the Graph from rows; it is not a replay.
- **File-mode bootstrap/migration can truncate the Change log** while keeping graph + revision: [[src/Server/DatabaseSetup.fs]] calls [[src/Server/Database.fs]] `rebuildFromDocumentFiles` when the DB is empty or disk and DB diverge; that clears `changes` and re-seeds the projection from parsed files — not DB+log recovery. An open Browser then hits `DataOutdated` ([[src/Shared/SyncLogic.fs]]) or submit rejection — **behavior to beat**; proposed fix is permanent log + DB+log restart ([[permanent-history-and-genesis.md]]).
- [[doc/arch.md]] still says last-write-wins and no client merge.

## Sources

[[src/Shared/GraphMutate.fs]], [[src/Shared/History.fs]], [[src/Shared/SyncLogic.fs]], [[src/Shared/ApiResponses.fs]], [[src/Shared/Serialization.fs]], [[src/Shared/ViewModelDeleteOps.fs]], [[src/Server/Api.fs]], [[src/Server/FileAgent.fs]], [[src/Server/DbAgent.fs]], [[src/Server/RouteRegistration.fs]], [[src/Client/Update.fs]], [[tests/Shared.Tests/AckReconcileTests.fs]], [[.scratch/selective-client-loading/undo-spec.md]], [[doc/api.md]].
