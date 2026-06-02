# Workspace Stage Plan

Status: Draft
Authority: Planning document for implementation sequencing.
See also: [[doc/roadmap/workspace-file-model.md]],
[[doc/roadmap/reference-expressions.md]],
[[doc/roadmap/persistence-vs-domain-model.md]]

## Scope Relationship

This document defines the implementation scope for the current stage only.

The documents [[doc/roadmap/workspace-file-model.md]] and
[[doc/roadmap/reference-expressions.md]] define broader target end-state behavior across multiple
stages. Differences between this stage scope and those target-scope documents are intentional.

## Goal

Implement workspace support now, without directory and file support yet.

For this stage, "workspace support" means:

- users can define workspaces
- users can manipulate workspaces
- users can configure local workspace mappings
- shared and local persistence can represent those operations safely

## Explicit Scope

### In Scope (this stage)

- Workspace identity and uniqueness.
- Workspace root nodes as first-class special nodes.
- A stable `WORKSPACES` container node directly under `ROOT`.
- Shared mapping: workspace label -> workspace root node.
- Local mapping: workspace label -> absolute local filesystem root.
- User-visible `client` commands to create, rename workspaces.
- User-visible `desktop` app configuration JSON file containing workspace mappings.
- A `desktop`-accessible workspace-node command for "open workspace in explorer".
- No commands to set, update, clear, or list local mappings.
- Clear unresolved-workspace indication when label is unknown.

### Out Of Scope (next stage)

- Directory node lifecycle.
- File node lifecycle.
- Workspace-relative directory/file path mapping.
- Path wildcard resolution under workspaces.
- Filesystem sync/import/reconciliation.
- Reference language elements

## Deliverables

- Shared model updates that enforce workspace invariants.
- Operation/change surface for workspace lifecycle.
- Shared persistence updates for workspace mappings.
- Desktop-local persistence updates for local workspace root mappings via config JSON.
- UI/command surface for workspace management and desktop workspace actions.
- Reference resolver support for workspace-only base references.
- Tests for invariants, persistence round-trips, and command behavior.

## Implementation Plan

## 1. Shared Model And Invariants

- Add explicit workspace identity type (label, normalized label).
- Define canonical workspace-label comparison rules.
- Ensure `ROOT` has exactly one `WORKSPACES` owner child node.
- Add graph-level workspace index: label -> workspace node id.
- Enforce one workspace root node per workspace label.
- Ensure workspace nodes are `Special Workspace`.
- Preserve existing owner/ref semantics unchanged.

Verification:

- Shared tests prove uniqueness and normalization rules.
- Shared tests prove workspace create/rename invariants.

## 2. Shared Operations And Change Replay

- Add change operations for workspace lifecycle:
  - create workspace
  - rename workspace
- Define conflict and idempotency behavior for replay.
- Ensure replay preserves canonical labels and uniqueness.

Verification:

- Reducer tests for each operation success/failure case.
- Replay tests with duplicate, out-of-order, and conflicting changes.

## 3. Shared Persistence Shape

Done: `WorkspaceLocalMapping` in `src/Shared/` implements the desktop-local config format (JSON) and load/save. The caller (desktop layer) supplies the DataDir-relative path; the module has no knowledge of DataDir itself.

- Desktop feature: local mapping config in DataDir maps workspace label -> absolute local root path.
- Keep local mapping storage fully separate from shared persistence.

## 4. Desktop-Local Workspace Configuration

Done: local config persistence and path resolution implemented in `src/Shared/WorkspaceLocalMapping.fs`
(encode/decode, loadFromFile/saveToFile, resolvePath). Validation covers label uniqueness, absolute-path
requirement for workspace roots, and relative-path safety (no `..`, no empty segments, no `:`, `#`, `^`
or other invalid filename chars; forward-slash separators only).
Tests: `tests/Shared.Tests/WorkspaceLocalMappingTests.fs`.

Remaining: wire HTTP endpoint surface.

- Expose desktop-local endpoints (loopback + local auth token required):
  - GET workspaces -> workspace labels only
  - GET dir -> directory contents with metadata (name, kind, size, modifiedUtc)
  - PUT dir -> create directory
  - DELETE dir -> delete directory
  - GET file -> text content + modifiedUtc
  - PUT file -> replace text content, requires expected modifiedUtc match
  - DELETE file -> delete file
- Path safety:
  - Client sends workspace label + relative path only
  - Reject absolute paths and any upward traversal (`..`)
  - Resolve to absolute path under mapped workspace root only; reject root escapes
- Timestamp validation:
  - PUT file compares expected modifiedUtc with current modifiedUtc
  - On mismatch, return conflict and include current modifiedUtc in response

Verification:

- Local config read/write tests.
- Config validation tests (missing path, duplicate label, malformed JSON).
- Tests proving local mapping changes do not alter shared graph state.
- Endpoint contract tests for GET/PUT/DELETE dir/file and GET workspaces.
- Security tests for loopback-only + token requirement.
- Path validation tests (absolute path, upward traversal, root escape).
- Conflict tests proving timestamp mismatch returns conflict + current modifiedUtc.

## 4b. Desktop Startup Workspace Registration

When the desktop app starts, it reads the local workspace config and ensures the cloud server's shared graph contains a workspace node for each configured label. This bootstraps the shared model from desktop-local config without requiring the user to issue create commands manually.

- Load local workspace config at startup (before the local proxy begins serving requests) and hold it in memory so `/_desktop/*` workspace endpoints can resolve paths immediately.
- If local config is absent or empty, skip registration; no requests are sent to the server.
- If credentials are not available at startup (user not yet logged in), defer registration until the first successful login is detected.
- Query the server for its current workspace label set.
- For each label in local config that is absent from the server's workspace set: send a
  create-workspace operation to the server using the stored credentials.
- Treat a conflict response (label already exists) as success; this covers concurrent startup or prior partial sync.
- Labels present in the server but absent from local config: leave unchanged. The server is authoritative for workspace existence; local config only supplies path bindings.
- On any server error during registration: log the failure and continue. Desktop remains usable; unregistered labels will simply lack workspace nodes on the server until the next startup or until the user creates them via the normal command surface.

Verification:

- Test: startup with local=[A,B,C], server=[B,C,D] → creates A only, leaves B/C/D unchanged.
- Test: startup with empty local config → no server requests sent.
- Test: server returns conflict for a label during sync → counted as success, not retried.
- Test: server unreachable during sync → error logged, remaining labels skipped, no crash.
- Test: no credentials at startup → registration deferred; on login, sync runs with full label set.
- Test: in-memory config is populated before the first `/_desktop/workspaces` request is served.


## 5. Command/UI Surface (Workspace Only)

### 5a. Existing search command shall also match nodes based on their path.
Done
- [[@src/Shared/ViewModelSearch.fs:79-93]] existing search method
- [[@src/Shared/RefExpr.fs:340-357]] new match method. Should return same NodeSearchResult.
- The search dialog will merge these results.  RefExpr matches first.

### 5b. Import children of node
Done

### 5c. Export node children to disk.

### 5d. 

- Add a desktop-accessible command on workspace nodes for opening the mapped workspace root in
   explorer.
- Add clear "not locally mapped" feedback when desktop action is invoked without a local mapping.
- Display unresolved workspace label state in UI where references are shown.

Verification:

- Command-level tests for happy path and invalid input.
- UI state tests for unresolved labels and unmapped desktop actions.

## 7. Stage Exit Criteria

This stage is complete when all of the following are true:

- Workspace labels can be created, renamed, and listed.
- Workspace root nodes exist and are uniquely mapped by label.
- Workspace root nodes are added under a stable `WORKSPACES` node under `ROOT`.
- Local workspace root mappings can be read from and persisted to desktop config JSON.
- No local-mapping edit/list command surface exists in this stage.
- `@workspace:` resolves for known labels and shows unresolved for unknown labels.
- No directory/file lifecycle support has been introduced yet.
- All new behavior has Shared/Server/Desktop tests where applicable.

## Suggested Order Of Implementation

1. Shared model and invariants.
2. Shared operations/replay.
3. Shared persistence.
4. Desktop-local mapping persistence.
5. Command/UI surface.
6. Resolver support and unresolved indicator.
7. End-to-end tests.

## Clarifications And Decisions

Decisions captured from discussion:

1. Lifecycle in this stage:
   - Create/list/rename only.
   - Removal is not in this stage.
2. Removal policy (for later stage):
   - Soft remove (hidden/disabled).
3. Label handling:
   - Store original casing for display.
   - Compare labels case-insensitively for identity/uniqueness.
4. Local mapping cardinality:
   - Exactly one local root per workspace label, per desktop.
5. Root graph structure:
   - No default workspace is auto-created.
   - `ROOT` contains a `WORKSPACES` node, and workspace nodes are added there.

Still unclear and needs a decision:

1. Unresolved behavior:
   - Commands that require resolution are blocked when `@workspace:` cannot resolve.
   - The client must show an explicit diagnostic; silent no-op is invalid.

## Notes

This plan is intentionally workspace-only. Directory and file node support should be added in a
separate follow-up stage after this stage exits.