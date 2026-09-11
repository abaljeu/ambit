---
name: cloud-agent-git
description: >-
  Staging: cloud agents work on a disposable branch (not master, not ready)
  and send done work to staging. Use when a cloud agent does git, or when
  downloading staging.
---

# Cloud-agent git

Cloud agents follow Work. Desktop git stays [[.agents/skills/git-protocol/SKILL.md]].

If this run **downloads staging**, follow [[LAND.md]] only: pull `origin/staging` onto local `staging`. `dev` never pulls from cloud.

**staging** is a published long-lived branch on `origin`. It is the drop for finished cloud-agent work. It is not a workplace. Desktop holds a local `staging` that tracks `origin/staging`.

The CloudAgents library still returns the vendor branch and PR URL. That is not the drop. Send to `staging` is this protocol.

## Work

First git step: [[scripts/gitstatus.sh]] per [[.agents/skills/git-protocol/SKILL.md]].

### 1. Disposable branch

Create or switch to a disposable branch. Base it on `origin/ready` (same start as a disposable workspace in [[.agents/skills/git-share/SKILL.md]]). If HEAD is `ready` or `master`, create the disposable branch from this tip and switch to it before the first commit.

Work on that disposable branch.

Done when `git branch --show-current` is not `dev`, `ready`, `master`, or `staging`.

### 2. Commit

Commit on the disposable branch with `git commit`. [[scripts/commit.sh]] stays on `dev`.

Done when this run's files are committed on that branch (`git status --short` is empty for them).

### 3. Send to staging

Fetch `origin`. Merge the disposable branch into `staging` (`--no-ff`). If `origin/staging` is missing, create `staging` from the disposable branch. Push `origin staging`. [[scripts/gitpush.sh]] does not take `staging`. Conflicts: [[.agents/skills/resolving-merge-conflicts/SKILL.md]].

Done when `git fetch origin` and `git merge-base --is-ancestor HEAD origin/staging` succeed for this run's tip.
