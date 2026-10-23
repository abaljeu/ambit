# Build context

## 1. Work

The work is the source the ticket names. Change that source when the build step is about to be worked.

## 2. Build template

The ticket is the template. Read the ticket and the source it names.

## 3. Fill

1. **Tests.** Follow [[../.agents/skills/tdd/SKILL.md]]. Write or change the tests the ticket names. Run `scripts/agent-test.sh --filter <names>` and see the new tests fail.
2. **Changes.** Make the changes the ticket names, in the files, types, and functions the ticket names.
3. **Focused tests.** Run `scripts/agent-test.sh --filter <names>` again. Commit and push only when those tests pass.

Done:

- those changes exist in that source
- the named tests pass in a focused run
