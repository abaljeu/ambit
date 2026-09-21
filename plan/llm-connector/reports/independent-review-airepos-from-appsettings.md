# Independent review: 16 — AiRepos from appsettings

Independent two-axis review. The reviewer did not write the implementation. Not approval. Ticket [16 — AiRepos from appsettings](../issues/16-airepos-from-appsettings.md) **Status:** stays `coded`.

**Verdict: Good**

**Pin:** tip `97e7f18098fefbd5a1039fce5576b8e5f0bb6015` (named `cursor/airepos-from-appsettings-bc2c`). User named `origin/staging`; three-dot `origin/staging...HEAD` is non-empty.

**Commits:** `97e7f180` Keep RouteRegistration line count while injecting AiRepos. `38ad0f41` Name ticket 15 in the AiRepos parse rule. `22afd85f` Wire AiRepos from appsettings into the AI Actor.

**Spec:** [16 — AiRepos from appsettings](../issues/16-airepos-from-appsettings.md). Also [15 — AiKeys from appsettings](../issues/15-aikeys-from-appsettings.md) patterns and CloudAgents settings-blind lock ([llm-connector architecture](../arch.md) injected `AiRepos`; [02 — Which LLM and where credentials live](../issues/02-which-llm-and-credentials.md) 2026-09-20). Glossary [CONTEXT.md](../../../CONTEXT.md) **AI** (not Ask).

**Mechanical scan:** `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` (script invoked as `python3`; `python` is absent). Exit 1.

```
tests/Server.Tests/AskCancelHarness.fs  .agents/rules/fsharp-source.md  FILE 478->480  already over 400 or new file over 400; change increased it
--- measure-fs-size ---
src/Server/AiKeys.fs::tokensFromText: lines 42-48 (7 lines)
src/Server/AiKeys.fs::keynameFromText: lines 49-52 (4 lines)
src/Server/AiRepos.fs::fromConfig: lines 21-37 (17 lines)
src/Server/AiRepos.fs::startingRef: lines 38-41 (4 lines)
src/Server/AiRepos.fs::toRepoConfig: lines 42-45 (4 lines)
src/Server/AiRepos.fs::resolve: lines 46-58 (13 lines)
src/Server/AiRepos.fs::hasName: lines 62-66 (5 lines)
src/Server/AiRepos.fs::fromText: lines 67-84 (18 lines)
src/Server/RunAgentActor.fs::commandArgs: lines 60-64 (5 lines)
src/Server/RunAgentActor.fs::complete: lines 94-107 (14 lines)
src/Server/RunAgentActor.fs::runBody: lines 152-171 (20 lines)
src/Server/RunAgentActor.fs::actorFn: lines 173-178 (6 lines)
```

Axis drafts: [Standards](code-review-standards-airepos-from-appsettings.md), [Spec](code-review-spec-airepos-from-appsettings.md). Axes stay separate below. No must-fix. [AskCancelHarness.fs](../../../tests/Server.Tests/AskCancelHarness.fs) FILE growth is a documented hit; the same [fsharp-source](../../../.agents/rules/fsharp-source.md) rule says the file-size split does not apply to tests. Grouped-parameter note on `actorFn` keys/repos is a documented hit; [16 — AiRepos from appsettings](../issues/16-airepos-from-appsettings.md) asked composition to inject the `AiRepos` list alongside `AiKeys`.

## Standards

# Standards — [16 — AiRepos from appsettings](../issues/16-airepos-from-appsettings.md)

Range: three-dot `origin/staging...HEAD` (`97e7f180`). Scan exit 1. Printed FILE 478→480 on [AskCancelHarness.fs](../../../tests/Server.Tests/AskCancelHarness.fs) with rule [fsharp-source.md](../../../.agents/rules/fsharp-source.md). Measure-fs-size bindings stay under 40 lines. [RouteRegistration.fs](../../../src/Server/RouteRegistration.fs) line count does not grow.

## Hard violations

### [AskCancelHarness.fs](../../../tests/Server.Tests/AskCancelHarness.fs) — FILE 478→480

[fsharp-source.md](../../../.agents/rules/fsharp-source.md): already over 400 or new file over 400; change increased it. The same rule: the file-size split does not apply to tests. No production split duty.

### [RunAgentActor.fs](../../../src/Server/RunAgentActor.fs) — grouped parameters

Same rule: group related parameters into a named reused type. When a parameter belongs with existing ones, extend that type. Reuse a type that already exists. `complete` now takes `apiKey`, `repos`, and `document`, then calls `AgentRunner.start config prompt repos askOptions`. `StartArgs` already holds those four fields. `actorFn` adds `repos` beside `keys` with no named type. The pair also travels through `runBody`, `commandArgs`, [AiRepos.fs](../../../src/Server/AiRepos.fs) `AiAskArgs.fromText`, and `withHostKeysRepos`.

## Judgement-call smells

**Mysterious Name.** New Facts on `AiReposActorTests` use Ask. [CONTEXT.md](../../../CONTEXT.md) **AI**: it is not named Ask.

```
member _.``Ask without reponame sends no repos``() =
```

**Divergent Change.** [AiRepos.fs](../../../src/Server/AiRepos.fs) binds `AiRepos` and also owns `AiAskArgs.fromText` (keyname and reponame).

**Shotgun Surgery.** One bind/inject change edits Server, appsettings, harness, plan map/spec/arch, and [16 — AiRepos from appsettings](../issues/16-airepos-from-appsettings.md). Expected for this ticket.

**Duplicated Code** (suppressed). `AiRepos.fromConfig` repeats the `AiKeys.fromConfig` indexer shape. The ticket asked for that same pattern.

## No hit

No mutable. No Exceptions. No line over 100 characters in the new F#. Public `fromConfig` / `resolve` / `fromText` require the module name (`RequireQualifiedAccess`). Core does not see repos ([core-api.md](../../../.agents/rules/core-api.md)). CloudAgents stays settings-blind. New plan links wrap number and name ([refer-by-name.md](../../../.agents/rules/refer-by-name.md)). Stage stays `done` on the follow-up. Surgical under-100-line preference is not a script fail ([core-agent-behavior.md](../../../.agents/rules/core-agent-behavior.md)).

Counts: 1 hard (grouped `keys` and `repos`), 1 scan FILE exempt, 3 smells (1 suppressed). Worst hard: `keys` and `repos` ungrouped on `actorFn`.

## Spec

# Spec review — 16 AiRepos from appsettings

Range: `git diff origin/staging...HEAD` at `97e7f18098fefbd5a1039fce5576b8e5f0bb6015`. Primary: [16 — AiRepos from appsettings](../issues/16-airepos-from-appsettings.md). Also [15 — AiKeys from appsettings](../issues/15-aikeys-from-appsettings.md), [llm-connector architecture](../arch.md) injected `AiRepos`, [02 — Which LLM and where credentials live](../issues/02-which-llm-and-credentials.md) 2026-09-20 lock, [CONTEXT.md](../../../CONTEXT.md) **AI**.

**Commits:** `97e7f180` Keep RouteRegistration line count while injecting AiRepos. `38ad0f41` Name ticket 15 in the AiRepos parse rule. `22afd85f` Wire AiRepos from appsettings into the AI Actor.

## 1. Checklist (all met)

1. **AiRepos section** — Spec: `Name`, `Url`, optional `StartingRef`. [AiRepos.fs](../../../src/Server/AiRepos.fs) `fromConfig` binds those children. Omit or `""` → `None`. Name match is case-insensitive. [appsettings.json](../../../src/Server/appsettings.json) and [appsettings.Development.json](../../../src/Server/appsettings.Development.json) hold a `life` URL. No secrets.
2. **Resolve by name; no first-repo default** — Spec: "Default when `?ai` has no reponame token: **no repos** (`None`). Do not auto-attach the first `AiRepos` entry." `resolve` returns `None` when omitted or missing; named hit → `Some [RepoConfig]`.
3. **`?ai` token rules** — Spec rules 1–5: no tokens = default key + no repo; two or more = first keyname, second reponame; one `AiKeys` Name = keyname, no repo (also when that Name is on both lists); one `AiRepos` Name only = reponame + default key; neither = keyname (unknown → empty `ApiKey`). `AiAskArgs.fromText` on Command text in [RunAgentActor.fs](../../../src/Server/RunAgentActor.fs).
4. **AI Actor passes `RepoConfig list option`** — Spec: CloudAgents "only receives the resolved `RepoConfig list option`." Architecture: "Use injected `AiRepos` for `AgentRunner.start` repos (omit when no reponame; CloudAgents stays settings-blind)." [RouteRegistration.fs](../../../src/Server/RouteRegistration.fs) injects `AiRepos.fromConfig` with `AiKeys`. Core unchanged.
5. **No secrets; Console CLI-only** — Spec: "CloudAgents.Console stays CLI-only (`--repo` / env); it does not read appsettings." Diff for `src/CloudAgents` and `src/CloudAgents.Console` is empty. [Program.fs](../../../src/CloudAgents.Console/Program.fs) still uses `--repo` / env.

## 2. What to build (also met)

Bind/resolve parallel to `AiKeys.fromConfig`. Composition injects both lists; Core does not see repos. Placeholder JSON. Non-goals hold: no Console appsettings, no Grok/High/Fast, `AiKeys` JSON shape unchanged. `addAppSettings` load order is unchanged.

## 3. (a) Missing or partial

1. **None** — required AI Actor behavior from [16 — AiRepos from appsettings](../issues/16-airepos-from-appsettings.md) and the 2026-09-20 lock.

## 4. (b) Scope creep

1. **None** — plan notes record this ticket and add no extra runtime. `withHostKeysRepos` is the test seam. `AiKeys.tokensFromText` splits Command tokens for the new parse.

## 5. (c) Wrong implementation

1. **None** — Spec: "`?ai life` is a repo only when `life` is not also a key Name." Default `?ai` attaches no repo.

## 6. Count

0 findings.

## Summary

Standards: 5 findings (2 documented-standard hits, 3 smell judgements, 1 suppressed duplicate). Worst: [RunAgentActor.fs](../../../src/Server/RunAgentActor.fs) `actorFn` takes `keys` and `repos` ungrouped. Spec: 0 findings. Worst: none.
