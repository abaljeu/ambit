---
name: code-review
description: Review working-tree changes vs HEAD (or a user-named fixed point when given) along Standards and Spec axes. Spec comes from local plan/ paths. Use when the user wants to review a branch, work-in-progress changes, or asks to "review since X".
---

Two-axis review of a working-tree or tip diff:

- **Standards** — does the code conform to this repo's documented coding standards?
- **Spec** — does the code faithfully implement the originating issue / PRD / spec?

Both axes run as **parallel sub-agents** so they don't pollute each other's context, then this skill aggregates their findings.

Issue tracker: [[doc/agents/issue-tracker.md]] (local `plan/`). Git: [[.agents/skills/git-protocol/SKILL.md]].

When the diff touches F# (`*.fs` / `*.fsi`), follow [[.agents/skills/code-review-fsharp/SKILL.md]] before spawning sub-agents.

## Process

### 1. Pin the review range

**Default**: uncommitted changes vs `HEAD` (`git diff HEAD` and `git status`). Do not invent a base SHA.

If the tree is clean (everything committed), the tip under review is `HEAD`. When the user names an older fixed point (commit, branch, tag, `HEAD~N`), use that; otherwise do not ask for one just to bookkeep SHAs.

For an explicit fixed point: `git diff <fixed-point>...HEAD` (three-dot) and `git log <fixed-point>..HEAD --oneline`. Confirm the ref resolves and the diff is non-empty before spawning sub-agents.

### 2. Identify the spec source

Look for the originating spec, in this order:

1. A `plan/` path the user passed (preferred) — read it per [[doc/agents/issue-tracker.md]].
2. A PRD/spec under `plan/<feature>/` (or rarely `doc/` / `specs/`) matching the feature.
3. If nothing is found, ask the user for the local spec path. If they say there isn't one, the **Spec** sub-agent will skip and report "no spec available".

Do **not** harvest GitHub/GitLab issue numbers from commit messages as the spec source. No SHA bookkeeping on tickets.

### 3. Identify the standards sources

Live coding standards for this repo live under [[.agents/rules/]]: [[.agents/rules/fsharp-source.md]] (F#), [[.agents/rules/core-api.md]] (Core vs Adapter), [[.agents/rules/core-agent-behavior.md]] (surgical changes), and the other scoped rules. Do not hunt missing `CODING_STANDARDS.md` or `CONTRIBUTING.md`.

The Standards axis also always carries the smell baseline in [[SMELLS.md]].

### 4. Spawn both sub-agents in parallel

Send a single message with two `Agent` tool calls. Use the `general-purpose` subagent for both.

**Standards sub-agent prompt** — include:

- The full diff command (default `git diff HEAD`) and commit list if a fixed point was named.
- The list of standards-source files you found in step 3, **plus [[SMELLS.md]]**: include that file's contents in the prompt, or give the path and instruct the sub-agent to read it. The sub-agent has no other access to the smell baseline.
- The brief: "Report — per file/hunk where relevant — (a) every place the diff violates a documented standard: cite the standard (file + the rule); and (b) any baseline smell you spot: name it and quote the hunk. Distinguish hard violations from judgement calls per the included smell baseline. Under 400 words."

**Spec sub-agent prompt** — include:

- The same diff command (and commit list if any).
- The path or contents of the local spec.
- The brief: "Report: (a) requirements the spec asked for that are missing or partial; (b) behaviour in the diff that wasn't asked for (scope creep); (c) requirements that look implemented but where the implementation looks wrong. Quote the spec line for each finding. Under 400 words."

If the spec is missing, skip the Spec sub-agent and note this in the final report.

### 5. Aggregate

Present the two reports under `## Standards` and `## Spec` headings, verbatim or lightly cleaned. Keep the axes separate: do not merge or rerank findings, and do not pick a single winner across axes.

End with a one-line summary: total findings per axis, and the worst issue _within each axis_ (if any).
