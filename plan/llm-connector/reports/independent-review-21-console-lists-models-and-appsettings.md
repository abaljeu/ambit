# Independent re-review: 21 — Console lists models and fills CLI gaps from appsettings

Independent two-axis re-review after the must-fix. The reviewer did not write the implementation. Prior implementer self-review is not authority. Not approval. Ticket [21 — Console lists models and fills CLI gaps from appsettings](../issues/21-console-lists-models-and-appsettings.md) **Status:** stays `coded`. Do not land. Do not set `done`.

**Verdict: Good (with nits).** Ready for Alan accept to land.

**Must-fix: fixed.** [RunAgentActor.fs](../../../src/Server/RunAgentActor.fs) `askOptions.ModelHint` is `None`. That file is not in `origin/staging...HEAD`. Commit `a67106ab` reverted the Browser / `?ai` selection hunk. No Browser model-picker remains.

**Pin:** tip `a67106ab31810d1d410d84d2ef6c73e7d239fe38` (named `cursor/console-list-models-3a7e`; this report sits on `cursor/review-21-console-models-f034` at the same tree plus reports). User named `origin/staging`; three-dot `origin/staging...HEAD` is non-empty. merge-base / `origin/staging` `430d7fee2ec36f2b7dcde18b65f3f1c95b54eb7c`.

**Commits:** `a67106ab` 21: Revert RunAgentActor ModelHint to None. `cff8020f` 21: Console lists models and fills CLI gaps from appsettings. `c085726c` sectering models. `97792f39` selecting models.

**Spec:** [21 — Console lists models and fills CLI gaps from appsettings](../issues/21-console-lists-models-and-appsettings.md). Locked: Console `GET /v1/models` at start; create `model: { id }` (omit when `ModelHint` is `None`); CLI > `appsettings.<level>` > `CURSOR_API_KEY` for `ApiKey`; CloudAgents settings-blind; no Browser / `?ai` picker. Glossary [CONTEXT.md](../../../CONTEXT.md).

**Mechanical scan:** `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` (script invoked as `python3`; `python` is absent). Exit 1. Bindings stay under 40 lines. One printed hit: `tests/CloudAgents.Tests/CursorHttpJsonTests.fs:33 LONG (163)`.

**Proofs run:** `dotnet test tests/CloudAgents.Tests/Gambol.CloudAgents.Tests.fsproj --filter FullyQualifiedName~CursorHttpJsonTests|FullyQualifiedName~ConsoleConfigTests` — 9 passed.

Axis drafts: [Standards re-review](code-review-standards-21-console-lists-models-and-appsettings-rereview.md), [Spec re-review](code-review-spec-21-console-lists-models-and-appsettings-rereview.md). Axes stay separate below. Console catalog, create `{ id }`, CLI > file > env, gitignore, settings-blind CloudAgents, and `ModelHint = None` are met. Remaining items are nits.

## Standards

# Standards re-review: [21 — Console lists models and fills CLI gaps from appsettings](plan/llm-connector/issues/21-console-lists-models-and-appsettings.md)

Range `git diff origin/staging...HEAD`. HEAD `a67106ab` on `cursor/review-21-console-models-f034` (implement-tip tree). Commits `a67106ab`, `cff8020f`, `c085726c`, `97792f39`. [RunAgentActor.fs](src/Server/RunAgentActor.fs) stays `ModelHint = None` and is outside the diff.

## Documented standards

Scan bindings stay under 40 lines (`listModels` 27, `runAgent` 26). That meets the 40-line cap in [fsharp-source](.agents/rules/fsharp-source.md). The under-100-line preference is not a script fail ([core-agent-behavior](.agents/rules/core-agent-behavior.md)).

Hard:

1. **Line length 163.** [CursorHttpJsonTests.fs](tests/CloudAgents.Tests/CursorHttpJsonTests.fs) line 33, scan `LONG (163)`. [fsharp-source](.agents/rules/fsharp-source.md): 100 characters or less per line on source code. "This rule does not apply to tests" is the 800-line split. The line cap still applies.
2. **Exceptions.** [Config.fs](src/CloudAgents.Console/Config.fs) `tryLoad` calls `JsonDocument.Parse` and returns `Some`. A bad file throws from `loadFiles` / `main`. Same rule: use Error types. `listModels` and `parseModelCatalog` return `Error`, same as `createAgent`.
3. **One-word public names.** [Config.fs](src/CloudAgents.Console/Config.fs) `pick`, `overlay`, and `resolve` are public. `ConsoleConfig` has no `RequireQualifiedAccess`. Same rule: a public name is more than one word, or the call requires context.
4. **Reuse the existing record.** [Program.fs](src/CloudAgents.Console/Program.fs) `runAgent` and `waitForResult` take loose `apiKey` and build `{ RunnerConfig.ApiKey = apiKey }`. `startRepos` takes `repoUrl` and `startingRef` then builds `RepoConfig`. Same rule: reuse a named type that already exists.
5. **Extra report.** The range adds [cloud-agents-continue-conversation.md](plan/llm-connector/reports/cloud-agents-continue-conversation.md). [core-agent-behavior](.agents/rules/core-agent-behavior.md): every changed line traces to the request. This file is outside that work.
6. **Bare ids.** That report says `tickets 14–20`. [refer-by-name](.agents/rules/refer-by-name.md): name every issue. The scan misses the plural.
7. **Ask for AI.** That report says "each live Ask". [markdown-writing](.agents/rules/markdown-writing.md) uses [CONTEXT](CONTEXT.md) words. **AI** is the Actor. Ask is not that name.
8. **ready as delivery.** Ticket Context: `Ready wires ModelHint as a JSON string on create.` [planning-docs](.agents/rules/planning-docs.md): plan text records delivery by ticket, section, or Point.

No `mutable`, TAB, or file over 400 lines. [core-api](.agents/rules/core-api.md): Core stays untouched. [no-retrofit](.agents/rules/no-retrofit.md): old tickets stay.

## Baseline smells

**Mysterious Name.** `optString2` hides camel case or snake case `displayName`.

```
let private optString2 json camel snake =
```

**Speculative Generality.** `modelNodes` accepts `items`, then `models`, then a raw array. The ticket asks for item fields.

**Duplicated Code.** `CliArgs` and `FileSettings` repeat ApiKey, Model, Repo, Ref, and Name. `parseVariant` and `parseModel` share the `optString "id"` None/Some shape.

Suppressed: **Shotgun Surgery** (ticket spans Console, CursorHttp, gitignore, and plan). **Primitive Obsession** (ticket keeps `ModelHint` a string id).

## Summary

Hard 8. Judgement 3. Suppressed smells 2. Worst: `tryLoad` throws on a bad settings file.

## Spec

# Spec re-review — 21 — Console lists models and fills CLI gaps from appsettings

Range: `git diff origin/staging...HEAD`. Tip `a67106ab` on `cursor/review-21-console-models-f034`. Spec: [21 — Console lists models and fills CLI gaps from appsettings](plan/llm-connector/issues/21-console-lists-models-and-appsettings.md).

## Re-review focus

The must-fix holds on this tip. [RunAgentActor](src/Server/RunAgentActor.fs) binds `askOptions` as follows, and `toStartArgs` passes that value as `Options`:

```fsharp
let private askOptions =
    { AgentOptions.DisplayName = Some "AI"
      ModelHint = None }
```

The net diff has no Server file and no Browser file. [Browser](src/Client) has no model-selection UI and no `?ai` path that sets a model. Spec non-goal: "Browser / `?ai` model picker."

## (a) Missing or partial

None. Catalog `GET /v1/models` uses the same Basic auth as other CursorHttp calls. Parse covers `id`, `displayName`, `description`, `aliases`, `parameters`, and `variants`. Create JSON writes `model` as `{ "id": … }` and omits `model` when `ModelHint` is `None`. `AgentOptions.ModelHint` stays `string option`. Console resolve order is CLI, then file, then `CURSOR_API_KEY` for `ApiKey` only, with `--model` and `--api-key`. After the key is known, Console prints the catalog and then starts and waits. Tracked [appsettings.json](src/CloudAgents.Console/appsettings.json) has empty placeholders. `.gitignore` ignores Console `appsettings.Development.json` and `appsettings.Production.json`. [CloudAgents.Console README](src/CloudAgents.Console/README.md) documents usage. Proofs parse the catalog and the create JSON with no live HTTP. Prompt stays a CLI argument. CloudAgents does not read appsettings. Server `AiKeys` / `AiRepos` are unchanged. Optional `params` stay off the object: the spec keeps `ModelHint` as a string id, and the create proof requires an `id` object, omitted when none.

## (b) Scope creep

Catalog print. Spec: "After the key is known: list models and print `id` / displayName / variants." [printModels](src/CloudAgents.Console/Program.fs) also prints `description` and `aliases`.

## (c) Implemented but wrong

File search. Spec: "Load base `appsettings.json`, then overlay `appsettings.<level>.json`. Search the current directory, then the exe directory." [settingsDirectory](src/CloudAgents.Console/Config.fs) selects one directory, and only when that directory contains base `appsettings.json` (current directory, else the exe directory). [loadFiles](src/CloudAgents.Console/Config.fs) then reads both files from that one directory. A level file in the exe directory is unused when the current directory already has the base file. A level file in the current directory is unused when that directory has no base file.

## Summary

Standards: 8 documented-standard hits, 3 unsuppressed smells. Worst in Standards: [Config.fs](../../../src/CloudAgents.Console/Config.fs) `tryLoad` throws on a bad settings file. Spec: 2 findings (0 missing, 1 scope creep, 1 wrong). Worst in Spec: overlay search is one directory, not per file. No remaining must-fix. Ticket Status stays `coded`.
