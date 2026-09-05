# Upload leading-dot Directory stub

HITL after FileAgent batch `classify`: Upload had **no timeout**. Fail: [[CONTEXT.md]] Workspace-mapped `plan` is on DataDir (MKCOL) but the Graph has no Directory named `plan`. Related: `.agents` already exists as Directory; Load shows Children and a sync checkmark; expand then looks Unloaded.

Timeout leftover: [[changes-post-timeout.md]]. Batch classify: [[ignored-destination-batch-classify.md]].

## What is not the omit

These do **not** skip a leading-dot Directory name (only exact `.git` and exact `.amb`):

- [[src/Shared/Filename.fs]] `create "plan"` / `create ".agents"` is Ok. Exact `.amb` is Invalid (Directory File).
- [[src/Shared/FileNodeOps.fs]] `planCreateOwnedDirectory` keeps `plan`. It falls back to `folder` only for the Directory File basename.
- [[src/Shared/History.fs]] `NewSpecialNode` accepts that Ok name.
- [[src/Shared/WorkspaceUploadStructure.fs]] `planStubOps` on a Loaded Workspace emits `NewSpecialNode` Directory `plan` and, with a file child, Directory `.agents`.
- [[src/Shared/dotnet/WorkspaceLocalInventory.fs]] skips `.git` only. gitignore `classify` can drop ignored paths. `plan` that is not ignored stays in inventory.
- [[src/Shared/dotnet/LazyLoadReconciliationPath.fs]] skips `.git` and reserved `gambol.*`. `plan/.amb` is Directory. Bare `plan` without `isDirectory` would classify as File (reconcile does not list empty dirs as bare names).

## Two causes, one seam

Selective Load leaves a named Workspace **Unloaded** on the client ([[src/Shared/WorkspaceUploadStructure.fs]] issue 21). `planStubOps` then returns `[]`. [[src/Client/UpdateWorkspaceSync.fs]] `completeUploadInventory` skips `POST /ambit/changes` and goes to WebDAV push. MKCOL/PUT still write DataDir. The server Graph does not get those stubs.

[[src/Server/DocumentPersistence.fs]] `discoverArtifactRelatives` lists **files** only. An empty `plan` dir has no file, so reconcile never saw `{rel}/.amb`. Cold load has the same hole.

`.agents` with files is different from empty `plan`. File paths create the Directory as an intermediate (`planPath`). The node can exist while Children File stubs never posted (Unloaded skip). Load of the Workspace package then shows a Directory with empty Children. Hollow / Unparsed looks like Unloaded on expand. [[src/Shared/ResidentProjection.fs]] does **not** mark Directory `.agents` Unloaded (only nested Workspace headers). SiteMap `expandEntry` does not set `childrenStatus`.

Shared root: **Unloaded stub skip plus file-only discovery**. Filename and `planStubOps` on Loaded are not the omit.

## Fix

[[src/Server/LazyLoadReconciliationServer.fs]] `discoveredAddedPaths` also walks empty dirs (not `.git`) and adds `{rel}/.amb`. `planAddedPaths` then creates Directory `plan`.

[[src/Client/UpdateWorkspaceSync.fs]] after a mapped directory/workspace push (no single-file parse): `ContinueDirectoryReconcile` then Load. Reconcile posts server-id stubs (no Unloaded name conflict). Load installs the package with those Children.

## Tests

Shared (Loaded stub and Load package; these were already green for `planStubOps`):

- `planStubOps creates Directory named plan from inventory`
- `planStubOps creates Directory named .agents with file child`
- `planCreateOwnedDirectory keeps leading-dot name that is not Directory File`
- `leading-dot directory File represents containing Directory named plan`
- `packagesForTarget keeps Directory named .agents Loaded with children`

Server (empty-dir discovery; this is the disk-without-node loop):

- `workspace reconcile creates Directory for empty leading-dot dir`
- `directory reconcile keeps .agents Loaded with discovered children`

Commands (focused only):

`dotnet test tests/Shared.Tests -c Debug --filter FullyQualifiedName~planStubOps creates Directory named`

`dotnet test tests/Server.Tests -c Debug --filter FullyQualifiedName~workspace reconcile creates Directory for empty leading-dot`

`./scripts/client.sh build`

Shared: 5 passed. Server empty-dir plus `.agents` plus two existing prefix tests: 4 passed. Client compile gate passed.

## Board

`move` [[changes-post-timeout.md]] — HITL timeout gone after batch `classify`. Leftover is leading-dot Directory stub/residency (this report).

`add` [[upload-dot-scratch-directory-stub.md]] — HITL: Upload mapped Workspace that has empty `plan` and `.agents` with files; Graph must show Directory `plan`; Load `.agents` then expand; Children stay Loaded (not Unloaded).
