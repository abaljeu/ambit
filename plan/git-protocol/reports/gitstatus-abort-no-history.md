# Gitstatus abort, no history

Date: 2026-09-18. Instruction change on [git protocol](.agents/skills/git-protocol/SKILL.md). [Git protocol](plan/git-protocol/project.md) Stage stayed `done`. No commit.

## 1. Goal

First git step is [gitstatus.sh](scripts/gitstatus.sh). If the workplace is wrong, abort and tell Alan. Do not run git history research unless Alan asked for that research.

## 2. Canonical Status

Normative text is [git protocol](.agents/skills/git-protocol/SKILL.md) Status.

1. **Run [gitstatus.sh](scripts/gitstatus.sh)** from the project root, no arguments, no `bash` prefix.
2. **Proceed only when the workplace is right.** On Desktop that means HEAD `dev`, and a tree that is not dirty in a way that blocks this work. Wrong workplace: abort, tell Alan what the script showed, stop.
3. **Extra git is current-task only.** Paths for [commit.sh](scripts/commit.sh), working-tree diff of files this task will change. Historical research (`git log`, merge-base, ancestor checks, `cat-file`, `git show` of old SHAs, reconstruct history) is opt-in: only when Alan asked.

The old line “use extra git when that output is not enough” is gone. That phrase let agents treat a surprising [gitstatus.sh](scripts/gitstatus.sh) line as a reason to walk log, SHAs, and ancestors.

## 3. Pointers

These files point at Status. They do not copy the abort rule.

- [core-agent-behavior](.agents/rules/core-agent-behavior.md) Multitasking / SubAgent Delegation: startup follows [git protocol](.agents/skills/git-protocol/SKILL.md) Status. Removed “run extra Git when that output is not enough.”
- [cloud-agent git](.agents/skills/cloud-agent-git/SKILL.md) Work: Git is [git protocol](.agents/skills/git-protocol/SKILL.md) Status.
- [environment](.agents/rules/environment.md) Git: still follows [git protocol](.agents/skills/git-protocol/SKILL.md). Removed “read-only local git (`log`, `rev-parse`, …) is always fine,” which contradicted the abort rule.
- [AGENTS.md](AGENTS.md) and the Codex / Copilot bridges already pointed and did not copy git procedure. No edit.

## 4. Left in place

Three Desktop places (`dev`, `ready`, `master`), [commit.sh](scripts/commit.sh), no Desktop merge, [cloud-agent git](.agents/skills/cloud-agent-git/SKILL.md) disposable branch and staging publish, [git share](.agents/skills/git-share/SKILL.md), [git master](.agents/skills/git-master/SKILL.md).

Later-skill git that is not Status archaeology stays: [cloud-agent git](.agents/skills/cloud-agent-git/SKILL.md) publish Done uses merge-base after approval; [code-review](.agents/skills/code-review/SKILL.md) uses log when Alan names a fixed point.

Old tickets and reports under [git protocol](plan/git-protocol/project.md) were not rewritten ([no-retrofit](.agents/rules/no-retrofit.md)).

## 5. Next step

No commit unless Alan asks. Later agents start with [gitstatus.sh](scripts/gitstatus.sh); a wrong workplace stops the run instead of opening git history.
