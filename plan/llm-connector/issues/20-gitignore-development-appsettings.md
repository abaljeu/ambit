# 20 — Gitignore Development appsettings

**Status:** done
**Blocked by:** None — [15 — AiKeys from appsettings](15-aikeys-from-appsettings.md) and [16 — AiRepos from appsettings](16-airepos-from-appsettings.md) are `done`.
**Type:** bug-fixing
Estimate: 30m
Actual: 30m

## Context

[15 — AiKeys from appsettings](15-aikeys-from-appsettings.md) treated `appsettings.Development.json` as a committed empty-`ApiKey` example. Cloud agents then committed live `AiKeys`. Development must be gitignored like Production. Tracked empty `AiKeys` / `AiRepos` placeholders stay in base `appsettings.json` only. `addAppSettings` load order does not change.

## What to build

### 1. Stop tracking Development

1. [x] `.gitignore` lists `src/Server/appsettings.Development.json` next to Production (local secrets; do not commit).
2. [x] Remove the file from the git tree (`git rm --cached`). The local file may remain ignored.
3. [x] Tracked `appsettings.json` keeps empty-placeholder `AiKeys` / `AiRepos` examples. No real secrets.

### 2. Docs

1. [x] [15 — AiKeys from appsettings](15-aikeys-from-appsettings.md) comments, architecture, and map Notes say Development is gitignored like Production; base `appsettings.json` holds empty placeholders.

### 3. Non-goals

1. Rewrite git history of old commits that once had keys.
2. Change Runtime config binding.

## See also

[15 — AiKeys from appsettings](15-aikeys-from-appsettings.md), [16 — AiRepos from appsettings](16-airepos-from-appsettings.md), [llm-connector architecture](../arch.md), [llm-connector map](../map.md)

## Comments

- 2026-09-21 — Alan accepted. Already on staging via #96; Status `done` with the 19 land.
- 2026-09-21 — Renumbered from 19. 19 is [19 — AI extract pack is XML with Focus cssClass](19-ai-xml-pack-focus-css.md). Status stays `coded`.
- 2026-09-21 — Coded: gitignore Development; untracked from the tree; placeholders remain in base `appsettings.json`. Status `coded`.

## Time

- 2026-09-21 30m — gitignore, untrack, doc amend (from chat)
