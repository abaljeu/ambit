# 04 — Serve ROOT files outside a named Workspace

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture, project spec

## Context

Open on a File node inside a named Workspace opens the Server copy in a new tab. A File node that is not inside a named Workspace has no URL, so Open does nothing. That includes a File directly under ROOT and a File under a Directory that is not inside a named Workspace. The name can contain an extension, such as `name.ext`, or no extension, such as `README`. The Server already stores a root File at the data directory as that file's name.

## What to build

1. [ ] **Open the Server copy** — Open on a File node outside a named Workspace opens that Server file in a new tab.
2. [ ] **The name is not a Workspace label** — The URL does not treat the file name as a Workspace label. A Workspace whose name contains `.` still opens as it does in [01 — File node: Open button opens the file in the browser](01-open-file-in-browser.md).
3. [ ] **Same login and content type** — The tab uses the existing login. The response uses the same content-type table and has no forced download.
4. [ ] **Missing file** — A missing or ignored file is a 404. The Browser does not break.

### Acceptance Criteria

1. [ ] **URL** — `fileOpenUrl`, or the function that replaces it for this case, returns a URL for a ROOT File with an extension and for a ROOT File with no extension.
2. [ ] **Not a fake label** — That URL is not `/ambit/dav/{filename}` when `{filename}` is not a Workspace label.
3. [ ] **Named Workspace stays** — A File in a Workspace named `my.notes` still opens `/ambit/dav/my.notes/...`.
4. [ ] **Tests** — Shared tests cover the ROOT File URL. A Server test covers the GET.

## Finish-up

1. [ ] **Piece answers** — Changes during implementation are written on this ticket, or the ticket records that none changed.
2. [ ] **Basis and reference docs** — The ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
3. [ ] **Current and user docs** — Where those sources reference current docs or user docs, update those referents to match the finished state of this ticket. Delete statements that are no longer true. Check off statements that are newly achieved.

## See also

1. [01 — File node: Open button opens the file in the browser](01-open-file-in-browser.md)
2. [File node Open architecture](plan/file-node-open/arch.md)
3. [File node Open](plan/file-node-open/spec.md)
4. [workspace file sync](doc/current/workspace-file-sync.md)
