# Implement issue 25

Date: 2026-09-06

Coder. [[../issues/25-bind-changes-at-core-seam.md|25 (Bind Changes at the Core seam)]] is Status `done`. Did not start [[../issues/17-cancel-a-job.md|17 (Cancel a job)]]. Did not start [[../issues/21-client-shows-lock-present.md|21 (Client shows lock-present)]]. Did not swallow [[../issues/07-define-core-files-contract.md|07 (Define the Core Files contract)]] or [[../issues/08-define-core-query-contract.md|08 (Define the Core Query contract)]]. No Core-level `postChange` facade ([[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]).

## What landed

- [[src/Server/Core/CoreRuntime.fs]] nested `bindChanges` and `browserChanges`. `browserChanges` binds the process-lifetime Browser credential with `CoreAuth.bindHandle`. `bindChanges` is the same bind for any sender. Apply stays on `postChange` / `postGraphOnlyChange`; other handle methods pass through.
- [[src/Server/RouteRegistration.fs]] `/ambit/changes` posts through `changesBound` (`core.browserChanges ()`). It does not pass `changes()`, credentials, and `browserCredential` into the Adapter.
- [[src/Server/Api.fs]] `postChange` takes a bound `CoreChanges` handle, decodes JSON, and maps HTTP status. It does not run `CoreAuth.post` or take a credentials set.

## Tests

Focused Server.Tests, all green:

- CoreRuntimeTests, CoreChangesTests, CoreCredentialsTests, CoreActorPoolTests, BrowserCredentialTests (27).
- HTTP path: StateEndpointTests, ChangeEndpointResilienceTests, GraphOnlyChangePostTests (69).

Full suite [[scripts/test.sh]] `all`: Server 369 passed; Shared 1566 passed, 1 skipped. All tests passed.

No Shared or Client edit. Client compile gate not run.

## Agent-done

Commit on `dev` via [[scripts/commit.sh]] with an explicit file list. Ask the human to run [[scripts/gitready.sh]] to bring `dev` into `ready`. Local dirty tree from skills-cleanup blocks a clean merge until that work is committed or set aside. No remotes.

## Leftover

- [[../issues/07-define-core-files-contract.md|07 (Define the Core Files contract)]] — `flushFileSnapshot` / `getFileRevision` / `dataDir`.
- [[../issues/08-define-core-query-contract.md|08 (Define the Core Query contract)]] — Poll and Load still in the Adapter.
- Parse HTTP still unpacks: `parseBound` and `Api.postParseFile` still take credentials and `parseCredential`. Same leftover shape as 25, not this ticket.
- Graph-only chunking not collapsed.
- [[../issues/17-cancel-a-job.md|17 (Cancel a job)]] not started.
- [[../issues/21-client-shows-lock-present.md|21 (Client shows lock-present)]] not started.
