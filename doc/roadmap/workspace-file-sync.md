# Workspace file sync

Category: Sync
Status: Partial
See also: [[plan/transport-layer/project.md]], [[plan/github-transport/project.md]], [[plan/roadmap/epics/chapters/automatic-upload-and-download.md]], [[plan/roadmap/epics/chapters/ambit-keeps-consistency-with-desktop-repo-for-agentic-work.md]], [[doc/current/workspace-local-mapping]], [[doc/current/desktop-local-files]]

Leftover pointer. Two Chapter homes own the work. This file does not hold transport locks.

## What it gives you

1. **Sync the folder with the Server** — A person maps a desktop folder to a Workspace. Upload and Download keep that folder consistent with the Server. The person works in the App and in the Browser.
2. **Send to and from GitHub** — When that Workspace maps to an external GitHub repo, the person sends work to GitHub and brings work from GitHub.

This does not replace live graph editing ([[doc/current/sync-mvp]]). File sync is a separate, explicit tree sync.

## Homes

| What the person wants | Chapter (objective) | Detail home |
| --- | --- | --- |
| Sync folder with Server (Upload / Download) | [[plan/roadmap/epics/chapters/automatic-upload-and-download.md]] | File channel on [[plan/transport-layer/project.md]]; [[plan/auto-download-persisted-files/project.md]] |
| Send to and from GitHub | [[plan/roadmap/epics/chapters/ambit-keeps-consistency-with-desktop-repo-for-agentic-work.md]] | [[plan/github-transport/project.md]] |

## File-channel leftover docs

Shipped WebDAV and App-folder detail stays on these leftovers and current baselines. Do not grow this file back into a dump.

- Server DAV surface: [[workspace-webdav]]
- Desktop Upload stubs, caps, mtime skip: [[workspace-upload-client-structure]]
- Mapping: [[doc/current/workspace-local-mapping]]
- App proxy / Upload / Download commands: [[doc/current/desktop-local-files]]
- Auto-download: [[plan/auto-download-persisted-files/project.md]]
- Checklist remainder: [[workspaces-checklist]]
