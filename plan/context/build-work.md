# Build work

## 1. Work

The work is the files Inputs names. Change those files when the build step is about to be worked.

Start with [Cursor scope](cursor-scope.md) in `implement` mode. Fill from the ticket and that report.

Scope:

- Build edits the files Outputs names, plus the ticket's status and checkboxes.
- A flaw found in any other file (GLOSSARY, spec, arch, `doc/`, `project.md`, another ticket) goes in the PR body under **Found elsewhere**. Each item is:
  - **Problem.** The file, the line, and the perceived flaw.
  - **Fix.** What to do.
  Review assesses each one.

## 2. Size stop

When the build would go over the [Size limit](size-limit.md), stop before committing. Report the files and tests it would change, and why each is needed. Continue only when the human explicitly approves that size.

## 3. Build template

The ticket is the template. Read the ticket and the files Inputs names.

## 4. Fill

1. **Process.** Follow [[.agents/skills/tdd/SKILL.md]]. Make the numbered edits, in the files Inputs names.
2. **Outputs.** Leave the files and the tests Outputs names.
3. **Check.** Run the ticket's Check. Commit and push only when that check passes.

Done:

- those edits exist in the files Inputs names
- the ticket's Check passes
- the ticket status is `coded`
