---
name: git-protocol
description: "Gambol git procedure: three named places (dev, ready, master), commits on dev, merge --no-ff into ready. First git step is scripts/gitstatus.sh. Use when committing, merging, branching, tagging, agent-done, implement, or any other git instruction."
---

# Git protocol

Canonical git procedure for this repo. Other rules and skills point here; they do not copy these steps.

## Status

At the outset, from the project root, run `scripts/gitstatus.sh` (NOT `bash scripts/gitstatus.sh`.  Never explicitly invoke bash.) with no arguments. This is the first git step. Use extra git commands only when that output is not enough.

## Places

Three Desktop places. Reuse these names. Do not add `w/` branches. Do not write per-project git notes. All Desktop work on **dev**; promote to **ready**. Cloud agents work on a disposable branch and send to **staging**: [[.agents/skills/cloud-agent-git/SKILL.md]].

**dev** — workplace on this machine. All Desktop edits and ordinary commits happen here. Local-only; do not push `dev`.

**ready** — integration. Sit on `ready` and `git merge --no-ff dev`. Bisect commit-by-commit here. This is “brought into ready.”

**master** — one commit per squashed merge from `ready`. Updating it is explicit invocation only, by the human: [[.agents/skills/git-master/SKILL.md]].

A hotfix is born on the oldest place that must contain it, then merged toward `dev`.

## Commits

Ordinary commits on `dev` go through [[scripts/commit.sh]] `"<message>"` `[files...]` (Cursor manual approval) or the human runs `git commit` in the CLI. The script refuses when HEAD is not `dev`, stages `.` (or only the given files), and commits with the message.
Commit AFTER writing any report files, not before.

## Merges

The Desktop agent does not run `git merge` or squash. Those moves go through [[scripts/gitready.sh]], [[scripts/gitmaster.sh]], and [[scripts/gitdev.sh]] (Cursor manual approval) or the human types them in the CLI. Land downloaded `staging` into `dev` is [[.agents/skills/cloud-agent-git/SKILL.md]]. `gitready.sh` with no argument lists dev commits not on `ready`. `gitmaster.sh` with no argument lists `ready` commits not on `master`. `gitready.sh "<msg>"` brings `dev` into `ready` (`--no-ff`); `gitdev.sh` brings a hotfix from `master` toward `dev` with a stock forward message. The merge scripts refuse a dirty tree, and refuse a local `ready` that is behind `origin/ready`.

**agent-done** is tests green, `/code-review`, and a commit on `dev` via [[scripts/commit.sh]] `"<message>"` or human `git commit`. Then ask the human to run `gitready.sh` (or type the merge) to put that work on `ready`.

## Bisect

The agent prepares a read-only recipe (log range and a red command). The human runs bisect. The agent does not check out bisect commits.

## Workplace

The three Desktop places exist. `dev` and `ready` were born together on the last work tip; `master` is an older ancestor of both. Desktop: edit only on `dev`.

`selective-client-sync` and the `w/` names remain as history. Do not resume one as the workplace. Do not create another.

The agent may create and switch places (`git branch`, `git switch`). Merges go through the scripts (or human CLI), except `staging` → `dev` ([[.agents/skills/cloud-agent-git/SKILL.md]]). Squashes stay with the human. Pulling `ready` is agent-ok; pushing `ready` is agent-ok only with Alan's push approval ([[.agents/skills/git-share/SKILL.md]]).

## Sharing

Agents may pull `ready` freely. Pushing `ready` is approval-gated. Procedure: [[.agents/skills/git-share/SKILL.md]].
`dev` stays local. `master` stays human-only: [[.agents/skills/git-master/SKILL.md]]. Cloud-agent `staging`: [[.agents/skills/cloud-agent-git/SKILL.md]].
