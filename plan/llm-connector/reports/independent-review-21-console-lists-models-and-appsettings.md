# Independent review: 21 — Console lists models and fills CLI gaps from appsettings

Independent two-axis review. The reviewer did not write the implementation. Implementer self-review is not authority. Not approval. Ticket [21 — Console lists models and fills CLI gaps from appsettings](../issues/21-console-lists-models-and-appsettings.md) **Status:** stays `coded`.

**Verdict: Needs changes**

**Must-fix**

1. Revert [RunAgentActor.fs](../../../src/Server/RunAgentActor.fs) `askOptions.ModelHint` to `None`. The range sets `Some "cursor-grok-4.7-high"`. Ticket Context: "This is not a Browser / `?ai` model-selection UI." Non-goals: "Browser / `?ai` model picker." That hunk is Server `?ai` model selection. It is not Console catalog or CLI > file > env.

**Pin:** tip `cff8020f9c0e232509613735b82a67bc05625741` (named `cursor/console-list-models-3a7e`). User named `origin/staging`; three-dot `origin/staging...HEAD` is non-empty. merge-base `430d7fee2ec36f2b7dcde18b65f3f1c95b54eb7c`.

**Commits:** `cff8020f` 21: Console lists models and fills CLI gaps from appsettings. `c085726c` sectering models. `97792f39` selecting models.

**Spec:** [21 — Console lists models and fills CLI gaps from appsettings](../issues/21-console-lists-models-and-appsettings.md). Locked: Console `GET /v1/models` at start; create `model: { id }` (omit when `ModelHint` is `None`); CLI > `appsettings.<level>` > `CURSOR_API_KEY` for `ApiKey`; CloudAgents settings-blind; no Browser / `?ai` picker. Glossary [CONTEXT.md](../../../CONTEXT.md).

**Mechanical scan:** `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` (script invoked as `python3`; `python` is absent). Exit 1. Bindings stay under 40 lines. One printed hit: `tests/CloudAgents.Tests/CursorHttpJsonTests.fs:33 LONG (163)`.

**Proofs run:** `dotnet test tests/CloudAgents.Tests/Gambol.CloudAgents.Tests.fsproj --filter FullyQualifiedName~CursorHttpJsonTests|FullyQualifiedName~ConsoleConfigTests` — 9 passed.

Axis drafts: [Standards](code-review-standards-21-console-lists-models-and-appsettings.md), [Spec](code-review-spec-21-console-lists-models-and-appsettings.md). Axes stay separate below. Console catalog, create `{ id }`, CLI > file > env, gitignore, and settings-blind CloudAgents are met. The Server `ModelHint` hunk blocks accept.

## Standards

# Standards review: [21 — Console lists models and fills CLI gaps from appsettings](../issues/21-console-lists-models-and-appsettings.md)

Range: `git diff origin/staging...HEAD` at `cff8020f9c0e232509613735b82a67bc05625741` (merge-base `430d7fee2ec36f2b7dcde18b65f3f1c95b54eb7c`). Scan exit 1. Bindings stay under 40 lines (`listModels` 27, `runAgent` 26). Surgical under-100-line preference is not a script fail ([.agents/rules/core-agent-behavior.md](.agents/rules/core-agent-behavior.md)).

## Documented standards

Hard violations:

1. **LONG 163** — [CursorHttpJsonTests.fs](tests/CloudAgents.Tests/CursorHttpJsonTests.fs) line 33. Scan `LONG (163)`. Rule [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md): 100 characters or less per line. The test file-split exemption does not drop the line cap.
2. **Exceptions** — Same rule: do not use Exceptions; use Error types. [Config.fs](src/CloudAgents.Console/Config.fs) `tryLoad` calls `JsonDocument.Parse` with no Error type; a bad file throws out of `loadFiles` / `main`.
3. **One-word public names** — Same rule: public names must be more than one word, or require context. [Config.fs](src/CloudAgents.Console/Config.fs) `pick`, `overlay`, and `resolve` are public; the module has no `RequireQualifiedAccess`.
4. **Grouped parameters** — Same rule: group related parameters into a named reused type. [Program.fs](src/CloudAgents.Console/Program.fs) `runAgent` and `waitForResult` take loose `apiKey` and rebuild `{ RunnerConfig.ApiKey = apiKey }` (the old `main` held one `config`). `startRepos` takes `repoUrl` and `startingRef` then builds `RepoConfig`.
5. **Surgical extras** — [.agents/rules/core-agent-behavior.md](.agents/rules/core-agent-behavior.md): touch only what you must; no features beyond the ask. [RunAgentActor.fs](src/Server/RunAgentActor.fs) sets `ModelHint = Some "cursor-grok-4.7-high"` (was `None`). That is `?ai` model selection, a non-goal. [cloud-agents-continue-conversation.md](cloud-agents-continue-conversation.md) is not this ticket. That report also says `tickets 14–20` (bare ids, [.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md)) and names **Ask** for AI ([.agents/rules/markdown-writing.md](.agents/rules/markdown-writing.md) / [CONTEXT.md](CONTEXT.md) **AI**).
6. **ready as delivery** — [21 — Console lists models and fills CLI gaps from appsettings](../issues/21-console-lists-models-and-appsettings.md) Context: `Ready wires ModelHint as a JSON string on create.` [.agents/rules/planning-docs.md](.agents/rules/planning-docs.md): do not discuss `ready` as delivery status in plan docs.

No `mutable`. No TAB. No scan `BARE_ID` / `BLANK_BLANK`. File sizes stay under 400. [CursorHttp.fs](src/CloudAgents/Internal/CursorHttp.fs) `listModels` `try/with` matches existing `createAgent`. [.agents/rules/core-api.md](.agents/rules/core-api.md): Core is untouched. [.agents/rules/project-stage.md](.agents/rules/project-stage.md): Stage stays `done`. [.agents/rules/no-retrofit.md](.agents/rules/no-retrofit.md): old tickets stay.

## Baseline smells

**Mysterious Name.** `optString2` does not say it reads camel or snake `displayName`:

```
let private optString2 json camel snake =
```

**Speculative Generality.** `modelNodes` accepts `items`, then `models`, then a raw array. The ticket asked to parse items.

**Duplicated Code.** `CliArgs` and `FileSettings` share ApiKey/Model/Repo/Ref/Name. `parseVariant` and `parseModel` share the `optString "id"` None/Some shape.

**Shotgun Surgery** (suppressed). The ticket asks Console, CursorHttp, gitignore, and plan. Repo standard wins.

**Primitive Obsession** (suppressed). `ModelHint` stays a string id per the ticket.

## Summary

Standards: 6 hard, 3 unsuppressed smells. Worst: **LONG 163** on [CursorHttpJsonTests.fs](tests/CloudAgents.Tests/CursorHttpJsonTests.fs) line 33, and `tryLoad` throws.

## Spec

# Spec review: [21 — Console lists models and fills CLI gaps from appsettings](../issues/21-console-lists-models-and-appsettings.md)

Range: `git diff origin/staging...HEAD` (HEAD `cff8020f9c0e232509613735b82a67bc05625741`). Ticket commits: `97792f39`, `c085726c`, `cff8020f`. Spec **Status:** `coded` (unchanged).

## (a) Missing or partial

None.

`CursorHttp.listModels` does `GET /v1/models` with the same Basic `createClient` as other CursorHttp calls. The range parses catalog fields, encodes create `model` as `{ "id": … }`, omits `model` when `ModelHint` is `None`, resolves CLI then file then `CURSOR_API_KEY` for `ApiKey` only, adds `--model` and `--api-key`, prints the catalog then waits, tracks empty placeholders, gitignores Console Development and Production, and documents usage in [CloudAgents.Console README](src/CloudAgents.Console/README.md). `AgentOptions.ModelHint` stays a string id. CloudAgents start still receives `RunnerConfig.ApiKey` and `AgentOptions`. There is no Browser picker, no `AiKeys` / `AiRepos` edit, and no [17 — CloudAgents Console stream](../issues/17-cloudagents-console-stream.md) stream code. Proofs parse the catalog with no live HTTP and check create JSON.

## (b) Behaviour not asked for

1. Unrelated continue-conversation report — the range adds [cloud-agents-continue-conversation.md](cloud-agents-continue-conversation.md). Spec What to build does not name that file.
2. Server AI Actor model id — [RunAgentActor.fs](src/Server/RunAgentActor.fs) sets `ModelHint = Some "cursor-grok-4.7-high"`. Spec Context: "This is not a Browser / `?ai` model-selection UI." Non-goals: "Browser / `?ai` model picker."

## (c) Implemented but wrong

1. Overlay search is not per file — `ConsoleConfig.settingsDirectory` picks one directory that contains `appsettings.json`, then loads `appsettings.<level>.json` only from that directory. Spec JSON shape: "Load base `appsettings.json`, then overlay `appsettings.<level>.json`. Search the current directory, then the exe directory." When cwd has no base file, a cwd overlay is ignored. README `dotnet run --project` from repo root uses the exe copy of empty placeholders and does not read gitignored `src/CloudAgents.Console/appsettings.<level>.json`.

## Summary

Spec findings: (a) 0, (b) 2, (c) 1. Worst in Spec: 2. Server AI Actor model id.

## Summary

Standards: 6 findings (6 documented-standard hits, 3 unsuppressed smells). Worst in Standards: [CursorHttpJsonTests.fs](../../../tests/CloudAgents.Tests/CursorHttpJsonTests.fs) line 33 is 163 characters, and `tryLoad` throws. Spec: 3 findings (0 missing, 2 scope creep, 1 wrong). Worst in Spec: Server `askOptions.ModelHint` hardcodes a Browser / `?ai` model.

Must-fix that blocks accept: the [RunAgentActor.fs](../../../src/Server/RunAgentActor.fs) `ModelHint` change. Other items are nits. Ticket Status stays `coded`.
