# Spec review — 15 AiKeys from appsettings

Range: `git diff origin/staging...HEAD` at `103511960ac4a62c73b71f0e43eb626f4cac4c08`. Primary: [15 — AiKeys from appsettings](plan/llm-connector/issues/15-aikeys-from-appsettings.md). Also [02 — Which LLM and where credentials live](plan/llm-connector/issues/02-which-llm-and-credentials.md) 2026-09-19 Amend, and [CONTEXT.md](CONTEXT.md) **AI** / **Run AI** / **Agent**.

## 1. Checklist (all met)

1. **AiKeys from IConfiguration** — [AiKeys.fs](src/Server/AiKeys.fs) `fromConfig` reads section `AiKeys` as Name + ApiKey children. Same indexer / `Option.ofObj` / `""` pattern as `RouteAuthentication.create` + `Auth:Username`.
2. **Resolve keyname or first** — `keynameFromText` plus `resolve`; [RunAgentActor.fs](src/Server/RunAgentActor.fs) sets `{ RunnerConfig.ApiKey = apiKey }` only.
3. **CloudAgents settings-blind** — no `IConfiguration` in `src/CloudAgents`.
4. **CURSOR_API_KEY removed** — env `runnerConfig` is gone from the AI Actor.
5. **No real secrets** — [appsettings.json](src/Server/appsettings.json) and [appsettings.Development.json](src/Server/appsettings.Development.json) use `"ApiKey": ""`. Production stays gitignored.
6. **Empty key names Cursor** — `""` still uses the ticket 14 CloudAgents `missing key` path (`providerName = "Cursor"`).

## 2. What to build (also met)

1. **Parse `?ai` keyname** — first token after `?ai` on Command Node text; extra tokens unused; Name match `OrdinalIgnoreCase`.
2. **Composition injects the list** — [RouteRegistration.fs](src/Server/RouteRegistration.fs) `CreateBoot` closes `AiKeys.fromConfig this.Config` into the AI `ActorFn`. CoreBoot / CoreActorPool see `ActorFn` only.
3. **Missing Name / empty list / empty ApiKey → `""`**.
4. **Non-goals held** — no CloudAgents settings or model defaults; no `CURSOR_API_KEY` env fallback; no Graph / cookie / DataDir key storage. Existing `addAppSettings` load order is unchanged.

## 3. (a) Missing or partial

1. **None** — required AI Actor behavior from ticket 15 and the ticket 02 Amend is present.

## 4. (b) Scope creep

1. **None** — map / arch / spec / project notes record this ticket. They add no extra runtime. `withHostKeys` is the test seam for the injected list.

## 5. (c) Wrong implementation

1. **None** — `behaviorFromText` lowercases the keyname; case-insensitive Name match still selects the `AiKeys` entry. Default `?ai` uses the first array entry.

## 6. Count

0 findings.
