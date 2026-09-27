# 12 — Run the Workspace git tracked-branch round-trip

**Status:** defined
**Blocked by:** None — can start immediately

## Context

An operator has configured a git remote, current branch, upstream, and host credentials on a Workspace work tree. A person then uses git Load or git Save. Ambit needs one Server capability that uses that work tree's git facts, moves the whole tracked branch, and reports a useful rejection without storing a second branch map or a GitHub credential.

## What to build

### 1. WorkspaceGit

Extend **WorkspaceGit** as defined by the Module map in [[../arch.md]]. This capability supports the **Config in git**, **Same tracked branch**, **Fast-forward only**, **Host git credentials**, **Skip list is `.gitignore`**, **Workspace-scoped git**, **Reject UX**, and **Persist stays independent of git Save** Story paths.

- [ ] 3.1.2 Read work-tree git facts — Read remote existence and the current upstream from only the selected Workspace work tree.
- [ ] 3.2.2 Report remote existence — Return whether any `git remote` exists for the Workspace root.
- [ ] 3.2.3 Use the tracked branch — Derive the current branch and upstream from git without a Server branch map.
- [ ] 3.2.4 Pull the whole work tree — git Load pulls the current tracked branch for the whole Workspace and does not use a file pathspec.
- [ ] 3.2.5 Commit then push the whole work tree — git Save uses `GitSave.commitAll`, then pushes the same tracked branch, without a file pathspec.
- [ ] 3.2.5 Reject conflict and non-FF — A conflicted or non-fast-forward operation fails; a conflict error names at least one file path.
- [ ] 3.2.6 Honor `.gitignore` — The git round-trip uses normal `.gitignore` behavior and adds no Ambit skip key or hard `.amb` default.
- [ ] 3.2.7 Condense other git failures — Other failures return a short matching error that reflects the git report.
- [ ] 3.2.8 Keep the attached branch — The capability does not checkout, switch, or move to an older commit.
- [ ] 3.2.9 Use host credentials — The interface takes no GitHub credential; host git loads its configured credentials.
- [ ] 3.2.10 Keep Persist separate — git Save does not invoke, own, or replace Graph→file Persist.
- [ ] 3.3.1 Use the existing git host — The implementation uses WorkspaceGit, GitSave, and GitRun rather than another process host.
- [ ] 3.3.2 Leave credential loading to git — Ambit adds no appsettings, user-secrets, Graph, or DataDir credential store.
- [ ] 3.2.2 Prove remote facts — Temp-work-tree tests cover a remote that is present and absent.
- [ ] 3.2.4 Prove the round-trip — Temp-work-tree tests cover tracked-branch pull, commit-then-push, fast-forward acceptance, non-FF rejection, `.gitignore`, and condensed errors.

## See also

- [github-transport architecture](../arch.md)
- [10 — git Save is commit then push](10-git-save-commit-then-push.md)
