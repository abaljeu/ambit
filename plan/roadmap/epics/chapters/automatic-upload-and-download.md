# Chapter: Automatic upload and download

**Part of:** [[plan/roadmap/epics/work-with-text-files-from-anywhere.md]]
**Blocked by:** None.

## Context

A person explores and works with their documents from any connected device, not only at one desk. The Browser or the App talks to the same Server. In the App a document on disk is a File Node.

Workspace sync today is half in the transport Actor scheme: Parse File is already an Actor; byte move (DAV / prepare-push / Client `UpdateWorkspace*`) is still a special Client↔HTTP path. The high-level redesign folds pull/parse and persist/push into FETCH and UPDATE Actors (ORGANIZE stays Graph authority).

## Goal

Replace today's manual Upload, Download, and Load for that path so files stay current on the App and in the Browser — through Workspace **FETCH** (pull → Parse → Changes) and **UPDATE** (Persist → push) Actors, not a parallel sync writer.

## Required for done

- [ ] [[plan/transport-layer/project.md]] — chart Workspace file-channel redesign as FETCH / UPDATE Actors (owns the redesign chart; Parse/Persist primitive)
- [ ] [[plan/auto-download-persisted-files/project.md]] — auto-download (HITL tabled)
- [ ] [[plan/roadmap/issues/07-chart-automatic-upload-and-download.md]] — chart auto-upload implementation Project (no Project yet)

## Notes

- Pattern home: [[plan/transport-layer/overview.md]], [[plan/transport-layer/details/parse-persist.md]]. Implementation legs stay file-channel Projects; transport-layer does not implement DAV.
- Next Chapter [[automatic-parse.md]] stays Unparsed-without-a-separate-Parse-command; FETCH may compose pull + Parse, but automatic Parse UX is that beat.
- Locked 2026-09-19: fold Actor-shaped Workspace sync into this Chapter; transport-layer owns the redesign chart (not a new Chapter).
