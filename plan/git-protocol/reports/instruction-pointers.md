# Instruction pointers

Git procedure is only [[.cursor/skills/git-protocol/SKILL.md]]. Other live instructions now point there. No commit. HEAD stayed on `selective-client-sync`. No `w/` branch created.

## Files changed

Canonical skill (verified: [[scripts/merge.sh]] mapping and Bisect are present; script is not written yet):

- [[.cursor/skills/git-protocol/SKILL.md]]

Rules:

- [[.cursor/rules/environment.mdc]] — Git bullets replaced by a pointer; remotes still need **manual approval** unless Sharing (cloud) applies; read-only local git stays here as tooling
- [[.cursor/rules/gambol.mdc]] — lists git-protocol; project-work is `plan` files and Stage
- [[.cursor/rules/project-stage.mdc]] — project-work plus git-protocol pointer; no `w/` or `git.md`

Skills (pointer only, or pointer plus a named leftover):

- [[.cursor/skills/project-work/SKILL.md]] — rewritten; not a git protocol; no `git.md` requirement
- [[.cursor/skills/to-archive/SKILL.md]]
- [[.cursor/skills/projects-overview/SKILL.md]]
- [[.cursor/skills/update-matt-skills/SKILL.md]] — pointer plus exception
- [[.agents/skills/implement/SKILL.md]] — `w/*` start/finish removed; agent-done follows git-protocol
- [[.agents/skills/request-refactor-plan/SKILL.md]]
- [[.agents/skills/code-review/SKILL.md]]
- [[.agents/skills/diagnosing-bugs/SKILL.md]]
- [[.agents/skills/resolving-merge-conflicts/SKILL.md]]
- [[.agents/skills/git-guardrails-claude-code/SKILL.md]]
- [[.agents/skills/scaffold-exercises/SKILL.md]]

Glossary and decisions:

- [[CONTEXT.md]] — **Agent-done** is commit on `dev` then human [[scripts/merge.sh]]; **dev** / **ready** / **master** added; Original branch, Project branch, and Git bookkeeping retired; **Manual approval** does not forbid merge/`master` against the skill
- [[doc/Decisions/0001-agent-git-hygiene.md]] — superseded; body is a pointer
- [[doc/Decisions/0002-git-protocol.md]] — new; points at the skill

Project:

- [[plan/git-protocol/project.md]] — Stage `active`
- [[plan/index.md]] — Git protocol row

Unchanged (no git procedure, or not competing):

- [[AGENTS.md]], [[.cursor/codex-context.md]], [[.cursor/copilot-instructions.md]]
- [[.cursor/rules/core-agent-behavior.mdc]] — subagent `status.sh` only
- [[.cursor/skills/implement-fsharp-feature/SKILL.md]], [[.cursor/skills/prepare-agent-instruction-change/SKILL.md]]
- [[doc/agents/]] — no branch protocol

Not rewritten (per request): `plan/**/git.md` history, `plan/done/` forks, `src/`, [[scripts/merge.sh]] (absent).

## What became a pointer

Every live rule and skill that used to describe `w/*`, original branch, commit/merge/squash/push/pull, agent-done git steps, bisect policy, or remotes/allowlist now wikilinks [[.cursor/skills/git-protocol/SKILL.md]] (and terms in [[CONTEXT.md]]) instead of copying steps.

## Diverging use cases

These cannot be a pointer only. Each live skill has a pointer plus one leftover sentence. No new protocol was invented.

1. [[.cursor/skills/update-matt-skills/SKILL.md]] — still uses `vendor/mattpocock-skills`; merge script [[.cursor/skills/update-matt-skills/scripts/merge-to-live.sh]] still expects a clean `w/*` tip, then creates `update/mattpocock-skills` — **diverging; needs human**.
2. [[.agents/skills/git-guardrails-claude-code/SKILL.md]] — Claude Code hooks still block push, reset --hard, clean, and related commands — **diverging; needs human** (can block cloud push of `ready` and [[scripts/merge.sh]] if those run as git).
3. [[.agents/skills/scaffold-exercises/SKILL.md]] — leftover: course exercises still imply `git commit` after lint. Vendor copy [[skills/misc/scaffold-exercises/SKILL.md]] still has the old line; the next flatten can restore it.
4. [[.agents/skills/resolving-merge-conflicts/SKILL.md]] — leftover: while a merge or rebase is in progress, the human owns start/continue/abort/checkout; this skill applies file resolutions only.
5. [[plan/daily-git-save/project.md]] — application `commitAll` of DataDir (not agent git). `src/` not edited.
6. [[plan/done/update-matt-skills/forks/]] — forks of implement, request-refactor-plan, code-review, diagnosing-bugs, resolving-merge-conflicts still describe `w/*`. Not rewritten. The next flatten plus fork re-apply can restore a competing protocol — **diverging; needs human**.
7. `research/*` branches — not found in live skills.

## Board mutations (parent)

- `remove` — [[.cursor/skills/git-protocol/SKILL.md]] pointers task, after parent verifies this report
- `add` (optional Pending) — align update-matt-skills merge script and done forks with git-protocol (`dev` / `ready`, no `w/*`)
