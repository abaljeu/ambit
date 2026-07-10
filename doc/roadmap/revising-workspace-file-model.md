# Revising Workspace File Model

## Current Truths

- The database is a node graph with an `owner` link.
- The `owner` link defines an unambiguous tree.
- The root is a `workspace` node.
- The root owns `workspaces`.
- `workspaces` owns `workspace` nodes.
- `workspace` nodes can own `directory`, `file`, and `normal` nodes.
- `directory` nodes can own `directory`, `file`, and `normal` nodes.
- Owned `file` / `directory` may not sit under `normal` (or under `file` / `workspaces`); see [[workspace-file-directory-placement]].

## Context

- A node's context is its ancestry in the ownership tree.
- Context traversal only uses `workspace`, `directory`, and `file` nodes.
- `normal` nodes are ignored for context traversal.

## Target Concept

- Outline editing stays flexible for `normal` nodes and refs.
- Structural restrictions: `workspaces` / named `workspace`, plus owned `file` / `directory` only under `workspace` or `directory` (including TRASH) — [[workspace-file-directory-placement]].
- A `file` may own `normal` content (and refs); it does not own other `file` / `directory` specials.
- Disk placement is still based on nearest owning directory ancestor.
- Nested documents (child workspace/directory/file roots) persist as separate artifacts; members of a nested document are not inlined in the parent document's payload.

## Documents

See [[doc/roadmap/postgres-roadmap.md]] §5 and [[doc/roadmap/workspace-file-model.md]] § Documents. Document membership — not "owned by a file" — determines which persisted artifact holds a node's serialized content.

## References
See [[doc/roadmap/reference-expression-interpretation.md]].

## Server File Persistence

- Not fully implemented yet.
- Database persistence of file names is implemented.
- **Primary:** live-save file and directory artifacts to the server `DataDir` on accepted graph changes.
- **Secondary:** desktop clients may download or export server content to workspace-mapped local paths.
- **Import unchanged:** desktop-local file → client graph edits → sync → server live-save persists.

### Workspace Persistence

- A workspace persists like a special directory.
- Workspace `wsname` persists as `@wsname` under the server data directory.

### Directory Persistence

- Every directory document (including workspace directories) persists under its owning directory on disk.
- Root content persists directly in the data directory.
- Normal nodes with document membership in that directory document persist in `.amb` in that directory.

### File Persistence

- A file document persists by writing its members according to the file format.
- Persistence traversal stops at each nested document root (child workspace, directory, or file special node).
- The nested document and its members persist as their own artifact.
- The parent document persists a reference to the nested document root.
- "Stops descending" means skip recursion into that nested document, while continuing serialization of the current document.
