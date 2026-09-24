# 23 — Outbound Cursor API key secrets

**Status:** done
**Blocked by:** None — [15 — AiKeys from appsettings](15-aikeys-from-appsettings.md) is `done`.
**Type:** coding
Estimate: 3h
Actual: 1h

## Context

A Cursor API key is an outbound secret. A leaked key can spend cloud-agent tokens. A second copy of the same key does not reduce that harm. A desktop key and a server key are different keys. Alan can rotate the desktop key and does not upload a new key to the server.

The value is in two git-tracked files, under `AiKeys`:

- [src/Server/appsettings.json](../../../src/Server/appsettings.json)
- [src/appsettings.Development.json](../../../src/appsettings.Development.json)

The nonempty value first appears in commit `f598262c` on both files. Commit `85186f23` added empty `AiKeys` placeholders on the Server file. [src/CloudAgents.Console/appsettings.json](../../../src/CloudAgents.Console/appsettings.json) has an empty `ApiKey`. [src/Server/appsettings.Development.json](../../../src/Server/appsettings.Development.json) is gitignored. This ticket does not print the key.

[AiKeys.fs](../../../src/Server/AiKeys.fs) binds `AiKeys` as a list of `{ Name, ApiKey }`. `AiKeys.resolve` uses the first entry when `?ai` has no keyname. [RunAgentActor](../../../src/Server/RunAgentActor.fs) passes that string to `RunnerConfig.ApiKey`. CloudAgents stays settings-blind.

[CloudAgents.Console](../../../src/CloudAgents.Console/Program.fs) and [Config.fs](../../../src/CloudAgents.Console/Config.fs) read `ApiKey` from the CLI, then appsettings (`ApiKey`, or the first `AiKeys` entry), then `CURSOR_API_KEY`.

[deploy-azure.md](../../../doc/reference/deploy-azure.md) already puts `DB_CONNECTION_STRING` in Azure App Settings. [Server.fs](../../../src/Server/Server.fs) `addAppSettings` loads `/home/appsettings.Production.json` on App Service. That call runs after `CreateBuilder`, so a key in that file replaces an environment variable.

This ticket amends the list shape and the first-entry default in [15 — AiKeys from appsettings](15-aikeys-from-appsettings.md). Do the parts below in order. Finish one part before the next.

## What to build

### 1. Remove the tracked key value

1. [x] Set `ApiKey` to `""` in [src/Server/appsettings.json](../../../src/Server/appsettings.json) and [src/appsettings.Development.json](../../../src/appsettings.Development.json). Leave the JSON keys. Keep the `{ Name, ApiKey }` list until part 3.
2. [x] Do not rewrite git history.
3. [x] Alan revoked the key at [cursor.com/settings](https://cursor.com/settings) because the value is already in history. This ticket does not rotate the key.

### 2. Shared user-secrets for local runs

1. [x] Set the same `UserSecretsId` on [Gambol.Server.fsproj](../../../src/Server/Gambol.Server.fsproj), [Gambol.Desktop.fsproj](../../../src/Desktop/Gambol.Desktop.fsproj), and [Gambol.CloudAgents.Console.fsproj](../../../src/CloudAgents.Console/Gambol.CloudAgents.Console.fsproj). Local runs read the key from that store, outside the repo.
2. [x] Server Development reads the key from user-secrets. `addAppSettings` runs after `CreateBuilder`, so a later JSON `ApiKey` can replace the user-secret. An empty or old `ApiKey` in JSON must not replace the user-secret. The gitignored [src/Server/appsettings.Development.json](../../../src/Server/appsettings.Development.json) is in that later load.
3. [x] Console stops reading `ApiKey` from appsettings. Console reads user-secrets or `CURSOR_API_KEY`. Model, Repo, Ref, Name, and `AiRepos` stay on the appsettings path.
4. [x] Update the Console help text and [CloudAgents.Console README](../../../src/CloudAgents.Console/README.md) so they do not tell the user to put `ApiKey` in appsettings.
5. [x] `--api-key` stays. A key on the command line lands in shell history. That cleanup is a follow-up, not this part. Part 2 still uses the list shape (`AiKeys__0__ApiKey`). Part 3 changes the names.

### 3. Name-keyed AiKeys and DefaultAiKey

1. [x] Change `AiKeys` from a list of `{ Name, ApiKey }` to a name-keyed map, plus `DefaultAiKey`. Tracked JSON keeps empty strings. Example shape, with no real key:

```json
"DefaultAiKey": "cursor",
"AiKeys": {
  "cursor": ""
}
```

2. [x] Environment variables are `AiKeys__<name>` and `DefaultAiKey`. They are not `AiKeys__0__ApiKey`. Move the part 2 user-secret to these names.
3. [x] `AiKeys.resolve` uses `DefaultAiKey` when `?ai` has no keyname. It does not use list order. A named lookup stays case-insensitive. A missing name, a missing default, or an empty value yields `""`.
4. [x] Update [AiKeysTests.fs](../../../tests/Server.Tests/AiKeysTests.fs). The default test is `DefaultAiKey`, not the first entry. `?ai` keyname selection stays. CloudAgents still receives only `RunnerConfig.ApiKey`.

The shared user-secrets file (`UserSecretsId` `a6b5ead7-8f2d-4482-acb0-0232585d7c6e`) is a general holder. Part 3 left `AiKeys` and `DefaultAiKey` in that file. `grokbot` is an additional top-level section. Provisional shape, placeholders only:

```json
{
  "grokbot": {
    "WakeUrl": "https://…",
    "WakeSecret": "…",
    "InboundSecret": "…"
  }
}
```

`WakeUrl` and `WakeSecret` are the pair this secrets strategy owns long-term. `InboundSecret` is Ambit Server config. The same user-secrets store is fine when that config is ready. A temporary inject of `InboundSecret` into this `grokbot` holder is fine for the first slice. This note does not write a secret value.

### 4. Production keys in Azure App Settings

1. [x] Document in [deploy-azure.md](../../../doc/reference/deploy-azure.md) that production secrets live in Azure App Settings (environment variables), not in `appsettings.Production.json`. Name the Cursor server key. Also name a `grokbot` section (`WakeUrl`, `WakeSecret`, and `InboundSecret` as a temporary resident).
2. [x] Use the part 3 names (`DefaultAiKey` and `AiKeys__<name>`). The server key is not the desktop key. The same App Settings can name a `grokbot` section (`WakeUrl`, `WakeSecret`, and `InboundSecret` as a temporary resident).
3. [x] `/home/appsettings.Production.json` stays for non-secrets. Do not put `AiKeys`, `DefaultAiKey`, or `grokbot` in that file. `addAppSettings` loads it after environment variables, so a secret there would replace the App Setting.
4. [x] Key Vault references are later, when there are many secrets. Do not add them in this part.

### 5. Secrets reference

1. [x] Add [doc/reference/secrets.md](../../../doc/reference/secrets.md). For each secret, name it, say which environments need it, and say where the value lives. Do not write values. The page names the Cursor desktop key, the Cursor server key, and a `grokbot` section (`WakeUrl`, `WakeSecret`, and `InboundSecret` as a temporary resident).
2. [x] Include the desktop Cursor key (Development, shared user-secrets; Console may use `CURSOR_API_KEY`) and the server Cursor key (Production, Azure App Settings). State that they are different keys. Include `grokbot` `WakeUrl` and `WakeSecret` as the pair this strategy owns, and `InboundSecret` as Ambit Server config with a temporary inject into the `grokbot` holder for the first slice. Same three facts. No values.
3. [x] Include the App Settings that [deploy-azure.md](../../../doc/reference/deploy-azure.md) already names (`DB_CONNECTION_STRING`, `Auth__Username`, `Auth__Password`) with the same three facts and no values.

### 6. Non-goals

1. Rewrite git history, or revoke the key at Cursor.
2. Remove `--api-key` (shell history is a follow-up).
3. Azure Key Vault references.
4. CloudAgents reading appsettings or user-secrets.
5. A change to the `AiRepos` shape.

## See also

[15 — AiKeys from appsettings](15-aikeys-from-appsettings.md), [20 — Gitignore Development appsettings](20-gitignore-development-appsettings.md), [21 — Console lists models and fills CLI gaps from appsettings](21-console-lists-models-and-appsettings.md), [02 — Which LLM and where credentials live](02-which-llm-and-credentials.md), [deploy-azure.md](../../../doc/reference/deploy-azure.md), [Secrets](../../../doc/reference/secrets.md)

## Comments

- 2026-09-24 — Filed from the agreed strategy. The nonempty value is in history from `f598262c`, not from the empty placeholders in `85186f23`. Status `defined`.
- 2026-09-24 — Part 3 binds `AiKeys` as a name-keyed map (`desktop` and `server`) plus `DefaultAiKey`. [src/appsettings.Development.json](../../../src/appsettings.Development.json) sets `DefaultAiKey` to `desktop`. Production sets `DefaultAiKey` on its own. The local user-secrets entry to set is `AiKeys:desktop` together with `DefaultAiKey`. `AiKeys:0:ApiKey` is no longer read. This note does not write a secret value. Status stays `defined`.
- 2026-09-24 — The shared user-secrets file (`a6b5ead7-8f2d-4482-acb0-0232585d7c6e`) is a general holder. `AiKeys` and `DefaultAiKey` stay as part 3 left them. `grokbot` is an additional top-level section (`WakeUrl`, `WakeSecret`, and a temporary `InboundSecret`). `WakeUrl` and `WakeSecret` are the pair this strategy owns. `InboundSecret` is Ambit Server config. This note does not write a secret value. Parts 4 and 5 stay open. Status stays `defined`.
- 2026-09-24 — Part 5 adds [Secrets](../../../doc/reference/secrets.md). Status `done`.
- 2026-09-24 — Correction. The configuration names are `AiKeys:desktop`, `AiKeys:server`, `grokbot:WakeUrl`, `grokbot:WakeSecret`, `grokbot:InboundSecret`, and `DefaultAiKey` in user-secrets and in Azure. An Azure App Setting writes a colon as a double underscore (`AiKeys:server` is set as `AiKeys__server`). Status stays `done`.

## Time

- 2026-09-24 1h — part 3 name-keyed AiKeys and DefaultAiKey (from chat)
