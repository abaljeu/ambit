# 03 — Native display of HTML

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture, project spec

## Context

A person runs Open on an HTML File node and expects the browser to show the page. The spec story Native display asks for the file's real content type. The architecture `.html` / `.htm` row sends `text/plain; charset=utf-8` so a Workspace file cannot run script on the Ambit origin. The browser then shows the HTML source.

The intended long-term answer is the sandboxed web panel in Chapter Two-panel view. A `Content-Security-Policy: sandbox` response header on DAV GET is a nearer-term server option.

## What to build

1. [ ] **HTML displays as HTML** — Open on an `.html` or `.htm` File node shows the markup as a page when the browser can show HTML.
2. [ ] **Script stays off** — Script in that Workspace file does not run on the Ambit origin.
3. [ ] **Other types stay** — The rest of the content-type table stays as built in [01 — File node: Open button opens the file in the browser](01-open-file-in-browser.md).

### Acceptance Criteria

1. [ ] **Real HTML display** — The GET response lets the browser render `.html` and `.htm` as HTML.
2. [ ] **No script on this origin** — That response does not let the file run script on the Ambit origin.
3. [ ] **Piece answer records the mechanism** — The implementation records how display and the script limit are both true.
4. [ ] **Tests** — A Server test covers the `.html` / `.htm` response.
5. [ ] **SVG stays in its ticket** — SVG script is [02 — SVG served as image/svg+xml can run script](02-svg-served-as-image-can-run-script.md).

## Finish-up

1. [ ] **Piece answers** — Changes during implementation are written on this ticket, or the ticket records that none changed.
2. [ ] **Basis and reference docs** — The ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
3. [ ] **Current and user docs** — Where those sources reference current docs or user docs, update those referents to match the finished state of this ticket. Delete statements that are no longer true. Check off statements that are newly achieved.

## See also

1. [01 — File node: Open button opens the file in the browser](01-open-file-in-browser.md)
2. [02 — SVG served as image/svg+xml can run script](02-svg-served-as-image-can-run-script.md)
3. [File node Open architecture](plan/file-node-open/arch.md)
4. [File node Open](plan/file-node-open/spec.md)
5. [workspace file sync](doc/current/workspace-file-sync.md)
6. [Two-panel view](plan/roadmap/epics/chapters/two-panel-view.md)
