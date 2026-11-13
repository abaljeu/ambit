# Files

Category: Reference

See Also:

[Commands](doc/user/reference/commands.md)

These commands open a node, move files, or change a name.

## Jump to Target

Keys: Alt+J and `j`. Any Mode.

What it does: A File node in a named Workspace opens in a new browser tab. A text file shows in that tab. A File node outside a named Workspace, a Workspace, and a Directory do not open. Any other node opens a link from its text, or from its first child, in a new browser tab.

## Load

Keys: Ctrl+Shift+`>`. Selection Mode.

What it does: git Load pulls files when a git upstream is defined. desk Load uploads the selected Workspace, Directory, or File when the Desktop app has a workspace mapping. After that, the rest of Load is the same, and a selection that spans more than one Workspace does not load.

## Save

Keys: Ctrl+S and Ctrl+Shift+`<`. Any Mode.

What it does: Plain Save runs git Save when the focus Workspace has a git remote, with or without a local mapping. ROOT uses the data directory as that repository. A Workspace with a local mapping and no git remote runs desk Save. desk Save copies the focused Workspace, Directory, or File onto the mapped folder on this computer when the Desktop app can sync a workspace. When the Desktop app cannot sync, desk Save skips that copy. A Workspace with no local mapping and no git remote persists the focus. When that folder is a git repository, Save commits it and does not pull or push. When that folder has no `.git` directory, Save skips the commit. It does not copy files. The command palette lists git Save and desk Save, and each one keeps its path. A selection that spans more than one Workspace does not save.

## Insert…

Keys: `f`. Any Mode.

What it does: Opens a search to insert a file or a folder at the focus. On the Workspaces node, the search is for a workspace.

## Rename

Keys: F2. Selection Mode.

What it does: Opens a prompt with the focused node's name. A Workspace cannot be renamed.
