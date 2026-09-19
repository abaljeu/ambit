# 13 — Delete runtime mirror Persistence:Mode

**Ticket:** [13 — Delete runtime mirror and remove production Persistence:Mode](../issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md)
**Status:** coded

## 1. What changed

1. **Production persist choice** — [CoreRuntime](../../../src/Server/Core/CoreRuntime.fs) `startHost` and [RouteRegistration](../../../src/Server/RouteRegistration.fs) `isWritable` use `DbStatus` only. `DbStatus.Ok` starts DbAgent and admits Changes. Any other status starts FileAgent and wraps `CoreRuntime.readOnly`.
2. **Legacy config ignored** — `registerPersistenceAndRoutes` does not read `Persistence:Mode`. `resolvePersistenceMode` is deleted. A leftover Mode value does not fail startup.
3. **Save prep** — [SavePrep](../../../src/Server/SavePrep.fs) branches on `DbStatus` only. `PersistenceContext` no longer carries Mode.
4. **Initialization kept** — [DatabaseSetup](../../../src/Server/DatabaseSetup.fs) `resolveDbConnection` still accepts `PersistenceMode.File` for test-called bootstrap and repair. Production always passes `PersistenceMode.Db`.
5. **Test-only FileAgent kept** — `CoreRuntime.create` with `DbStatus.Absent` still starts writable FileAgent. [FileAgentFailureTests](../../../tests/Server.Tests/FileAgentFailureTests.fs) and `createAdmittedFile` are unchanged.

## 2. Tests

1. **Adjusted startup and fallback** — [StateEndpointTests](../../../tests/Server.Tests/StateEndpointTests.fs) `unavailable Database serves read-only file fallback and ignores Persistence:Mode` uses leftover Mode `mirror` with no connection. `legacy Persistence:Mode file still uses Database persistence` posts a Change and does not import files. The File half of the HTTP backend matrix is dropped.
2. **HTTP writable helpers** — [TestBackend](../../../tests/Server.Tests/TestBackend.fs) `createClientForDir` uses the test Database so production-shaped posts stay writable. File-authority HTTP persist cases now call `createAdmittedFile`.
3. **Removed config parser tests** — [DatabaseSetupTests](../../../tests/Server.Tests/DatabaseSetupTests.fs) no longer asserts `resolvePersistenceMode`.
4. **Focused run** — `dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~DatabaseSetupTests|FullyQualifiedName~SavePrepTests|FullyQualifiedName~StateEndpointTests|FullyQualifiedName~CoreRuntimeTests|FullyQualifiedName~ChangeEndpointResilienceTests|FullyQualifiedName~FileAgentFailureTests|FullyQualifiedName~CredentialedChangePostsTests"` passed 73.
5. **Server.Tests** — 443 passed. One flake: `DbAgent serves reads while sweep buffers FIFO mutations then trims` failed once, then passed alone. Not this change.
6. **Client compile gate** — not run. Client and Shared were not edited.

## 3. Out of this ticket

1. **[19 — Database down and host stop](../issues/19-database-down-and-host-stop.md)** — live probe and host-stop stay later. This ticket only sets the startup and fallback boundary.

## 4. Review cleanup

1. **CoreBoot** — unused `PersistenceMode` field deleted. [RouteRegistration](../../../src/Server/RouteRegistration.fs) no longer writes it.
2. **HTTP helpers** — `createFileClient` deleted. [TestBackend](../../../tests/Server.Tests/TestBackend.fs) `createClientForDir` is a Database data-dir client, not a File backend.
3. **withClient** — File arm and `BackendKind` deleted. State HTTP facts use one Database client.
