# Spec review — 16 AiRepos from appsettings

Range: `git diff origin/staging...HEAD` at `97e7f18098fefbd5a1039fce5576b8e5f0bb6015`. Primary: [16 — AiRepos from appsettings](plan/llm-connector/issues/16-airepos-from-appsettings.md). Also [15 — AiKeys from appsettings](plan/llm-connector/issues/15-aikeys-from-appsettings.md), [llm-connector architecture](plan/llm-connector/arch.md) injected `AiRepos`, [02 — Which LLM and where credentials live](plan/llm-connector/issues/02-which-llm-and-credentials.md) 2026-09-20 lock, [CONTEXT.md](CONTEXT.md) **AI**.

**Commits:** `97e7f180` Keep RouteRegistration line count while injecting AiRepos. `38ad0f41` Name ticket 15 in the AiRepos parse rule. `22afd85f` Wire AiRepos from appsettings into the AI Actor.

## 1. Checklist (all met)

1. **AiRepos section** — Spec: `Name`, `Url`, optional `StartingRef`. [AiRepos.fs](src/Server/AiRepos.fs) `fromConfig` binds those children. Omit or `""` → `None`. Name match is case-insensitive. [appsettings.json](src/Server/appsettings.json) and [appsettings.Development.json](src/Server/appsettings.Development.json) hold a `life` URL. No secrets.
2. **Resolve by name; no first-repo default** — Spec: "Default when `?ai` has no reponame token: **no repos** (`None`). Do not auto-attach the first `AiRepos` entry." `resolve` returns `None` when omitted or missing; named hit → `Some [RepoConfig]`.
3. **`?ai` token rules** — Spec rules 1–5: no tokens = default key + no repo; two or more = first keyname, second reponame; one `AiKeys` Name = keyname, no repo (also when that Name is on both lists); one `AiRepos` Name only = reponame + default key; neither = keyname (unknown → empty `ApiKey`). `AiAskArgs.fromText` on Command text in [RunAgentActor.fs](src/Server/RunAgentActor.fs).
4. **AI Actor passes `RepoConfig list option`** — Spec: CloudAgents "only receives the resolved `RepoConfig list option`." Architecture: "Use injected `AiRepos` for `AgentRunner.start` repos (omit when no reponame; CloudAgents stays settings-blind)." [RouteRegistration.fs](src/Server/RouteRegistration.fs) injects `AiRepos.fromConfig` with `AiKeys`. Core unchanged.
5. **No secrets; Console CLI-only** — Spec: "CloudAgents.Console stays CLI-only (`--repo` / env); it does not read appsettings." Diff for `src/CloudAgents` and `src/CloudAgents.Console` is empty. [Program.fs](src/CloudAgents.Console/Program.fs) still uses `--repo` / env.

## 2. What to build (also met)

Bind/resolve parallel to `AiKeys.fromConfig`. Composition injects both lists; Core does not see repos. Placeholder JSON. Non-goals hold: no Console appsettings, no Grok/High/Fast, `AiKeys` JSON shape unchanged. `addAppSettings` load order is unchanged.

## 3. (a) Missing or partial

1. **None** — required AI Actor behavior from [16 — AiRepos from appsettings](plan/llm-connector/issues/16-airepos-from-appsettings.md) and the 2026-09-20 lock.

## 4. (b) Scope creep

1. **None** — plan notes record this ticket and add no extra runtime. `withHostKeysRepos` is the test seam. `AiKeys.tokensFromText` splits Command tokens for the new parse.

## 5. (c) Wrong implementation

1. **None** — Spec: "`?ai life` is a repo only when `life` is not also a key Name." Default `?ai` attaches no repo.

## 6. Count

0 findings.
