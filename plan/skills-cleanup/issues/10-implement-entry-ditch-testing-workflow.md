# 10 — `/implement` is the entry; ditch testing-workflow

**Status:** ready-for-agent
**Blocked by:** [[02-allow-agents-skill-home-in-prepare-rule.md|02 Allow the agents skill home in the prepare rule]]

## Context

An Agent that builds a ticket can follow `/implement`, `/tdd`, implement-fsharp-feature, and the always-apply testing-workflow rule. testing-workflow says write a test, then stop for review. `/implement` says keep going. The Agent hits two loops.

## What to build

An Agent that builds a ticket starts at `/implement`. tdd, implement-fsharp-feature, and add-shared-test stay as referenced augmentations. They do not publish a second red-green loop or a stop-for-review always-apply rule. testing-workflow is gone from always-apply and from the gambol.mdc index. The Client compile gate still runs from the F# skill when Client dependencies change. tdd is not deleted.

- [ ] [[.cursor/rules/testing-workflow.mdc]] is gone from always-apply and from the gambol.mdc index.
- [ ] tdd and implement-fsharp-feature augment `/implement` by reference. They do not copy a second loop or stop-for-review.
- [ ] The Client compile gate still runs from the F# skill when Client dependencies change.

## See also

[[plan/skills-cleanup/reports/lock-implement-path.md]], [[.agents/skills/implement/SKILL.md]]
