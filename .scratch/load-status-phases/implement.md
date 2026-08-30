# Load status phases — implement

Implemented `SyncState.Parsing` so Load shows three status phases: Uploading (desktop file push), Parsing (server disk parse/reconcile), Loading (graph fetch). Web Load does not enter Uploading.

## Files changed

- [[src/Shared/ViewModelSync.fs]] — added `Parsing`
- [[src/Shared/SyncPlanner.fs]] — `isBusy` includes `Parsing` (blocks submit; poll/load already require Idle)
- [[src/Shared/WorkspaceUpload.fs]] — `queueBlockedDetail` parse string; `canStartWeb` already rejects non-Idle/Polling
- [[src/Client/StatusView.fs]] — `"Parsing…"` with `amb-syncing`
- [[src/Client/UpdateWorkspaceLoad.fs]] — web `ReconcileServerDisk` sets Parsing, not Uploading
- [[src/Client/UpdateImport.fs]] — `parseFileOp` sets Parsing; complete/fail set Idle then fetch or fail
- [[src/Client/Update.fs]] — `PollDone` keeps Parsing the same way as Uploading
- [[tests/Shared.Tests/SyncPlannerTests.fs]] — Parsing blocks poll, submit, load
- [[tests/Shared.Tests/WorkspaceUploadTests.fs]] — `canStartWeb` false; queue detail for parse
- [[.scratch/load-status-phases/]] — git.md, project.md, this report

Desktop `keepUploading` paths in [[src/Client/UpdateWorkspaceSync.fs]] are unchanged. Directory reconcile complete/fail still call `clearUploading` (sets Idle).

## Tests run

```
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~SyncPlannerTests|FullyQualifiedName~WorkspaceUploadTests"
```

Result: Passed 42, Failed 0.

Also: `dotnet build src/Client/Gambol.Client.fsproj -c Debug` succeeded (FS0025 exhaustive match).

Did not run the full test suite.

## WORK.md mutations

- `remove` [[src/Client/StatusView.fs]] — Load status: Uploading (desktop push only), Parsing (disk parse/reconcile), Loading (graph fetch); web skips Uploading (plan: load_status_phases)

No `add` / `move` / `block`.

## Leftovers

- No commit (per request). Still on `w/owner-edge-db-repair`.
- HITL: web directory Load must show Parsing then Loading; web file Load must show Parsing then Loading; desktop push must still show Uploading.
- Fable/wwwroot bundle was not rebuilt here. Browser UI needs a Client Fable rebuild to show the new status text.
- App.fs 50ms paint delay was left unchanged (plan).
