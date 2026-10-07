# Chapter: Automatic upload and download

**Part of:** [[plan/roadmap/epics/work-with-text-files-from-anywhere.md]]
**Blocked by:** None.

## Context

A person explores and works with their documents from any connected device, not only at one desk. The Browser or the App talks to the same Server. In the App a document on disk is a File Node.

## Goal

Keep the person's files current on the App and in the Browser. Upload and Download move files between the App folder and Server DataDir.

## Required for done

- [ ] [[plan/transport-layer/project.md]] — chart the Desktop App sync redesign: FETCH (Unparsed → pull → push onto the one Parse thread → Changes → done) and UPDATE (graph edit → Unpersisted → Core Persist stack → push → done). Keep-files-current outbound is UPDATE = Persist (Graph → text) then push. Auto-upload is not a separate Project. GitHub stays a separate Chapter. Persist is Core async, not an Actor. Parse home stays [[plan/parse-thread/project.md]]. Further Desktop App sync implementation is the Projects this chart names.
- [ ] [[plan/auto-download-persisted-files/project.md]] — auto-download (HITL tabled)

Auto-upload as a sibling Project is withdrawn (Alan, 2026-10-07). Remaining outbound work is the transport-layer UPDATE chart above, plus the Desktop App sync implementation Projects that chart names.

## Notes

This Chapter is keep-files-current. Implemented tree sync is [[doc/current/workspace-file-sync.md]] (Upload and Download between the App folder and Server DataDir). Send to and from GitHub is [[send-to-and-from-github.md]]. Pattern home: [[plan/transport-layer/overview.md]]. File-shaped file→Graph stays [[plan/parse-thread/project.md]]. Want-driven Graph→Browser stays [[plan/browser-residency/project.md]]. 2026-10-07: [07 — Chart Automatic upload and download onto Projects](plan/roadmap/issues/07-chart-automatic-upload-and-download.md) is done. It does not chart an auto-upload Project.
