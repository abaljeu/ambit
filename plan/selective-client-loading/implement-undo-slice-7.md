# Undo Slice 7 worker report

## Outcome

Focused Shared and Server suites for the changed Undo modules passed. The Client F# project and the Fable Browser build passed. The delivered 2,000-Node paste inverse has no per-created-Node `Graph.fromNodes` rebuild. Reachable Graph equality holds. Phase timings are in [[undo-slice-7-measure.md]]. A 300 ms projected-apply budget was added after measurement. No SiteMap, validation, encoding, network, or persistence optimization was done. The Undo implementation plan has no further slices.

## Files changed

- [[tests/Shared.Tests/LargeChangeApplyTests.fs]] — delivered inverse phase timing, reachable equality, no-create-Op check, 300 ms projected-apply budget
- [[tests/Server.Tests/StateEndpointTests.fs]] — File-backend 2,000-Node paste inverse total-response timing
- [[undo-slice-7-measure.md]] — measurement report
- [[implement-undo-slice-7.md]] — this worker result

## Checkpoint

- Required tests and Browser build pass.
- Inverse of the large paste is one `Replace` (`ops=1`); no `NewNode` or `NewSpecialNode`.
- Reachable Graph equality holds on that paste Undo/Redo and in HistoryTests nested paste / NewSpecialNode / split cases.
- Remaining phases are reported. Persistence is not a separate clock. It is nested in File-backend total response.

## Verification

```bash
dotnet build tests/Shared.Tests -c Debug
```

Passed: 0 warnings, 0 errors.

```bash
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~HistoryTests|FullyQualifiedName~LargeChangeApplyTests|FullyQualifiedName~ClientHistoryTests|FullyQualifiedName~ClientHistoryRuntimeTests|FullyQualifiedName~AckReconcileTests|FullyQualifiedName~SyncPlannerTests|FullyQualifiedName~SyncLogicTests|FullyQualifiedName~SerializationTests|FullyQualifiedName~ViewModelCmdLastResultTests"
```

Passed: 200 of 200.

```bash
dotnet build tests/Server.Tests -c Debug
```

Passed: 0 warnings, 0 errors.

```bash
dotnet test tests/Server.Tests -c Debug --no-build --filter "FullyQualifiedName~FileAgentFailureTests|FullyQualifiedName~StateEndpointTests|FullyQualifiedName~DatabaseProjectionContractTests|FullyQualifiedName~ChangeEndpointResilienceTests|FullyQualifiedName~IgnoredDestinationValidationTests|FullyQualifiedName~LazyLoadReconciliationServerTests"
```

Passed: 97 of 97. This filter includes Db-backed `StateEndpointTests` theories and `DatabaseProjectionContractTests`. `TEST_DB_CONNECTION_STRING` was unset. Those tests used the sibling test database derived from `appsettings.Development.json` and passed.

`DbAgentTests` was not in this filter. Slice 5 hang/failure cases (`Internal server error in DbAgent PostChan`) did not reproduce here.

```bash
dotnet build src/Client/Gambol.Client.fsproj -c Debug
```

Passed: 0 warnings, 0 errors.

```bash
bash ./scripts/client.sh build
```

Passed: Fable compilation and esbuild bundle.

No commit was created.

## Leftover risks

- `State.history` remains an unused empty History field so Graph apply still has a State value.
- DB startup sweep can still drop detached Undo headers. ChangeLog still has the inverse Change.
- Two `DbAgentTests` hang/failure cases from Slice 5 were not re-run.
- Prompt Enter stays `commandBarOnly`, so Rename / Edit classes success does not stamp `#cmd-last-result` via `withDiagnostic`.
- Measured phases do not show a failure that would justify SiteMap, encoding, network, or persistence work.

## WORK.md mutations

- `remove` [[.scratch/selective-client-loading/undo-implementation-plan.md]] — implement Slice 7 (verify and measure)
- `add` none
- `move` none
- `block` none
