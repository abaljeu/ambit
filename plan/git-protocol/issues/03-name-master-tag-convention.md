# 03 — Name the master tag convention

**Status:** ready-for-human
**Blocked by:** None — can start immediately.

## Context

[[.cursor/skills/git-master/SKILL.md]] says tags name commits on `master`. It does not name the tag pattern or who applies a tag. Agents and humans need one convention.

## Locked convention

Grill complete. Confirm this, then implement. Do not implement until the human accepts.

- Squash-merge into `master` and tagging a version name are two functions. [[scripts/gitmaster.sh]] does not tag. Drop “tag it” from the squash path.
- The human tags only. The agent does not run `git tag`, even if asked.
- The name is whatever the human types. Not a forced `v*` pattern.
- Command: `git tag -f NAME master`. Lightweight. No helper script. No checkout. Human keeps `master` current.
- `-f` may re-point an existing name at the current `master` tip. Replacing an annotated tag with lightweight is fine.
- [[scripts/gitpush.sh]] `master` publishes only tags that point at the `master` tip being pushed. Not `--tags`. If that name exists on origin at another commit, force-update it. Local pin wins.

## What to build

The git-master skill states this convention. Other instructions keep pointing at that skill.

- [ ] Tag section names the command, who runs it, and the free-form name.
- [ ] Squash path does not say “tag it.”
- [ ] Publish path: `gitpush master` pushes tip tags and force-updates a moved name. No second copy of the convention.

## Comments

- 2026-09-02: Filed unclaimed from WORK.md.
- 2026-09-02 grill Q1: Two separate functions. Squash-merge into master does not tag. Tagging a version name onto a commit is a separate operation.
- 2026-09-02 grill Q2–Q4: Tag only the current master tip. Human only; the agent does not run git tag. The name is whatever the human types (not a forced v* pattern).
- 2026-09-02 grill Q5–Q7: Always tag the master branch tip (no checkout). Human may overwrite: re-point an existing name at that tip. Lightweight.
- 2026-09-02 grill Q8: Raw `git tag -f NAME master`. No helper script. Human keeps master current.
- 2026-09-02 grill Q9–Q11: `gitpush master` publishes only tags that point at the master tip being pushed. Overwrite of an annotated tag with lightweight is fine.
- 2026-09-02 grill Q12: Force-update those tip tags on origin. Local pin wins.
- 2026-09-02 grill wrap: Frontier empty. Status set to ready-for-human so the next agent does not re-grill. Project Stage stays `active`.

## See also

[[.cursor/skills/git-protocol/SKILL.md]], [[CONTEXT.md]]
