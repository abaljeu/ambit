# 137 — Plain Load prefers the GitHub mapping

**Status:** `defined`

## 1. Type

coding

## 2. Blockers

None.

## 3. Problem

A plain Load on a Directory or File hits `PathPick.plainLoadStaysDesk`, which returns `LoadSavePath.Desk`, even when the workspace has a GitHub remote. The case with no desktop access and no upload is the Neither case. [23 — Run the Load actor for Git and Neither](../../actor-as-client/issues/23-run-the-load-actor-for-git-and-neither/23-run-the-load-actor-for-git-and-neither-ticket.md) runs the Git arm and that Neither arm inside `loadActorFunction`. The desk path reads the locally mapped folder, so Load on a node in a GitHub-backed workspace fails:

- A file present on the server but not in the local folder fails with "scope path not found" (`WorkspaceLocalInventory.listForPush`).
- The desktop stub planner (`WorkspaceUploadStructure.planStubOps`) fails with "parent children not loaded".

## 4. Change

- When a node's workspace has both a GitHub mapping and a local mapping, a plain Load on any Load subject (Workspace, Directory, File) takes the Git path.
- A plain Load with no desktop access and no upload is the Neither case in 23 — Run the Load actor for Git and Neither. Desktop_mapped WebDAV waits for its own design.
- An explicit Desk or Git pick is unchanged.

Source: `PathPick.fs`, and the Git Load path where a Directory or File subject needs it.

## 5. Tests

[x] A node in a workspace with both a GitHub mapping and a local mapping gets a plain Load that chooses GitHub (Git path).

## 6. Done

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[x] All executed tests pass.
