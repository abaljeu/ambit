# Infinite Outliner

Ambit is an infinite outliner.

It simultaneously maps the outline to files and directories on the server.

Nodes can be linked.
Nodes have a canonical owner.  They also have references.

Nodes have applied CSS classes.

## Fold

These commands act on the selected nodes and on the open nodes under them.
An open node shows its children.
Each press changes one level.

[x] Cursor unfold right opens the nearest folded level. Key: Shift+ArrowRight.
[x] Cursor fold left closes the deepest open level. Keys: Shift+ArrowLeft and A.
[x] The next Cursor fold left press closes the next shallower level, until the selected nodes are closed.
[x] Cursor unfold right and Cursor fold left undo each other. From a folded selection, the same number of presses of each command restores that folded selection.
[x] Cursor fold left does nothing when no node in that scope is open.
[x] Cursor unfold right does nothing when no node in that scope is folded.
[x] The selection stays on the same nodes.
[x] Cursor left to parent closes every selected node when any selected node is open. Keys: ArrowLeft and a.
[x] When no selected node is open, Cursor left to parent moves the selection to the parent.

You open a deep outline one level at a time.
Cursor fold left closes the levels that Cursor unfold right opened.
Keys: [Commands](doc/user/reference/commands.md).
