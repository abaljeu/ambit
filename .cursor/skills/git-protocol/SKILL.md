---
name: git-protocol
description: "Gambol git procedure: three named places (dev, ready, master), commits on dev, merge --no-ff into ready. Use when committing, merging, branching, tagging, agent-done, implement, or any other git instruction."
---

# Git protocol

Canonical git procedure for this repo. Other rules and skills point here; they do not copy these steps.

## Places

Three long-lived branches. Reuse these names. Do not add `w/` branches.

**dev** — workplace on this machine. All Desktop edits and ordinary commits happen here. Local-only; do not push `dev`.

**ready** — integration. Sit on `ready` and `git merge --no-ff dev`. Bisect commit-by-commit here. This is “brought into ready.”

**master** — one commit per squashed merge from `ready`. Updating it is explicit invocation only, by the human: [[.cursor/skills/git-master/SKILL.md]].

A hotfix is born on the oldest place that must contain it, then merged toward `dev`.

## Commits

Ordinary commits on `dev` go through [[scripts/commit.sh]] `"<message>"` (Cursor manual approval) or the human runs `git commit` in the CLI. The script refuses when HEAD is not `dev`, stages `.`, and commits with the message.

## Merges

The Desktop agent does not run `git merge` or squash. Those moves go through [[scripts/merge.sh]] (Cursor manual approval) or the human types them in the CLI. `merge.sh ready [-m <msg>]` brings `dev` into `ready` (`--no-ff`); `merge.sh forward [master|ready] [-m <msg>]` brings a hotfix toward `dev`. The script refuses a dirty tree, and refuses a local `ready` that is behind `origin/ready`.

**agent-done** is tests green, `/code-review`, and a commit on `dev` via [[scripts/commit.sh]] `"<message>"` or human `git commit`. Then ask the human to run `merge.sh` (or type the merge) to put that work on `ready`.

## Bisect

The agent prepares a read-only recipe (log range and a red command). The human runs bisect. The agent does not check out bisect commits.

## Workplace

The three places exist. `dev` and `ready` were born together on the last work tip; `master` is an older ancestor of both. Edit only on `dev`.

`selective-client-sync` and the `w/` names remain as history. Do not resume one as the workplace. Do not create another.

The agent may create and switch places (`git branch`, `git switch`). Merges, squashes, and remotes stay with the human.

## Sharing

Remotes are explicit invocation only, by the human: [[.cursor/skills/git-share/SKILL.md]].
