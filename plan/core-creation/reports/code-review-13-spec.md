# Spec-axis report for [13 — Delete runtime mirror and remove production Persistence:Mode](plan/core-creation/issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md)

This report checks `git diff 47b11647...HEAD` and current `src/` against the ticket. Plan bookkeeping is ignored. The implementation report is not a source.

## 1. Acceptance criteria

1. **Database available selects DbAgent** — Met. Spec: "With a database available, production startup selects DbAgent for writable Changes regardless of `Persistence:Mode`." Evidence: [RouteRegistration](src/Server/RouteRegistration.fs) `CreateBoot` always passes `PersistenceMode.Db` and does not read `Config["Persistence:Mode"]`; [CoreRuntime](src/Server/Core/CoreRuntime.fs) `startHost` matches `DbStatus.Ok` to `DbAgent.persist`; `RouteRegistration.isWritable` is `DbStatus.Ok` only. Test: [StateEndpointTests](tests/Server.Tests/StateEndpointTests.fs) ``legacy Persistence:Mode file still uses Database persistence``.
2. **No Database Rejects Changes** — Met. Spec: "With no database available, the Server Rejects Changes while existing Graph-data and file queries still work." Evidence: `CoreRuntime.startHost` else arm uses `FileAgent.persist`; `RouteRegistration.boundChanges` wraps `CoreRuntime.readOnly` when not writable; GET `/ambit/file` still reads `DataDir`. Test: `StateEndpointTests` ``unavailable Database serves read-only file fallback and ignores Persistence:Mode``.
3. **Mirror gone and Mode unread** — Met. Spec: "The runtime mirror path is deleted and production behavior does not read `Persistence:Mode`." Evidence: no `ofFileWithDbMirror` in `src/`. [DatabaseSetup](src/Server/DatabaseSetup.fs) `resolvePersistenceMode` is deleted. `RouteRegistration.registerPersistenceAndRoutes` no longer reads the config key. [PersistenceContext](src/Server/RouteTypes.fs) `Mode` is gone. [SavePrep](src/Server/SavePrep.fs) matches `DbStatus` only.
4. **Init and FileAgent unchanged** — Met. Spec: "Existing initialization, repair, reconciliation, secondary-file, and test-only FileAgent behavior remains unchanged." Evidence: `DatabaseSetup.resolveDbConnection` File arm remains. [FileAgentFailureTests](tests/Server.Tests/FileAgentFailureTests.fs) and [TestBackend](tests/Server.Tests/TestBackend.fs) `createAdmittedFile` are not in the source diff. Reconciliation and Graph/file functions are not in the source diff.
5. **Focused tests adjusted** — Met. Spec: "Reuse or adjust focused startup and fallback tests; do not add duplicate behavior matrices." Evidence: `StateEndpointTests.backends` is only `BackendKind.Db`. The two startup facts were reused. [DatabaseSetupTests](tests/Server.Tests/DatabaseSetupTests.fs) dropped `resolvePersistenceMode` facts. Former HTTP-file persist facts now use `TestBackend.createAdmittedFile`.

## 2. Findings

1. **Missing or partial** — None. Spec: "Delete the unused runtime mirror and remove `Persistence:Mode` from production Server decisions." Production persist choice uses `DbStatus` only.
2. **Scope creep** — None in Server behavior. `TestBackend.createClientForDir` now needs a test Database so HTTP Changes stay writable. That is test adjustment, not extra product behavior.
3. **Implemented but wrong** — None. `CoreBoot.PersistenceMode` is still set to `Db` and unused by `CoreRuntime.startHost`. Spec: "Legacy `Persistence:Mode` configuration is ignored." The leftover field is not a Mode read.

## 3. Summary

Five acceptance criteria met. Zero spec findings. Worst issue: none.
