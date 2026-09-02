# 04 — Named UX scripts for git-protocol moves

**Status:** done
**Blocked by:** None — can start immediately.

## Context

[[scripts/commit.sh]], [[scripts/merge.sh]], and [[scripts/push.sh]] already perform the protocol operations. The human wants clearer names and one extra [[scripts/commit.sh]] behavior. Spec: [[plan/git-protocol/scripts-spec.md]].

Today:

- [[scripts/commit.sh]] requires a message. With no argument it prints usage and exits 1.
- [[scripts/merge.sh]] `ready [-m <msg>]` brings `dev` into `ready`.
- [[scripts/merge.sh]] `master -m <msg>` squash-commits `ready` onto `master` and then forward-merges `master` toward `dev`.
- [[scripts/merge.sh]] `forward [master|ready]` propagates that source toward `dev` with stock messages. Default source is `master`. There is no `forward dev` command. `dev` is the destination of every forward, not the CLI place.
- [[scripts/push.sh]] `<ready|master>` pushes origin for that place. It does not switch HEAD. It refuses `dev`.

## What to build

Named scripts under [[scripts/]] match [[plan/git-protocol/scripts-spec.md]]. Do not add UX that the spec does not name. Keep the same git operations as today.

- [x] [[scripts/commit.sh]] `"desc"` commits the current `dev` branch with that message (same as today: refuse when HEAD is not `dev`, stage `.`, commit).
- [x] [[scripts/commit.sh]] with no argument runs `git status` (it must not print usage and exit 1).
- [x] [[scripts/gitready.sh]] `"desc"` does what [[scripts/merge.sh]] `ready -m "desc"` does.
- [x] [[scripts/gitmaster.sh]] `"desc"` does what [[scripts/merge.sh]] `master -m "desc"` does.
- [x] [[scripts/gitdev.sh]] does what [[scripts/merge.sh]] `forward` does with default source `master`: forward-merge the squash from `master` into `ready`, then `ready` into `dev`, with the stock forward messages. No dest or desc argument: `forward` does not take `-m`. Spec text `gitdev.sh "dest"` and `merge.sh forward dev` names the destination of the move, not a CLI flag.
- [x] [[scripts/gitpush.sh]] does what [[scripts/push.sh]] does: takes `ready` or `master`, pushes that place to origin, refuses `dev`. Current push.sh does not switch HEAD; do not add a checkout.
- [x] [[scripts/merge.sh]] and [[scripts/push.sh]] are deleted. Their operations live in the named UX scripts. [[scripts/commit.sh]] stays. Shared merge helpers (including `forward_from ready`) live in [[scripts/_git-protocol.sh]], not as a public command.
- [x] References to these scripts are updated.

## Comments

- 2026-09-02: Filed from [[plan/git-protocol/scripts-spec.md]]. Spec typos (`whate`, `"dest"` vs `"desc"`, `forward dev`) mapped to current [[scripts/merge.sh]] `forward` behavior. This issue does not edit instruction skills or retire the old script names.
- 2026-09-02: Implemented. Named wrappers call [[scripts/merge.sh]] and [[scripts/push.sh]]. [[scripts/commit.sh]] with no argument runs `git status`. Live pointers in [[.cursor/skills/git-protocol/SKILL.md]], [[.cursor/skills/git-master/SKILL.md]], [[.cursor/skills/git-share/SKILL.md]], and [[CONTEXT.md]] now name the new scripts. [[.cursor/skills/git-share/SKILL.md]] still uses `merge.sh forward ready` because [[scripts/gitdev.sh]] takes no dest. Did not delete the old scripts. Did not change [[plan/git-protocol/project.md]] or [[plan/index.md]].
- 2026-09-02: Not accepted. Missing criterion: old scripts deleted. Reopened. Wrappers are rejected; inline into named UX scripts and delete [[scripts/merge.sh]] and [[scripts/push.sh]].
- 2026-09-02: Old public names deleted. Helpers from merge.sh live in [[scripts/_git-protocol.sh]]. [[scripts/gitpush.sh]] holds the old push.sh body. git-share Pull names [[scripts/gitdev.sh]]. 12-line tmp verify passed and was deleted.

## See also

[[plan/git-protocol/scripts-spec.md]], [[.cursor/skills/git-protocol/SKILL.md]], [[.cursor/skills/git-master/SKILL.md]], [[.cursor/skills/git-share/SKILL.md]]
