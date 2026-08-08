---
name: project-work
description: Git protocol for any .scratch project — start on a w/* project branch, record the cut-from branch, commit only approved work, offer to merge back when done. Use before editing a project's files, when starting a .scratch effort, or when another skill touches .scratch.
---

# Project work

Policy and tool allowlist: [[.cursor/rules/environment.mdc]]. Stages: [[docs/agents/project-status.md]].

Every `.scratch/<slug>/` effort runs on a **project branch** (`w/*`). Do the branch step before editing any of its files.

## 1. Start on a project branch

Read the current branch. If it already matches `w/*`, stay. Otherwise create and checkout `w/<slug>` once from HEAD.

Completion: `git rev-parse --abbrev-ref HEAD` matches `w/*`.

## 2. Record the cut-from

Write the project's `.scratch/<slug>/git.md`:

```text
# <name> — git

- **Project branch:** `w/<slug>`
- **Cut from:** `<branch this was branched off>`
- **Notes:** <one line>
```

Completion: `git.md` names the project branch and the cut-from branch.

## 3. Work

Edit freely on the project branch. Never switch back to the cut-from branch, never touch main/master, run no remote ops — each is manual-approval per [[.cursor/rules/environment.mdc]].

## 4. Commit only approved work

Commit as **agent-done**: tests green, `/code-review`, then `git commit` — only on `w/*`, and only the changes the user approved. Not on `w/*`? Leave the tree dirty and suggest a message.

Completion: HEAD carries only approved changes, or a suggested message is offered.

## 5. Offer to merge back

When the project reaches `done`, suggest merging the project branch back into its cut-from. Do not merge without the user's go-ahead (manual approval).

Completion: a merge back to the cut-from branch is offered.
