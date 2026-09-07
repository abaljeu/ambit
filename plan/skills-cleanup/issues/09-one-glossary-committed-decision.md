# 09 — One glossary; say Committed Decision

**Status:** ready-for-agent
**Blocked by:** [[02-allow-agents-skill-home-in-prepare-rule.md|02 Allow the agents skill home in the prepare rule]]

## Context

This repo has one glossary: [[CONTEXT.md]]. Hard choices that are costly to reverse live under [[doc/Decisions/]] as Committed Decisions. domain-modeling and other skills still say ADR. ubiquitous-language still writes a second glossary file. An Agent can open the wrong file or use the wrong name.

## What to build

domain-modeling writes [[CONTEXT.md]] and records hard choices as Committed Decisions under [[doc/Decisions/]]. It does not say ADR. ubiquitous-language is not a second glossary owner and does not create UBIQUITOUS_LANGUAGE.md. Skills this ticket touches use Committed Decision for that record.

- [ ] domain-modeling writes [[CONTEXT.md]] and says Committed Decision, not ADR.
- [ ] ubiquitous-language does not own a second glossary and does not create UBIQUITOUS_LANGUAGE.md.

## Comments

Ask-Matt wording waits for [[12-ask-matt-and-gambol-mdc-agree.md]]. Architecture-review wording can land here or in [[08-reports-land-under-plan-reports.md]]; the same files must not fight.

## See also

[[plan/skills-cleanup/reports/lock-grill-reports-refactor-glossary.md]], [[CONTEXT.md]]
