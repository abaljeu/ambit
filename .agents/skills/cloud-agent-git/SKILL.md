---
name: cloud-agent-git
description: >-
  Staging: cloud agents work on a disposable branch (not master, not ready,
  not staging) and open a draft PR toward staging for review. Publish to
  GitHub ready, master, or staging only after Alan says Good. Use when a
  cloud agent does git, or when downloading staging.
---

# Cloud-agent git

Cloud agents follow Work. Desktop git stays [[.agents/skills/git-protocol/SKILL.md]].

Toolchain and Postgres connection strings: [[.agents/rules/environment.md]] (Cursor Cloud Agents).

If this run **downloads staging**, follow [[LAND.md]] only: pull `origin/staging` onto local `staging`. `dev` never pulls from cloud.

**staging** is a published long-lived branch on `origin`. It is the drop for finished cloud-agent work after Alan says **Good**. It is not a workplace. Desktop holds a local `staging` that tracks `origin/staging`.

The CloudAgents library still returns the vendor branch and PR URL. A draft PR toward `staging` is the review vehicle, not the drop. Publishing onto GitHub `ready`, `master`, or `staging` is this protocol's **Good** gate. This skill is the authority for that gate; [[.agents/skills/git-protocol/SKILL.md]] and [[LAND.md]] point here.

## Work

First git step: [[scripts/gitstatus.sh]] per [[.agents/skills/git-protocol/SKILL.md]].

### 1. Disposable branch

Create or switch to a disposable branch. Base it on `origin/ready` (same start as a disposable workspace in [[.agents/skills/git-share/SKILL.md]]). If HEAD is `ready` or `master`, create the disposable branch from this tip and switch to it before the first commit.

Work and commit on that disposable branch. Git is free on a disposable or `cursor/*` branch, and on other local work that is not a publish to GitHub `ready`, `master`, or `staging`.

Done when `git branch --show-current` is not `dev`, `ready`, `master`, or `staging`.

### 2. Commit

Before you commit code changes, apply the Build / test toolchain gate in [[.agents/rules/core-agent-behavior.md]].
Commit on the disposable branch with `git commit`. [[scripts/commit.sh]] stays on `dev`.

Done when this run's files are committed on that branch (`git status --short` is empty for them).

### 3. Send to staging

Open a draft PR from the disposable branch toward `staging` for review. The disposable remote branch and that PR are not a publish. Local `origin/*` worktrees are also outside this gate.

Do not push or merge onto GitHub `ready`, `master`, or `staging` until Alan says **Good** (exact gate word). After **Good**, send to staging: fetch `origin`, merge the disposable branch into `staging`. If `origin/staging` is missing, create `staging` from the disposable branch. Push `origin staging`. [[scripts/gitpush.sh]] does not take `staging`. Conflicts: [[.agents/skills/resolving-merge-conflicts/SKILL.md]].

The first **Good** land of a ticket onto GitHub `staging` is one commit. Squash the disposable branch when it has more than one. After that approved land, later revision follow-ups on the same ticket may add extra commits.

Off-ticket items (review reports, small docs, rule tweaks that are not on a ticket) do not publish as their own staging lands. Pool them into one shared land, or fold them into a related ticket's single first-land commit.

Done when Alan has said **Good** and `git fetch origin` plus `git merge-base --is-ancestor HEAD origin/staging` succeed for this run's published tip — or, before **Good**, when the draft PR toward `staging` is open and GitHub `ready`, `master`, and `staging` were not pushed or merged.
