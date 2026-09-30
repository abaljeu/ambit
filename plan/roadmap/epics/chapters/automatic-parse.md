# Chapter: Automatic parse

**Part of:** [[plan/roadmap/epics/work-with-text-files-from-anywhere.md]]
**Blocked by:** None.

## Context

A person works with documents from any connected device. File Nodes can be Unparsed.

## Goal

Unparsed File Nodes parse without a separate Parse command. A continuous Server Parse thread turns file-shaped disk into Graph, takes priority from Browser wants, and emits Changes.

## Required for done

- [ ] [[plan/parse-thread/project.md]] — continuous Server Parse thread; file-shaped disk→Graph; priority from Browser wants; emits Changes
- [ ] Unparsed File Nodes parse without a separate Parse command.

## Notes

Workspace Download and Workspace Upload do not Parse on the Browser or the App. File transit stays on [[plan/transport-layer/project.md]]. Browser wants come from [[plan/browser-residency/project.md]]. See [[incremental-operations.md]].
