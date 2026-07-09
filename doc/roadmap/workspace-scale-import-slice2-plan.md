# Workspace Scale Import — Slice 2 Plan

Status: In progress
Category: Workspace scale
See also: [[doc/roadmap/workspace-scale-import]], [[doc/roadmap/workspace-scale-import-slice1-plan]], [[doc/roadmap/git-sync-gateway]], [[doc/current/workspace-local-mapping]], [[doc/current/desktop-local-files]]

This document locks Slice 2: desktop folder open for git-backed workspaces, git gateway pull/push, and graph follow-up via sync-tree + poll. Parent overview: [[doc/roadmap/workspace-scale-import]].

## What it gives you

1. **Open workspace folder…** — native folder picker; accept paths that are or contain `.git`.
2. **Connect wizard** — label, create/link cloud workspace, initial download or upload, `ambit` remote URL.
3. **Download / Upload** — `git pull ambit` / `git push ambit` via server smart HTTP gateway.
4. **Graph catch-up** — after push, sync-tree + existing poll delivers new File/Directory stubs.
5. **Stale after pull** — changed local paths mark parsed file nodes stale for reparse.

Prerequisite: Slice 1 (tree sync, autosave, per-workspace `.git` under `@label/`).

## Two layers

| Layer | Mechanism |
| --- | --- |
| Files | `git pull ambit` / `git push ambit` via gateway |
| Graph | `POST /ambit/changes` + `GET /ambit/poll`; sync-tree after push |

Git push runs **receive-pack** on server (not upload-pack). Upload-pack is server→client during pull.

## Remote name

Gambol uses remote **`ambit`** (never `origin`). User folders may already have `origin` for GitHub etc.

## Desktop endpoints

| Method | Path | Role |
| --- | --- | --- |
| POST | `/_desktop/pick-folder` | WPF folder picker; returns `{ path, gitRoot }` |
| GET | `/_desktop/workspace-mappings` | Read `config.json` mappings |
| PUT | `/_desktop/workspace-mappings` | Write mappings; hot-reload in proxy |
| POST | `/_desktop/git-pull` | `{ label }` → `git pull ambit`; returns `{ ok, changedPaths }` |
| POST | `/_desktop/git-push` | `{ label }` → `git push ambit`; returns `{ ok, detail? }` |
| POST | `/_desktop/git-remote-setup` | `{ label, url }` → `git remote add/set-url ambit` |

## Server git gateway

Smart HTTP under `/ambit/git/@{label}.git/`:

- `info/refs?service=git-upload-pack` — JIT commit first, then advertise refs
- `git-upload-pack` — fetch/pull
- `git-receive-pack` — push (FF-only, clean work tree)

Auth: same session cookie as `/ambit/*` (v0).

## Client commands

- **Open workspace folder…** — desktop only; opens connect wizard overlay
- **Download workspace** — async pull + mark stale on `changedPaths`
- **Upload workspace** — async push + sync-tree on workspace + poll

## Async flow (Upload)

1. Client → `POST /_desktop/git-push`
2. Desktop → `git push ambit`
3. Client → sync-tree on workspace node
4. Poll → change tail with new nodes

## Async flow (Download)

1. Client → `POST /_desktop/git-pull`
2. Desktop → `git pull ambit`
3. Client marks matching file nodes stale (no server graph change)

## Startup registration (§4b)

On login, for each local mapping label absent from server graph: create workspace node via change batch.

## Tests

| Case | Proves |
| --- | --- |
| GitRepoDetect: folder with `.git` | Returns repo root |
| GitRepoDetect: path is `.git` | Returns parent |
| GitRepoDetect: no git | Error |
| WorkspaceGitRemote URL | Builds gateway URL from base + label |
| planMarkStaleAfterPull | Changed paths → SetFileState ops |
| Gateway JIT commit | Dirty tree committed before upload-pack |
| Gateway push dirty | receive-pack rejected |

## Success criterion

Open folder → connect with label → pull or push → edit via Slice 1 → push updates server graph via sync-tree → pull marks stale → restart preserves mapping.
