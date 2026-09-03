# Implement 04 — Delete old scripts

Issue [[plan/git-protocol/issues/04-named-ux-scripts.md]] is **done** again. Old public names are gone. No commit. No remotes.

## Why 10 became 100

The growth was the **tmp verify script**, not new product logic.

The rejected Shell one-liner was about ten checks: files gone, named scripts present, `bash -n`. I turned that into a 233-line harness (`pass`/`fail`, usage matrix, skill greps). That was over-engineering. Replaced with a 12-line file [[tmp/check-04-named-ux.sh]] (Write, no semicolons), ran `bash ./tmp/check-04-named-ux.sh`, exit 0, then deleted the file.

[[scripts/_git-protocol.sh]] (91 lines) is the old [[scripts/merge.sh]] helper body (require, merge --no-ff, forward_from), not a new library. [[scripts/gitpush.sh]] (32 lines) is the old [[scripts/push.sh]] body. The named merge scripts hold the old command cases.

## What was deleted

- [[scripts/merge.sh]]
- [[scripts/push.sh]]

[[scripts/commit.sh]] stays.

## Where logic lives

- [[scripts/gitready.sh]] — dev (`dev`) into `ready`
- [[scripts/gitmaster.sh]] — squash `ready` onto `master`, then `forward_from master`
- [[scripts/gitdev.sh]] — `forward_from master` (no dev or desc argument)
- [[scripts/_git-protocol.sh]] — shared helpers, including `forward_from ready` (no public CLI)
- [[scripts/gitpush.sh]] — `origin` `ready` or `master`, refuses dev (`dev`)

## Callers

Live skills name the new scripts. [[.cursor/skills/git-share/SKILL.md]] Pull uses [[scripts/gitdev.sh]] (toward dev). That is not a 1:1 of old `forward ready`. Issue 02 cites the named scripts only. History reports and [[plan/git-protocol/scripts-spec.md]] still name the old files as the mapping.

## Tmp verify

Path written: [[tmp/check-04-named-ux.sh]]. Checked: `merge.sh`/`push.sh` absent, four named scripts plus [[scripts/commit.sh]] present, `bash -n` on the four. Result: **pass** (exit 0). File deleted after the run.

## Issue 04

Yes. **Status:** `done`. Old-scripts criterion is met.
