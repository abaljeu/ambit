---
name: ""
overview: ""
todos: []
isProject: false
---

---
name: trashcan-delete-semantics-v2
overview: Updated plan for TRASH node semantics, documenting completed data/model work and remaining delete and testing work.
todos:
  - id: model-node-kind
    content: Extend Node model with NodeKind/SpecialKind and add TRASH node id constant
    status: completed
  - id: graph-bootstrap-trash
    content: Ensure TRASH node exists under ROOT and cannot be deleted/moved/renamed
    status: completed
  - id: ownership-helpers
    content: Add shared helpers for occurrence/owner queries and TRASH ancestry checks
    status: completed
  - id: snapshot-special-nodes
    content: Make snapshot read/write round-trip special nodes by canonical short ids instead of depth/text heuristics
    status: completed
  - id: delete-classification
    content: Implement delete classification (MoveToTrash, HardDeleteSubtreeInTrash, LocalDeleteWithPromotion, LocalDeleteRefOnly)
  status: completed
  - id: client-delete-update
    content: Refactor deleteSelectionOp to use new classification and generate appropriate Replace ops
  status: completed
  - id: tests-trash-semantics
    content: Add and update shared view model tests to cover trash semantics and invariants
  status: in_progress
isProject: false
---

### What is already implemented

- **Node data model extensions**
  - `Node` in `src/Shared/Model.fs` now has:
    - `owner : NodeId` (the unique owning parent along the `Ownership.Owner` edge).
    - `kind : NodeKind`, where `NodeKind = Normal | Special of SpecialKind` and `SpecialKind = Trash`.
  - All node construction sites initialise these fields:
    - `Graph.rootPlaceholder` is `{ id = rootId; text = "ROOT"; ...; owner = rootId; kind = Normal }`.
    - `Graph.newNode` creates nodes with `owner = rootId` and `kind = Normal`.
    - `History.Op.NewNode` constructs a corresponding `Node` with the same defaults.
  - JSON encoding/decoding in `src/Shared/Serialization.fs` has been updated to:
    - Encode/decode `kind` via `encodeNodeKind` / `decodeNodeKind`.
    - Default missing `kind` on decode to `Normal` for backward compatibility.
    - Continue to omit `owner` from the wire format (it is recomputed from edges by `Graph.fromNodes`).

- **Canonical TRASH node and graph bootstrap/migration**
  - `Graph` defines:
    - `rootId : NodeId = NodeId Guid.Empty`.
    - `trashId : NodeId = NodeId(Guid.Parse "00000000-0000-0000-0000-000000000001")`.
  - `Graph.ensureTrashNode` enforces the invariant that there is always a Trash node:
    - If `trashId` is missing from `nodes`, it creates a node `{ id = trashId; text = "Trash"; name = None; children = []; cssClasses = CssClass.empty; owner = rootId; kind = Special Trash }`.
    - It ensures that root’s children contain exactly one `ChildNode` with `id = trashId` and `ref = Owner`, appending it if necessary.
    - If an existing Trash node is present but not as an `Owner` child of `rootId`, it normalises root’s children so that Trash appears exactly once as an `Owner` child.
  - `Graph.applyOwnerField` recomputes `Node.owner` from `ownerParentByChild`:
    - Root’s `owner` is `root` itself.
    - For all other nodes, `owner` comes from `ownerParentByChild`, defaulting to `root` when not present.
  - `Graph.fromNodes` calls both helpers:
    - Runs `ensureTrashNode` to guarantee a canonical Trash node and root child.
    - Rebuilds structural and owner parent maps.
    - Applies `applyOwnerField` so every node has a consistent `owner`.
  - This migration is used everywhere `Graph.fromNodes` is used (server load, snapshots, tests), so older graphs automatically gain a proper Trash node after load.

- **Guards against mutating TRASH**
  - `Graph.setText` and `Graph.setClasses` now reject direct edits to Trash:
    - Root text/classes remain immutable as before.
    - When `nodeId = trashId`, both functions return an error string and leave the graph unchanged.
  - `Graph.replace` enforces structural invariants around Trash:
    - Rejects any `Replace` where `parentId = trashId` (Trash cannot be used as a parent container).
    - When `parentId = rootId`:
      - If there was an owner occurrence of Trash before, the `updatedChildren` must still contain exactly one `Owner` child with `id = trashId`, otherwise the operation is rejected.
    - When `parentId <> rootId`:
      - Rejects any `Replace` that would introduce a child with `id = trashId` under a non-root parent.
  - Collectively this ensures Trash is always an `Owner` child of Root, never appears as a child of other nodes, and cannot be edited via the standard text/class mutations.

- **Helper functions for ownership and trash-related queries**
  - Implemented in `src/Shared/ViewModelOps.fs` under `module ViewModel`:
    - `getAllOccurrences : Graph -> NodeId -> (NodeId * int * ChildNode) list`
      - Returns all `(parentId, index, child)` occurrences where `child.id = nodeId`, across the entire graph (owner + refs).
    - `getOwnerOccurrence : Graph -> NodeId -> (NodeId * int * ChildNode)`
      - Returns the unique owner occurrence for `nodeId` by filtering `getAllOccurrences` for `child.ref = Ownership.Owner`.
    - `isOwnerUnderTrash : Graph -> NodeId -> bool`
      - Finds the owner’s parent via `getOwnerOccurrence` and walks `graph.ownerParentByChild` up to `graph.root`, returning `true` if `Graph.trashId` is encountered on this chain.
    - `occurrencesOutsideSelection : Graph -> SiteNodeRange -> NodeId -> (NodeId * int * ChildNode) list`
      - Filters `getAllOccurrences` to those not within the given `SiteNodeRange` under the same parent.
  - These functions are shared between client and any future server-side logic that needs to classify deletes or reason about ownership and Trash ancestry.

- **Snapshot (outline) read/write support for special nodes**
  - `Snapshot.write` now uses stable, canonical short ids for special nodes:
    - A helper `ensureCanonicalSid` maps `Graph.trashId` to the reserved SID `"TRASH"`.
    - When writing:
      - Refs to Trash are always emitted as `-> #TRASH`.
      - Owner occurrences of Trash (and other future special nodes) are always emitted as `#TRASH ...`, regardless of depth or sharing.
      - Other shared nodes continue to use automatically-generated `#n1`, `#n2`, etc.
  - `Snapshot.read` recognises these canonical SIDs:
    - A helper `canonicalNodeIdForSid` maps `"TRASH"` back to `Graph.trashId`.
    - `resolveOwnerSid` and `resolveRefSid` both:
      - Prefer the canonical `NodeId` for a known SID (e.g. `TRASH` → `trashId`).
      - Otherwise, reuse an existing `NodeId` for the same SID or mint a new one.
    - As a result, outlines that contain `#TRASH` and `-> #TRASH` round-trip through snapshot save/load using the single canonical Trash node, at any depth.
  - `outlineTextNode` assigns `kind = Special Trash` when `id = Graph.trashId`, ensuring the reconstructed node’s `kind` matches its canonical identity.
  - After outline parsing, `Snapshot.finalizeOutlineGraph` calls `Graph.fromNodes`, so the usual Trash and owner migration still applies.

### What remains to do (from the original plan)

- **Delete classification logic (shared)**
  - Introduce a classification function in shared code (likely `ViewModelOps`) that, for each selected child, computes:
    - `isOwnerHere` – whether this occurrence is the `Owner` occurrence (can reuse `getOwnerOccurrence`).
    - `ownerUnderTrash` – whether the unique owner’s ancestor chain includes Trash (via `isOwnerUnderTrash`).
    - `otherOccurrences` – all occurrences of the same node id outside the current `SiteNodeRange` (via `occurrencesOutsideSelection`).
  - Based on these values, classify each node into one of:
    - `MoveToTrash`
    - `HardDeleteSubtreeInTrash`
    - `LocalDeleteWithPromotion`
    - `LocalDeleteRefOnly`
  - Design and implement a representation for this classification (e.g. a discriminated union) and a helper that produces a per-selection plan consumable by the client update.

- **Subtree destroy logic under TRASH**
  - For nodes classified as `HardDeleteSubtreeInTrash`:
    - Traverse descendants starting from the owner occurrence under Trash.
    - For each node id in the subtree:
      - Use `getAllOccurrences` to collect all occurrences globally (both under Trash and outside).
    - Plan `Op.Replace` operations for every parent that removes all of these occurrences in one `Change`.
  - Ensure this planning produces a single consistent final graph that passes `History.validateOwnershipSemantics`.

- **Client-side delete integration (`deleteSelectionOp`)**
  - Refactor `deleteSelectionOp` in `src/Client/UpdateOps.fs` to:
    - Use the shared classification function instead of ad-hoc owner/ref logic.
    - For each selected node:
      - **MoveToTrash**:
        - Do not generate a delete `Replace` for the owner occurrence.
        - Instead:
          - Generate a `Replace` removing the owner occurrence from its current parent.
          - Generate a `Replace` appending a new child `{ id = nodeId; ref = Owner }` under `Graph.trashId`.
      - **HardDeleteSubtreeInTrash**:
        - Use the subtree destroy planner to add `Replace` ops removing all occurrences of the subtree from all parents.
      - **LocalDeleteWithPromotion**:
        - Choose a reference occurrence outside the selection to promote to `Owner`.
        - Generate a `Replace` that changes that child’s `ref` from `Ref` to `Owner`.
        - Generate a `Replace` that removes the original owner occurrence inside the selection.
      - **LocalDeleteRefOnly**:
        - Generate a `Replace` removing only that single `Ref` child from its parent’s `children`.
    - Combine these ops with existing selection-adjustment logic into a single `Change` passed through `applyAndPost`.
  - Confirm that no path allows Trash itself to be deleted or moved via `deleteSelectionOp` (it should be filtered or cause a no-op).

- **Selection update semantics after delete**
  - Reuse and, if needed, refine the existing selection-adjustment logic so that:
    - After move-to-trash, the selection remains near where the node used to be under its original parent.
    - After hard delete (especially within Trash), selection falls back to a reasonable neighbour or parent, consistent with current UX.
  - Ensure this logic does not depend on special treatment for Trash beyond what is already enforced at the graph level.

- **Tests for trash semantics (`tests/Shared.Tests/ViewModelTests.fs`)**
  - Add and/or update tests to cover:
    - **TRASH bootstrap and invariants**:
      - Any loaded or newly created graph contains a Trash node under Root with `kind = Special Trash`.
      - Trash not deletable via bad ops (e.g. wipe all root children): reject + **graph unchanged**; full clear under a non-root parent (e.g. `buildFlat`’s `cont`) is allowed. Move-to-trash tests: root `Replace` must not span TRASH.
    - **Move-to-trash behaviour**:
      - Deleting the last non-Trash owner occurrence of a node moves it under Trash instead of rejecting.
      - After move, `History.validateOwnershipSemantics` succeeds and the owner chain for the node becomes `ROOT → TRASH → ...`.
    - **Soft delete with promotion outside Trash**:
      - For a node with an owner and refs, deleting the owner occurrence in one parent promotes a ref elsewhere to owner and preserves other refs.
    - **Hard delete within Trash**:
      - When the unique owner is under Trash and there are no occurrences outside Trash, deleting in Trash removes the entire subtree globally (owner + refs) and undo restores it.
      - When there are refs outside Trash, deleting within Trash only removes occurrences under Trash; external refs remain.
    - **Mixed selections**:
      - Selections that mix nodes of different classes (`MoveToTrash`, `HardDeleteSubtreeInTrash`, `LocalDeleteRefOnly`) are processed in one `Change` while `History.validateOwnershipSemantics` continues to pass.

### Notes and future-proofing considerations

- **Special nodes beyond Trash**
  - The snapshot and model extensions are designed to support more special nodes later:
    - `Node.kind` can encode future `SpecialKind` cases.
    - Snapshot SIDs (`#TRASH`, etc.) are mapped through `canonicalNodeIdForSid`, so adding a new special node requires only:
      - Adding a new case and mapping there.
      - Assigning the right `kind` based on `Node.id`.
  - All algorithmic logic (classification, delete behaviour) should prefer checking `Node.kind` in combination with ancestry rather than relying purely on ids, so behaviour composes if more special nodes are introduced.