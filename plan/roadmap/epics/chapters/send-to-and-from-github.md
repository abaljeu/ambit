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

- **Parse after Git Load** — Git Load: Unparsed on Workspace → pull → push Parse ([17 — Git Load: Unparsed then Parse stack](plan/github-transport/issues/17-post-pull-cascade-and-gate-handoff.md)). Workspace/directory Parse is immediate members only. Persist is Core’s async stack (graph → disk), not a directory-reconcile worker. Detail: [[plan/github-transport/project.md]]; Graph↔Browser residency stays [[plan/browser-residency/project.md]].
