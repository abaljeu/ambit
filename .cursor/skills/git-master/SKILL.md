---
name: git-master
description: Squash ready onto master and publish.
disable-model-invocation: true
---

# Master

Explicit invocation only. Updating `master` is a decision the human makes by hand.

Places, daily commits, and `dev` → `ready` merges are in [[.cursor/skills/git-protocol/SKILL.md]].

## Squash

Each commit on `master` is one squashed merge from `ready`. Squash `ready` into it:

```bash
./scripts/gitmaster.sh "<message>"
```

That squash-commits `ready` onto `master`, then propagates forward: `master` into `ready` (`--no-ff`), then `ready` into `dev` (`--no-ff`). The forward pass keeps the next squash from replaying shipped work, so [[scripts/gitmaster.sh]] runs it in the same breath as the squash.

Bisect squash-by-squash on `master`.

## Tag

The human tags only. The agent does not run `git tag`, even if asked.

The name is whatever the human types.

```bash
git tag -f NAME master
```

Lightweight. No helper script. No checkout. `-f` may re-point an existing name at the current `master` tip, including replacing an annotated tag with a lightweight tag.

## Publish

```bash
./scripts/gitpush.sh master
```

[[scripts/gitpush.sh]] refuses `dev` and pushes `origin` `master`. It also force-pushes every local tag that points at that `master` tip, so origin follows a moved name. It does not `--tags`. It fast-forwards once the public presentation commits on `origin/master` are in this line's ancestry.
