# Op

Category: Information

See Also:

- [Mailbox](mailbox.md)
- [Operations](operations.md)
- [Workspace graph](workspace-graph.md)
- [Parse and persist](parse-persist.md)

A single Graph modification, to a Node Header or to its Children.

## Sources

[History](../../src/Shared/History.fs)
[Graph mutate](../../src/Shared/GraphMutate.fs)

## Shape

[x] NewNode: node id, text. Apply adds a detached Node. Undo removes that Node.
[x] SetText: node id, old text, new text.
[x] SetClasses: node id, old classes, new classes.
[x] Replace: parent id, old Children, new Children. Apply calls `replace` at index 0.
[x] NewSpecialNode: node id, kind, name. Apply adds a detached Special Node with DocumentState Unparsed. Undo removes that Node.
[x] SetName: node id, old name, new name.
[x] SetDocumentState: node id, old DocumentState, new DocumentState.
[x] SetUpdateTime: node id, old time, new time. Apply writes the new stamp. Undo writes the old stamp. Apply ignores an old-time mismatch.

## Invariants

[x] Apply result: Changed, Unchanged, or Invalid. Invalid returns the input state.
[x] NewNode: the canonical root id is refused.
[x] NewSpecialNode: a canonical id, kind Workspaces, a reserved system name, or an invalid filename is refused.
[x] SetDocumentState: the stored DocumentState must match the old value. `setDocumentState` writes DocumentState.
[x] A content Op on a member of an Unparsed document is refused. SetUpdateTime and NewSpecialNode stay allowed. Replace exceptions: `isBlockedByInaccessibleDocument`.
[x] invert swaps old and new on SetText, SetClasses, Replace, SetName, SetDocumentState, and SetUpdateTime.
[x] invertAll reverses the list and drops NewNode and NewSpecialNode.
[x] SetText, SetClasses, SetName, and Replace mark the enclosing axis carrier Unpersisted. commandName Parse leaves PersistState unchanged. `marksOwningPersist`.

[o] `Op.SetPersistState`: not a writer.  
[o] `Op.SetDocumentState`: not the writer of the parsed axis.
[ ] Op.SetPersistState and Op.SetDocumentState replaced by [[InMsg]] calls because these track system status.

## Store

[x] Change, Undo, and Redo: an Op list on the Event body.
[x] ActorStart and ActorStop: an Actor record. No Op list.

## Explanation

A content Op changes the Graph away from disk, so the enclosing carrier becomes Unpersisted and Persist can write the file. A Parse Action copies disk into the Graph, so that apply leaves PersistState unchanged. DocumentState and the parsed axis are different fields.
