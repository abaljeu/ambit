# Secrets

Category: Operations
See also: [[doc/reference/deploy-azure.md]], [23 — Outbound Cursor API key secrets](../../plan/llm-connector/issues/23-outbound-cursor-api-key-secrets.md)

This page names each secret, the environments that need it, and the store that holds the value. Values stay in those stores.

Production setup is [[doc/reference/deploy-azure.md]].

## Cursor keys

The desktop key and the server key are different keys.

The desktop key name is `AiKeys:desktop`. Development needs it. Server, Desktop, and Console read one shared user-secrets store. The `UserSecretsId` is `a6b5ead7-8f2d-4482-acb0-0232585d7c6e`. Development `DefaultAiKey` is `desktop`. Console may use `CURSOR_API_KEY`.

The server key name is `AiKeys:server`. Production needs it. The value lives in the Azure App Setting for `AiKeys:server`. Production `DefaultAiKey` is `server`. Tracked appsettings and `/home/appsettings.Production.json` omit `AiKeys`, `DefaultAiKey`, and `grokbot`.

An Azure App Setting writes a colon as a double underscore (`AiKeys:server` is set as `AiKeys__server`).

## grokbot

`WakeUrl` and `WakeSecret` are the pair this secrets strategy owns.

Development and production use the names `grokbot:WakeUrl` and `grokbot:WakeSecret`. Development stores them in the same user-secrets store. Production stores them in Azure App Settings.

`InboundSecret` is Ambit Server config. The first slice uses a temporary inject of `InboundSecret` into the same `grokbot` holder. The name is `grokbot:InboundSecret` in the user-secrets store and in Azure App Settings.

## Other App Settings

[[doc/reference/deploy-azure.md]] already names these production App Settings.

| Name | Environments | Where the value lives |
| --- | --- | --- |
| `DB_CONNECTION_STRING` | Production | Azure App Setting |
| `Auth__Username` | Production | Azure App Setting |
| `Auth__Password` | Production | Azure App Setting |

Development also requires `DB_CONNECTION_STRING`. See [[doc/reference/postgres-environments.md]].

Azure Key Vault references come later, when there are many secrets.
