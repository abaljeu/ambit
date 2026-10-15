# 02 — SVG served as image/svg+xml can run script

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture, project spec

## Context

A person runs Open on an SVG File node inside a named Workspace. The Server sends that file as `image/svg+xml`. The same GET sends `.html` and `.htm` as plain text so a Workspace file cannot run script on the Ambit origin. A browser can run script in an SVG document that uses `image/svg+xml` from that origin.

The intended long-term answer is the sandboxed web panel in Two-panel view. A `Content-Security-Policy: sandbox` response header on DAV GET is a nearer-term server option.

## What to build

1. [ ] **SVG does not run script** — Open on an SVG File node still shows the image when the browser can show an SVG. Script in that file does not run on the Ambit origin.
2. [ ] **Other image types stay** — PDF, PNG, JPEG, GIF, WebP, and HEIC keep the content types they have now.

### Acceptance Criteria

1. [ ] **SVG response** — `GET` of an `.svg` Workspace file does not execute script on the Ambit origin.
2. [ ] **Display** — The browser can still show the SVG when it can show an image.
3. [ ] **HTML row stays here** — `.html` and `.htm` stay plain text in this ticket. Native HTML display is [03 — Native display of HTML](03-native-display-of-html.md).
4. [ ] **Tests** — A Server test covers the SVG response.

## Finish-up

1. [ ] **Piece answers** — Changes during implementation are written on this ticket, or the ticket records that none changed.
2. [ ] **Basis and reference docs** — The ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
3. [ ] **Current and user docs** — Where those sources reference current docs or user docs, update those referents to match the finished state of this ticket. Delete statements that are no longer true. Check off statements that are newly achieved.

## See also

1. [01 — File node: Open button opens the file in the browser](01-open-file-in-browser.md)
2. [03 — Native display of HTML](03-native-display-of-html.md)
3. [File node Open architecture](plan/file-node-open/arch.md)
4. [File node Open](plan/file-node-open/spec.md)
5. [workspace file sync](doc/current/workspace-file-sync.md)
6. [Two-panel view](doc/roadmap/two-panel-view.md)
