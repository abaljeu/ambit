# Grill retry — issue 03, wrap

Issue: [[plan/git-protocol/issues/03-name-master-tag-convention.md]]. Skill: [[.agents/skills/grilling/SKILL.md]]. Grill complete. No implement. Project [[plan/git-protocol/project.md]] stays `Stage: active`.

## Status

Issue `Status:` was `grilling`. Frontier is empty. Set to `ready-for-human` so the next agent does not re-grill. The human accepts the locked convention, then implement.

## Locked convention

- Squash and tag are two functions. `gitmaster` does not tag. Drop “tag it” from the squash path.
- Human only. Agent does not run `git tag`, even if asked.
- Name: whatever the human types.
- `git tag -f NAME master`. Lightweight. No script. No checkout.
- Overwrite allowed, including annotated → lightweight.
- `gitpush master` publishes only tags on that `master` tip. Force-update if origin has the name on another commit. Not `--tags`.
