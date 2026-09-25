# API key runtime resolution diagnosis

Write-once diagnosis. No source or store was changed. No secret value is in this file.

## 1. Symptom

A Cursor API key that the operator stored in .NET User Secrets or Azure App Settings is reported missing at run time. The miss looks like a search for `ApiKey` on appsettings JSON.

The user-visible miss on Server is [CursorAdapter.fs](../../../src/CloudAgents/Internal/CursorAdapter.fs) lines 58–59: empty `RunnerConfig.ApiKey` becomes `AuthenticationFailed` with reason `missing key`. [RunAgentActor.fs](../../../src/Server/RunAgentActor.fs) line 307 sets that field from `AiKeys.resolve`. Console prints `Error: no API key` in [Program.fs](../../../src/CloudAgents.Console/Program.fs) lines 255–258 when `ResolvedSettings.ApiKey` is `None`.

## 2. Repro command and result

Command (already run):

```
./tmp/run-apikey-diag.sh
```

That script ran focused tests, then `dotnet run --project tmp/apikey-config-order/apikey-config-order.fsproj`.

Existing tests (green; they lock the bind rules, not the live store):

- `dotnet test tests/Server.Tests/Gambol.Server.Tests.fsproj --filter "FullyQualifiedName~AiKeysTests|FullyQualifiedName~empty_AiKeys"` — Passed 12.
- `dotnet test tests/CloudAgents.Tests/Gambol.CloudAgents.Tests.fsproj --filter "FullyQualifiedName~ConsoleConfigTests"` — Passed 7.

Harness (synthetic fake values only; live store printed as presence flags, never values): all synthetic cases PASS. Live User Secrets on this machine: `DefaultAiKey` absent, `AiKeys:desktop` absent, `AiKeys:server` absent, `AiKeys:0:ApiKey` present and nonempty.

That live shape is the old list path from [23 — Outbound Cursor API key secrets](../issues/23-outbound-cursor-api-key-secrets.md) part 2 (`AiKeys__0__ApiKey`). Part 3 changed the names to `DefaultAiKey` and `AiKeys:<name>`. The store on this workplace was not moved.

## 3. Config path (Server / Desktop)

[Server.fs](../../../src/Server/Server.fs) `main` (lines 278–285): `WebApplication.CreateBuilder` (appsettings, env-specific JSON, Development user-secrets, environment variables, command line), then `addAppSettings` (lines 83–95), then `addDevelopmentUserSecrets` (lines 97–107).

`addAppSettings` adds JSON after CreateBuilder. On Azure it adds `/home/appsettings.json` and `/home/appsettings.{env}.json`. Off Azure it adds `appsettings.{Environment}.json` again. Later JSON replaces earlier values, including empty strings.

`addDevelopmentUserSecrets` runs only when `IsDevelopment()`. It exists so an empty or old `ApiKey` in later JSON does not replace user-secrets. It does **not** add environment variables again. Production Azure App Settings are environment variables. A later `/home` JSON object with empty `AiKeys` replaces `AiKeys__server`.

[AiKeys.fs](../../../src/Server/AiKeys.fs) lines 22–32: `fromConfig` reads `DefaultAiKey` and `GetSection("AiKeys").GetChildren()` as a name-to-string map. A child with no `Value` (an object, including `AiKeys:0`) stores `""`. [AiKeys.fs](../../../src/Server/AiKeys.fs) lines 46–51: `resolve` uses `DefaultAiKey` when `?ai` has no keyname. A missing name or empty value is `""`.

[RouteRegistration.fs](../../../src/Server/RouteRegistration.fs) line 33 binds `AiKeys.fromConfig this.Config` once at boot into [RunAgentActor](../../../src/Server/RunAgentActor.fs).

Tracked placeholders: [src/Server/appsettings.json](../../../src/Server/appsettings.json) lines 14–18 (`DefaultAiKey` `""`, `AiKeys.desktop` and `AiKeys.server` `""`). [src/appsettings.Development.json](../../../src/appsettings.Development.json) lines 15–19 set `DefaultAiKey` to `desktop` and the same empty map. [doc/reference/secrets.md](../../../doc/reference/secrets.md) names `AiKeys:desktop` / `AiKeys:server` and `UserSecretsId` `a6b5ead7-8f2d-4482-acb0-0232585d7c6e`.

[Gambol.Server.fsproj](../../../src/Server/Gambol.Server.fsproj), [Gambol.Desktop.fsproj](../../../src/Desktop/Gambol.Desktop.fsproj), and [Gambol.CloudAgents.Console.fsproj](../../../src/CloudAgents.Console/Gambol.CloudAgents.Console.fsproj) share that `UserSecretsId`. [launchSettings.json](../../../src/Server/Properties/launchSettings.json) sets `ASPNETCORE_ENVIRONMENT=Development` for Server.

Desktop does not bind `AiKeys`. [Desktop.fs](../../../src/Desktop/Desktop.fs) opens a WebView to local Server or cloud. [LocalProxy.fs](../../../src/Desktop/LocalProxy.fs) line 645 uses `WebApplication.CreateBuilder([||])` for the proxy only. The Desktop `UserSecretsId` only shares the store with Server and Console. Local headed runs still go through Server `main`.

No Committed Decision under [doc/Decisions/](../../../doc/Decisions/) covers this bind.

## 4. Config path (CloudAgents.Console)

[Program.fs](../../../src/CloudAgents.Console/Program.fs) `loadUserSecretApiKey` (lines 229–238) builds **only** `AddUserSecrets`. It does not load appsettings. It reads `DefaultAiKey` from that store, then `AiKeys:{name}`. If `DefaultAiKey` is missing, [Config.fs](../../../src/CloudAgents.Console/Config.fs) `apiKeyFromSecrets` (lines 218–224) returns `None` even when `AiKeys:desktop` exists.

[Config.fs](../../../src/CloudAgents.Console/Config.fs) `loadFiles` / `fromElement` (lines 138–206) read Model, ModelParams, Repo, Ref, Name, and `AiRepos`. They do **not** read `ApiKey`, `DefaultAiKey`, or `AiKeys`. [Config.fs](../../../src/CloudAgents.Console/Config.fs) `resolve` (lines 226–238) picks CLI, then the secret result, then `CURSOR_API_KEY`.

[src/CloudAgents.Console/appsettings.json](../../../src/CloudAgents.Console/appsettings.json) still has root `"ApiKey": ""`. That field is unused. Help text still says user-secrets `AiKeys:desktop` ([Program.fs](../../../src/CloudAgents.Console/Program.fs) lines 20–21). [Console launchSettings](../../../src/CloudAgents.Console/Properties/launchSettings.json) sets working directory to `src/Server`, so file load can see Server JSON, but that JSON cannot supply the API key.

## 5. Ranked hypotheses

1. **Shape mismatch (confirmed).** If the store still uses `AiKeys:0:ApiKey`, then `AiKeys.resolve` and Console `AiKeys:{DefaultAiKey}` stay empty, and the operator still sees an `ApiKey` field. Live store flags and [AiKeysTests](../../../tests/Server.Tests/AiKeysTests.fs) `fromConfig ignores the old list path` (lines 43–51) match.
2. **Later JSON overwrites Azure env (confirmed on synthetic chain).** If `/home` JSON repeats empty `AiKeys`, App Setting `AiKeys__server` is replaced. Harness H1: env then later empty JSON → empty resolve. Env last → `FAKE_ENV_SERVER`. [Server.fs](../../../src/Server/Server.fs) does not re-add environment variables after `addAppSettings`.
3. **Later JSON overwrites user-secrets unless re-added (confirmed on synthetic chain).** Harness H3. Development re-add in [Server.fs](../../../src/Server/Server.fs) lines 97–107 blocks this for name-keyed secrets only. It cannot turn an old list secret into `AiKeys:desktop`.
4. **Console ignores appsettings `DefaultAiKey` (confirmed).** Harness H2. [src/appsettings.Development.json](../../../src/appsettings.Development.json) `DefaultAiKey=desktop` never reaches `loadUserSecretApiKey`.
5. **Unused Console `ApiKey` JSON (confirmed).** Harness H5. Looks like the key is read from appsettings; it is not.

Hypothesis 1 is the load-bearing miss on this workplace. Hypothesis 2 is the production footgun if `/home` JSON copies the tracked `AiKeys` placeholders.

## 6. Can User Secrets or Azure settings reach `RunnerConfig.ApiKey`?

Yes, only when the **name-keyed** values win in the final `IConfiguration` (Server) or in the user-secrets-only builder plus `DefaultAiKey` (Console).

No, when (a) the store still has `AiKeys:0:ApiKey`, or (b) later JSON writes empty `AiKeys:<name>` after environment variables, or (c) Console has keys but no `DefaultAiKey` in the secrets store.

CloudAgents stays settings-blind ([PublicTypes.fs](../../../src/CloudAgents/PublicTypes.fs) lines 51–54). The caller must put a nonempty string in `RunnerConfig.ApiKey`.

## 7. Minimal likely fix direction

Do not change CloudAgents HTTP. Fix the bind, then the store.

1. Move the shared user-secrets store from `AiKeys:0:ApiKey` to `AiKeys:desktop` (and `DefaultAiKey=desktop` for Console). Do not print the value. `dotnet user-secrets set` with the new names, then remove the old list keys.
2. On Server, after `addAppSettings`, re-add environment variables the same way Development re-adds user-secrets, so empty `/home` JSON cannot replace `AiKeys__server`. Keep `AiKeys` out of `/home` JSON as [doc/reference/deploy-azure.md](../../../doc/reference/deploy-azure.md) already says.
3. Optional Console harden: read `DefaultAiKey` from the same appsettings files that already set `desktop` in Development, then look up `AiKeys:{name}` in user-secrets. Stop shipping unused root `ApiKey` in Console appsettings so the miss does not look like an appsettings `ApiKey` read.

A correct regression seam is the harness chain (later JSON after env/secrets) plus `fromConfig` on the old list path (already in [AiKeysTests.fs](../../../tests/Server.Tests/AiKeysTests.fs)). Do not add a test that reads the live secrets store.
