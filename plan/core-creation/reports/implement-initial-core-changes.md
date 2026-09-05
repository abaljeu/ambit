# Implement initial Core Changes

## Outcome

Implemented the locked initial Core Changes increment for resolved issues 03–06. Core now accepts typed Change lists for Normal and Graph-only operations and returns typed CoreChangesAccepted facts. GraphAgentHandle is the production Graph Change capability and owns agent construction and selection. Api.postChange remains the HTTP Adapter and keeps JSON decode, response fields, JSON encode, and HTTP error mapping.

FileAgent and DbAgent still own sequencing, apply, amendment, validation, persistence, publication, readiness, and the eight-second timeout. Parse, GraphOnlyChangePost, lazy-load reconciliation, and git reconciliation now use typed Graph-only Changes without internal JSON.

## Files changed

Core types and production selection:

- Added [[src/Server/Core/CoreChanges.fs]].
- Added [[src/Server/Core/GraphAgentHandle.fs]].
- Updated [[src/Server/Gambol.Server.fsproj]].

Server integration:

- Updated [[src/Server/FileAgent.fs]].
- Updated [[src/Server/DbAgent.fs]].
- Updated [[src/Server/Api.fs]].
- Updated [[src/Server/GraphOnlyChangePost.fs]].
- Updated [[src/Server/LazyLoadReconciliationServer.fs]].
- Updated [[src/Server/RouteRegistration.fs]].
- Updated [[src/Server/SavePrep.fs]].

Test evidence and forced signature adaptations:

- Added [[tests/Server.Tests/CoreChangesTests.fs]].
- Updated [[tests/Server.Tests/Gambol.Server.Tests.fsproj]].
- Updated [[tests/Server.Tests/GraphOnlyChangePostTests.fs]].
- Updated [[tests/Server.Tests/ApiGetStateTests.fs]].
- Updated [[tests/Server.Tests/ApiPostLoadTests.fs]].
- Updated [[tests/Server.Tests/SavePrepTests.fs]].
- Updated [[tests/Server.Tests/FileAgentFailureTests.fs]].
- Updated [[tests/Server.Tests/DatabaseProjectionContractTests.fs]].
- Updated [[tests/Server.Tests/DbAgentTests.fs]].
- Updated [[tests/Server.Tests/DbAgentFailureTests.fs]].
- Updated [[tests/Server.Tests/IgnoredDestinationValidationTests.fs]].
- Updated [[tests/Server.Tests/LazyLoadReconciliationServerTests.fs]].

Project tracking:

- Updated [[plan/core-creation/project.md]] to Stage active, added Started 2026-09-05, and added 20m to the prior 4h10m Project Actual aggregate.
- Regenerated the Core creation row in [[plan/index.md]].

## TDD evidence

Baseline before edits:

- Agent and persistence filter: 40 passed.
- Handle and save filter: 15 passed.
- HTTP and reconciliation filter: 91 passed.
- Existing seam filter: 1 passed.

Red: after adding the two CoreChangesTests and changing the Graph-only recorder to typed Change lists, the seam filter failed to compile. The expected errors said GraphAgentHandle was not defined and FileAgent.postChange still required string rather than Change list.

Green: after the typed accepted record, typed mailbox messages, GraphAgentHandle, HTTP Adapter split, and typed Graph-only callers were implemented, the seam filter passed 3 tests. The direct typed Normal caller test proves that Poll returns the accepted Change and Revision. The recording HTTP Adapter test proves that valid JSON reaches Core as typed Changes and malformed JSON does not call Core.

## Focused verification

- dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~FileAgentFailureTests|FullyQualifiedName~DatabaseProjectionContractTests|FullyQualifiedName~DbAgentTests|FullyQualifiedName~DbAgentFailureTests|FullyQualifiedName~IgnoredDestinationValidationTests" — 40 passed.
- dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~ApiGetStateTests|FullyQualifiedName~ApiPostLoadTests|FullyQualifiedName~SavePrepTests" — 15 passed.
- dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~StateEndpointTests|FullyQualifiedName~ChangeEndpointResilienceTests|FullyQualifiedName~GraphOnlyChangePostTests|FullyQualifiedName~LazyLoadReconciliationServerTests" — 91 passed.
- dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~CoreChangesTests|FullyQualifiedName~GraphOnlyChangePostTests" — 3 passed.
- dotnet build src/Server/Gambol.Server.fsproj -c Debug — succeeded with 0 warnings and 0 errors.
- git diff --check — passed.

The IDE linter retained stale errors for the new F# compile-order files. The workspace rules state that the linter needs an environment reload after project edits. The command-line compiler and all focused tests resolved the new files and passed.

## Scope confirmation

Issue 01 and issue 13 were not implemented. No production Server Actor, producer path, mirror deletion, mirror test, persistence-selection change, Core Files, general Query, Command, Actor pool, cancellation, finish, failure, shutdown, ACID apply, initialization change, repair change, reconciliation redesign, or new Graph/file function was added. The obsolete mirror received only typed input and result adaptation; its file-first call order, best-effort database write, and file result remain unchanged.

No Shared or Browser file changed, so the Browser compile gate was not required. No commit, merge, push, or remote operation ran.

## Remaining risks and deviations

The existing eight-second timeout can still abandon work that completes later, as the locked plan states. The obsolete mirror remains in production selection and has no new mirror-specific test, by explicit exclusion.

There were no implementation deviations from the locked plan. Time was recorded only on the Project aggregate because no implementation issue was in scope; no delivery time was assigned to issue 01 or issue 13.
