# Chapter: Send to and from GitHub

**Part of:** [[plan/roadmap/epics/work-with-text-files-from-anywhere.md]]
**Blocked by:** [[automatic-upload-and-download.md]].

## Context

A person works with documents from any connected device. Mapping a desktop folder to a Workspace Node in the App is Current.

## Goal

The person sends work to GitHub and brings work from GitHub. The Workspace stays consistent with that GitHub repo. The person works in the App and in the Browser.

## Required for done

- [ ] [[plan/github-transport/project.md]] — send to and from GitHub (lock workspace, receive files, inform Core)
- [ ] [[plan/core-refinement/project.md]] — Core works through changes after files land

## Notes

Keep files current with the Server stays on [[automatic-upload-and-download.md]] ([[doc/current/workspace-file-sync.md]]). Mapping is Current ([[doc/current/workspace-local-mapping.md]]). Transit detail lives on [[plan/github-transport/project.md]]. Core handoff after files land lives on [[plan/core-refinement/project.md]]. [[plan/roadmap/epics/agent-chat-managed-context.md]] depends on this Chapter. That Epic does not own it.

- **After files land** — Transport informs Core; Core works through changes ([02 — Git Load: Unparsed then Parse stack](plan/core-refinement/issues/02-git-load-unparsed-then-parse-stack.md)). Detail: [[plan/core-refinement/project.md]]; Graph↔Browser residency stays [[plan/browser-residency/project.md]].
