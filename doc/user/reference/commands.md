# Commands

Category: Reference

See Also:

[Infinite Outliner](doc/user/guide/infinite-outliner.md)

Jump to Target opens the focused node.

Jump to Target: keys Alt+j and j.
File node in a named Workspace: opens the Server copy of that file in a new browser tab.
File node outside a named Workspace: does not open.
Workspace or Directory: does not open.
Other node: opens a link in a new browser tab.

These commands act on the selected nodes and on the open nodes under them.
An open node shows its children.
Each press changes one level.

[x] Cursor unfold right opens the nearest folded level. Key: Shift+ArrowRight.
[x] When a selected node is folded and has children, Cursor unfold right opens that node.
[x] When every selected node is already open, Cursor unfold right opens the nearest folded nodes under them.
[x] Cursor fold left closes the deepest open level. Keys: Shift+ArrowLeft and A.
[x] The next Cursor fold left press closes the next shallower level, until the selected nodes are closed.
[x] Cursor unfold right and Cursor fold left undo each other. From a folded selection, the same number of presses of each command restores that folded selection.
[x] Cursor fold left does nothing when no node in that scope is open.
[x] Cursor unfold right does nothing when no node in that scope is folded.
[x] The selection stays on the same nodes.
[x] Cursor left to parent closes every selected node when any selected node is open. Keys: ArrowLeft and a.
[x] When no selected node is open, Cursor left to parent moves the selection to the parent.
