# Graph

Category: Information

See Also

[Workspace local mapping](workspace-local-mapping.md) Local filesystem mapping for `//label/relative` paths.
[Desktop local files](desktop-local-files.md)
[Workspace file sync](workspace-file-sync.md)
[Workspace stage plan](workspace-stage-plan.md)
[Workspace file model](doc/roadmap/workspace-file-model.md) TRASH.
[Reference expression interpretation](doc/roadmap/reference-expression-interpretation.md) Reference expression grammar.
[Revising the workspace file model](doc/roadmap/revising-workspace-file-model.md) Authority for context.
[Workspace file directory placement](doc/roadmap/workspace-file-directory-placement.md) Artifact names.

The graph is the central data structure of the outliner.  There are rules for workspace special nodes, and the graph layer enforces those rules.

## Shape

[x] Stable `NodeId` values: `Graph` in [Model](src/Shared/Model.fs).

### Canonical special nodes

[x] Root id: `00000000-0000-0000-0000-000000000000`. Text: `ROOT`. Kind: nameless `Special Workspace`.
[x] Trash id suffix: `…000000000001`. Text: `Trash`. Kind: `Special Directory`.
[ ] `SpecialKind.Trash` is retired. TRASH: `Special Directory` with `Node.name = TRASH`. Permanence and delete semantics stay the same: `MoveToTrash` reparents the owner under `trashId`. Path: `//TRASH/`. UI trash styling maps by `trashId`, not by kind.
[x] Workspaces id suffix: `…000000000002`. Text: `Workspaces`. Kind: `Special Workspaces`.
[x] Every graph from `Graph.fromNodes` or `Graph.create` has exactly one Owner child `Workspaces` under root.
[x] Every such graph has exactly one Owner child `Trash` under root.
[x] Root order: user root children come first, then `Workspaces`, then `Trash`.
[x] `Workspaces` and `Trash` cannot be edited with `setText` or `setClasses`. `replace` on root rejects their removal or duplication.
[x] TRASH cannot be renamed. `setName` rejects `trashId`.

### Serialization

[x] `Serialization.encodeNodeKind` and `decodeNodeKind` support every `SpecialKind` discriminator: `workspaces`, `workspace`, `directory`, `file`, and `trash`.
[ ] The `trash` discriminator is retired. TRASH persists as `directory` with `Node.name = TRASH`.
[x] Kind is stored on each node in the graph JSON payload.

### Document state

[x] `Current`: graph content represents the server artifact.
[x] `Unparsed`: a server artifact exists and has not been parsed into current graph content.
[x] `NoServerFile`: a File node has no server body.
[x] `NoServerFile` and Unparsed documents block content edits.
[x] Desktop Upload creates new File stubs as `NoServerFile`.
[x] A successful PUT, or an already-present mtime skip, changes matching paths to Unparsed before Parse.
[x] Directories keep their existing state behavior.
[o] `DocumentState` (`Current` | `Unparsed` | `NoServerFile`): the special-node document axis in graph JSON and the PostgreSQL node projection.
[ ] `DocumentState`: not the special-node document axis.
[x] `ParseState` and `PersistState` markers exist on Workspace, Directory, and File content nodes. A Directory File does not carry those axes. Values: `ParseState` (`Parsed` | `Unparsed`) and `PersistState` (`Persisted` | `Unpersisted`).
[ ] A state change on a special node writes `ParseState` and `PersistState`.
[x] The filesystem file whose basename is exactly `.amb`, ignoring case, is the persistence and proxy artifact of its containing Directory document, or of the Workspace document at workspace root.
[x] That file never has a graph node of its own. It must not appear as a child File.
[x] `Filename.create`, `NewSpecialNode`, and `SetName` reject that exact basename. Create helpers fall back to a default name.
[x] If an illicit `.amb`-named node exists, trash, delete, and rename do not plan or execute a `DataDir` move, delete, or write for it. The artifact stays discoverable and transferable so its content is preserved.
[x] Names such as `notes.amb` are ordinary File nodes.

### Reference expressions

[x] Code: `RefExprTypes.fs`, `RefExprParse.fs`, and `RefExprMatch.fs`. Facade: `RefExpr.fs`.
[x] Anchors: context with no prefix, `/`, `//`, `.`, `^`, and `#`.
[x] Path steps: `DirStep` (`name/`), `FileStep` (`name`), `**`, and glob patterns in names.
[x] A tag step `#name` matches named `normal` nodes by `Node.name` inside the content scope.
[x] `refContext` walks the owner chain for the workspace root, the current directory, the structural container, and the tagged normal ancestor. When no workspace root is found, the workspace root is ROOT.
[x] `match_` resolves expressions against the graph for search.
[ ] The reference expression grammar includes postfixes (`.text`, `[n]`, filters), command and assignment syntax, and the view-root anchor.

### Reference search

[x] The search dialog merges two result sources. Code: [ViewModelSearch](src/Shared/ViewModelSearch.fs).
[x] Namespace-style queries are parsed and matched first.
[x] The second source is node text matching.
[ ] Every keypress recomputes on the client only and updates the Find dialog immediately. Code: [ViewModelSearch](../../src/Shared/ViewModelSearch.fs), [Search dialog](../../src/Client/SearchDialog.fs).
[ ] A keypress sends no server message.
[ ] Move uses this same client path when Move recomputes on each keypress.
[ ] Server start after the quiet gap: [Search Actor](search-actor.md).
[x] Workspace nodes expose `//label` as their desktop file path through `NodeDesktopPath`. The file-status indicator uses that path.

## Invariants

### Context

[x] A node's context is its ancestry along the ownership tree. The walk keeps `workspace`, `directory`, and `file` special nodes. It skips `normal` nodes.
[x] Context drives reference resolution. Context does not restrict where nodes may be placed.

### Structural invariants

[x] `Graph.replace` enforces these rules. A separate command layer does not.
[x] The owner of `Workspaces` is root only. The node is permanent and canonical.
[x] The owner of a `Workspace` is `Workspaces` only.
[x] For a `Directory`, the owner chain must reach a `Workspace` (ROOT or named) or a `Directory` (including TRASH) before any `File`. `Normal` and `Workspaces` are skipped on that walk.
[x] A `File` uses the same owner rule as a `Directory`.
[x] A `Normal` node may be owned anywhere.
[x] ROOT is the implicit nameless workspace: `Special Workspace` with no filename.
[x] Named `Workspace` nodes stay under `Workspaces` only.
[x] Ref links are unrestricted.
[x] `Workspaces` and `Trash` are not children of any non-root parent.
[x] An owned `File` or `Directory` may sit under `Normal` or `Workspaces` when a Workspace or Directory ancestor ends the owner chain.
[x] A `File` ancestor of an owned `File` or `Directory` is illegal.
[x] Names that persist into the same system directory are unique among owned File, Directory, and named Workspace nodes in that artifact directory.
[x] Tests: [Model tests](tests/Shared.Tests/ModelTests.fs), the workspaces bootstrap cases and the placement cases.

### Workspace lifecycle

[x] Workspace nodes are created through the general change-op surface. The name is fixed at creation. Create: `Op.NewSpecialNode(nodeId, Special Workspace, name)`, then `Op.Replace` to attach the node under `Graph.workspacesId`.
[ ] Insert creates a workspace under `Workspaces`, or a `Special Directory` or `Special File` as an owner child of the focus. Pick-existing insert through search stays as it is.
[x] A workspace name is immutable after creation. `Graph.setName` rejects `Special Workspace` with `cannot rename a workspace`. `NodeRenameOps.isRenameAllowed` is false, so F2 and Rename do not open a prompt.
[x] Directory, File, and Normal nodes rename through `Op.SetName` with `Graph.setName` validation. Uniqueness in an artifact directory ignores case. Invalid filename characters are rejected.
[ ] F2 rename is `Op.SetName` for a directory or a file. A normal node rename changes `Node.name` only. Workspace rename stays refused. Edit node keeps Enter only.
[x] Named workspaces and Root-owned Files and Directories share one `DataDir` top-level namespace. Comparison ignores case. `Graph.replace` and `Graph.setName` reject a collision. Create planners auto-rename with the unused-name helpers.
[x] Nested File and Directory names collide only inside the same artifact directory. That directory is the nearest Workspace or Directory on the owner chain.
[x] The canonical `Workspaces`, `Trash`, and `ROOT` ids cannot be renamed.
[x] No dedicated workspace-removal op.
[x] Soft delete reparents the owner under `trashId` (`MoveToTrash`).
[x] Tests: [Workspace ops tests](tests/Shared.Tests/WorkspaceOpsTests.fs).

## Store

### Bootstrap and round-trip

[x] `Graph.fromNodes` calls `ensureWorkspacesNode`, then `ensureTrashNode`, before it rebuilds parent maps.
[x] `Snapshot.write` and `Snapshot.read` use the canonical sid `#WORKSPACES`, parallel to `#TRASH`.
[x] The Trash owner line has no name token.
[ ] `#TRASH` owner lines include the name token `TRASH`.
[x] `GraphProjection.graphFromPersistence` assigns `Special Workspaces` when the node id is `Graph.workspacesId`.
[x] Existing graphs and empty outlines gain the `Workspaces` node on load. No migration step is required.
