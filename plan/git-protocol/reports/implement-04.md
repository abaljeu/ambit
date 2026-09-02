# Implement 04 — Named UX scripts

Issue [[plan/git-protocol/issues/04-named-ux-scripts.md]] is **done**. No commit. No remotes. No merge to ready or master.

## What changed

- [[scripts/commit.sh]] — with no argument, run `git status` and exit 0. With a message, same as before: refuse when HEAD is not `dev`, stage `.`, commit.
- [[scripts/gitready.sh]] — new. Requires `"desc"`. Calls `merge.sh ready -m "desc"`.
- [[scripts/gitmaster.sh]] — new. Requires `"desc"`. Calls `merge.sh master -m "desc"`.
- [[scripts/gitdev.sh]] — new. No dest or desc argument. Calls `merge.sh forward` (default source `master`).
- [[scripts/gitpush.sh]] — new. Passes arguments to [[scripts/push.sh]]. No checkout.
- [[.cursor/skills/git-protocol/SKILL.md]] — merge entry names [[scripts/gitready.sh]], [[scripts/gitmaster.sh]], [[scripts/gitdev.sh]].
- [[.cursor/skills/git-master/SKILL.md]] — squash and publish use [[scripts/gitmaster.sh]] and [[scripts/gitpush.sh]].
- [[.cursor/skills/git-share/SKILL.md]] — publish uses [[scripts/gitpush.sh]]. Pull still uses `merge.sh forward ready`.
- [[CONTEXT.md]] — Agent-done and Manual approval name [[scripts/gitready.sh]].
- [[plan/git-protocol/issues/04-named-ux-scripts.md]] — Status `done`; checklist marked.

## Acceptance criteria

- [[scripts/commit.sh]] `"desc"` — body after the no-arg branch is unchanged (refuse non-`dev`, `git add .`, `git commit -m`). Not executed (would create a commit).
- [[scripts/commit.sh]] with no argument — ran it: `git status`, exit 0. It does not print usage.
- [[scripts/gitready.sh]] `"desc"` — `exec` of `merge.sh ready -m "$1"`. Missing or extra args print usage and exit 1.
- [[scripts/gitmaster.sh]] `"desc"` — `exec` of `merge.sh master -m "$1"`. Missing args print usage and exit 1.
- [[scripts/gitdev.sh]] — `exec` of `merge.sh forward`. An extra arg prints usage and exit 1. Did not run with no args (that would merge).
- [[scripts/gitpush.sh]] — `exec` of `push.sh "$@"`. No-arg and invalid place hit [[scripts/push.sh]] usage. `gitpush.sh dev` refuses `dev` and does not push. Did not run `ready` or `master`.
- Old scripts deleted — **not applied**. Filing comment: this issue does not retire the old script names. Wrappers need the callees. [[scripts/gitdev.sh]] has no dest, so `merge.sh forward ready` still needs [[scripts/merge.sh]]. Issue 02 still names the old scripts.
- References updated — live git-protocol, git-master, git-share, and [[CONTEXT.md]] now name the new scripts. git-share still names `merge.sh forward ready` because there is no named UX for that source.

`bash -n` passed on the five scripts.

## Left unchanged

- [[scripts/merge.sh]] and [[scripts/push.sh]] — callees. Same git operations as today.
- [[plan/git-protocol/scripts-spec.md]] — user artifact; typos left as filed.
- [[plan/git-protocol/issues/02-git-guardrails-may-block-ready-push.md]] and [[plan/git-protocol/issues/03-name-master-tag-convention.md]] — not this issue.
- [[plan/git-protocol/project.md]] and [[plan/index.md]] — Stage already `active`. Issue 01 may also touch them.
- Historical reports under [[plan/git-protocol/reports/]].

## Issue 04 marked done

Yes. **Status:** `done` on [[plan/git-protocol/issues/04-named-ux-scripts.md]].

## Collision avoidance

Did not edit:

- `.cursor/skills/update-matt-skills/**`
- `plan/done/update-matt-skills/**`
- [[plan/git-protocol/issues/01-align-merge-to-live-with-git-protocol.md]]
- [[plan/git-protocol/reports/implement-01.md]]
- [[plan/git-protocol/reports/issue-01-done-check.md]]

The filing comment said this issue does not edit instruction skills. The What to build checklist required updated references, and the implementation request said to update those pointers when the issue requires it. Updated only git-protocol, git-master, git-share, and [[CONTEXT.md]]. Did not touch the issue 01 skill tree.
