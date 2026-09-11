# Land staging

Local follow-up after a cloud agent sent work to `staging`. Desktop commits and `dev` → `ready` stay [[.agents/skills/git-protocol/SKILL.md]]. Push is [[.agents/skills/git-share/SKILL.md]].

## 1. Download

`git fetch origin`.

Done when `origin/staging` exists (`git rev-parse --verify origin/staging`). If it is missing, stop and report: nothing to land.

## 2. Merge into dev

Switch to a clean `dev`. Merge `origin/staging` with `--no-ff`. This skill owns that merge.

Done when `git merge-base --is-ancestor origin/staging dev` succeeds.

## 3. Review

[[.agents/skills/code-review/SKILL.md]] of the landed range: the `dev` tip before this merge ... `HEAD`.

Done when Standards and Spec both have a recorded result.

## 4. Ready

After Alan approves the land, bring `dev` into `ready` with [[scripts/gitready.sh]] (or human CLI) per [[.agents/skills/git-protocol/SKILL.md]].

Done when the landed commits are on `ready`.

## 5. Push

Publish `ready` per [[.agents/skills/git-share/SKILL.md]].

Done when `origin/ready` holds that tip.
