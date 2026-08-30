---
name: git-protocol
description: "Gambol git procedure: three named places (dev, ready, master), merge --no-ff, squash slices, catch-up merge. Use when committing, merging, branching, tagging, agent-done, implement, or any other git instruction."
---

# Git protocol

Canonical git procedure for this repo. Other rules and skills point here; they do not copy these steps.

## Places

Three long-lived branches. Reuse these names. Do not add `w/` branches.

**dev** — workplace on this machine. All Desktop edits and ordinary commits happen here. Piece history is born here. Local-only; do not push `dev`.

**ready** — integration. Sit on `ready` and `git merge --no-ff dev`. Piece bisect lives here. This is “brought into ready.”

**master** — slices. Sit on `master` and squash `ready` into it. Slice bisect lives here. Tags name commits on `master`.  Updating master is a manual decision, never automatic.

After each squash, sit on `ready` and `git merge --no-ff master` so the next squash does not replay shipped work. Then sit on `dev` and `git merge --no-ff ready` so `dev` has current code.

A hotfix is born on the oldest place that must contain it, then merged toward `dev`.

## Merges

The Desktop agent does not run `git merge` or squash. Those moves go through [[scripts/merge.sh]] (Cursor manual approval) or the human types them in the CLI. That script is the Desktop merge entry for `dev` → `ready` (`--no-ff`), `ready` → `master` (squash), catch-up `master` → `ready` then `ready` → `dev`, and hotfix toward `dev`. The script is not written yet.

**agent-done** is tests green, `/code-review`, and `git commit` on `dev`. Then ask the human to run `merge.sh` (or type the merge) to put that work on `ready`.

Before every merge into `ready` (Desktop `merge.sh` or cloud), update local `ready` to the published tip, then `git merge --no-ff` the workplace. That keeps first-parent as “this `ready`” and turns a race into a rejected push or a file conflict instead of two `ready` tips mashed together.

## Bisect

The agent prepares a read-only recipe (log range and a red command). The human runs bisect. The agent does not check out bisect commits.

## Today

Current HEAD is just where work sits now (today: `selective-client-sync`). Do not keep that name as the workplace. Do not create another `w/` name. Do not resume an old `w/` as the workplace.

Once: create `dev` and `ready` on the same commit as current HEAD. After that, edit only on `dev`. Old names remain as history.

`master` already exists and may be behind this HEAD. The first squash onto it is a human merge.sh (or CLI) choice, not an agent default.

## Sharing

One human operator on this machine. `dev` is local-only; do not push it. Publish `ready` and `master` with push. No pull on this machine until `ready` or `master` moved elsewhere (including cloud).

A Cursor cloud agent works on **its workspace** (disposable). It does not use this machine's `dev`. It sits on `ready`, `git merge --no-ff` from that workspace, and **pushes `ready`**. It does not push a new long-lived place. It does not squash onto `master`.

After a cloud push, this machine pulls `ready`, then catch-up `ready` → `dev` (`--no-ff`) before further Desktop commits. Do that before the next `dev` → `ready` merge.

The Desktop agent still does not run remotes unless the user asks (manual approval).
