# Land staging

Local follow-up after a cloud agent sent work to `staging`. Desktop commits and `dev` → `ready` stay [[.agents/skills/git-protocol/SKILL.md]]. Push is [[.agents/skills/git-share/SKILL.md]].

Pull `origin/staging` onto local `staging`. `dev` never pulls from cloud. After that download, land is a local `--no-ff` merge of already-downloaded `staging` into `dev`.

## 1. Download

`git fetch origin`. Create or update local `staging` so it holds `origin/staging`: `git switch staging` then `git merge --ff-only origin/staging`, or `git switch -c staging --track origin/staging` when local `staging` is missing. Leave `dev` on its Desktop tip.

Done when `git rev-parse staging` equals `git rev-parse origin/staging`. If `origin/staging` is missing, stop and report: nothing to land.

## 2. Merge into dev

Switch to a clean `dev`. Merge local `staging` with `--no-ff`. This skill owns that merge. The merge parent is already-downloaded local `staging`, not a `git pull` of `origin/staging` or a vendor branch into `dev`.

Done when `git merge-base --is-ancestor staging dev` succeeds.

The following actions are manually requested, never automatic. 

## 3. Review

The user reviews code.  He may invoke [[.agents/skills/code-review/SKILL.md]] of the landed range: the `dev` tip before this merge ... `HEAD`. `gitready.sh`, no arguments, lists that commit set.

## 4. Ready

After user approves the land, bring `dev` into `ready` with [[scripts/gitready.sh]] (or human CLI) per [[.agents/skills/git-protocol/SKILL.md]].

Done when the landed commits are on `ready`.

## 5. Push

Publish `ready` per [[.agents/skills/git-share/SKILL.md]].

Done when `origin/ready` holds that tip.
