---
name: git-protocol
description: "Gambol git procedure: four named places (dev, ready, master, staging), commits, merge only when Alan asks, merge --no-ff into ready. First git step is scripts/gitstatus.sh. Wrong workplace: abort and tell Alan. Do not research git history unless Alan asked. Use when committing, merging, branching, tagging, implement, or any other git instruction."
---

# Git protocol

Canonical git procedure for this repo. Other rules and skills point here; they do not copy these steps.

## Status

1. From the project root, run `scripts/gitstatus.sh` with no arguments. Do not write `bash` in front of the command. This is the first git step. Done when the script printed the current branch and short status.
2. Read that output. Proceed only when the workplace is right for this run. On Desktop, a right workplace has HEAD `dev`, and the tree is not dirty in a way that blocks this work. If the workplace is wrong, abort. Tell Alan what the script showed. Stop the task. Done when the workplace is right, or when you stopped and told Alan.
3. Extra git after a right workplace is only for a current-task fact that `scripts/gitstatus.sh` did not print (paths for [[scripts/commit.sh]], a working-tree diff of files this task will change). Historical research is opt-in: run `git log`, merge-base, ancestor checks, `cat-file`, `git show` of old SHAs, or reconstruct history only when Alan asked for that research. Done when you proceed from the status output with extra git only for the current task, or you abort.

## Places

Three Desktop places. Reuse these names. Do not add `w/` branches. Do not write per-project git notes. All Desktop work on **dev**; promote to **ready**. Cloud agents work on a disposable branch and send to **staging**: [[.agents/skills/cloud-agent-git/SKILL.md]].

**dev** — workplace on this machine. All Desktop edits and ordinary commits happen here. Local-only; do not push `dev`.

**ready** — integration. Sit on `ready` and `git merge --no-ff dev`. Bisect commit-by-commit here. This is “brought into ready.”

**master** — one commit per squashed merge from `ready`. Updating it is explicit invocation only, by the human: [[.agents/skills/git-master/SKILL.md]].

A hotfix is born on the oldest place that must contain it, then merged toward `dev`.

## Commits

Desktop commits on `dev` only when Alan asks to commit. Then run [[scripts/commit.sh]] `"<message>"` `[files...]` or the human runs `git commit` in the CLI. The script refuses when HEAD is not `dev`, stages `.` (or only the given files), and commits with the message. A tool approval card is not that ask. Done when Alan asked and that commit exists, or when Alan did not ask and no commit ran.

Do not commit on `dev`, `ready`, `staging`, or `master` unless Alan asked. Cloud agents commit freely on a disposable local branch: [[.agents/skills/cloud-agent-git/SKILL.md]].

Commit AFTER writing any report files, not before.

## Merges

The Desktop agent does not run `git merge` or squash. Those moves go through [[scripts/gitready.sh]], [[scripts/gitmaster.sh]], and [[scripts/gitdev.sh]] or the human types them in the CLI. Run a merge script only when Alan asked. Land downloaded `staging` into `dev` is [[.agents/skills/cloud-agent-git/SKILL.md]]. `gitready.sh` with no argument lists dev commits not on `ready`. `gitmaster.sh` with no argument lists `ready` commits not on `master`. `gitready.sh "<msg>"` brings `dev` into `ready` (`--no-ff`); `gitdev.sh` brings a hotfix from `master` toward `dev` with a stock forward message. The merge scripts refuse a dirty tree, and refuse a local `ready` that is behind `origin/ready`.



## Bisect

The agent prepares a read-only recipe (log range and a red command). The human runs bisect. The agent does not check out bisect commits.

## Workplace

The three Desktop places exist. `dev` and `ready` were born together on the last work tip; `master` is an older ancestor of both. Desktop: edit only on `dev`.

`selective-client-sync` and the `w/` names remain as history. Do not resume one as the workplace. Do not create another.

The agent may create and switch places (`git branch`, `git switch`). Merges go through the scripts (or human CLI), except `staging` → `dev` ([[.agents/skills/cloud-agent-git/SKILL.md]]). Squashes stay with the human. Pulling `ready` is agent-ok; pushing `ready` is agent-ok only with Alan's push approval ([[.agents/skills/git-share/SKILL.md]]).

## Sharing

Agents may pull `ready` freely. Pushing `ready` is approval-gated. Procedure: [[.agents/skills/git-share/SKILL.md]].
`dev` stays local. `master` stays human-only: [[.agents/skills/git-master/SKILL.md]]. Cloud-agent `staging`: [[.agents/skills/cloud-agent-git/SKILL.md]].
