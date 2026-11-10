# 31 — Migrate onto the workspace lock — implement scope

Ticket: [31 — Migrate onto the workspace lock](../issues/31-migrate-onto-the-workspace-lock.md). Project: [core-refinement](../project.md). Alan constraints dated 2026-11-10: Persist, parse-thread file use, and git Load and git Save (commit included) go through `NodeLockPool.withLock`; remove `WorkspaceGit.withWorkTreeGate` and the `GitRun` private gate in the same change with no dual-run; a Directory or Workspace lock still holds off parse reads because a parse read is a pool request; nobody takes two locks (`HolderHoldsLock`); Directory reconcile uses `withLock`; git fetch stays outside the lock and [32 — Workspace lock handoff](../issues/32-workspace-lock-handoff.md) owns the locked pull if assigned there.

## Signatures

[ ] `NodeLockPool.withLock` in `src/Server/NodeLockPool.fs` has type `Graph -> NodeId -> (unit -> 'a) -> Result<'a, NodeLockRejection>`.
[ ] `NodeLockRejection` in `src/Server/NodeLockPool.fs` has case `HolderHoldsLock` only; an id absent from `graph.nodes` runs `action` with no pool entry and returns `Ok`.
[ ] `WorkspaceGit.withWorkTreeGate` in `src/Server/WorkspaceGit.fs` has type `string -> (unit -> Result<'value, string>) -> Result<'value, string>`.
[ ] `WorkspaceGit.pullTracked` in `src/Server/WorkspaceGit.fs` wraps `trackedBranch` and `pullTrackedBranch` inside `withWorkTreeGate`.
[ ] `WorkspaceGit.saveTracked` in `src/Server/WorkspaceGit.fs` wraps `commitTracked` inside `withWorkTreeGate`, then calls `pushTrackedBranch` outside that gate.
[ ] `WorkspaceGit.pullTrackedBranch` in `src/Server/WorkspaceGit.fs` runs `git pull` via `GitSave.runGit` / `GitRun.gitExec` with no separate fetch API.
[ ] `DocumentPersistPath.withWorkTreeGates` in `src/Server/DocumentPersistPath.fs` nests `WorkspaceGit.withWorkTreeGate` over sorted workspace roots.
[ ] `DocumentPersistWrite.writeDocument` in `src/Server/DocumentPersistWrite.fs` has type `string -> Graph -> NodeId -> Result<DocumentWriteOk, string>` and gates via `workspaceRootFor` + `withWorkTreeGate`.
[ ] `DocumentPersistWrite.writeArtifactText` in `src/Server/DocumentPersistWrite.fs` is private and gates the core write the same way.
[ ] `DocumentPersistWrite.planParseFile` in `src/Server/DocumentPersistWrite.fs` has type `string -> Graph -> NodeId -> string option -> Result<Op list, string>` and calls `preparedParseText` with no `NodeLockPool`.
[ ] `DocumentPersistChange.persistNode` in `src/Server/DocumentPersistChange.fs` calls `DocumentPersistWrite.writeDocument`.
[ ] `GitRun.gitExec` in `src/Shared/dotnet/GitRun.fs` has type `string -> string -> Result<string, string>` and runs inside private `withWorkTreeGate` on the work directory.
[ ] `GithubTransportGit.withWorkTreeGate` in `src/Server/GithubTransportActor.fs` is record field `GithubTransportWorkTreeGate.applyAction`.
[ ] `GithubTransportActor.commitInGate` in `src/Server/GithubTransportActor.fs` calls `git.withWorkTreeGate.applyAction` before `commitAll` and the pull continuation.
[ ] `ParseThread.parseFile` and `ParseThread.parseDirectory` in `src/Server/ParseThread.fs` call `DocumentPersistWrite.planParseFile` and `DirectoryReconcile.planDirectoryReconcile` with no `NodeLockPool`.
[ ] `GraphQuery.enclosingWorkspace` in `src/Shared/GraphQuery.fs` has type `Graph -> NodeId -> NodeId option`.
[ ] `DocumentPartition.documentRootForNode` in `src/Shared/DocumentPartition.fs` has type `Graph -> NodeId -> NodeId option`.
[ ] `DocumentPartition.isDocumentRootNode` in `src/Shared/DocumentPartition.fs` is true for `Special Workspace`, `Special Directory`, and `Special File`.
[ ] `PersistThread.runRequest` in `src/Server/Core/PersistThread.fs` calls `deps.persistNode` with no lock; the ticket leaves this function unchanged.

## Match sites

[ ] `DocumentPersistWrite.writeDocument` lines 283–287 call `WorkspaceGit.withWorkTreeGate` when `DocumentPersistPath.workspaceRootFor` returns `Some root`.
[ ] `DocumentPersistWrite.writeArtifactText` lines 61–65 gate `writeArtifactTextCore` the same way; `preparedParseText` lines 80 and 86 invoke `writeArtifactText` or disk read without `NodeLockPool`.
[ ] `DocumentPersistWrite.writeAllDocuments` lines 366–371 call `DocumentPersistPath.withWorkTreeGates` before `writeDocuments`.
[ ] `DocumentPersistChange.persistGraphChangeWith` lines 206–212 build `workTreeRoots` and call `DocumentPersistPath.withWorkTreeGates` around path moves and `writeDocumentsSoft`.
[ ] `DocumentPersistChange.persistNode` line 266 is the persist-thread write seam into `writeDocument`.
[ ] `ParseThread.parseFile` line 85 calls `DocumentPersistWrite.planParseFile` for a File Node parse read.
[ ] `ParseThread.parseDirectory` lines 104–109 call `DirectoryReconcile.planDirectoryReconcile` for Directory reconcile with no lock.
[ ] `WorkspaceGit.pullTracked` lines 277–280 hold `withWorkTreeGate` for the full pull.
[ ] `WorkspaceGit.saveTracked` lines 318–321 hold `withWorkTreeGate` for commit; push runs after release.
[ ] `GithubTransportActor.commitInGate` line 78 is the git Load pre-commit and pull entry; `pullTracked` line 83 and `commitAndPull` line 309 use it for Load and Save.
[ ] `GithubTransportActor.workspaceGit` lines 399–407 wire `withWorkTreeGate` to `WorkspaceGit.withWorkTreeGate` and expose `pullTracked = WorkspaceGit.pullTrackedBranch` (no outer gate on branch pull alone).
[ ] `GitSave.runGit` line 14 delegates every host git command to `GitRun.gitExec`.
[ ] `src/Shared/dotnet/GitCheckIgnore.fs` calls `GitRun.gitExec` and `GitRun.gitCapture` outside Persist wiring; the ticket removes the private gate from Persist and git paths, which implies `gitExec` loses the private gate when those paths already hold `withLock`.

## Existing tests

[ ] ``taking a directory waits while a descendant file is held and a sibling file enters`` in `tests/Server.Tests/NodeLockPoolTests.fs` exercises `NodeLockPool.withLock` conflict and sibling entry.
[ ] ``a parse read under a locked directory waits until release`` in `tests/Server.Tests/NodeLockPoolTests.fs` models a parse read as `withLock` on a File under a held Directory.
[ ] ``a second request on a held node waits and the same thread is rejected`` in `tests/Server.Tests/NodeLockPoolTests.fs` expects `Error NodeLockRejection.HolderHoldsLock` for a nested call from the holding thread.
[ ] ``work tree gate waits then continues without overlap`` in `tests/Server.Tests/WorkspaceGitTests.fs` sequences two `WorkspaceGit.withWorkTreeGate` calls on one root.
[ ] ``Persist write waits on the Workspace work tree gate`` in `tests/Server.Tests/WorkspaceGitTests.fs` holds the gate while `DocumentPersistWrite.writeDocument` waits on `wsId` (Workspace document root).
[ ] ``pullTracked waits for the work tree gate then continues`` and ``saveTracked waits for the work tree gate then continues`` in `tests/Server.Tests/WorkspaceGitTests.fs` queue git Load and git Save behind `withWorkTreeGate`.
[ ] ``second git waits instead of failing on index.lock`` in `tests/Shared.Tests/GitRunTests.fs` shows two `GitRun.gitExec` calls on one folder run one at a time via the private gate.
[ ] ``Edit reply returns while the persist thread still holds the write`` in `tests/Server.Tests/FileAgentFailureTests.fs` uses `WorkspaceGit.withWorkTreeGate` inside a fake `persistNode`.
[ ] `GithubTransportLoadPullTests.fs` and `GithubTransportActorTests.fs` inject `{ withWorkTreeGate = ... }` on `GithubTransportGit`.

## Copy-from

[ ] ``Persist write waits on the Workspace work tree gate`` in `tests/Server.Tests/WorkspaceGitTests.fs` is the wait pattern to retarget at `NodeLockPool.withLock` on the Workspace Node id instead of `WorkspaceGit.withWorkTreeGate` on the path string.
[ ] ``a parse read under a locked directory waits until release`` in `tests/Server.Tests/NodeLockPoolTests.fs` is the copy for parse-read versus pending Workspace or Directory `withLock`.
[ ] `NodeLockPool.ownerChain` and the `gate` monitor in `src/Server/NodeLockPool.fs` are the pool wait implementation; Persist and git callers only wrap actions in `withLock`.
[ ] [09 — Relocate under the node lock](../../node-move-disk/issues/09-relocate-under-the-node-enclosure-lock.md) Working section names the same `withLock` sites: `writeDocument`, `writeArtifactText`, `planParseFile`, and `parseDirectory` around `DirectoryReconcile.planDirectoryReconcile`; [31 — Migrate onto the workspace lock](../issues/31-migrate-onto-the-workspace-lock.md) removes the gates that 09 assumes are already gone.
[ ] [core-refinement architecture](../arch.md) §6 **Locks**, §6 **Workspace lock**, §6 **Per-member persist locks**, and §6 **Parse and the workspace lock** state the target protocol Alan locked 2026-11-10.
[ ] [doc/current/parse-persist.md](../../doc/current/parse-persist.md) § Lock lists unchecked claims for `withLock` on Persist, parse reads, git Load, git Save, and Directory reconcile.

## implement

### Ticket claims that differ from the code

[ ] [31 — Migrate onto the workspace lock](../issues/31-migrate-onto-the-workspace-lock.md) **Persist locks** claims `NodeLockPool.withLock` on the document-root node; code gates `writeDocument` and `writeArtifactText` with `WorkspaceGit.withWorkTreeGate` on the workspace path string from `workspaceRootFor`, not `withLock` on the `documentRootId` argument.
[ ] **Parse reads** claims new parse reads use `withLock` on the parse node and drain under a pending Directory or Workspace lock; `planParseFile` / `preparedParseText` read disk with no `NodeLockPool`, relying only on the work-tree gate when writing upload text.
[ ] **Directory reconcile** (Alan 2026-11-10) claims `withLock` on the Directory Node; `ParseThread.parseDirectory` calls `DirectoryReconcile.planDirectoryReconcile` unlocked.
[ ] **Git Load and git Save** claims `withLock` on the Workspace Node including the git Save commit; `GithubTransportActor.commitInGate` and `WorkspaceGit.pullTracked` / `saveTracked` use `withWorkTreeGate` on the workspace directory string, and Save’s push runs outside the gate today.
[ ] **Remove the gates** claims no `withWorkTreeGate` and no `GitRun` private gate on Persist and git paths; `DocumentPersistPath.withWorkTreeGates`, `WorkspaceGit.withWorkTreeGate`, `GithubTransportGit.withWorkTreeGate`, and `GitRun.withWorkTreeGate` remain.
[ ] **Nobody takes two locks** is already coded: `NodeLockPool.enqueue` returns `Error HolderHoldsLock` when `threadId` already has a `Pending` or `Held` entry.
[ ] **Git fetch outside the lock** is not implemented; `pullTrackedBranch` runs a single `git pull` (fetch+merge) inside today’s gate, and [32 — Workspace lock handoff](../issues/32-workspace-lock-handoff.md) owns splitting fetch unlocked and pull+mark inside Workspace `withLock`.
[ ] **Landing order after drain** (pull, Unparsed mark, release, Parse stack) stays on [32 — Workspace lock handoff](../issues/32-workspace-lock-handoff.md); this ticket wires `withLock` and removes gates without owning that handoff test.

### Named new tests at public seams

[ ] **Caller test** (obligation 2 on [31 — Migrate onto the workspace lock](../issues/31-migrate-onto-the-workspace-lock.md)): hold `NodeLockPool.withLock` on the Workspace Node id from `graphWithWorkspace` while another thread calls `DocumentPersistWrite.writeDocument` on that Workspace’s `documentRootId`; the persist call stays outside until release, observing the `writeDocument` public seam and document-root id.
[ ] **Caller test** parse slice: with `withLock` Pending or Held on the Workspace Node, a concurrent call to `DocumentPersistWrite.planParseFile` on a descendant File Node stays outside until release, reusing the graph shape from ``a parse read under a locked directory waits until release`` but with Workspace as holder.
[ ] **Caller test** git Load slice: inject or production-call `WorkspaceGit.pullTracked` or `GithubTransportActor` Load body with a held `NodeLockPool.withLock` on `workspaceId`; the pull entry waits until release, replacing the assertion in ``pullTracked waits for the work tree gate then continues``.
[ ] **Caller test** git Save slice: same pattern for `commitAndPull` / `saveTracked` commit phase on `workspaceId`, including commit inside `withLock` per ticket note on git Save commit.
[ ] **Gate test** (obligation 3): after the change, `Reflect` or a seam test shows `DocumentPersistWrite.writeDocument` does not reference `withWorkTreeGate`, and `GitRun.gitExec` does not call private `withWorkTreeGate` (or the private gate is deleted).
[ ] **Gate test** git transport: `GithubTransportActor.workspaceGit` no longer exposes `withWorkTreeGate`; Load and Save use `NodeLockPool.withLock` with the live `Graph` and `workspaceId` from `withWorkspace` / `readWorkspace`.

### Edit map (file and function facts)

[ ] Replace gates in `DocumentPersistWrite.writeDocument`, `writeArtifactText`, and `writeAllDocuments` with `NodeLockPool.withLock graph documentRootId` around the core write and parse-read bodies; `documentRootId` is already the document-root node id passed to `writeDocument`.
[ ] Wrap `DocumentPersistWrite.planParseFile` (or `preparedParseText`) in `NodeLockPool.withLock graph fileId` for File parse reads; propagate `Result` from `NodeLockRejection` only where the pool returns `Error HolderHoldsLock` (parse thread today ignores parse errors).
[ ] Wrap `ParseThread.parseDirectory` body in `NodeLockPool.withLock graph directoryId` around `DirectoryReconcile.planDirectoryReconcile`.
[ ] Replace `DocumentPersistPath.withWorkTreeGates` in `DocumentPersistChange.persistGraphChangeWith` with per-root or per-document-root `withLock` using the live `preGraph` / `postGraph` and affected node ids.
[ ] Replace `WorkspaceGit.pullTracked` and `saveTracked` work-tree semaphores with callers that take `Graph`, `workspaceId`, and `workspaceRoot`; outer `withLock` on `workspaceId` must wrap the same work today's gate wraps, without moving fetch-outside-lock into this ticket.
[ ] Refactor `GithubTransportActor.commitInGate` to acquire `NodeLockPool.withLock` on `workspaceId` with the actor’s current graph instead of `git.withWorkTreeGate.applyAction`.
[ ] Remove `GitRun` private `withWorkTreeGate` from `gitExec` once Persist and git paths hold `withLock`; keep `GitSave.runGit` as the host git entry.
[ ] Update or retire `WorkspaceGitTests` gate tests and `GitRunTests` ``second git waits`` to assert serialization via `NodeLockPool` rather than semaphores or the git-process gate.
[ ] Leave `PersistThread.runRequest` unchanged; persist waiting is inside `writeDocument` per [09 — Relocate under the node lock](../../node-move-disk/issues/09-relocate-under-the-node-enclosure-lock.md).
[ ] Do not implement [32 — Workspace lock handoff](../issues/32-workspace-lock-handoff.md) fetch-first pull, Unparsed marking inside the lock, or Upload land order in this ticket.
