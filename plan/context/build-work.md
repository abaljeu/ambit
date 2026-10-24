# Build work

## 1. Work

The work is the source the ticket names. Change that source when the build step is about to be worked.

Scope:

- Build edits source and tests, plus the ticket's status and checkboxes.
- A flaw found in any other file (GLOSSARY, spec, arch, `doc/`, `project.md`, another ticket) goes in the PR body under **Found elsewhere**. Each item is:
  - **Problem.** The file, the line, and the perceived flaw.
  - **Fix.** What to do.
  Review assesses each one.

## 2. Build template

The ticket is the template. Read the ticket and the source it names.

## 3. Fill

1. **Tests.** Follow [[.agents/skills/tdd/SKILL.md]]. Write or change the tests the ticket names. 
2. **Changes.** Make the changes the ticket names, in the files, types, and functions the ticket names.
3. **Done checks.** Run the Done checks below. Commit and push only when those checks pass.

Done:

- those changes exist in that source
- Run `scripts/agent-test.sh` with no arguments.
- If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
- All executed tests pass.
- the ticket status is `coded`
