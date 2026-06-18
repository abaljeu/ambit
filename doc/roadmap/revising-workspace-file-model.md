# Revising Workspace File Model

## Current Truths

- The database is a node graph with an `owner` link.
- The `owner` link defines an unambiguous tree.
- The root is a `workspace` node.
- The root owns `workspaces`.
- `workspaces` owns `workspace` nodes.
- `workspace` nodes can own `directory`, `file`, and `normal` nodes.
- `directory` nodes can own `directory`, `file`, and `normal` nodes.
- `normal` nodes can own `directory`, `file`, and `normal` nodes.

## Context

- A node's context is its ancestry in the ownership tree.
- Context traversal only uses `workspace`, `directory`, and `file` nodes.
- `normal` nodes are ignored for context traversal.

## Target Concept

- The editor should be free-form.
- Only `workspaces` and `workspace` nodes stay structurally restricted.
- All other node types can be organized freely.
- A `file` may own `file` or `directory` nodes for outline structure.
- Disk placement is still based on nearest owning directory ancestor.
- If a node is inside a file-owned subtree, it persists beside the parent file's directory.

## References
All references are resolved according to context.

- Workspace namespace: `/` addresses objects in that workspace tree.
- Directory namespace: address as `dir / member`.
- Directory members are not globally addressable without first naming the directory.
- Inside a directory, `./` addresses the current directory.
- File, directory, and workspace nodes also provide namespace scope for `normal` nodes.
- From within a file tree (or directory or workspace), `^` resolves to the owning special node and `^name` finds the `name` under that node.

## Server File Persistence

- Not fully implemented yet.
- Database persistence of file names is implemented.

### Workspace Persistence

- A workspace persists like a special directory.
- Workspace `wsname` persists as `@wsname` under the server data directory.

### Directory Persistence

- Every directory (including workspace directories) persists under its owning directory.
- Root content persists directly in the data directory.
- `normal` nodes directly owned by a directory persist in `.amb` in that directory.

### File Persistence

- A file persists by writing the tree it owns according to the file format.
- Persistence traversal stops descending when another special node is reached.
- That special node and its descendants persist as their own file or directory.
- The parent file persists a reference to that child special node.
- "Stops descending" means skip recursion into that child tree, while continuing full traversal of the current file tree.
