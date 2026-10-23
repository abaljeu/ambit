# 89 — Delete the File-mode database check

**Status:** `defined`

## 1. Type
coding

## 2. Blockers
None.

## 3. Look at
- Files:
  - `src/Server/DatabaseSetup.fs`
  - `src/Server/RouteRegistration.fs`
  - `tests/Server.Tests/DocumentLoaderTests.fs`
- Types:
  - `DatabaseSetup.PersistenceMode`, `DatabaseSetup.DbStatus`
  - `CoreBoot`
- Functions:
  - `DatabaseSetup.resolveDbConnection`, `DatabaseSetup.bootstrapFromFileIfEmpty`, `DatabaseSetup.validateAmbNetworkAgainstDb`, `DatabaseSetup.loadPersistedDbState`
  - `RouteRegistration.CreateBoot`
  - `CoreRuntime.startHost` (picks `DbAgent` when `DbStatus.Ok`, else `FileAgent`)

## 4. Context
File mode with a database connected is not a supported setup.

- `DatabaseSetup.resolveDbConnection persistenceMode connStr dataDir` runs `Database.initSchema`. With `PersistenceMode.File` it also seeds an empty database from the `.amb` files (`bootstrapFromFileIfEmpty`), or compares the files with the database and rebuilds it (`validateAmbNetworkAgainstDb`), and returns `Ok`, `Mismatch1` or `Mismatch2`.
- The only `src/` caller is `RouteRegistration.CreateBoot`. It always passes `PersistenceMode.Db`. Only two tests in `DocumentLoaderTests.fs` pass `PersistenceMode.File`.
- `DatabaseSetup` is the only `src/` caller of `DocumentLoader.loadState` and `DocumentLoader.tryLoadState` apart from `FileAgent.loadOrFail`.
- The `Persistence:Mode` config key is read by nothing in `src/`.

## 5. Changes
- Delete `DatabaseSetup.PersistenceMode`, `bootstrapFromFileIfEmpty`, `validateAmbNetworkAgainstDb` and `loadPersistedDbState`.
- `resolveDbConnection` takes only `connStr`. It returns `Absent` for an empty string, `Ok` after `Database.initSchema`, and `Absent` when that throws. Update its doc comment.
- `RouteRegistration.CreateBoot` calls `DatabaseSetup.resolveDbConnection dbConnString`.

## 6. Tests
- Delete `resolveDbConnection file mode matching amb network returns Ok` in `tests/Server.Tests/DocumentLoaderTests.fs`.
- Delete `resolveDbConnection file mode divergent amb rebuild returns Mismatch1` in the same file.
- Filters:
  - `scripts/agent-test.sh --filter "DocumentLoaderTests"`
  - `scripts/agent-test.sh --filter "DatabaseSetupTests"`
  - `scripts/agent-test.sh --filter "StateEndpointTests"`

## 7. Done
- The named filters pass.
- No file under `src/` or `tests/` names `PersistenceMode`, `bootstrapFromFileIfEmpty` or `validateAmbNetworkAgainstDb`.
- Upload, import, parse, Load, undo and persist complete.
