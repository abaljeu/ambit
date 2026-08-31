# Unloaded Normal node after workspace Load

Stage note: investigation on `selective-client-sync` (2026-08-17). No commit.

## Symptom

1. User runs **Load** on an **Unloaded Normal** node (hollow circle, empty resident children).
2. Load does not appear to finish (likely queued behind sync).
3. User navigates toward the owning workspace; that Load is also queued.
4. User refreshes the view (F5 / `/state`).
5. User loads the **workspace** successfully (sync returns to idle).
6. The original Normal node **still shows Unloaded** (hollow circle, no visible content).
7. **Second full refresh** — the node is **Loaded** and shows content.

## New evidence (user follow-up)

Step 7 is the key: a **second** full refresh fixes presentation. That pattern means:

- The **server** (and `/state` bootstrap) already had the loaded workspace subgraph.
- The first successful workspace Load likely **did** persist data server-side.
- The bug is primarily **client projection / view sync**, not missing server data.

Full refresh uses `StateLoaded` → `render` (full DOM rebuild from graph). Incremental path uses `LoadDone` → `withSiteMap` → `patchDOM`.

## How Load is supposed to work

### Target intent (`UpdateHelpers.loadTargetIntent`)

```56:61:src/Client/UpdateHelpers.fs
let private loadTargetIntent (graph: Graph) (targetId: NodeId) : LoadTarget =
    let includeWorkspace =
        match Map.tryFind targetId graph.nodes with
        | Some node when node.childrenStatus = Unloaded -> true
        | _ -> false
    { targetId = targetId; includeWorkspace = includeWorkspace }
```

Unloaded targets request the **owning Workspace package** (`includeWorkspace = true`).

### Server response (`Api.postLoad`)

Returns ordered **changes** (base→R) plus **packages** (complete workspace subgraph at R) via `ResidentProjection.packagesForTargets` / `projectWorkspaceNodes`. Every owned node in the package is projected with `childrenStatus = Loaded` and filtered resident children.

### Client apply (`SyncLogic.applyLoadResponse` → `applySyncResponse`)

1. Apply change tail under Loaded rules.
2. `ResidentProjection.installPackages` merges package nodes into the resident graph (`Map.add` overwrite + `Graph.fromNodes` rebuild).
3. `withSiteMap` reconciles SiteMap from `zoomRoot`.
4. `patchDOM` plans row mutations from graph/siteMap diff.

### UI indicator (`ViewModelChildrenIndicator.rowChildrenIndicator`)

- **FoldChevron** when `node.children` is non-empty (Loaded content present).
- **HollowCircle** when children empty and (`childrenStatus = Unloaded` **or** `documentState = Unparsed`).
- **SolidCircle** for Loaded parsed leaves.

Hollow → chevron requires either **graph node update** (packages installed) or full re-render.

## Root causes (ranked)

### 1. LoadDone rejected packages — graph never updated (most likely)

`SyncLogic.applyLoadResponse` refuses **package-only** responses when revision drifted or local work is pending:

```107:122:src/Shared/SyncLogic.fs
let applyLoadResponse ...
    let packageOnly =
        List.isEmpty response.changes
        && not (List.isEmpty response.packages)
    if
        packageOnly
        && (hasPendingLocal || responseRevision <> state.revision.Value)
    then
        Error "raced package payload"
```

**Stale Poll during Loading:** `PollDone` did **not** treat `Loading` as busy. It fell through to the default branch, set sync to **Idle**, and could **apply a poll tail** — advancing `revision` without installing workspace packages. When `LoadDone` arrived with packages-only at the captured R, `applyLoadResponse` returned **Error**, and `Update.fs` left the graph unchanged (sets `DataOutdated`, sync Idle). User sees “Load finished”; node stays Unloaded in **model** until F5.

Covered by tests: `ClientHistoryRuntimeTests` “package-only Load refuses a revision mismatch”.

**Fix applied:** `Update.fs` — ignore `PollDone` while `syncState = Loading` (same spirit as blocking new polls in `SyncPlanner`).

### 2. Incremental DOM did not reflect graph (secondary / defensive)

When `childrenStatus` flips Unloaded→Loaded or hollow→chevron, `planPatchDOM` emits **`RecreateRow`** (see `ViewModelDomPlan.fs` ~169–172). That normally forces `needsDomOrderWalk` and a full visible-row walk.

`View.fs` had a fast path when `not needsDomOrderWalk` that **only applied `PatchRow`**, silently skipping `RecreateRow` / `CreateRow` / `RemoveRow` if that guard ever misfires.

**Fix applied:** fast path now applies structural row mutations too (defensive; chevron/hollow is not patchable via `SetFoldArrow` alone — see `RowView.applyRowPatches`).

### 3. Not the primary issue: empty packages

If workspace header is already `childrenStatus = Loaded`, `includeWorkspace = false` and packages may be `[]` (change catch-up only). That leaves deep Unloaded stubs.unrefreshed. User scenario (workspace Load after F5) normally uses `includeWorkspace = true` for an Unloaded workspace header; packages should be non-empty.

## Code paths (checklist)

| Stage | File | Notes |
|-------|------|-------|
| Load command | `UpdateWorkspaceLoad.fs` | Desktop push / web reconcile → `tryStartLoadFetch` |
| Fetch | `App.fs` `runLoadServer` | POST `/ambit/load` |
| Apply | `Update.fs` `LoadDone` | `applyLoadResponse` → `withSiteMap` |
| Graph merge | `ResidentProjection.installPackages` | Overwrites nodes by id |
| SiteMap | `ViewModelSiteMap.reconcileSiteMapFrom` | Walk from `zoomRoot`; collapsed nodes keep `children = []` in map but row indicator reads **graph** |
| DOM plan | `ViewModelDomPlan.planPatchDOM` | `RecreateRow` on indicator/kind change |
| DOM apply | `View.fs` `patchDOM` | Full walk vs fast path |
| Full rebuild | `View.fs` `render` | `StateLoaded` only |

## Fixes applied (this investigation)

| File | Change |
|------|--------|
| `src/Client/Update.fs` | Drop stale `PollDone` while `Loading`; do not reset to Idle mid-load |
| `src/Client/View.fs` | Fast `patchDOM` path applies `RecreateRow` / `CreateRow` / `RemoveRow` |
| `tests/Shared.Tests/ViewModelRowStateTests.fs` | `planPatchDOM recreates row when Unloaded leaf becomes Loaded with children` |

## Tests run

```text
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~ViewModelRowStateTests|FullyQualifiedName~ClientHistoryRuntimeTests|FullyQualifiedName~SyncLogicTests"
```

## Recommended follow-ups

- **HITL:** reproduce original queue scenario; confirm node goes hollow→chevron without second F5 after workspace Load completes.
- **Optional hardening:** on `applyLoadResponse` Error after Load, auto-retry Load or GET `/state` scoped to workspace (avoid silent `DataOutdated` with stale graph).
- **Optional:** log when `LoadDone` rejects packages (`consoleLog` in Error branch) for easier diagnosis.
- Rebuild Fable client bundle before browser verification.

## WORK.md mutations (for parent)

- `add` [[plan/load-status-phases/unloaded-node-after-workspace-load.md]] — investigated Unloaded node after workspace Load; PollDone/Loading race + patchDOM hardening (fixes in `Update.fs`, `View.fs`)
