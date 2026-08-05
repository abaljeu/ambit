# Simplify selective client loading

Type: grilling
Status: resolved

## Question

What is the smallest coherent selective-client-loading design after the completed grill, and which earlier decisions does it replace?

## Answer

### Authority

- This is the sole current decision for this selective-client-loading effort. It supersedes the conflicting portions of [[01-represent-child-list-residency.md]], [[02-set-partial-graph-boundaries.md]], [[03-define-load-snapshot-protocol.md]], [[04-choose-startup-bootstrap-scope.md]], [[05-restore-session-partial-residency.md]], [[06-define-unloaded-navigation-ui.md]], [[07-define-selective-residency-search.md]], [[08-define-move-edit-dependencies.md]], [[09-define-sync-revision-correctness.md]], [[10-set-explicit-load-command.md]], [[11-unify-loading-decision-function.md]], [[12-place-selective-loading-seams.md]], and [[13-finalize-permanent-delete-undo.md]]. Those tickets remain resolved historical deliberation; this answer restates the surviving decisions needed for a specification.
- This is an independently shippable client-only phase. The server remains fully resident. The broader later direction in [[doc/roadmap/on-demand-graph-residency.md]] may replace this phase's Workspace granularity and protocol.

### Residency and graph model

- Client residency grows monotonically by complete Workspace during one webpage session. Explicit loads retain every previously loaded Workspace. Revisit this unit only if loading any one Workspace takes more than 10 seconds.
- ROOT is a fully loaded Workspace. Nested Workspaces appear as ordinary members whose children are unloaded.
- Startup installs exactly complete ROOT plus the complete Workspace owning the saved zoom target when that target exists outside ROOT. With no saved target, or with a target in ROOT, there is no second Workspace. Saved fold preferences never add residency.
- `Node` keeps ordinary `children: ChildNode list` and separate `childrenStatus: Unloaded | Loaded`. `Unloaded` requires `children = []`; functions that do not require completeness operate on the resident projection and therefore see an unloaded node as a leaf.
- Only receipt of a complete authoritative child list, including an empty list, changes that node to `Loaded`. Incremental child operations never promote status. Loading is global synchronization state, not a per-node graph status.
- Only `Loaded` child lists contribute authoritative child edges and derived indexes. External and Ref targets encountered by a loaded closure are ordinary resident Node headers, not a separate stub type.

### User surface and Load

- Rename the existing user-facing Upload command to Load and keep `Ctrl+Shift+>`. Bootstrap and Load are the only ways to obtain residency; Zoom, Find, Move, edit, traversal, restoration, and every other action never load implicitly.
- Unparsed source state and Unloaded residency remain distinct facts but share the hollow-circle affordance and the same Load command. Clicking a hollow circle dispatches the normal full-selection Load: first make the clicked occurrence the sole selection when it was not selected, otherwise preserve the full selection.
- Zoom treats an unloaded node as an ordinary leaf. Find searches only the resident projection, and committing a result delegates to exactly the same Zoom behavior. Keyboard unfold, traversal, and range commands consume `children = []` and naturally no-op or continue; only hollow-circle click invokes Load.
- Full-selection Load preserves every current Upload filter, stage, ordering rule, and source-side effect, including desktop push and parse/reconciliation. A loaded target may still run those stages, often as no-ops.
- The request carries each selected target ID and `includeWorkspace`. Use `true` for an Unloaded target and `false` for a Loaded target. The server resolves and deduplicates owning Workspaces and may process selections spanning Workspaces.
- A target with `includeWorkspace = false` receives only the normal poll diff, including source or parse changes. A target with `includeWorkspace = true` additionally receives its complete owning Workspace snapshot. There are no Direct, ArtifactClosure, or Workspace load modes.

### Synchronization and projected correctness

- Load joins the existing single-flight synchronization mechanism through `QueuedLoad` and global `Loading`. Poll, submit, and Load remote responses are serialized; there are no concurrent response compare-and-swap, retry, or replay paths.
- A Load response contains ordinary poll changes from the request base through one atomically captured response revision and any requested complete Workspace snapshots at that same revision.
- Keep the current optimistic local edit/Poll conflict behavior: pending local changes may make a response conflict and force reload. Do not add edit blocking, a full projection refresh, or per-node loading state.
- Projected Poll and Load catch-up skips structural child-list operations when the parent is Unloaded, applies non-structural changes to already-present Node headers, and retains every full History entry so projected Undo and Redo match canonical server Undo and Redo.
- Incremental diffs modify child lists only when they are already Loaded. Only a complete authoritative list or Workspace snapshot changes Unloaded to Loaded.

### Structural commands

- Use one Shared pre-commit guard for every local command that plans a new Change except MoveSelected: if any planned operation would modify an Unloaded node's child list, commit nothing and silently no-op. Add Child, Paste, ordinary move behavior, and other structural commands need no command-specific residency handling.
- MoveSelected is the deliberate exception and never loads its destination. It may submit a move to an unloaded destination; projected apply removes the source edge but skips destination insertion, so the moved node disappears until that destination Workspace is explicitly loaded. Normal command feedback still names the destination.
- The server receives the complete move and History. Undo restores the source in the resident projection while removing the hidden destination canonically; projected Undo and Redo remain History actions rather than newly planned Changes.
- Any other structural move that intends to retain or focus the moving node no-ops before commit when its destination is unloaded. MoveSelected is the sole command allowed to cause projected disappearance.
- ROOT is fully loaded, so ordinary delete, permanent delete, and their Undo behavior need no bootstrap exception or server-only Delete command.

### Responsibility seams

- Shared owns residency invariants, projected Change/Undo/Redo application, derived-index rebuilding, and the common structural guard. Client owns selection, affordances, command dispatch, and the existing single-flight effects. Server owns selected-target-to-Workspace resolution and atomic poll-plus-snapshot capture.
