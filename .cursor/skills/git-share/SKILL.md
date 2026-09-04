---
name: git-share
description: >-
  Publish ready to GitHub, pull origin/ready, and catch up. Use when sharing
  work across agents or machines, after work lands on ready, or when local
  ready may be behind origin/ready. Code pushes are approval-gated.
---

# Share

Agents and humans may **pull** `ready` freely and **push `ready` only after approval**. `dev` stays local. `master` stays human-only ([[.cursor/skills/git-master/SKILL.md]]).

Places and daily merges are in [[.cursor/skills/git-protocol/SKILL.md]].

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

## Publish `ready` (approval-gated)

After `dev` is on `ready` via [[scripts/gitready.sh]]:

```bash
./scripts/gitpush.sh ready
```

[[scripts/gitpush.sh]] refuses `dev` and pushes `origin` `ready`.

**Code push gate:** do not run `gitpush.sh ready` (or any `git push` of application/plan commits) until Alan has approved that push in chat or via the tool approval card. Pull/fetch needs no approval. Never push `dev`. Never push `master` from this skill.

## Agent workplaces

An agent may work on this machine's `dev`, or on a disposable workspace that starts from current `origin/ready`:

- Land finished work onto `ready` with `--no-ff` (via [[scripts/gitready.sh]] on this machine, or the same merge on a disposable workspace).
- Push `ready` only after approval.
- Leave `master` and any new long-lived places alone.

Do not two-write the same files without fetching first. Prefer disjoint paths when several agents co-edit.

## Still human-only / gated

- Squash and publish `master` ([[.cursor/skills/git-master/SKILL.md]]) — human only
- Tags — human only
- Pushing `dev` — forbidden
- Pushing `ready` — agent-allowed only with Alan's push approval
