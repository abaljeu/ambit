# 02 — Allow the agents skill home in the prepare rule

**Status:** done
**Blocked by:** [[01-move-skills-to-agents-home.md|01 Move repo-shared skills to the agents home]]
**Actual:** 45m

## Context

After the move, later slices in this Project edit skills in [[.agents/skills/]]. The prepare-agent-instruction-change skill still forbids that home and tells the Agent to store repo-shared skills in [[.cursor/skills/]]. An Agent that obeys that skill would move the files back.

## What to build

An Agent that follows prepare-agent-instruction-change (now under [[.agents/skills/]]) may keep repo-shared skills in [[.agents/skills/]]. The old forbid rule is gone. Later slices in this Project are legal in the locked home.

- [x] prepare-agent-instruction-change no longer forbids storing repo-shared skills in [[.agents/skills/]].
- [x] That skill names [[.agents/skills/]] as the repo-shared skill home.

## See also

[[plan/skills-cleanup/reports/lock-tickets-home-wayfinder.md]], [[plan/skills-cleanup/project.md]]

## Time

- 2026-09-06 45m — flipped the prepare skill home rule to [[.agents/skills/]] (from chat)
