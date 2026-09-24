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
