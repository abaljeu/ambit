# Slow “Parsing…” on web Load — diagnosis

Stage note: evidence-only; no fix shipped. Branch: `w/owner-edge-db-repair`.

## Likely path

**Directory Load on web → `SyncState.Parsing` → `POST /ambit/workspace/reconciliation/directory`.**

That matches the status paint (`StatusView` → `"Parsing…"`) for the whole reconcile HTTP. After the POST returns, client clears busy and enters `Loading` via `tryStartLoadFetch` (`completeDirectoryReconcile` → `okWithPoll`).

Web **File** Load uses `parseFileOp` → `POST /ambit/file/parse` (also painted as Parsing). That path is one document, not a full-tree rediscover; it is a secondary concern unless the focus File is a large Amb.

Related desktop **Uploading** cost (PROPFIND / ledger) is covered in [[tmp/load-performance-audit.md]] and is **not** what `Parsing` means after the status-phases change.

## Call chain (directory)

| Step | Where | Always? |
|------|-------|---------|
| Set `Parsing` + `ContinueDirectoryReconcile` | `UpdateWorkspaceLoad.loadOp` | yes (web `ReconcileServerDisk`) |
| `POST …/reconciliation/directory` | `App.fs` (~50ms paint delay) | yes |
| `reconcileWorkspace` / `reconcileDirectory` | `LazyLoadReconciliationServer` | yes |
| `handle.getState` + decode full graph | same | **always** |
| `Directory.EnumerateFiles(…, AllDirectories)` as every path `Added` | `discoveredAddedPaths` → `DocumentPersistence.discoverArtifactRelatives` | **always** |
| Read every Directory File (`.amb`) text | `readDirInfoArtifacts` | **always** for discovered `.amb` |
| `planChangedPathsWithArtifacts` over **all** rediscovered paths | `LazyLoadReconciliationReport` | **always** |
| Per `.amb`: export graph → `previousArtifactText`, then `DocumentParseOps.planApplyArtifact` | `LazyLoadReconciliationApply.parseDirInfoIfPresent` | **always** when Current + artifact present |
| `postGraphOnlyChange` | only if `report.ops` non-empty | skipped when truly no-op |

There is **no** “stub matches / disk not newer → done” short-circuit. Web directory Load always rediscovers the scoped tree and re-plans it as `Added`.

## Root causes (with evidence)

### 1. Rediscovery treats the whole tree as `Added` (primary)

`reconcileChangedPathsWithDiscovery` unions caller changes with `discoveredAddedPaths`, which maps **every** relative under the workspace/dir to `LazyLoadReconciliation.Added` — including paths that already have Current stubs.

```119:156:src/Server/LazyLoadReconciliationServer.fs
    let reconcileChangedPathsWithDiscovery
        ...
                    match discoveredAddedPaths dataDir workspaceLabel discoveryDirRel with
                    ...
                        let allChanges = changedPaths @ discovered
                        let artifacts =
                            readDirInfoArtifacts dataDir workspaceLabel allChanges
                        match
                            LazyLoadReconciliationReport.planChangedPathsWithArtifacts
                                graph
                                workspaceLabel
                                allChanges
                                artifacts
```

User expectation (“node matches or is newer so done”) matches sync-ledger / mtime skip thinking; that logic lives in desktop push (`WorkspaceFileSync` / ledger), **not** here.

### 2. Current Directory Files still re-parse on every rediscovery (hot loop)

For each rediscovered `.amb` where the directory node is `Current`:

```117:142:src/Shared/dotnet/LazyLoadReconciliationApply.fs
                        let previousText =
                            match docState with
                            | Unparsed
                            | NoServerFile -> None
                            | Current ->
                                previousArtifactText graph nodeId relativePath
                        ...
                        DocumentParseOps.planApplyArtifact
                            graph
                            nodeId
                            relativePath
                            text
                            previousText
```

`previousArtifactText` always re-exports the live document. Then Amb warm path, when disk text equals export, still runs **cold re-read** because `AmbReconcile.whenUnchanged = None`:

```83:86:src/Shared/documents/OutlineDocumentWarm.fs
        if editedText = previousText then
            match hooks.whenUnchanged with
            | Some f -> f previousText contextGraph documentRootId
            | None -> readCold previousText contextGraph documentRootId
```

(Plain/CStyle use a cheap `copyDocumentFromGraph` hook; Amb does not.)

Stub create is mostly no-op (`ensureChild` finds existing), and `markAddedDocumentsUnparsed` only touches new/`NoServerFile` stubs — so **ops often end empty**, but CPU still scales with tree size.

### 3. Full-graph `getState` + full disk walk always run

Even when `ops = []` (no agent change / no projection/ownership work), every Load still:

- JSON-encodes/decodes the entire agent graph
- Enumerates all files under the focus (e.g. local `data/life` ~540 files, `Alibre` ~277, ~155 `.amb` under `data/`)

Git / ledger / ownership-repair / projection rebuild are **not** on this no-op path (`postGraphOnlyChange` only when ops non-empty).

### File parse (web Current / Unparsed)

`Api.postParseFile` → `DocumentPersistence.planParseFile` → always read DataDir (or body) → `ImportDocument.planParseFile` → same `previousArtifactText` + `planApplyArtifact`. No mtime early exit before parse (mtime only may append `SetUpdateTime` after). Same Amb `whenUnchanged = None` tax for one file — usually smaller than directory rediscover.

## Why second Load is still slow

Second Load is **not** a no-op path today. It repeats rediscover-as-Added + per-`.amb` export/reparse. Empty ops only skip applying a change batch; they do not skip the expensive plan.

### Measured feedback loop (red-capable)

Temporary asserts (removed after run):

| Probe | Setup | Result |
|-------|--------|--------|
| Shared planner | 80 Current dirs with matching `.amb` artifacts; second `planChangedPathsWithArtifacts` as all `Added` | **277ms, ops=0** (threshold 100ms → red). Same graph, 80 file stubs only: **42ms** |
| Server `reconcileWorkspace` | 80 dirs × (`.amb` + `f.txt`); second call after first seeded | **683ms** (threshold 200ms → red) |

Commands (already run):

```text
dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~DIAG second Added"
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~DIAG second workspace"
```

Scale: real workspaces with hundreds of files / many Directory Files will sit in Parsing for seconds even when nothing changed.

## Ranked fix options (smallest first)

1. **Byte-equal short-circuit in `DocumentParseOps.planApplyArtifact`**  
   If `previousText = Some text`, return `Ok []` (skip Amb cold re-read + `planOpsFromGraphs`). Helps directory finalize **and** file parse. Still pays export + disk read.

2. **`AmbReconcile.whenUnchanged = Some(copyDocumentFromGraph…)`** (mirror Plain)  
   Same equal-text case, Amb-specific; slightly more surgical than (1) if you only want Amb.

3. **Skip `parseDirInfoIfPresent` when `ensureChild` emitted no create ops and node is already `Current`**  
   Avoid reading `.amb` / exporting unless the path is truly new or marked Modified. Closest small step toward “already matches → done”.

4. **Mtime gate** (user mental model)  
   If `File`/`Directory` `updateTime` matches DataDir mtime (and stub exists), skip artifact read + parse. Needs a clear precision rule (already used in `planParseFile` for `SetUpdateTime`).

5. **Stop classifying rediscovery as blanket `Added`**  
   Diff disk listing vs owned stubs: only plan creates for missing paths, deletes for missing disk, Modified for drifted mtimes. Biggest design change; unlocks true near-no-op second Load.

6. **Cache / ledger for DataDir inventory** (analogous to workspace sync ledger)  
   Avoid full `EnumerateFiles` every Load when fingerprints unchanged. Larger infra; pairs with (5).

## Not the main Parsing cost

- Client 50ms `setTimeout` before POST  
- `withPathSyncRefresh` after complete (desktop ledger waterfall; web mappings miss is cheap)  
- Ownership repair / projection (only if ops applied)  
- Desktop PROPFIND push ([[tmp/load-performance-audit.md]]) — that is **Uploading**, not Parsing  

## WORK.md mutations (for parent)

- `add` [[plan/load-status-phases/slow-parsing.md]] — web Load Parsing slowness: rediscover-all-as-Added + per-`.amb` reparse even when ops empty; ranked fixes (1)–(5)
