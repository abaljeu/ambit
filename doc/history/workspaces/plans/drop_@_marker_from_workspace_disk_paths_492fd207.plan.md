---
name: Drop @ marker from workspace disk paths
overview: Remove the `@`-prefix auto-add/strip convention from on-disk workspace folder naming and the git gateway/repo naming, so a folder name is always exactly the workspace name (verbatim, including a literal leading `@` if that's genuinely part of the name). Correct docs that describe a `@label:` colon address syntax that was never implemented — the real, locked reference syntax is `//&lt;name&gt;/relative`.
todos:
  - id: code-nodedesktoppath
    content: Remove @ add/strip in NodeDesktopPath (diskWorkspacePrefix, workspaceLabel)
    status: pending
  - id: code-documentpartition
    content: Remove @ prepend in DocumentPartition disk-path builders
    status: pending
  - id: code-documentassembly
    content: Make isWorkspaceArtifact structural-only; stop trimming @ in stubName
    status: pending
  - id: code-documentpersistence
    content: Fix hasArtifactSet marker check in DocumentPersistence
    status: pending
  - id: code-workspacegit
    content: Remove @ prepend in WorkspaceGit.workspaceRelative
    status: pending
  - id: code-workspacegitremote
    content: Remove @ add/strip in WorkspaceGitRemote (normalizeLabel, gatewayUrl)
    status: pending
  - id: code-workspacetreesyncio
    content: Remove @ prepend in WorkspaceTreeSyncIo.directoryRelative
    status: pending
  - id: tests-update
    content: Update existing tests' @home expectations to home
    status: pending
  - id: tests-new
    content: Add tests for verbatim @-in-name workspace (@dd) and //@dd/ vs //dd/ distinctness
    status: pending
  - id: docs-update
    content: "Correct docs: strike @label: syntax, describe //<name>/ and verbatim disk folder naming"
    status: pending
isProject: false
---

## Locked design

1. **Reference/address syntax (no code change — already correct):** `//<workspacename>/relative`, implemented in [src/Shared/NodeDesktopPath.fs](src/Shared/NodeDesktopPath.fs) via `tryParseWorkspacePath` / `pathForNode`. No colon, no automatic `@`.
2. **Disk layout:** `DataDir/<workspacename>/...` — the folder name **is** the workspace name, verbatim. The list of *named, discoverable* workspaces on the server is the list of top-level folders in `DataDir`. `@` is an ordinary character a workspace could have in its name (e.g. a workspace literally named `@dd`), never a decoration the system adds or strips.
3. **ROOT is not a folder.** `Graph.rootId` is itself a unique, nameless `Special Workspace` node (`owner = rootId`, `name = Filename.Empty` — see `Graph.rootPlaceholder`), distinct from named workspaces owned by `Graph.workspacesId`. ROOT's artifacts live directly at the `DataDir` top level (`.amb`, and any root-owned directories placed straight under `DataDir/`), never inside a `<name>/` subfolder, and it is already excluded from folder-driven stub-seeding (`DocumentAssembly.seedStub` skips `documentRootId = Graph.rootId`). "List of workspaces = list of DataDir folders" describes named workspaces only; it does not add or remove anything about ROOT, and this change doesn't touch ROOT's handling.
4. **No migration.** Confirmed no production workspaces exist. Existing dev folders `data/@dd`, `data/@workspace` simply become workspaces literally named `@dd` and `@workspace` once the marker logic is removed — no rename step needed.

## Code changes — remove all `@`-marker special-casing

Each site below currently either prepends `@` to a name for disk/URL use, or strips a leading `@` to recover a "bare" name. All such add/strip logic is deleted; the value is used verbatim end-to-end.

- [src/Shared/NodeDesktopPath.fs](src/Shared/NodeDesktopPath.fs) — `diskWorkspacePrefix` (adds `@` unless already present) and `workspaceLabel` (strips leading `@` when parsing a `//` path segment) are removed; callers use the segment name as-is.
- [src/Shared/DocumentPartition.fs](src/Shared/DocumentPartition.fs) — three `"@" + name + ...` constructions (`workspaceDiskPrefix`, and both branches of `artifactDirectoryRelative`/`artifactFileRelative` for `Special Workspace`) become plain `name + ...`.
- [src/Shared/DocumentAssembly.fs](src/Shared/DocumentAssembly.fs) — `isWorkspaceArtifact` drops the `relativePath.StartsWith("@")` condition, becoming purely structural (directory artifact with no nested slash before `.amb`, i.e. a top-level `DataDir` child). `stubName` drops `TrimStart '@'`; the folder name becomes the node name verbatim.
- [src/Server/DocumentPersistence.fs](src/Server/DocumentPersistence.fs) — `hasArtifactSet`'s `rel.StartsWith("@")` disjunct is removed/replaced with the equivalent structural check now that `@` carries no meaning.
- [src/Server/WorkspaceGit.fs](src/Server/WorkspaceGit.fs) — `workspaceRelative` stops conditionally prepending `@`; the repo directory is `dataDir/<label>` verbatim.
- [src/Shared/WorkspaceGitRemote.fs](src/Shared/WorkspaceGitRemote.fs) — `normalizeLabel` no longer strips `@`; `gatewayUrl` no longer prepends `@` (`.../git/<label>.git`, not `.../git/@<label>.git`).
- [src/Server/WorkspaceTreeSyncIo.fs](src/Server/WorkspaceTreeSyncIo.fs) — `directoryRelative`'s `Ok("@" + name)` for a workspace root becomes `Ok(name)`.

## Tests

Update existing expectations from `@home` to `home` (folder-name convention changed, not behavior):

- [tests/Shared.Tests/DocumentAssemblyTests.fs](tests/Shared.Tests/DocumentAssemblyTests.fs)
- [tests/Shared.Tests/DocumentPartitionTests.fs](tests/Shared.Tests/DocumentPartitionTests.fs)
- [tests/Shared.Tests/NodeDesktopPathTests.fs](tests/Shared.Tests/NodeDesktopPathTests.fs)
- [tests/Server.Tests/DocumentPersistenceTests.fs](tests/Server.Tests/DocumentPersistenceTests.fs)
- [tests/Server.Tests/WorkspaceGitTests.fs](tests/Server.Tests/WorkspaceGitTests.fs)
- [tests/Server.Tests/DocumentPathMoveExecutionTests.fs](tests/Server.Tests/DocumentPathMoveExecutionTests.fs)
- [tests/Shared.Tests/WorkspaceConnectTests.fs](tests/Shared.Tests/WorkspaceConnectTests.fs) — `gatewayUrl` expectation `@home.git` → `home.git`

Add new coverage proving verbatim `@`-in-name behavior:

- A workspace named literally `@dd` round-trips through `DocumentAssembly` stub naming and `DocumentPartition` disk-path building without stripping.
- `//@dd/...` resolves to a workspace named `@dd`; `//dd/...` resolves to a distinct workspace named `dd` (no collapsing).

## Docs

Strike every `@label:` colon-syntax mention and every "auto-prepend `@`" disk-convention description; replace with the locked design above.

- [doc/current/workspace-local-mapping.md](doc/current/workspace-local-mapping.md)
- [doc/current/desktop-local-files.md](doc/current/desktop-local-files.md)
- [doc/current/workspace-graph.md](doc/current/workspace-graph.md)
- [doc/current/workspace-stage-plan.md](doc/current/workspace-stage-plan.md)
- [doc/current/persistence-model.md](doc/current/persistence-model.md)
- [doc/roadmap/workspace-file-persistence.md](doc/roadmap/workspace-file-persistence.md)
- [doc/roadmap/workspace-file-model.md](doc/roadmap/workspace-file-model.md)
- [doc/roadmap/postgres-roadmap.md](doc/roadmap/postgres-roadmap.md)
- [doc/roadmap/git-sync-gateway.md](doc/roadmap/git-sync-gateway.md)
- [doc/roadmap/workspace-scale-import.md](doc/roadmap/workspace-scale-import.md)
- [doc/roadmap/workspace-scale-import-slice1-plan.md](doc/roadmap/workspace-scale-import-slice1-plan.md)
- [doc/roadmap/workspace-scale-import-slice2-plan.md](doc/roadmap/workspace-scale-import-slice2-plan.md)
- [doc/arch.md](doc/arch.md)
- [doc/index.md](doc/index.md)

## Verification

- `dotnet build tests/Shared.Tests -c Debug` and `dotnet test tests/Shared.Tests -c Debug --no-build` — full Shared suite green.
- `dotnet test tests/Server.Tests -c Debug` — Server suite green (DocumentPersistence, WorkspaceGit, DocumentPathMoveExecution).
- Manual: with local `data/@dd` folder present, server startup assembles a workspace node named `@dd` (not `dd`).
