# Independent review: 20 — Gitignore Development appsettings

Independent two-axis review. The reviewer did not write the implementation. Not approval. Ticket [20 — Gitignore Development appsettings](../issues/20-gitignore-development-appsettings.md) **Status:** stays `coded`. Ticket 19 is the XML pack; this review is ticket 20.

**Verdict: Good with nits**

**Pin:** tip `b92053b76219b5408f9367d5258e7aa9d90c91ea` (named `cursor/gitignore-appsettings-development-d27a`). User named `origin/staging`; three-dot `origin/staging...HEAD` is non-empty. merge-base `48c7a99ccbaf9b6bc6f78744ae817d27dc5c1838`.

**Commits:** `b92053b7` Renumber gitignore ticket from 19 to 20 to avoid collision. `17e2e1b1` Stop tracking appsettings.Development.json so live AiKeys stay local. `8349a803` llm connection. `a00dabb6` changed files plan. `6e6a054f` / `6d8bb09d` / `53f1ffee` Merge branch 'staging' into dev. `5c98c9ba` misc.

**Spec:** [20 — Gitignore Development appsettings](../issues/20-gitignore-development-appsettings.md). Locked: Development gitignored like Production; `git rm --cached`; empty placeholders only in tracked `appsettings.json`; docs/load-order notes updated; no secrets in tracked files. Glossary [CONTEXT.md](../../../CONTEXT.md).

**Mechanical scan:** `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` (script invoked as `python3`; `python` is absent). Exit 1.

```
plan/llm-connector/issues/20-gitignore-development-appsettings.md:23  .agents/rules/refer-by-name.md  BARE_ID  Ticket 15
```

Axis drafts: [Standards](code-review-standards-20-gitignore-development-appsettings.md), [Spec](code-review-spec-20-gitignore-development-appsettings.md). Axes stay separate below. Locked gitignore, untrack, placeholders, and load-order notes are met. `addAppSettings` is unchanged. Findings are naming and extra files, not a missed secret.

## Standards

# Standards review: [20 — Gitignore Development appsettings](../issues/20-gitignore-development-appsettings.md)

Range: `git diff origin/staging...HEAD` at `b92053b76219b5408f9367d5258e7aa9d90c91ea` (merge-base `48c7a99ccbaf9b6bc6f78744ae817d27dc5c1838`). No `*.fs` / `*.fsi`. Scan exit 1.

## Documented standards

Hard violations:

1. **Bare id Ticket 15** — [20 — Gitignore Development appsettings](../issues/20-gitignore-development-appsettings.md) line 23: `Ticket 15 comments, architecture, and map Notes...`. Scan `BARE_ID`. Rule [.agents/rules/refer-by-name.md](../../../.agents/rules/refer-by-name.md): never refer by only the id; the name wraps the link. Write `[15 — AiKeys from appsettings](15-aikeys-from-appsettings.md)`.
2. **Bare id 17** — [llm-connector map](../map.md) Implementation item 2 (added line): `Blocked by 17.` Same rule. Write `[17 — CloudAgents Console stream](issues/17-cloudagents-console-stream.md)`.

No other documented-standard hits. [.gitignore](../../../.gitignore) lists `src/Server/appsettings.Development.json` next to Production. `git ls-files` tracks only [src/Server/appsettings.json](../../../src/Server/appsettings.json) (`ApiKey` `""`, public `life` URL). Development is absent and ignored. [.agents/rules/fsharp-source.md](../../../.agents/rules/fsharp-source.md) and [.agents/rules/core-api.md](../../../.agents/rules/core-api.md): no F# or Core hunks. Extra net files [.agents/skills/code-review/SKILL.md](../../../.agents/skills/code-review/SKILL.md) and [changed-files-directional-links.md](../../core-creation/reports/changed-files-directional-links.md): no scan hits; mermaid uses 20px font. [.agents/rules/project-stage.md](../../../.agents/rules/project-stage.md): Stage stays `done` for a follow-up, same as prior notes. [.agents/rules/no-retrofit.md](../../../.agents/rules/no-retrofit.md): the skill one-line is a process edit; [15 — AiKeys from appsettings](../issues/15-aikeys-from-appsettings.md) and [16 — AiRepos from appsettings](../issues/16-airepos-from-appsettings.md) comments change because this ticket asks.

## Baseline smells

Judgement call, suppressed: possible Shotgun Surgery (gitignore, untrack, and six plan files). [.agents/rules/planning-docs.md](../../../.agents/rules/planning-docs.md) and the ticket Docs section ask for those plan edits. Repo standard wins.

No Mysterious Name, Feature Envy, or Speculative Generality in the hunks. Repeated “gitignored like Production” sentences follow the plan pattern, not Duplicated Code.

## Summary

Standards: 2 hard (bare ids), 0 unsuppressed smells. Worst: **Bare id Ticket 15** on [20 — Gitignore Development appsettings](../issues/20-gitignore-development-appsettings.md).

## Spec

# Spec review: [20 — Gitignore Development appsettings](../issues/20-gitignore-development-appsettings.md)

Range: `git diff origin/staging...HEAD` (HEAD `b92053b76219b5408f9367d5258e7aa9d90c91ea`). Ticket commits: `17e2e1b1`, `b92053b7`. Spec **Status:** `coded` (unchanged).

## (a) Missing or partial

None.

`.gitignore` lists `src/Server/appsettings.Development.json` next to Production. `git ls-files` at HEAD has only `src/Server/appsettings.json`; the working tree has no Development file. Tracked `src/Server/appsettings.json` has `"ApiKey": ""` and the public `life` URL. [15 — AiKeys from appsettings](../issues/15-aikeys-from-appsettings.md) comments, [llm-connector architecture](../arch.md), [llm-connector map](../map.md) Notes, and [16 — AiRepos from appsettings](../issues/16-airepos-from-appsettings.md) load-order notes say Development is gitignored like Production and that base `appsettings.json` holds empty placeholders. `src/Server/Server.fs` is not in the range.

## (b) Behaviour not asked for

1. Unrelated Markdown-lists skill line — the range edits [.agents/skills/code-review/SKILL.md](../../../.agents/skills/code-review/SKILL.md) to add "Markdown lists." Spec What to build: "`.gitignore` lists `src/Server/appsettings.Development.json` next to Production"; "Remove the file from the git tree"; "Tracked `appsettings.json` keeps empty-placeholder `AiKeys` / `AiRepos` examples"; "Ticket 15 comments, architecture, and map Notes say Development is gitignored like Production."
2. Unrelated directional-links report — the range adds [changed-files-directional-links.md](../../core-creation/reports/changed-files-directional-links.md). Spec What to build does not name that file.

## (c) Implemented but wrong

None.

The deleted Development blob had empty `ApiKey`, empty Auth, local `postgres`/`postgres` DB string, and the public `life` URL. Tests still name `appsettings.Development.json`. Spec: "The local file may remain ignored."

## Summary

Spec findings: (a) 0, (b) 2, (c) 0. Worst in Spec: 1. Unrelated Markdown-lists skill line.

## Summary

Standards: 2 findings (2 documented-standard hits, 0 unsuppressed smells). Worst: [20 — Gitignore Development appsettings](../issues/20-gitignore-development-appsettings.md) line 23 says `Ticket 15` without the name. Spec: 2 findings (0 missing, 2 scope creep, 0 wrong). Worst: unrelated Markdown-lists line in [.agents/skills/code-review/SKILL.md](../../../.agents/skills/code-review/SKILL.md).
