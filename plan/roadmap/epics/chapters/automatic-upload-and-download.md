# Chapter: Automatic upload and download

**Part of:** [[plan/roadmap/epics/work-with-text-files-from-anywhere.md]]
**Blocked by:** None.

## Context

A person explores and works with their documents from any connected device, not only at one desk. The Browser or the App talks to the same Server. In the App a document on disk is a File Node.

## Goal

Keep the person's files current on the App and in the Browser. Upload and Download move files between the App folder and the Server.

## Required for done

- [ ] [[plan/transport-layer/project.md]] — chart Workspace file-channel redesign as FETCH / UPDATE Actors (owns the redesign chart; Parse/Persist primitive)
- [ ] [[plan/auto-download-persisted-files/project.md]] — auto-download (HITL tabled)
- [ ] [[plan/roadmap/issues/07-chart-automatic-upload-and-download.md]] — chart auto-upload implementation Project (no Project yet)

## Notes

This Chapter is keep-files-current. Implemented tree sync is [[doc/current/workspace-file-sync.md]]. Send to and from GitHub is [[send-to-and-from-github.md]]. Pattern home: [[plan/transport-layer/overview.md]]. File-shaped file→Graph stays [[plan/parse-actor/project.md]]. Want-driven Graph→Browser stays [[plan/browser-residency/project.md]].
