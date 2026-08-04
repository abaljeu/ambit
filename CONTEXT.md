# Gambol

Concise glossary for agent collaboration terms in this repo. Prefer these words; do not invent synonyms.

## Language

**Agent-done**:
A finished slice on a project branch: tests green, `/code-review` passed, and a commit on the current `w/*` branch. Tickets do not record commit SHAs.
_Avoid_: done, finished, shipped, complete

**Original branch**:
The human-owned long-lived line that is not main or master; the human squashes onto it.
_Avoid_: base branch, long-lived branch, integration branch, feature branch

**Project branch**:
The agent's default workplace branch, always prefixed `w/`. The agent may create or check out `w/<slug>` once from HEAD when not already on `w/`, then stays there.
_Avoid_: work branch, agent branch, workplace branch, scratch branch

**Git bookkeeping**:
The file `.scratch/<feature>/git.md` that records the current project branch, the original it was cut from, and short notes.
_Avoid_: branch notes, git status file, branch tracker

**Manual approval**:
A direct user request that authorizes a named git operation otherwise off-limits (for example checkout of another branch, merge, rebase, reset, clean, remotes, or writes to main/master).
_Avoid_: permission, override, allowlist exception

**Issue tracker**:
Local Markdown under `.scratch/` for specs and issues; see [[docs/agents/issue-tracker.md]]. Not GitHub or GitLab issues.
_Avoid_: backlog, GitHub issues, GitLab issues, tickets board
