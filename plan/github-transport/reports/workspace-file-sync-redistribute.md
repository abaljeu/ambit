# workspace-file-sync redistribute

Date: 2026-09-26
Source: staging `doc/roadmap/workspace-file-sync.md` (deleted). Homes after Alan’s redistribute lock.

## New current home

Implemented WebDAV / Upload / Download / ignore-inventory / prepare-push / finish-commit / download-manager / sync-ledger: [[doc/current/workspace-file-sync.md]].

Index: [[doc/index.md]] **Workspace file sync**. Pointers: [[plan/transport-layer/project.md]], [[plan/roadmap/epics/chapters/automatic-upload-and-download.md]].

## Former leftover sections

| Staging leftover section | Home |
| --- | --- |
| What it gives you — Upload / Download / command surface | [[doc/current/workspace-file-sync.md]] §3, §4, §7 |
| What it gives you — client-first stubs / `∅` / Parse | leftover [[doc/roadmap/workspace-upload-client-structure.md]]; recap in current §3 |
| What it gives you — never transfer `.git/` / ignored paths | current §2 |
| What it gives you — external git / hard-skip `.amb` | [[plan/github-transport/map.md]] Decision 5 — now **optional / config-driven**, not a hard default |
| Inventory (defined) | current §2 |
| Desktop sync functions (Post / Get) | current §3, §4 |
| What it avoids for now | current §9 as not-implemented (not a product exclusion) |
| Decision table (transport, ignore SoT, mapping, overwrite, auth) | current §1, §2, §5; mapping [[doc/current/workspace-local-mapping.md]] |
| How `.gitignore` is followed | current §2 (as-built: missing git still walks and skips `.git/`; does not fail Upload) |
| WebDAV subset (v1) | leftover [[doc/roadmap/workspace-webdav.md]]; current §1 cites it |
| Command surface | current §7; endpoints [[doc/current/desktop-local-files.md]] |
| Upload limits | current §3; locked planner detail leftover [[doc/roadmap/workspace-upload-client-structure.md]] |
| Download is unlimited | current §4 |
| Download manager | current §4; code `WorkspaceDownloadManager` / `WorkspaceDownloadQueue` |
| Auto-download on persist | current §6; HITL tabled [[plan/auto-download-persisted-files/project.md]] |
| Sync ledger + mtime skip | current §5 |
| Minimal API / ops (prepare-push, finish-commit, sequence) | current §1, §3, §4 |
| On-disk layout | current §8 |
| Status vs code / still open | current §9 |
| Tests / Success criteria | not promoted; tests stay in Shared / Server |

## `.amb` lock

github-transport Destination and Decision 5: skip Directory File `.amb` on the GitHub remote only when repo configuration says so. Backup via WebDAV / DataDir when excluded. WebDAV of `.amb` stays Ambit↔Ambit.

## Left as-is

Write-once reports that still name the deleted leftover. Prior spec [[plan/workspace-git/project.md]] (not this home). Agent chat Epic stays a pointer only.
