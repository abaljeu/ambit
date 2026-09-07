# 04 — Remove the vendor merge and setup-matt bootstrap

**Status:** done
**Blocked by:** [[02-allow-agents-skill-home-in-prepare-rule.md|02 Allow the agents skill home in the prepare rule]]
**Actual:** 45m

## Context

This repo already has a local issue tracker under [[plan/]]. Some skills still say run setup-matt first. update-matt-skills still teaches a vendor merge onto `vendor/` and `update/` places. A coding Agent can re-scaffold a tracker that already exists, or open git places that git-protocol forbids.

## What to build

A coding Agent does not run update-matt-skills or setup-matt. Those skills and the seed copies under [[.agents/skills/setup-matt-pocock-skills/]] are deleted or archived. Live skills do not say run setup-matt first. Ask-Matt stays. Git places `vendor/mattpocock-skills` and `update/mattpocock-skills` are not taught.

- [x] update-matt-skills is not a live workflow skill.
- [x] Seed copies under [[.agents/skills/setup-matt-pocock-skills/]] are deleted or archived.
- [x] No live skill says run setup-matt first.
- [x] Ask-Matt remains.

## See also

[[plan/skills-cleanup/reports/lock-vendor-merge-obsolete.md]], [[plan/skills-cleanup/reports/lock-git-all-on-dev.md]]

## Time

- 2026-09-06 45m — deleted vendor merge and setup-matt skills; stripped run-setup-matt-first from live skills (from chat)
