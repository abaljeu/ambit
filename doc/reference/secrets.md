# Secrets

Category: Operations
See also: [[doc/reference/deploy-azure.md]], [23 — Outbound Cursor API key secrets](../../plan/llm-connector/issues/23-outbound-cursor-api-key-secrets.md), [26 — Named AiKeys win over later JSON](../../plan/llm-connector/issues/26-apikey-named-keys-provider-order.md)

This page names each secret, the environments that need it, and the store that holds the value. Values stay in those stores.

Production setup is [[doc/reference/deploy-azure.md]].

## Cursor keys

The logical key name is `cursor`. Local User Secrets use `AiKeys:cursor`. Azure App Settings use `AiKeys__cursor`. `DefaultAiKey` selects that name and is `cursor` in each secret store.

Server, Desktop, and Console share one user-secrets store. The `UserSecretsId` is `a6b5ead7-8f2d-4482-acb0-0232585d7c6e`. Console reads `DefaultAiKey` and `AiKeys:cursor` from that store only, then `CURSOR_API_KEY`. Tracked appsettings do not set `DefaultAiKey` or `AiKeys`. The old list name `AiKeys:0:ApiKey` is not read. Move a leftover list entry to `AiKeys:cursor` and set `DefaultAiKey` to `cursor`. Do not print the key.

Production stores the same names in Azure App Settings. Tracked appsettings and `/home/appsettings.Production.json` omit `AiKeys`, `DefaultAiKey`, and `grokbot`.

An Azure App Setting writes a colon as a double underscore (`AiKeys:cursor` is set as `AiKeys__cursor`).

## grokbot

`WakeUrl` and `WakeSecret` are the pair this secrets strategy owns.

Development and production use the names `grokbot:WakeUrl` and `grokbot:WakeSecret`. Development stores them in the same user-secrets store. Production stores them in Azure App Settings. The outbound wake POST sends `WakeSecret` as `Authorization: Bearer <WakeSecret>`.

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
