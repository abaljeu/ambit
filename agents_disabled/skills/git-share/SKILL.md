---
name: git-share
description: >-
  Publish ready to GitHub, pull origin/ready, and catch up. Use when sharing
  work across agents or machines, after work lands on ready, or when local
  ready may be behind origin/ready. Code pushes are approval-gated.
---

# Share

Places, push gates, and human-only rules: [[.agents/skills/git-protocol/SKILL.md]]. This skill is the pull and publish-`ready` procedure.

## Before editing (shared checkout)

Fetch, then make local `ready` hold the published tip before merging into it or catching `dev` up:

```bash
git fetch origin
git switch ready
git merge --ff-only origin/ready
```

If `--ff-only` fails, stop and report. Do not mash two `ready` tips together.

Then catch `dev` up:

```bash
./scripts/gitdev.sh
```

[[scripts/gitdev.sh]] forward-merges `master` into `ready`, then `ready` into `dev`, with the stock forward messages. After `ready` moved elsewhere and `master` is already in `ready`, the first merge is already up to date; the second is the catch-up.

Local `ready` must hold the published tip before anything merges into it. That keeps first-parent as "this `ready`" and turns a race into a rejected push or a file conflict instead of two `ready` tips mashed together. [[scripts/gitready.sh]] and [[scripts/gitmaster.sh]] enforce it: they refuse a local `ready` behind `origin/ready`.

Done when local `ready` matches `origin/ready` (or you stopped on `--ff-only` failure) and `dev` is caught up via [[scripts/gitdev.sh]].

## Publish `ready` (approval-gated)

After `dev` is on `ready` via [[scripts/gitready.sh]]:

```bash
./scripts/gitpush.sh ready
```

[[scripts/gitpush.sh]] refuses `dev` and pushes `origin` `ready`.

**Code push gate:** do not run `gitpush.sh ready` (or any `git push` of application/plan commits) until Alan has approved that push in chat or via the tool approval card. Pull/fetch needs no approval.

Done when `gitpush.sh ready` ran after Alan's approval, or you did not push.

## Agent workplaces

An agent may work on this machine's `dev`, or on a disposable workspace that starts from current `origin/ready`.

Cloud agents send finished work to `staging`: [[.agents/skills/cloud-agent-git/SKILL.md]]. Do not land cloud work onto `ready` from the cloud run.

Other disposable desktops:

- Land finished work onto `ready` with `--no-ff` (via [[scripts/gitready.sh]] on this machine, or the same merge on a disposable workspace).
- Push `ready` only after approval per [[.agents/skills/git-protocol/SKILL.md]].
- Leave `master` alone. `staging` is the cloud drop.

Do not two-write the same files without fetching first. Prefer disjoint paths when several agents co-edit.

Done when work is on this machine's `dev` or a disposable from `origin/ready`, and cloud work uses `staging`.

## Still human-only / gated

Place and push rules: [[.agents/skills/git-protocol/SKILL.md]]. Squash and tags: [[.agents/skills/git-master/SKILL.md]].
