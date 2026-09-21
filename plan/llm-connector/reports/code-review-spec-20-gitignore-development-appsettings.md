# Spec review: [20 — Gitignore Development appsettings](plan/llm-connector/issues/20-gitignore-development-appsettings.md)

Range: `git diff origin/staging...HEAD` (HEAD `b92053b76219b5408f9367d5258e7aa9d90c91ea`). Ticket commits: `17e2e1b1`, `b92053b7`. Spec **Status:** `coded` (unchanged).

## (a) Missing or partial

None.

`.gitignore` lists `src/Server/appsettings.Development.json` next to Production. `git ls-files` at HEAD has only `src/Server/appsettings.json`; the working tree has no Development file. Tracked `src/Server/appsettings.json` has `"ApiKey": ""` and the public `life` URL. [15 — AiKeys from appsettings](plan/llm-connector/issues/15-aikeys-from-appsettings.md) comments, [llm-connector architecture](plan/llm-connector/arch.md), [llm-connector map](plan/llm-connector/map.md) Notes, and [16 — AiRepos from appsettings](plan/llm-connector/issues/16-airepos-from-appsettings.md) load-order notes say Development is gitignored like Production and that base `appsettings.json` holds empty placeholders. `src/Server/Server.fs` is not in the range.

## (b) Behaviour not asked for

1. Unrelated Markdown-lists skill line — the range edits [.agents/skills/code-review/SKILL.md](.agents/skills/code-review/SKILL.md) to add "Markdown lists." Spec What to build: "`.gitignore` lists `src/Server/appsettings.Development.json` next to Production"; "Remove the file from the git tree"; "Tracked `appsettings.json` keeps empty-placeholder `AiKeys` / `AiRepos` examples"; "Ticket 15 comments, architecture, and map Notes say Development is gitignored like Production."
2. Unrelated directional-links report — the range adds [changed-files-directional-links.md](plan/core-creation/reports/changed-files-directional-links.md). Spec What to build does not name that file.

## (c) Implemented but wrong

None.

The deleted Development blob had empty `ApiKey`, empty Auth, local `postgres`/`postgres` DB string, and the public `life` URL. Tests still name `appsettings.Development.json`. Spec: "The local file may remain ignored."

## Summary

Spec findings: (a) 0, (b) 2, (c) 0. Worst in Spec: 1. Unrelated Markdown-lists skill line.
