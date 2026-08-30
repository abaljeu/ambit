---
name: git-share
description: Publish ready to the remote, pull, and catch up afterward.
disable-model-invocation: true
---

# Share

Explicit invocation only. Remotes are the human's to run.

Places and daily merges are in [[.cursor/skills/git-protocol/SKILL.md]]. Publishing `master` is [[.cursor/skills/git-master/SKILL.md]].

## Publish

One human operator on this machine. `dev` stays local.

```bash
./scripts/push.sh ready
```

[[scripts/push.sh]] refuses `dev` and pushes `origin` `ready`.

## Pull

Pull only after `ready` moved elsewhere. Then catch up, before further Desktop commits and before the next `dev` → `ready` merge:

```bash
./scripts/merge.sh forward ready
```

Local `ready` must hold the published tip before anything merges into it. That keeps first-parent as “this `ready`” and turns a race into a rejected push or a file conflict instead of two `ready` tips mashed together. [[scripts/merge.sh]] enforces it: it refuses a local `ready` behind `origin/ready`.

## Cloud

Parked; nothing runs in the cloud today.

A Cursor cloud agent would work on its own disposable workspace rather than this machine's `dev`. It would sit on `ready`, merge from that workspace with `--no-ff`, and push `ready`. It would leave `master` and any new long-lived place alone.

Making that real needs a model-invoked home for these three sentences. This skill is user-invoked, so a cloud agent cannot reach it.
