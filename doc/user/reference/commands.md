# Commands

Category: Reference

Jump to Target opens the focused node.

Jump to Target: keys Alt+j and j.
File node in a named Workspace: opens the Server copy of that file in a new browser tab.
File node outside a named Workspace: does not open.
Workspace or Directory: does not open.
Other node: opens a link in a new browser tab.

Cursor unfold right opens folded nodes in the selection, one level for each press.
The scope is the selected nodes and the open nodes under them.
When a selected node is folded and has children, the command opens that node.
When every selected node is already open, the command opens the nearest folded nodes under them.
When no node in the scope is folded, the command does nothing.
Keys: Shift+ArrowRight.

Cursor fold left closes the deepest open nodes in that same scope, one level for each press.
An open node shows its children.
The command closes every open node at the deepest level in the scope.
The next press closes the next shallower level.
Repeated presses close levels until the selected nodes are closed.
When no node in the scope is open, the command does nothing.
From a folded selection, the same number of Cursor unfold right presses and then Cursor fold left presses restore that folded selection.
Keys: Shift+ArrowLeft and A.

Cursor left to parent closes every selected node when any selected node is open.
When no selected node is open, the command moves the selection to the parent node.
Keys: ArrowLeft and a.
