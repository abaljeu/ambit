# Shared success envelope build

## Outcome

Post and Poll now use one complete `ChangeSuccessResponse` type and codec. Both HTTP paths return Revision, real deploy and page stamps, readiness, `externalChanges`, a required and possibly empty Change list, and an optional persistence message. Post keeps confirmation reconciliation with `externalChanges = false`; Poll keeps Change-tail dispatch. Load keeps its separate package response.

## Files changed

- Tracking: [[.scratch/event-sourced-ops/project.md]], [[.scratch/index.md]], and [[.scratch/event-sourced-ops/issues/01-shared-success-envelope-expand.md]]
- Shared contract: [[src/Shared/ApiResponses.fs]], [[src/Shared/ApiResponseSerialization.fs]], [[src/Shared/Serialization.fs]], and [[src/Shared/SyncLogic.fs]]
- Server: [[src/Server/Api.fs]], [[src/Server/RouteRegistration.fs]], [[src/Server/FileAgent.fs]], and [[src/Server/DbAgent.fs]]
- Browser: [[src/Client/UpdateCodec.fs]], [[src/Client/Update.fs]], [[src/Client/UpdateWorkspaceSync.fs]], and [[src/Client/App.fs]]
- Shared tests: [[tests/Shared.Tests/SerializationTests.fs]], [[tests/Shared.Tests/SyncLogicTests.fs]], and [[tests/Shared.Tests/LargeChangeApplyTests.fs]]
- Server tests: [[tests/Server.Tests/StateEndpointTests.fs]], [[tests/Server.Tests/FileAgentFailureTests.fs]], and [[tests/Server.Tests/DatabaseProjectionContractTests.fs]]
- Current docs: [[doc/api.md]] and [[doc/current/sync-mvp.md]]
- Report: [[.scratch/event-sourced-ops/reports/shared-success-envelope-build.md]]

The pre-existing root-agent change in [[WORK.md]] was not edited.

## Red evidence

Command:

```bash
dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~SerializationTests"
```

Result: exit 1. The new contract tests failed to compile because `ChangeSuccessResponse`, `externalChanges`, `encodeChangeSuccessResponse`, and `decodeChangeSuccessResponseDecoder` did not exist. This was the expected red state.

## Green evidence

Commands and results:

```bash
dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~SerializationTests"
```

Exit 0: 34 passed, 0 failed, 0 skipped.

```bash
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~StateEndpointTests"
```

Exit 0: 52 passed, 0 failed, 0 skipped.

```bash
dotnet build tests/Shared.Tests -c Debug
dotnet build src/Client -c Debug
dotnet build tests/Server.Tests -c Debug
```

Each command exited 0 with 0 warnings and 0 errors.

```bash
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~SerializationTests|FullyQualifiedName~SyncLogicTests|FullyQualifiedName~AckReconcileTests"
```

Exit 0: 77 passed, 0 failed, 0 skipped.

```bash
dotnet test tests/Server.Tests -c Debug --no-build --filter "FullyQualifiedName~StateEndpointTests|FullyQualifiedName~FileAgentFailureTests|FullyQualifiedName~DatabaseProjectionContractTests"
```

Exit 0: 67 passed, 0 failed, 0 skipped.

## Review

The final diff matches the accepted plan. Post decodes the shared envelope and dispatches only confirmation data to `SubmitResponse`; Poll separately dispatches the envelope Change list to `PollDone`. Server endpoint tests decode both paths with the same codec and confirm nonzero stamps, readiness, Post `externalChanges = false`, Poll `externalChanges = true` for a non-empty tail, and the expected Changes.

```bash
bash .agents/skills/code-review-fsharp/scripts/measure-fs-size.sh --diff HEAD
git -c core.whitespace=cr-at-eol diff --check -- . ":(exclude)WORK.md"
```

Both checks exited 0. All changed F# bindings are within the 40-line limit; no added over-limit source lines were reported. Searches found no legacy `ChangeBatchAck` or `PollResponse` references in source or the updated current contract docs.

## Remaining concerns

No code concern remains from focused verification. Cursor diagnostics still show the expected stale pre-implementation errors until the environment reloads; fresh Browser, Shared test, and Server test builds all pass with no warnings or errors.

## Requested board mutation

`remove` [[.scratch/event-sourced-ops/issues/01-shared-success-envelope-expand.md]] from [[WORK.md]].
