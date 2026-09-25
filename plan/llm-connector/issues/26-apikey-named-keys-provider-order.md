# 26 — Named AiKeys win over later JSON

**Status:** coded
**Blocked by:** None
**Type:** bug-fixing
Estimate: 2h
Actual: 2h 30m

## Context

Diagnosis: [apikey-runtime-resolution-diagnosis](../reports/apikey-runtime-resolution-diagnosis.md) (write-once; do not edit). A stored Cursor key looks missing at run time. The live local store still has obsolete `AiKeys:0:ApiKey`. Server extra JSON after `CreateBuilder` can replace Azure App Settings (`AiKeys__cursor`, `DefaultAiKey`) with empty placeholders. CloudAgents stays settings-blind and receives only `RunnerConfig.ApiKey`.

Map, spec, and architecture already name the secrets policy. This ticket does not change those layers.

The logical key is `cursor`. Local User Secrets use `AiKeys:cursor`. Azure uses `AiKeys__cursor`. `DefaultAiKey` selects that name and is `cursor` in each secret store. Tracked appsettings do not infer the selector.

## What to build

1. [x] Server Development reloads shared user secrets after extra JSON so `AiKeys:cursor` and `DefaultAiKey` win over empty later JSON.
2. [x] After that JSON, re-add environment variables so Production Azure App Settings win over empty or stale `/home` JSON. Conventional order: JSON below secrets/environment; environment is the highest relevant provider.
3. [x] Do not add a read path for `AiKeys:0:ApiKey`. Keep the name-keyed map. Canonical child is `cursor`.
4. [x] Tracked JSON must not hold real keys. Remove empty `AiKeys` / unused Console `ApiKey` placeholders when they mislead or overwrite. Do not set `DefaultAiKey` in tracked appsettings.
5. [x] Console help and README must match real precedence: CLI, then user-secrets `DefaultAiKey=cursor` plus `AiKeys:cursor`, then `CURSOR_API_KEY`. Change Console code only if it violates that contract.
6. [x] Regression test at the real provider-order seam (later empty `AiKeys:cursor` JSON after environment `AiKeys__cursor`). Do not read the live secrets store.
7. [x] Document exact local migration commands and Azure App Setting names. Do not print secret values.

## Local migration (human)

Do not paste the key into chat. Copy the old value yourself, then set the named keys. Shared `UserSecretsId` is `a6b5ead7-8f2d-4482-acb0-0232585d7c6e`.

List names only:

```
dotnet user-secrets list --project src/Server
```

Set the Cursor key and selector (replace the value locally; do not log it):

```
dotnet user-secrets set "AiKeys:cursor" "<paste locally>" --project src/Server
dotnet user-secrets set "DefaultAiKey" "cursor" --project src/Server
```

Remove the obsolete list path after the named key works:

```
dotnet user-secrets remove "AiKeys:0:ApiKey" --project src/Server
dotnet user-secrets remove "AiKeys:0:Name" --project src/Server
```

Server, Desktop, and Console share that store. `DefaultAiKey=cursor` has no secret value.

## Azure App Settings

Set these names (portal colon form or environment double-underscore form):

- `DefaultAiKey` = `cursor`
- `AiKeys:cursor` / `AiKeys__cursor` = the production Cursor key
- Keep `AiKeys`, `DefaultAiKey`, and `grokbot` out of `/home` JSON

## Non-goals

1. CloudAgents HTTP or `RunnerConfig` shape.
2. Backward compatibility for the indexed list path.
3. Executing `dotnet user-secrets` commands that require or print a key value.

## See also

[23 — Outbound Cursor API key secrets](23-outbound-cursor-api-key-secrets.md), [15 — AiKeys from appsettings](15-aikeys-from-appsettings.md), [secrets.md](../../../doc/reference/secrets.md), [deploy-azure.md](../../../doc/reference/deploy-azure.md)

## Comments

- 2026-09-25 — Filed from diagnosis. Status `defined`.
- 2026-09-25 — Coded: `ConfigurationOrder.addOverridesAfterJson` reloads Development user secrets then environment after extra JSON; tracked empty `AiKeys` / Console `ApiKey` placeholders removed; Console help/README match user-secrets `AiKeys:cursor`. Regression: later empty JSON after env. Status `coded`.
- 2026-09-25 — Canonical logical key is `cursor` (`AiKeys:cursor` / `AiKeys__cursor` / `DefaultAiKey=cursor`). Status stays `coded`.

## Time

- 2026-09-25 2h 30m — provider-order fix, `cursor` key names, regression test (from chat)
