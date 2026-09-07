# Implement issue 05 — Wayfinder is process; git on `dev`

Wayfinder now charts a destination and chooses when to research, grill, or prototype. Claim, Type, Status, frontier, and file layout live in [[doc/agents/issue-tracker.md]]. Prototype capture is on `dev`, then `ready` / `master`. Research was already on `plan/<slug>/reports/` with no extra git places, so it was not edited. Product F# was not touched.

## What changed

- [[.agents/skills/wayfinder/SKILL.md]]: dropped GitHub-shaped ops (`wayfinder:` labels, assignee claim, close-the-issue). Chart and work steps follow the tracker for those ops. Research findings point at [[.agents/skills/research/SKILL.md]]. Type (`research` | `prototype` | `grilling` | `task`) stays Type, not Status. Commits point at [[.agents/skills/git-protocol/SKILL.md]].
- [[.agents/skills/prototype/SKILL.md]]: capture commits the prototype on `dev`, then `ready` / `master`. The throwaway-branch / out-of-main line is gone.
- [[.agents/skills/research/SKILL.md]]: unchanged. Ticket 08 already set `plan/<slug>/reports/`. No `research/<name>` place in that skill.
- Ticket [[plan/skills-cleanup/issues/05-wayfinder-process-git-on-dev.md]] is `Status: done`.

## How verified

- [[.agents/skills/wayfinder/SKILL.md]] contains `doc/agents/issue-tracker.md` and does not contain `wayfinder:`, `assignee`, `research/<name>`, or close/assign/label verbs.
- That skill still names Type values `research`, `prototype`, `grilling`, and `task`, and says Type is not Status.
- [[.agents/skills/prototype/SKILL.md]] contains capture on `dev` and `git-protocol`, and does not contain `prototype/<name>` or `out of main`.
- [[.agents/skills/research/SKILL.md]] still contains `plan/<slug>/reports/` and does not contain `research/<name>`.

## Leftover risks

- [[.agents/skills/ask-matt/SKILL.md]] still names a `prototype/<name>` place. Ticket 12 owns that file.
- [[.agents/skills/prototype/UI.md]] and [[.agents/skills/prototype/LOGIC.md]] still say throwaway branch / main. Out of exclusive paths.
- Wayfinder still invokes `/grilling`. Default grill is `/grill-me` per ticket 12.
- Wayfinder Refer-by-name still uses `#42` as the illegible-id example. Harmless process; not a git place.
- `./status.sh` is absent (ticket 04 deleted it). Git status was [[scripts/gitstatus.sh]] on `dev`.
