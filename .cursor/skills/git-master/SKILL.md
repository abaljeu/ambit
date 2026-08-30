---
name: git-master
description: Squash ready onto master, tag it, and publish.
disable-model-invocation: true
---

# Master

Explicit invocation only. Updating `master` is a decision the human makes by hand.

Places, daily commits, and `dev` → `ready` merges are in [[.cursor/skills/git-protocol/SKILL.md]].

## Squash

Each commit on `master` is one squashed merge from `ready`. Squash `ready` into it:

```bash
./scripts/merge.sh master -m "<message>"
```

That squash-commits `ready` onto `master`, then propagates forward: `master` into `ready` (`--no-ff`), then `ready` into `dev` (`--no-ff`). The forward pass keeps the next squash from replaying shipped work, so [[scripts/merge.sh]] runs it in the same breath as the squash.

Bisect squash-by-squash on `master`.

## Tag

Tags name commits on `master`.

## Publish

Push `master` to `origin`. It fast-forwards: the public presentation commits on `origin/master` are in this line's ancestry.
