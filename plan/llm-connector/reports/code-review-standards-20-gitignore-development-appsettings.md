# Standards review: [20 — Gitignore Development appsettings](../issues/20-gitignore-development-appsettings.md)

Range: `git diff origin/staging...HEAD` at `b92053b76219b5408f9367d5258e7aa9d90c91ea` (merge-base `48c7a99ccbaf9b6bc6f78744ae817d27dc5c1838`). No `*.fs` / `*.fsi`. Scan exit 1.

## Documented standards

Hard violations:

1. **Bare id Ticket 15** — [20 — Gitignore Development appsettings](../issues/20-gitignore-development-appsettings.md) line 23: `Ticket 15 comments, architecture, and map Notes...`. Scan `BARE_ID`. Rule [.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md): never refer by only the id; the name wraps the link. Write `[15 — AiKeys from appsettings](15-aikeys-from-appsettings.md)`.
2. **Bare id 17** — [llm-connector map](../map.md) Implementation item 2 (added line): `Blocked by 17.` Same rule. Write `[17 — CloudAgents Console stream](issues/17-cloudagents-console-stream.md)`.

No other documented-standard hits. [.gitignore](.gitignore) lists `src/Server/appsettings.Development.json` next to Production. `git ls-files` tracks only [src/Server/appsettings.json](src/Server/appsettings.json) (`ApiKey` `""`, public `life` URL). Development is absent and ignored. [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) and [.agents/rules/core-api.md](.agents/rules/core-api.md): no F# or Core hunks. Extra net files [.agents/skills/code-review/SKILL.md](.agents/skills/code-review/SKILL.md) and [changed-files-directional-links.md](../../core-creation/reports/changed-files-directional-links.md): no scan hits; mermaid uses 20px font. [.agents/rules/project-stage.md](.agents/rules/project-stage.md): Stage stays `done` for a follow-up, same as prior notes. [.agents/rules/no-retrofit.md](.agents/rules/no-retrofit.md): the skill one-line is a process edit; [15 — AiKeys from appsettings](../issues/15-aikeys-from-appsettings.md) and [16 — AiRepos from appsettings](../issues/16-airepos-from-appsettings.md) comments change because this ticket asks.

## Baseline smells

Judgement call, suppressed: possible Shotgun Surgery (gitignore, untrack, and six plan files). [.agents/rules/planning-docs.md](.agents/rules/planning-docs.md) and the ticket Docs section ask for those plan edits. Repo standard wins.

No Mysterious Name, Feature Envy, or Speculative Generality in the hunks. Repeated “gitignored like Production” sentences follow the plan pattern, not Duplicated Code.

## Summary

Standards: 2 hard (bare ids), 0 unsuppressed smells. Worst: **Bare id Ticket 15** on [20 — Gitignore Development appsettings](../issues/20-gitignore-development-appsettings.md).
