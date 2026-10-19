# Files

Category: Reference

See Also:

[Commands](doc/user/reference/commands.md)

These commands open a node, move files, or change a name.

## Jump to Target

Keys: Alt+J and `j`. While you are selecting or editing.

What it does: A File node in a named Workspace opens in a new browser tab. A File node outside a named Workspace, a Workspace, and a Directory do not open. Any other node opens a link from its text, or from its first child, in a new browser tab.

## Load

Keys: Ctrl+Shift+`>`. While you are selecting.

What it does: git Load pulls files when a git upstream is defined. desk Load uploads the selected Workspace, Directory, or File when the Desktop app has a workspace mapping. After that, the rest of Load is the same, and a selection that spans more than one Workspace does not load.

## Save

Keys: Ctrl+S. While you are selecting or editing.

What it does: Sends a save of the current work. The command palette also lists git Save and desk Save, which run Save.

## Download

Keys: Ctrl+Shift+`<`. While you are selecting.

What it does: Copies the workspace that contains the focus onto the mapped folder on this computer. Download does nothing when this Ambit cannot sync a workspace.

## Insert…

Keys: `f`. While you are selecting or editing.

What it does: Opens a search to insert a file or a folder at the focus. On the Workspaces node, the search is for a workspace.

## Rename

Keys: F2. While you are selecting.

What it does: Opens a prompt with the focused node's name. A Workspace cannot be renamed.
