# Code review — [13 — Delete runtime mirror and remove production Persistence:Mode](../issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md)

Independent review. Not approval. Ticket Status left `coded`.
**Pin:** `47b11647...ade66c19`. Command: `git diff 47b11647...HEAD`. Commits: `203757a1` Select DbAgent from DbStatus and ignore Persistence:Mode; `ade66c19` Mark ticket 13 coded and adjust fallback tests.
**Spec:** [13 — Delete runtime mirror and remove production Persistence:Mode](../issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md). Verified against the DIFF and current `src/`, not [13 — Delete runtime mirror Persistence:Mode](13-delete-runtime-mirror-persistence-mode.md).
**Mechanical scan:** `python3 .agents/skills/code-review/scripts/standards-scan.py --diff 47b11647` — scan: none. `RouteRegistration.createPersistenceContext` 8 lines.
**Axis reports:** [code-review-13-standards](code-review-13-standards.md), [code-review-13-spec](code-review-13-spec.md).

**Verdict: Approve with nits.** Spec acceptance holds. Standards leftovers are non-blocking. Do not set Status `done` from this report.

## Spec checklist

1. **Database available selects DbAgent** — Met. Spec: "With a database available, production startup selects DbAgent for writable Changes regardless of `Persistence:Mode`." Evidence: `RouteRegistration.CreateBoot`; `CoreRuntime.startHost`; `RouteRegistration.isWritable`; `StateEndpointTests` ``legacy Persistence:Mode file still uses Database persistence``.
2. **No Database Rejects Changes** — Met. Spec: "With no database available, the Server Rejects Changes while existing Graph-data and file queries still work." Evidence: `CoreRuntime.startHost` else arm `FileAgent.persist`; `RouteRegistration.boundChanges` wraps `CoreRuntime.readOnly`; GET `/ambit/file` still reads `DataDir`. Test: `StateEndpointTests` ``unavailable Database serves read-only file fallback and ignores Persistence:Mode``.
3. **Mirror gone and Mode unread** — Met. Spec: "The runtime mirror path is deleted and production behavior does not read `Persistence:Mode`." Evidence: no `ofFileWithDbMirror` in `src/`; `DatabaseSetup.resolvePersistenceMode` deleted; `RouteRegistration.registerPersistenceAndRoutes` does not read `Config["Persistence:Mode"]`; `PersistenceContext.Mode` removed; `SavePrep.syncDataDir` matches `DbStatus` only.
4. **Init and FileAgent unchanged** — Met. Spec: "Existing initialization, repair, reconciliation, secondary-file, and test-only FileAgent behavior remains unchanged." Evidence: `DatabaseSetup.resolveDbConnection` File arm remains; `TestBackend.createAdmittedFile` and `FileAgentFailureTests` are not in the source diff.
5. **Focused tests adjusted** — Met. Spec: "Reuse or adjust focused startup and fallback tests; do not add duplicate behavior matrices." Evidence: `StateEndpointTests.backends` is only `BackendKind.Db`; the two startup facts were reused; `DatabaseSetupTests` dropped `resolvePersistenceMode` facts; former HTTP-file persist facts now use `TestBackend.createAdmittedFile`.

Out of scope left alone: init/repair/reconciliation Graph↔file protocols; test-only writable FileAgent; [19 — Database down and host stop](../issues/19-database-down-and-host-stop.md).

## Standards findings

Blocking: none.

Non-blocking:

1. **Orphan CoreBoot.PersistenceMode** — `CoreRuntime.startHost` no longer reads `boot.PersistenceMode`. `RouteRegistration.CreateBoot` still writes `PersistenceMode.Db`. Tests still set `PersistenceMode.File` with no effect. [.agents/rules/core-agent-behavior.md](../../../.agents/rules/core-agent-behavior.md) orphan rule.
2. **Stale File-backend names** — `TestBackend.createClientForDir` comment still says "file backend, no DB" after the hunk opened a test Database. `createFileClient` now builds a Database HTTP client.
3. **Dead withClient File arm** — `StateEndpointTests.backends` is Database-only; `withClient` still has a `BackendKind.File` arm.
4. **Duplicate open** — `ChangeEndpointResilienceTests` keeps two `open Gambol.Shared`. Pre-existing; this change did not create it.

Production does not read `Persistence:Mode` or `Config["Persistence:Mode"]`. Test FileAgent was not deleted. The `PersistenceMode.File` arm in `DatabaseSetup.resolveDbConnection` is init and out of scope.

## Standards

Range: `47b11647`…`ade66c19`. Command: `git diff 47b11647...HEAD`.

### 1. Mechanical scan

```
--- measure-fs-size ---
src/Server/RouteRegistration.fs::createPersistenceContext: lines 55-62 (8 lines)
```

scan: none (no threshold hits). Surgical under-100-line preference is not a script fail. `createPersistenceContext` is 8 lines, under the 40-line rule in [.agents/rules/fsharp-source.md](../../../.agents/rules/fsharp-source.md).

### 2. Extra flags

1. **Production Persistence:Mode** — [RouteRegistration](../../../src/Server/RouteRegistration.fs) `registerPersistenceAndRoutes` does not read `Config["Persistence:Mode"]`. `resolvePersistenceMode` is gone. No production branch on that key.
2. **Dead PersistenceMode branches** — [CoreRuntime](../../../src/Server/Core/CoreRuntime.fs) `startHost`, [SavePrep](../../../src/Server/SavePrep.fs), and `isWritable` match `DbStatus` only. The `PersistenceMode.File` arm in [DatabaseSetup](../../../src/Server/DatabaseSetup.fs) `resolveDbConnection` is init; out of scope.
3. **Test FileAgent** — `createAdmittedFile` and [FileAgentFailureTests](../../../tests/Server.Tests/FileAgentFailureTests.fs) remain. Not deleted.

### 3. Hard violations

1. **Orphan CoreBoot.PersistenceMode (non-blocking)** — [.agents/rules/core-agent-behavior.md](../../../.agents/rules/core-agent-behavior.md) orphan rule: remove variables your change made unused. `startHost` no longer reads `boot.PersistenceMode`. [RouteRegistration](../../../src/Server/RouteRegistration.fs) `CreateBoot` still writes `PersistenceMode.Db`. Tests still set `PersistenceMode.File` with no effect. The type stays for init `resolveDbConnection` (out of scope). The [CoreBoot](../../../src/Server/Core/CoreRuntime.fs) field is leftover persist-choice data.
2. **Stale File-backend names after the TestBackend hunk (non-blocking)** — same orphan / own-mess rule. [TestBackend](../../../tests/Server.Tests/TestBackend.fs) `createClientForDir` still says "file backend, no DB" after the hunk opened a test Database. `createFileClient` and `createClientForDirWithAuth` keep File names and comments while they now open Database clients. Leftover `Persistence:Mode` keys in in-memory config are ignore probes; not a fail.

### 4. Judgement calls

1. **Mysterious Name (non-blocking)** — `createFileClient` now builds a Database HTTP client.
2. **Duplicated Code (non-blocking)** — [ChangeEndpointResilienceTests](../../../tests/Server.Tests/ChangeEndpointResilienceTests.fs) keeps two `open Gambol.Shared` after adding `open Gambol.Server`. Pre-existing duplicate; this change did not create it.
3. **Shotgun Surgery (non-blocking)** — persist-choice removal touches CoreRuntime, RouteRegistration, RouteTypes, SavePrep, DatabaseSetup, and several test files. Expected for this cut.
4. **Speculative Generality (non-blocking)** — `CoreBoot.PersistenceMode` remains as unused persist-choice data.
5. **Dead withClient File arm (non-blocking)** — [StateEndpointTests](../../../tests/Server.Tests/StateEndpointTests.fs) `backends` is Database-only; `withClient` still has a `BackendKind.File` arm that calls `createFileClient`. Orphan match arm, not FileAgent deletion.

### 5. Summary

0 blocking findings. 2 non-blocking documented leftovers (unused `CoreBoot.PersistenceMode`; stale File-backend names and comments). Worst: unused persist-choice field on `CoreBoot` after production selection moved to `DbStatus`. Mechanical scan: no threshold hits.

## Spec

This report checks `git diff 47b11647...HEAD` and current `src/` against the ticket. Plan bookkeeping is ignored. The implementation report is not a source.

### 1. Acceptance criteria

1. **Database available selects DbAgent** — Met. Spec: "With a database available, production startup selects DbAgent for writable Changes regardless of `Persistence:Mode`." Evidence: [RouteRegistration](../../../src/Server/RouteRegistration.fs) `CreateBoot` always passes `PersistenceMode.Db` and does not read `Config["Persistence:Mode"]`; [CoreRuntime](../../../src/Server/Core/CoreRuntime.fs) `startHost` matches `DbStatus.Ok` to `DbAgent.persist`; `RouteRegistration.isWritable` is `DbStatus.Ok` only. Test: [StateEndpointTests](../../../tests/Server.Tests/StateEndpointTests.fs) ``legacy Persistence:Mode file still uses Database persistence``.
2. **No Database Rejects Changes** — Met. Spec: "With no database available, the Server Rejects Changes while existing Graph-data and file queries still work." Evidence: `CoreRuntime.startHost` else arm uses `FileAgent.persist`; `RouteRegistration.boundChanges` wraps `CoreRuntime.readOnly` when not writable; GET `/ambit/file` still reads `DataDir`. Test: `StateEndpointTests` ``unavailable Database serves read-only file fallback and ignores Persistence:Mode``.
3. **Mirror gone and Mode unread** — Met. Spec: "The runtime mirror path is deleted and production behavior does not read `Persistence:Mode`." Evidence: no `ofFileWithDbMirror` in `src/`. [DatabaseSetup](../../../src/Server/DatabaseSetup.fs) `resolvePersistenceMode` is deleted. `RouteRegistration.registerPersistenceAndRoutes` no longer reads the config key. [PersistenceContext](../../../src/Server/RouteTypes.fs) `Mode` is gone. [SavePrep](../../../src/Server/SavePrep.fs) matches `DbStatus` only.
4. **Init and FileAgent unchanged** — Met. Spec: "Existing initialization, repair, reconciliation, secondary-file, and test-only FileAgent behavior remains unchanged." Evidence: `DatabaseSetup.resolveDbConnection` File arm remains. [FileAgentFailureTests](../../../tests/Server.Tests/FileAgentFailureTests.fs) and [TestBackend](../../../tests/Server.Tests/TestBackend.fs) `createAdmittedFile` are not in the source diff. Reconciliation and Graph/file functions are not in the source diff.
5. **Focused tests adjusted** — Met. Spec: "Reuse or adjust focused startup and fallback tests; do not add duplicate behavior matrices." Evidence: `StateEndpointTests.backends` is only `BackendKind.Db`. The two startup facts were reused. [DatabaseSetupTests](../../../tests/Server.Tests/DatabaseSetupTests.fs) dropped `resolvePersistenceMode` facts. Former HTTP-file persist facts now use `TestBackend.createAdmittedFile`.

### 2. Findings

1. **Missing or partial** — None. Spec: "Delete the unused runtime mirror and remove `Persistence:Mode` from production Server decisions." Production persist choice uses `DbStatus` only.
2. **Scope creep** — None in Server behavior. `TestBackend.createClientForDir` now needs a test Database so HTTP Changes stay writable. That is test adjustment, not extra product behavior.
3. **Implemented but wrong** — None. `CoreBoot.PersistenceMode` is still set to `Db` and unused by `CoreRuntime.startHost`. Spec: "Legacy `Persistence:Mode` configuration is ignored." The leftover field is not a Mode read.

### 3. Summary

Five acceptance criteria met. Zero spec findings. Worst issue: none.

## Summary

Standards: 0 blocking, 2 leftover. Worst in-axis: unused `CoreBoot.PersistenceMode`. Spec: 0 findings. Worst in-axis: none.

**Verdict: Approve with nits.** Ticket Status left `coded`.
