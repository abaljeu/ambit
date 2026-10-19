# Op

Category: Information

See Also:

- [Mailbox](mailbox.md)
- [Operations](operations.md)
- [Workspace graph](graph.md)
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
[o] SetDocumentState: node id, old DocumentState, new DocumentState.
[ ] `Notice.NodeUnpersisted` of `nodeId: NodeId` is the Unpersisted mark. [Message shape](../../plan/core-refinement/cqrs-messages.md) decision 12 NodeUnpersisted.
[x] SetUpdateTime: node id, old time, new time. Apply writes the new stamp. Undo writes the old stamp. Apply ignores an old-time mismatch.

## Invariants

[x] Apply result: Changed, Unchanged, or Invalid. Invalid returns the input state.
[x] NewNode: the canonical root id is refused.
[x] NewSpecialNode: a canonical id, kind Workspaces, a reserved system name, or an invalid filename is refused.
[o] SetDocumentState: the stored DocumentState must match the old value. `setDocumentState` writes DocumentState.
[ ] The Unpersisted mark is `Notice.NodeUnpersisted` of `nodeId: NodeId`. [Message shape](../../plan/core-refinement/cqrs-messages.md) decision 12 NodeUnpersisted.
[x] A content Op on a member of an Unparsed document is refused. SetUpdateTime and NewSpecialNode stay allowed. Replace exceptions: `isBlockedByInaccessibleDocument`.
[o] invert swaps old and new on SetText, SetClasses, Replace, SetName, SetDocumentState, and SetUpdateTime.
[ ] invert swaps old and new on SetText, SetClasses, Replace, SetName, and SetUpdateTime. [Message shape](../../plan/core-refinement/cqrs-messages.md) decision 12 NodeUnpersisted.
[x] invertAll reverses the list and drops NewNode and NewSpecialNode.
[o] SetText, SetClasses, SetName, and Replace mark the enclosing axis carrier Unpersisted. commandName Parse leaves PersistState unchanged. `marksOwningPersist`.
[ ] After an ordinary edit, the server's `Change` handler posts `Notice.NodeUnpersisted`. Parse posts its Changes, then `Notice.NodeParsed`. [Message shape](../../plan/core-refinement/cqrs-messages.md) decision 4 One Change.

`Op` has no `SetPersistState` case.
[o] `Op.SetDocumentState`: not the writer of the parsed axis.

## Store

[o] Change, Undo, and Redo: an Op list on the Event body.
[ ] `EventBody` is `Order of PlacedOrder | Notice of Notice`. `Order.Change` carries an `Op` list. `Order.Undo` and `Order.Redo` carry the target `EventId`. [Message shape](../../plan/core-refinement/cqrs-messages.md) decision 1 Logged as placed and decision 4 One Change.
[o] ActorStart and ActorStop: an Actor record. No Op list.
[ ] `Order.StartActor` carries `ActorStart`. `Notice.ActorStop` carries `NodeId * ActorResult`. [Message shape](../../plan/core-refinement/cqrs-messages.md) §2 StartActor and §2 Notice.

## Explanation

A content Op changes the Graph away from disk, so the enclosing carrier becomes Unpersisted and Persist can write the file. A Parse Action copies disk into the Graph, so that apply leaves PersistState unchanged. The parsed axis is `DocumentState.parseState` on the Node field `documentState`.
