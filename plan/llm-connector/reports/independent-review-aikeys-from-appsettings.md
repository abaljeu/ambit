# Independent review: 15 — AiKeys from appsettings

Independent two-axis review. The reviewer did not write the implementation. Not approval. Ticket [15 — AiKeys from appsettings](../issues/15-aikeys-from-appsettings.md) **Status:** stays `coded`.

**Verdict: Good**

**Pin:** tip `103511960ac4a62c73b71f0e43eb626f4cac4c08` (named `cursor/aikeys-from-appsettings-567f`). User named `origin/staging`; three-dot `origin/staging...HEAD` is non-empty.

**Commits:** `10351196` Wire AiKeys from appsettings into the Run Agent Actor.

**Spec:** [15 — AiKeys from appsettings](../issues/15-aikeys-from-appsettings.md). Also [02 — Which LLM and where credentials live](../issues/02-which-llm-and-credentials.md) 2026-09-19 AiKeys amend. Glossary [CONTEXT.md](../../../CONTEXT.md) **AI** (not Ask).

**Mechanical scan:** `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` (script invoked as `python3`; `python` is absent). Exit 1.

```
plan/llm-connector/issues/15-aikeys-from-appsettings.md:11  .agents/rules/refer-by-name.md  BARE_ID  ticket 14
src/Server/RouteRegistration.fs  .agents/rules/fsharp-source.md  FILE 415->417  already over 400 or new file over 400; change increased it
tests/Server.Tests/AskCancelHarness.fs  .agents/rules/fsharp-source.md  FILE 476->478  already over 400 or new file over 400; change increased it
--- measure-fs-size ---
src/Server/AiKeys.fs::fromConfig: lines 15-27 (13 lines)
src/Server/AiKeys.fs::resolve: lines 28-41 (14 lines)
src/Server/AiKeys.fs::keynameFromText: lines 42-51 (10 lines)
src/Server/RunAgentActor.fs::commandKeyname: lines 60-64 (5 lines)
src/Server/RunAgentActor.fs::apiKeyFor: lines 65-67 (3 lines)
src/Server/RunAgentActor.fs::complete: lines 97-110 (14 lines)
src/Server/RunAgentActor.fs::runBody: lines 155-171 (17 lines)
src/Server/RunAgentActor.fs::actorFn: lines 173-178 (6 lines)
```

Axis drafts: [Standards](code-review-standards-aikeys-from-appsettings.md), [Spec](code-review-spec-aikeys-from-appsettings.md). Axes stay separate below. No must-fix. [RouteRegistration.fs](../../../src/Server/RouteRegistration.fs) FILE growth is a documented hit; the same [fsharp-source](../../../.agents/rules/fsharp-source.md) rule says the split is a later standalone commit.

## Standards

# Standards review: AiKeys from appsettings

Range: `git diff origin/staging...HEAD` at `103511960ac4a62c73b71f0e43eb626f4cac4c08`. Mechanical scan hits are documented-standard hits.

## Documented-standard hits

1. **Bare id on [15 — AiKeys from appsettings](../issues/15-aikeys-from-appsettings.md)** — [refer-by-name.md](../../../.agents/rules/refer-by-name.md): include the name with every id. Line 11 says "stays the ticket 14 path". Use [14 — Provider-named AI errors](../issues/14-provider-named-ai-errors.md).
2. **[RouteRegistration.fs](../../../src/Server/RouteRegistration.fs) 415 to 417 lines** — [fsharp-source.md](../../../.agents/rules/fsharp-source.md): a file already over 400 lines must split when a change increases it. Hunk in `CreateBoot`: `RunAgentActor.actorFn (AiKeys.fromConfig this.Config)`.
3. **[AskCancelHarness.fs](../../../tests/Server.Tests/AskCancelHarness.fs) 476 to 478 lines** — scan cites [fsharp-source.md](../../../.agents/rules/fsharp-source.md) FILE. The same rule says the file-size split does not apply to tests. No production split duty.

Measured bindings in [AiKeys.fs](../../../src/Server/AiKeys.fs) and [RunAgentActor.fs](../../../src/Server/RunAgentActor.fs) are under 40 lines. No 40-line hit. New F# has no line over 100 characters and no mutable or exceptions.

## Smells (judgement, not violations)

1. **Middle Man** — [RunAgentActor.fs](../../../src/Server/RunAgentActor.fs) `apiKeyFor` only forwards:

```
    let private apiKeyFor keys (input: ActorInput) =
        AiKeys.resolve keys (commandKeyname input)
```

2. **Duplicated Code** — [AgentAuthErrorTests.fs](../../../tests/Server.Tests/AgentAuthErrorTests.fs) `empty AiKeys names Cursor missing key on ActorStop` repeats the `EventBody.ActorStop` / `CmdLastResult.Error (Some "AI", named)` block from `setFake unauthorized names Cursor on ActorStop`.
3. **Mysterious Name** — [AiKeysTests.fs](../../../tests/Server.Tests/AiKeysTests.fs) facts `Ask without keyname sends the first AiKeys ApiKey`, `Ask keyname sends that entry ApiKey`, `Ask unknown keyname sends empty ApiKey`. [CONTEXT.md](../../../CONTEXT.md): the Actor is AI, not Ask.

No [core-api.md](../../../.agents/rules/core-api.md) miss: composition injects keys; Core takes `ActorFn` only.

## Spec

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

## Summary

Standards: 6 findings (3 documented-standard hits, 3 smell judgements). Worst: [RouteRegistration.fs](../../../src/Server/RouteRegistration.fs) already over 400 lines and grew. Spec: 0 findings. Worst: none.
