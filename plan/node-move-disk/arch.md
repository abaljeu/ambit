# Node move and disk architecture

Spec: [[spec.md]] (user-facing Projects only. An internal Project may have no spec.md.)

Marks: [x] exists today. [ ] required. [o] obsolescent.

## 1. Bounds

In scope: An Owned File Node or Owned Directory Node whose derived path changes, including a nested path in the same change, a never-parsed File Node under a new Directory Node, a same-Workspace `git mv`, a cross-Workspace delete and add, and a change conflict amended before the change lands.

Out of scope: Workspace rename and Workspace relocation.

## 2. Types

[x] `DocumentPathMove` is in `src/Shared/DocumentPathMove.fs`.

```fsharp
type DocumentPathMove = {
    nodeId: NodeId
    oldPath: string
    newPath: string
}
```

## 3. Interfaces

### Derived path

[x] `expandedPathForNodeId` walks `node.owner`.

```fsharp
expandedPathForNodeId : Graph -> NodeId -> string option
```

[x] A Normal Node on that walk passes the walk to its owner.
[x] `GraphBuild.appendChildren` records `ownerParentByChild` for an `Ownership.Owner` edge.
[x] The private `GraphBuild.addOwnerEdges` records `ownerParentByChild` for an `Ownership.Owner` edge.
[x] A Ref edge stays out of `ownerParentByChild`.
[x] `GraphMutate.setName` returns `cannot rename a workspace` for a Workspace Node.
[x] `NodeRenameOps.isRenameAllowed` is false for a Workspace Node.

### Path comparison

[x] `planPathMovesBetweenGraphs` compares `expandedPathForNodeId` on the pre graph and the post graph.

```fsharp
planPathMovesBetweenGraphs : Graph -> Graph -> DocumentPathMove list
```

[x] A document root is a File Node, a Directory Node, or a Workspace Node.
[x] A move is recorded when both graphs have a path for that root and the paths differ.
[x] An illicit `.amb` name is skipped.
[x] `planPathMoveForSetName` and `planPathMoveForReparent` plan one root from a name or a parent.

### Path coalesce

[x] `coalescePathMoves` reads the pre-graph relative path.

```fsharp
coalescePathMoves : Graph -> DocumentPathMove list -> DocumentPathMove list
```

[x] A kept Directory move drops a later move whose old relative path has that Directory's old relative path as a strict prefix.
[x] The new path is not an input to that drop.
[x] A nested node whose new derived path is the path that Directory move produces is dropped.
[o] A nested node whose new derived path differs is dropped by that same prefix test.
[ ] A nested node whose new derived path differs stays in the list.

### Path class

[ ] `sameWorkspaceMoves` keeps a File Node or Directory Node whose `GraphQuery.enclosingWorkspace` id is the same on the pre graph and the post graph.

```fsharp
sameWorkspaceMoves : Graph -> Graph -> DocumentPathMove list -> DocumentPathMove list
crossWorkspaceMoves : Graph -> Graph -> DocumentPathMove list -> DocumentPathMove list
```

[ ] `crossWorkspaceMoves` keeps a File Node or Directory Node whose enclosing Workspace id differs.
[ ] A Workspace Node is in neither list.
[ ] `GraphQuery.enclosingWorkspace` is the Workspace id. The callee is `enclosingWorkspace : Graph -> NodeId -> NodeId option`.

### Accept validation

[x] `validatePathMoves` checks each coalesced move.

```fsharp
validatePathMoves : string -> Graph -> Graph -> Result<unit, string>
```

[x] A destination full path that exists, and that differs from the source full path, returns `disk path already exists`.
[x] `FileAgent.validatePostChange` calls `validatePathMoves` for a persisted change.
[x] A graph-only change returns Ok from `FileAgent.validatePostChange`.
[x] `DbAgent.validatePostChange` calls `validatePathMoves` when `liveSaveDataDir` is set.
[x] A graph-only change returns Ok from `DbAgent.validatePostChange`.
[x] `DbAgent.validatePostChange` returns Ok when `liveSaveDataDir` is None.
[o] That destination Error is the change-conflict result.

### Change amendment

[x] `applyPosted` amends a compare-and-swap mismatch.

```fsharp
applyPosted : bool -> EventId -> Ev -> Op list -> GraphAtEvent -> ApplyResult * bool * Op list
```

[x] The mismatch texts are `old text does not match`, `old name does not match`, `old classes do not match`, and `old span does not match`.
[ ] A change conflict is one of these cases.

- The destination path already exists.
- The source and the destination disagree.

[ ] `applyPosted` amends that conflict before the caller stores the graph.
[ ] The amended change is the change that lands.
[ ] The operation does not overwrite.
[ ] The operation does not merge.

### Post change

[x] `FileAgent.processPostEvents` runs `ChangeAmendment.applyPosted`, then `validatePostChange`.

```fsharp
processPostEvents : LoadedFile -> Ev list -> bool -> Result<CoreChangesAccepted, string>
```

[x] A validation Error returns before `loaded.state` is stored.
[x] `DbAgent.finishAppliedPostChange` runs `validatePostChange` after apply.
[x] A validation Error returns before `loaded.state` is stored in `DbAgent.finishAppliedPostChange`.
[ ] A change conflict goes to Change amendment, and the caller stores the amended graph.

### Disk relocate

[x] `executePathMoves` moves a file with `File.Move` and a directory with `Directory.Move` when the source path exists.

```fsharp
executePathMoves : string -> Graph -> Graph -> DocumentPathMove list -> Result<unit, string>
persistGraphChange : string -> Graph -> Graph -> Result<PersistGraphOk, string>
persistGraphOps : string -> Graph -> Graph -> Op list -> Result<PersistGraphOk, string>
```

[x] `persistGraphChangeWith` calls `executePathMoves`, then `DocumentPersistWrite.writeDocumentsSoft`.
[x] `persistGraphChange` and `persistGraphOps` call `persistGraphChangeWith`.
[x] Call sites of `persistGraphChange` and `persistGraphOps` outside their definitions are in `tests/Server.Tests`.
[o] `File.Move` and `Directory.Move` are the relocation.

### Same-workspace relocate

[ ] `gitMove` runs `git mv` in that Workspace repository.

```fsharp
gitMove : string -> string -> string -> Result<unit, string>
```

[ ] The arguments are the repository root, the old git path, and the new git path.
[ ] The repository root and the git path come from Workspace repository.
[ ] One same-workspace move is one `git mv`.
[ ] `git mv` of a Directory moves that Directory tree.
[ ] `GitSave.runGit` is the git process call. The callee is `runGit : string -> string -> Result<string, string>`.

### Cross-workspace move

[ ] `moveAcrossWorkspaces` deletes the path from the source Workspace repository and adds it to the destination Workspace repository.

```fsharp
moveAcrossWorkspaces : string -> string -> string -> string -> Result<unit, string>
```

[ ] The arguments are the source repository root, the source git path, the destination repository root, and the destination git path.
[ ] The destination work tree holds the File or Directory at the derived path before the destination repository adds it.
[ ] A File is `git add` in the destination repository and `git rm` in the source repository.
[ ] A Directory is `git add` of that directory in the destination repository and `git rm -r` in the source repository.
[ ] The repository root and the git path come from Workspace repository.
[ ] One cross-workspace move is one delete and one add.
[ ] `GitSave.runGit` is the git process call.

### Persist thread write

[x] `PersistThread.runRequest` calls `persistNode` when `CoreEventDispatch.persistedNode` is false.

```fsharp
runRequest : PersistThreadDeps -> PersistRequest -> unit
persistNode : string -> Graph -> NodeId -> Result<DateTime, string>
```

[x] `persistedNode` is true when persist state is Persisted.
[x] `persistNode` calls `DocumentPersistWrite.writeDocument`.
[x] `writeDocument` writes the artifact at the post-graph path.
[x] The old path is left in place.
[x] `CoreEventDispatch.pushChangeSpecials` calls `pushPersist` for each id from `DocumentOpImpact.owningSpecials`.
[ ] `PersistThread.runRequest` rejects the request at the top of the persist request stack when `PersistThread.unparsedNode` is true for that node. It does not call `persistNode`.
[ ] Today's code does not satisfy this rule. `runRequest` checks only `persistedNode`. The Unparsed check was dropped when [71 — Persist thread takes from the stack](../core-refinement/issues/71-persist-thread-takes-from-the-stack.md) (PR 330, 2026-10-21) moved the thread to take from the stack.

### Unparsed save gate

[x] `DocumentPartition.shouldWriteDocumentRoot` is true when `Node.documentStateEnum` is Current.

```fsharp
shouldWriteDocumentRoot : Node -> bool
```

[x] `shouldWriteDocumentRoot` is false when `Node.documentStateEnum` is Unparsed.
[x] `documentRootsAffectedByNodeIds` keeps a root when `shouldWriteDocumentRoot` is true.
[x] `persistGraphChange` and `persistGraphOps` write that root set.
[x] `writeAllDocuments` writes a root when `shouldWriteDocumentRoot` is true.
[x] `writeDocumentCore` resolves the artifact path and writes that file.
[x] `PersistThread.unparsedNode` is true when `parseState` is `ParseState.Unparsed`.
[x] `DbAgent.startFileDiff` pushes an id when `unparsedNode` is false and `opsNotifiedIds` omits that id.
[x] An id that `unparsedNode` accepts stays on `diffWorkspaces`.
[x] `CoreMailboxFilesAxis.pushParsedUnpersisted` calls `pushPersist` when parse state is Parsed and persist state is Unpersisted.
[x] Unparsed keeps a document root out of the graph-diff write set.
[x] Unparsed holds a workspace id off the DbAgent file-diff push.
[x] An Unparsed root still reaches `writeDocument` when `pushChangeSpecials` has pushed it and persist state is Unpersisted.

### Workspace repository

[x] `WorkspaceGit.isRepo` is true when `workspaceRoot/.git` exists.

```fsharp
WorkspaceGit.isRepo : string -> bool
WorkspaceGit.ensureInit : string -> Result<unit, string>
GitSave.isRepo : string -> bool
GitSave.runGit : string -> string -> Result<string, string>
DailyGitSave.discoverRepoRoots : string -> Result<string list, string>
DocumentPersistPath.workspaceRootFor : string -> Graph -> NodeId -> string option
```

[x] `DocumentPersistWrite.ensureWorkspaceRepo` calls `WorkspaceGit.ensureInit` when a Workspace Node is written and `WorkspaceGit.isRepo` is false.
[x] `GitSave.isRepo` is true when `dataDir/.git` exists.
[x] `GitSave.commitAll` excludes a child directory that is itself a repository.
[x] `DailyGitSave.discoverRepoRoots` returns the data directory when `GitSave.isRepo` is true, and each immediate child directory where `WorkspaceGit.isRepo` is true.
[x] `DailyGitSave.commitRoot` commits the data directory with `GitSave.commitAll` and a child Workspace with `WorkspaceGit.commitAll`.
[x] `DocumentPersistPath.workspaceRootFor` returns the named Workspace directory.
[x] `workspaceRootFor` returns None when the enclosing Workspace is ROOT.
[ ] The repository root of a named Workspace is `workspaceRootFor`.
[ ] The repository root of ROOT is the data directory.
[ ] `DocumentPartition.artifactFileRelative` and `DocumentPartition.artifactDirectoryRelative` are data-directory paths.
[ ] The git path of a named Workspace is that data-directory path without the workspace directory prefix.
[ ] The git path of ROOT is that data-directory path.

## 4. Flow paths

1. **Accept a persisted change**

[x] Post change runs Change amendment, then Accept validation. A validation Error returns before the graph is stored. Persist thread write runs for each pushed owning special.

```fsharp
Post change
Change amendment
Accept validation
Persist thread write
```

2. **Same-workspace relocate**

[ ] Path comparison, Path coalesce, and Path class select the same-workspace moves. A never-parsed File Node under a new Directory Node is one of those moves when its derived path changes and its enclosing Workspace id stays the same. Each selected move is one `git mv`.

```fsharp
Path comparison
Path coalesce
Path class
Same-workspace relocate
```

3. **Nested derived path**

[ ] Path coalesce keeps a nested File or Directory whose new derived path differs from the path the kept Directory move produces. Path class sends that move to Same-workspace relocate or to Cross-workspace move.

```fsharp
Path comparison
Path coalesce
Path class
```

4. **Cross-workspace move**

[ ] Path class selects a File or Directory whose enclosing Workspace id changes. Cross-workspace move deletes that path from the source repository and adds it to the destination repository.

```fsharp
Path comparison
Path coalesce
Path class
Cross-workspace move
```

5. **Change conflict**

[ ] Change amendment amends the conflict before Post change stores the graph.

```fsharp
Post change
Change amendment
Accept validation
```

## 5. Module map

1. **Derived path**
   - Interface: `src/Shared/NodeDesktopPath.fs` `expandedPathForNodeId`
   - Uses:
     1. [x] none
2. **Path comparison**
   - Interface: `src/Shared/DocumentPathMove.fs` `planPathMovesBetweenGraphs`
   - Type `DocumentPathMove` in `src/Shared/DocumentPathMove.fs`
   - Uses:
     1. [x] Derived path
3. **Path coalesce**
   - Interface: `src/Shared/DocumentPathMove.fs` `coalescePathMoves`
   - Uses:
     1. [x] Path comparison
4. **Path class**
   - Interface: `sameWorkspaceMoves`, `crossWorkspaceMoves`
   - Uses:
     1. [ ] Path comparison
5. **Accept validation**
   - Interface: `src/Server/DocumentPersistChange.fs` `validatePathMoves`
   - Uses:
     1. [x] Path comparison
     2. [x] Path coalesce
6. **Change amendment**
   - Interface: `src/Shared/ChangeAmendment.fs` `applyPosted`
   - Uses:
     1. [ ] Accept validation
7. **Post change**
   - Interface: `src/Server/Core/FileAgent.fs` `processPostEvents`
   - Interface: `src/Server/Core/DbAgent.fs` `finishAppliedPostChange`
   - Uses:
     1. [x] Change amendment
     2. [x] Accept validation
8. **Disk relocate**
   - Interface: `src/Server/DocumentPersistChange.fs` `executePathMoves`
   - [o] `File.Move` and `Directory.Move`
   - Uses:
     1. [x] Path comparison
     2. [x] Path coalesce
9. **Same-workspace relocate**
   - Interface: `gitMove`
   - Uses:
     1. [ ] Path class
     2. [ ] Workspace repository
10. **Cross-workspace move**
    - Interface: `moveAcrossWorkspaces`
    - Uses:
      1. [ ] Path class
      2. [ ] Workspace repository
11. **Persist thread write**
    - Interface: `src/Server/Core/PersistThread.fs` `runRequest`
    - Interface: `src/Server/DocumentPersistChange.fs` `persistNode`
    - Uses:
      1. [x] none
12. **Unparsed save gate**
    - Interface: `src/Shared/DocumentPartition.fs` `shouldWriteDocumentRoot`
    - Uses:
      1. [x] none
13. **Workspace repository**
    - Interface: `src/Server/WorkspaceGit.fs` `ensureInit`
    - Interface: `src/Server/GitSave.fs` `runGit`
    - Interface: `src/Server/DailyGitSave.fs` `discoverRepoRoots`
    - Interface: `src/Server/DocumentPersistPath.fs` `workspaceRootFor`
    - Uses:
      1. [x] none

## 6. Seams

1. [x] **Path comparison** / **Derived path** — ``planPathMovesBetweenGraphs includes file reparent across workspaces`` in `tests/Shared.Tests/DocumentPathMoveTests.fs`.
2. [x] **Path coalesce** / **Path comparison** — ``coalescePathMoves drops nested roots covered by directory move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs`.
3. [x] **Disk relocate** / **Path comparison** — ``persistGraphChange moves renamed file artifact before writing`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs`.
4. [x] **Disk relocate** / **Path comparison** — ``persistGraphChange rejects destination artifact conflict`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs`.
5. [x] **Accept validation** / **Path coalesce** — ``validatePathMoves refuses rename of non-allowlisted SYSTEM file`` in `tests/Server.Tests/DocumentPersistChangeTests.fs`.
6. [ ] **Path coalesce** / **Path class** — ``coalesce keeps a nested node whose derived path differs`` in `tests/Shared.Tests/DocumentPathMoveTests.fs`.
7. [ ] **Same-workspace relocate** / **Workspace repository** — ``same-workspace relocate runs git mv in the Workspace repository`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs`.
8. [ ] **Cross-workspace move** / **Workspace repository** — ``cross-workspace move deletes from the source repository and adds to the destination repository`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs`.
9. [ ] **Change amendment** / **Post change** — ``a change conflict is amended before the graph is stored`` in `tests/Server.Tests/DocumentPersistChangeTests.fs`.

## 7. Slicing strategy

[ ] tracer-cut

The cuts are the flow paths marked [ ].

A refactor slice keeps current behavior.

A behavior change is a build slice.

## 8. Open questions

1. Same-Workspace never-parsed enclosure — Is a never-parsed File Node whose enclosing artifact becomes a Workspace Node in the same Workspace in scope?
2. Cross-Workspace commit — Are the cross-Workspace delete and the cross-Workspace add committed at move time, or at that repository's next commit?
3. Disagree comparison — Which comparison shows that the source and the destination disagree?
4. Conflict ops — Which ops does Change amendment emit for a change conflict?
5. Relocate caller — Which caller runs Same-workspace relocate and Cross-workspace move?
6. Conflict data directory — How does Change amendment obtain the data directory for the conflict check?
7. Unparsed during the move — Does the move set Unparsed on the moved document root for the time the disk relocation runs?
