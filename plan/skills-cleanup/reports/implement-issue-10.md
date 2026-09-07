# Implement issue 10 — `/implement` is the entry

Ditched [[.cursor/rules/testing-workflow.mdc]]. Ticket-build entry is [[.agents/skills/implement/SKILL.md]]. tdd, implement-fsharp-feature, and add-shared-test stay and point at that entry. They do not copy a second red-green loop or a stop-for-review rule. The Client compile gate lives in the F# skill. tdd is not deleted. Product F# was not touched.

## What changed

- Deleted [[.cursor/rules/testing-workflow.mdc]] (stop-for-review always-apply / glob rule).
- [[.cursor/rules/gambol.mdc]]: dropped the testing-workflow index line; dropped the update-matt-skills index line (so ticket 04 need not touch this file). implement-fsharp-feature blurb now says F# layout and Client compile gate.
- [[.agents/skills/implement/SKILL.md]] is the entry: keep going; reference tdd, the F# skill, and add-shared-test; do not copy the loop.
- [[.agents/skills/tdd/SKILL.md]] names `/implement` as the ticket-build entry and says it does not stop for review after each color.
- [[.agents/skills/implement-fsharp-feature/SKILL.md]] no longer follows testing-workflow. It owns the Client compile gate (`./scripts/client.sh build` after Client-dependency edits).
- [[.agents/skills/add-shared-test/SKILL.md]] augments `/implement` and runs tests per the F# skill.
- Ticket [[plan/skills-cleanup/issues/10-implement-entry-ditch-testing-workflow.md]] is `Status: done`.

## How verified

- `test -e .cursor/rules/testing-workflow.mdc` is absent.
- [[.cursor/rules/gambol.mdc]] has no `testing-workflow`, `update-matt-skills`, or `setup-matt`.
- Owned skills do not say write-test-then-stop-for-review.
- [[.agents/skills/implement-fsharp-feature/SKILL.md]] still names `./scripts/client.sh build` and the Client-dependency trigger.
- [[.agents/skills/tdd/SKILL.md]] still exists.

## Leftover risks

- [[.cursor/rules/core-agent-behavior.mdc]] still points at the deleted testing-workflow file for the Client compile gate. Out of scope.
- [[.agents/skills/investigate-fable-client/SKILL.md]] still follows testing-workflow. Out of scope.
- Leftover copies under [[.cursor/skills/]] still name testing-workflow (implement-fsharp-feature, add-shared-test, investigate-fable-client). Out of scope.
- [[.cursor/rules/gambol.mdc]] does not list `/implement` or tdd. Ticket 12 owns Ask-Matt and gambol.mdc agreement.
- tdd still says ADR. Ticket 09 owns Committed Decision wording.
- Historical plan reports and other project docs still name testing-workflow.
