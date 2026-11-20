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

What it does: A git pull happens only when the subject is a Workspace. Workspace Load runs the transport step (desk Load when only a local mapping exists, git Load pull when a git remote exists, nothing otherwise), then reconciles that Workspace. ROOT is a Workspace with no work tree, so its Load runs no transport and then reconciles. A failed step aborts with no reconcile. Any Load (plain or git Load) of a File, a Directory, or another non-Workspace node does no git pull or push and only reconciles that selected node from disk. desk Load uploads the selected Workspace, Directory, or File when the Desktop app has a workspace mapping. A selection that spans more than one Workspace does not load.

## Save

Keys: Ctrl+S and Ctrl+Shift+`<`. Any Mode.

What it does: `Ctrl+S` and `Ctrl+Shift+<` both run Save. Plain Save runs git Save when the focus Workspace has a git remote, with or without a local mapping. ROOT uses the data directory as that repository. A Workspace with a local mapping and no git remote runs desk Save. desk Save posts the data-directory save when that save is enabled. A Workspace with no local mapping and no git remote persists the focus. When that folder has its own `.git` directory, Save commits it and leaves pull and push for a remote. The command palette lists git Save and desk Save, and each one keeps its path. A selection that spans more than one Workspace does not save.

## Insert…

Keys: `f`. Any Mode.

What it does: Opens a search to insert a file or a folder at the focus. On the Workspaces node, the search is for a workspace.

## Rename

Keys: F2. Selection Mode.

What it does: Opens a prompt with the focused node's name. A Workspace cannot be renamed.
