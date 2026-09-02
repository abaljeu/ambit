# 02 — Git guardrails must not block protocol scripts

**Status:** ready-for-agent
**Blocked by:** None — can start immediately.

## Context

[[.agents/skills/git-guardrails-claude-code/SKILL.md]] installs hooks that block `git push` and other dangerous git commands. Git protocol still needs [[scripts/gitready.sh]], [[scripts/gitmaster.sh]], [[scripts/gitdev.sh]], [[scripts/gitpush.sh]], and a human cloud push of `ready`. A hook that matches those commands can stop a legal protocol step.

## What to build

Guardrails stay in place for the dangerous cases they name. They do not block [[scripts/gitready.sh]], [[scripts/gitmaster.sh]], [[scripts/gitdev.sh]], [[scripts/gitpush.sh]], or a human push of `ready` that git-protocol allows.

- [ ] The hook skill and bundled script name what they block vs what git-protocol allows.
- [ ] [[scripts/gitready.sh]], [[scripts/gitmaster.sh]], [[scripts/gitdev.sh]], and [[scripts/gitpush.sh]] are not blocked when used as git-protocol specifies.
- [ ] Cloud push of `ready` (human, [[.cursor/skills/git-share/SKILL.md]]) is not blocked by the same hook.

## Comments

- 2026-09-02: Filed unclaimed from WORK.md.
- 2026-09-02: Issue 04 deleted the old public names; this issue now cites the named UX scripts. Guardrail fix is still open.

## See also

[[.cursor/skills/git-protocol/SKILL.md]], [[.cursor/skills/git-share/SKILL.md]]
