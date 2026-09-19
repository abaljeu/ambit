# Standards-axis report for [13 — Delete runtime mirror and remove production Persistence:Mode](plan/core-creation/issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md)

Range: `47b11647`…`ade66c19`. Command: `git diff 47b11647...HEAD`.

## 1. Mechanical scan

```
--- measure-fs-size ---
src/Server/RouteRegistration.fs::createPersistenceContext: lines 55-62 (8 lines)
```

scan: none (no threshold hits). Surgical under-100-line preference is not a script fail. `createPersistenceContext` is 8 lines, under the 40-line rule in [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md).

## 2. Extra flags

1. **Production Persistence:Mode** — [RouteRegistration](src/Server/RouteRegistration.fs) `registerPersistenceAndRoutes` does not read `Config["Persistence:Mode"]`. `resolvePersistenceMode` is gone. No production branch on that key.
2. **Dead PersistenceMode branches** — [CoreRuntime](src/Server/Core/CoreRuntime.fs) `startHost`, [SavePrep](src/Server/SavePrep.fs), and `isWritable` match `DbStatus` only. The `PersistenceMode.File` arm in [DatabaseSetup](src/Server/DatabaseSetup.fs) `resolveDbConnection` is init; out of scope.
3. **Test FileAgent** — `createAdmittedFile` and [FileAgentFailureTests](tests/Server.Tests/FileAgentFailureTests.fs) remain. Not deleted.

## 3. Hard violations

1. **Orphan CoreBoot.PersistenceMode (non-blocking)** — [.agents/rules/core-agent-behavior.md](.agents/rules/core-agent-behavior.md) orphan rule: remove variables your change made unused. `startHost` no longer reads `boot.PersistenceMode`. [RouteRegistration](src/Server/RouteRegistration.fs) `CreateBoot` still writes `PersistenceMode.Db`. Tests still set `PersistenceMode.File` with no effect. The type stays for init `resolveDbConnection` (out of scope). The [CoreBoot](src/Server/Core/CoreRuntime.fs) field is leftover persist-choice data.
2. **Stale File-backend names after the TestBackend hunk (non-blocking)** — same orphan / own-mess rule. [TestBackend](tests/Server.Tests/TestBackend.fs) `createClientForDir` still says "file backend, no DB" after the hunk opened a test Database. `createFileClient` and `createClientForDirWithAuth` keep File names and comments while they now open Database clients. Leftover `Persistence:Mode` keys in in-memory config are ignore probes; not a fail.

## 4. Judgement calls

1. **Mysterious Name (non-blocking)** — `createFileClient` now builds a Database HTTP client.
2. **Duplicated Code (non-blocking)** — [ChangeEndpointResilienceTests](tests/Server.Tests/ChangeEndpointResilienceTests.fs) keeps two `open Gambol.Shared` after adding `open Gambol.Server`. Pre-existing duplicate; this change did not create it.
3. **Shotgun Surgery (non-blocking)** — persist-choice removal touches CoreRuntime, RouteRegistration, RouteTypes, SavePrep, DatabaseSetup, and several test files. Expected for this cut.
4. **Speculative Generality (non-blocking)** — `CoreBoot.PersistenceMode` remains as unused persist-choice data.
5. **Dead withClient File arm (non-blocking)** — [StateEndpointTests](tests/Server.Tests/StateEndpointTests.fs) `backends` is Database-only; `withClient` still has a `BackendKind.File` arm that calls `createFileClient`. Orphan match arm, not FileAgent deletion.

## 5. Summary

0 blocking findings. 2 non-blocking documented leftovers (unused `CoreBoot.PersistenceMode`; stale File-backend names and comments). Worst: unused persist-choice field on `CoreBoot` after production selection moved to `DbStatus`. Mechanical scan: no threshold hits.
