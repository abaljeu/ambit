# Chapter: Incremental operations

**Part of:** [[plan/roadmap/epics/robust-outliner.md]]
**Blocked by:** [[actors-supported.md]].

## Context

The Epic aims at incremental work: send a modest amount, then send more. Upload of a Workspace and Load of a Workspace into the Browser are not incremental. A hang then forces a full redo.

## Goal

Upload and Load send a modest amount, then more. A hang can abort mid-stream without a full redo.

## Required for done

- [ ] [[plan/parse-load-demote/project.md]]
- [ ] Upload of a Workspace and Load of a Workspace into the Browser are incremental.

## Notes

Full Workspace Upload/Load redesign stays parked relative to the Solid core bar until the Core and Actors Chapters are met.
