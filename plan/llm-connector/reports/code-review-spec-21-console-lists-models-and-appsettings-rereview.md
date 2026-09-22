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
