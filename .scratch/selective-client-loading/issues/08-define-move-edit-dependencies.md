# Define move and edit residency dependencies

Type: grilling
Status: resolved
Blocked by: 03

## Question

What authoritative node and direct-child-list closure must be loaded before move-target selection and each relevant current edit, undo, redo, and clipboard operation, and should a missing dependency cause load-and-resume, rejection, or another explicit outcome without applying against partial state?

## Answer

- Move Selected retains resident-only target search. Highlighting has no side effects. Committing a target whose child list is `Unknown` closes the search like Find, preserves the current frame and move intent, and requests the target's `Direct` snapshot before constructing the destination range or any move operation.
- A move is ready when its visible selected source range and destination parent's complete direct-child list are resident. If the destination list is `Unknown`, request `Direct` for that destination; success constructs and applies the move against the returned list, while load failure, a missing destination, or a stale source intent cancels it. There is no artifact-closure dependency loop or move-specific replanning deadline.
- The client applies the resulting move optimistically. Hidden ownership placement and artifact-name invariants require no extra client residency: the fully resident server validates them and may reject the submission. Existing generic post-response handling owns that rejection and authoritative reload behavior rather than the Move command performing a special rollback.
- Move Selected is the only edit command that implicitly loads residency. Startup closure, visible selection, and monotonic residency normally leave text, class, rename, split/join, keyboard structural edits, and their validation dependencies resident. If any non-Move edit nevertheless encounters `Unknown`, it rejects with normal command-result feedback rather than loading or applying partially.
- Copy and Cut continue to include only selected rows and their visible unfolded descendants. An unloaded node has no visible children, so clipboard commands do not load it. Link paste derives Owner-versus-Ref from authoritative owner knowledge rather than a global scan of resident child lists; other paste forms use their already-resident insertion parent. An unexpected missing paste dependency rejects.
- Delete no longer needs a global client occurrence scan for ordinary removal. Deleting an Owner occurrence outside TRASH moves that Owner to TRASH without promoting a Ref. Deleting a Ref occurrence sends Delete for that occurrence. Deleting an Owner occurrence already in TRASH sends permanent Delete: the client may promote a known Ref, but when it supplies no promotion the fully resident server promotes an unseen Ref if one exists and otherwise hard-deletes. The server expands only an omitted promotion; it does not replace a concrete client promotion.
- Undo and Redo load nothing. Client and server apply the same revisioned history action to their projected and canonical Histories respectively, so Undo reverses a hidden server promotion without loading its parent list; see [Finalize permanent Delete undo semantics](13-finalize-permanent-delete-undo.md).
