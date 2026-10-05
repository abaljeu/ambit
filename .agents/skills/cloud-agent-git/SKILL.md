---
name: cloud-agent-git
description: >-
  Staging: cloud agents work on a disposable branch (not dev, not master, not ready,
  not staging) and open a draft PR toward staging for review. Push or merge
  to GitHub ready, master, or staging only after explicit publish approval.
  Use when a cloud agent does git, or when downloading staging.
---

# Cloud-agent git

Cloud agents follow Work. Desktop git stays [[.agents/skills/git-protocol/SKILL.md]].

Toolchain and Postgres: [[.agents/rules/environment-cursor-cloud.md]].

If this run **downloads staging**, follow [[LAND.md]] only: pull `origin/staging` onto local `staging`. `dev` never pulls from cloud.

**staging** is a published long-lived branch on `origin`. It is the drop for finished cloud-agent work. It is not a workplace. Desktop holds a local `staging` that tracks `origin/staging`.

The CloudAgents library still returns the vendor branch and PR URL. That is not the drop. Send to `staging` is this protocol.

## Work

GitHub `git push` auth: [Push auth recovery](#push-auth-recovery).

### 1. Disposable branch

Create or switch to a disposable branch. Base it on `origin/ready` (same start as a disposable workspace in [[.agents/skills/git-share/SKILL.md]]). If HEAD is `ready` or `master`, create the disposable branch from this tip and switch to it before the first commit.

Work and commit on that disposable branch. Git is free on a disposable or `cursor/*` branch. Do not commit on `dev`, `ready`, `master`, or `staging` unless Alan asked.

Done when `git branch --show-current` is not `dev`, `ready`, `master`, or `staging`.

### 2. Commit

Before you commit code changes, apply the Build / test toolchain gate in [[.agents/rules/core-agent-behavior.md]].
Commit on the disposable branch with `git commit`. [[scripts/commit.sh]] stays on `dev`.

Done when this run's files are committed on that branch (`git status --short` is empty for them).

### 3. Send to staging

Open a draft PR from the disposable branch toward `staging` for review. The disposable remote branch and that PR are not a publish. Local `origin/*` worktrees are also outside this rule.

Do not push or merge onto GitHub `ready`, `master`, or `staging` until explicit publish approval is given. After that approval, send to staging: fetch `origin`, merge the disposable branch into `staging`. If `origin/staging` is missing, create `staging` from the disposable branch. Push `origin staging`. [[scripts/gitpush.sh]] does not take `staging`. Conflicts: [[.agents/skills/resolving-merge-conflicts/SKILL.md]].

The first approved publish of a ticket onto GitHub `staging` is one commit. Squash the disposable branch when it has more than one. After that land, later revision follow-ups on the same ticket may add extra commits.

Off-ticket items (review reports, small docs, rule tweaks that are not on a ticket) do not publish as their own staging lands. Pool them into one shared land, or fold them into a related ticket's single first-land commit.

Done when explicit publish approval is given and `git fetch origin` plus `git merge-base --is-ancestor HEAD origin/staging` succeed for this run's published tip — or, before that approval, when the draft PR toward `staging` is open and GitHub `ready`, `master`, and `staging` were not pushed or merged.

### Push auth recovery

This repo's day-to-day remote (`abaljeu/ambit`) is GitHub, not Cursor Origin. Authenticate `git` through `gh` for GitHub remotes. Run `gh auth setup-git` (or configure the git credential helper from `gh`) so `git push` uses that helper.

On any `git push` failure that looks like auth ( Invalid username or token, Authentication failed, could not read Username, HTTP 401 or 403 on push ) recover at once: run `gh auth setup-git` (or the same helper setup), then retry that push once. Start this recovery as soon as the push fails as auth. Do not wait for Alan.

If the retry succeeds, continue the Work path. If it still fails, report the local commit SHA (`git rev-parse HEAD`), the remote tip (`git rev-parse @{u}` or `git ls-remote` when the upstream ref is missing), and that this recovery ran, then stop. One recovery and one retry is the whole auth path. Do not run four exponential retries on an auth failure without this recovery.

Done when the push succeeded after at most one recovery and one retry, or when that report is written and you stopped.
