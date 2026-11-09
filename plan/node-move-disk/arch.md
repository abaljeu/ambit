# Node move and disk architecture

Spec: [[spec.md]] (user-facing Projects only. An internal Project may have no spec.md.)

Marks: [x] exists today. [ ] required. [o] obsolescent.

## 1. Bounds

In scope: An Owned File Node or Owned Directory Node whose derived path changes, including a nested path in the same change, a File Node moved under a new Directory Node, a same-Workspace `git mv`, a cross-Workspace delete and add, and a change conflict amended before the change lands.

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
[ ] A change conflict is two files going to the same spot: one existing file already at the destination path, and one incoming move or rename that would put a new file at that path. It is not two changes.

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

### Move write gate

Research: [03 — Research: directory-level persist gate for moves](issues/03-research-directory-level-persist-gate.md).

[ ] While a git move of a Directory or a Workspace runs, no write is allowed under that Directory or that Workspace until the move completes.
[ ] The gate applies to any Directory or Workspace being moved.
[ ] The gate generalizes the git pull design, which blocked all persists until the pull completed. That design was Workspace-wide only.
[ ] The move does not set Unparsed on the moved document root. The gate replaces that approach.

#### Node lock

[ ] The gate is a lock on one node. That node is a Directory Node or a Workspace Node.
[ ] The lock records the node, not the path. Every node under the locked node is locked. The lock does not check whether a path matches.
[ ] git pull locks the Workspace Node. A Directory move locks the Directory Node.
[ ] The holder is the git pull caller or the relocate caller. The hold lasts for that git operation.
[ ] The node lock is the [core-refinement architecture](../core-refinement/arch.md) §6 workspace lock, generalized to a Directory Node. §6 calls it the aggregate lock.
[ ] A per-member persist lock is the write of one document root. It is §6 **Per-member persist locks**.
[ ] `writeDocument` and `writeArtifactText` grant the per-member persist lock around the file bytes. The grant stores every owner-chain ancestor id from `GraphQuery.enclosing`, plus the document root.
[ ] The callee is `GraphQuery.enclosing`. It walks the owner chain and includes the start node.
[ ] After the post graph is stored, a descendant's owner chain still contains the moved Directory Node. One id covers the source path and the destination path.

#### What the lock holds off

[ ] Node lock acquire marks the lock pending. It then drains every in-flight per-member persist lock that stored that node id.
[ ] A new per-member persist lock waits while an ancestor node lock is pending or held.
[ ] A Directory lock holds off parse reads under that Directory the same way the Workspace lock does. It drains in-flight parse reads and holds new ones until it releases.
[ ] The moved Directory Node's own file waits with its descendants. That file is `DirName/.amb` inside the moved Directory.
[ ] A single File move holds that File Node's per-member persist lock for the `git mv`.
[ ] The Workspace lock covers the git Save commit. It also holds while git Save is doing a pull.
[ ] Sibling files in the source Workspace and the destination Workspace may keep writing during a cross-Workspace move. A sibling whose stored ancestor ids omit the locked node id takes its per-member persist lock.

#### Blocked persist

[ ] A persist blocked by an ancestor node lock treats it as a lock. The persist thread itself blocks and waits until the lock is available.
[ ] The persist thread does not set the node aside on a deferred list.
[ ] A blocked persist posts no `Notice.PersistFailed`.
[ ] This rests on the node lock being held well under ten seconds.
[ ] **Persist thread write** keeps its Unparsed reject. That is a separate rule. The move keeps the moved document root Parsed, so the node lock is what blocks persist for a move.

#### Work-tree gate and git process lock

[ ] Expand: the node lock stands beside `WorkspaceGit.withWorkTreeGate`. The holder marks the node lock pending, drains, takes `withWorkTreeGate`, runs git, releases `withWorkTreeGate`, and then releases the node lock.
[ ] While both exist, a persist takes its per-member persist lock first. It takes `withWorkTreeGate` only after that grant. It releases `withWorkTreeGate` before it releases the per-member persist lock.
[ ] Contract: `withWorkTreeGate` is removed from the persist paths and the git pull paths.
[ ] `GitRun.gitExec` stays the git process lock. It is taken after the node lock. `GitSave.runGit` calls `gitExec`.
[ ] Lock order is node lock, then `withWorkTreeGate` while both exist, then the `GitRun` lock.
[ ] A thread that holds the `GitRun` lock takes no node lock.
[ ] A holder takes one node lock. A Directory move holds no Workspace lock and no per-member persist lock.

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

[ ] Path comparison, Path coalesce, and Path class select the same-workspace moves. A File Node moved under a new Directory Node is one of those moves when its derived path changes and its enclosing Workspace id stays the same. Each selected move is one `git mv`.

[ ] A same-Workspace move moves the file on disk, whether or not the File Node or its enclosing Directory Node has been parsed. Parse state does not affect this rule. This holds when the strictly enclosing artifact becomes a Directory Node or the Workspace Node.

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

6. **Move under the node lock**

[ ] The relocate caller locks the moved Directory Node, or holds the moved File Node's per-member persist lock. The lock drains in-flight persists and parse reads under that node. Same-workspace relocate or Cross-workspace move runs git. The caller releases the lock. A persist that waited on the lock then writes.

```fsharp
Move write gate
Same-workspace relocate
Cross-workspace move
Persist thread write
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
11a. **Move write gate**
    - Uses:
      1. [ ] Same-workspace relocate
      2. [ ] Cross-workspace move
      3. [ ] Persist thread write
      4. [ ] Workspace repository
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

1. Same-Workspace Workspace enclosure — Resolved (Alan 2026-11-09): yes. When a move makes a File Node's strictly enclosing artifact the Workspace Node, in the same Workspace, the file moves on disk, the same as under a Directory Node. Parse state does not matter: the File moves whether or not it or its enclosing Directory Node has been parsed. **Same-workspace relocate** states the rule.
2. Cross-Workspace commit — Resolved (Alan 2026-11-09): commit at move time. The cross-Workspace delete and the cross-Workspace add are each committed when the move runs, not at that repository's next commit. Git has no cross-repository transaction, so the move is not atomic across the two repositories. Committing at move time keeps the half-moved window to one step. If the add fails, there is a recovery path: re-add from the source, or flag the half-moved node.
3. Conflict comparison — Resolved (Alan 2026-11-09): a path comparison. The File Node's derived destination path is checked against the disk for a file that already exists at that path. A pre-existing file there is the conflict.
4. Conflict ops — Which ops does Change amendment emit for a change conflict?
5. Relocate caller — Which caller runs Same-workspace relocate and Cross-workspace move?
6. Conflict data directory — How does Change amendment obtain the data directory for the conflict check?
7. Unparsed during the move — Resolved (Alan 2026-11-09): no. The move does not set Unparsed on the moved document root. **Move write gate** holds writes under the moved Directory or Workspace until the move completes. Its lock design comes from [03 — Research: directory-level persist gate for moves](issues/03-research-directory-level-persist-gate.md) and Alan's answers there (2026-11-09).
