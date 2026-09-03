# Implement 03 — Name the master tag convention

Issue [[plan/git-protocol/issues/03-name-master-tag-convention.md]] is **done**. No commit. No remotes. Did not run [[scripts/gitpush.sh]] or [[scripts/gitmaster.sh]].

## What changed

- [[.cursor/skills/git-master/SKILL.md]] — description no longer says “tag it.” Squash path is squash-only. Tag section names `git tag -f NAME master`, human-only, free-form name, lightweight, overwrite including annotated → lightweight. Agent must not run `git tag`, even if asked. Publish says [[scripts/gitpush.sh]] `master` force-pushes tags at that tip, not `--tags`.
- [[scripts/gitpush.sh]] — after `git push origin master`, collect `git tag --points-at` that tip and `git push --force origin` those `refs/tags/` names. `ready` still pushes the branch only. No `--tags`. No [[scripts/gittag.sh]].
- [[.cursor/rules/gambol.mdc]] — git-master pointer dropped “tag” from the squash path so it matches the skill.
- [[plan/git-protocol/issues/03-name-master-tag-convention.md]] — Status `done`; checklist marked.

## Locked bullets

- Squash and tag are two functions. [[scripts/gitmaster.sh]] has no `git tag`. Skill squash path does not say “tag it.”
- Human only. Skill forbids the agent from `git tag`, even if asked.
- Name is whatever the human types. No `v*` rule.
- Command in the skill is `git tag -f NAME master`. Lightweight. No helper. No checkout.
- `-f` overwrite, including annotated → lightweight, is in the Tag section.
- [[scripts/gitpush.sh]] `master` publishes only tags that point at the tip being pushed, with `--force` so origin follows a moved name. Not `--tags`.

## Verify

`bash -n` passed on [[scripts/gitpush.sh]] and [[scripts/gitmaster.sh]]. Current `master` tip has no pointing tags, so a `master` push would run `git push origin master` and skip the force-push. Did not push.

## Left unchanged

- [[.cursor/skills/git-protocol/SKILL.md]], [[CONTEXT.md]], [[.cursor/skills/git-share/SKILL.md]] — they did not say squash tags `master` or copy old merge.sh names.
- [[scripts/gitmaster.sh]] and [[scripts/_git-protocol.sh]] — squash path already did not tag.
- [[plan/git-protocol/issues/02-git-guardrails-may-block-ready-push.md]] — not this issue. Force-update of tip tags uses `git push --force` on tag refs.
- Issues 01 and 04 — not reopened.
- [[plan/git-protocol/project.md]] Stage stays `active`.

## Not done

- No commit.
- No remotes, no `gitpush`, no `gitmaster`.
