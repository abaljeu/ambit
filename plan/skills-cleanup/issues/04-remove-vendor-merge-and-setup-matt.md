# 04 — Remove the vendor merge and setup-matt bootstrap

**Status:** ready-for-agent
**Blocked by:** [[02-allow-agents-skill-home-in-prepare-rule.md|02 Allow the agents skill home in the prepare rule]]

## Context

This repo already has a local issue tracker under [[plan/]]. Some skills still say run setup-matt first. update-matt-skills still teaches a vendor merge onto `vendor/` and `update/` places. A coding Agent can re-scaffold a tracker that already exists, or open git places that git-protocol forbids.

## What to build

A coding Agent does not run update-matt-skills or setup-matt. Those skills and the seed copies under [[.agents/skills/setup-matt-pocock-skills/]] are deleted or archived. Live skills do not say run setup-matt first. Ask-Matt stays. Git places `vendor/mattpocock-skills` and `update/mattpocock-skills` are not taught.

- [ ] update-matt-skills is not a live workflow skill.
- [ ] Seed copies under [[.agents/skills/setup-matt-pocock-skills/]] are deleted or archived.
- [ ] No live skill says run setup-matt first.
- [ ] Ask-Matt remains.

## See also

[[plan/skills-cleanup/reports/lock-vendor-merge-obsolete.md]], [[plan/skills-cleanup/reports/lock-git-all-on-dev.md]]
