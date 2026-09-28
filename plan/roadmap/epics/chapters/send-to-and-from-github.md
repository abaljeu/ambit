# Chapter: Send to and from GitHub

**Part of:** [[plan/roadmap/epics/work-with-text-files-from-anywhere.md]]
**Blocked by:** [[automatic-upload-and-download.md]].

## Context

A person works with documents from any connected device. Mapping a desktop folder to a Workspace Node in the App is Current.

## Goal

The person sends work to GitHub and brings work from GitHub. The Workspace stays consistent with that GitHub repo. The person works in the App and in the Browser.

## Required for done

- [ ] [[plan/github-transport/project.md]] — send to and from GitHub

## Notes

Keep files current with the Server stays on [[automatic-upload-and-download.md]] ([[doc/current/workspace-file-sync.md]]). Mapping is Current ([[doc/current/workspace-local-mapping.md]]). Detail lives on [[plan/github-transport/project.md]]. [[plan/roadmap/epics/agent-chat-managed-context.md]] depends on this Chapter. That Epic does not own it.

- **Directory reconciliation** — Keep three surfaces reconciled: the local working directory, the remote GitHub copy, and the Graph. Added, removed, and moved files are reflected between the local directory and GitHub, and between the local directory and the Graph (directory changes appear in the Graph; Graph changes that affect files appear back in the directory). Detail: [[plan/github-transport/project.md]]; Graph↔Browser residency stays [[plan/browser-residency/project.md]]. Post-pull fine locks: [17 — Post-pull cascade and gate handoff](plan/github-transport/issues/17-post-pull-cascade-and-gate-handoff.md).
