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
