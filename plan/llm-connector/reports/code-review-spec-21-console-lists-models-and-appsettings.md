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
