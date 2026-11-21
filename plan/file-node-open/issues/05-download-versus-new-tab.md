# 05 — Download versus new tab

**Status:** `needs-info`
**Type:** grilling
**Actual:** 2h
**Blocked by:** None — can start immediately
**Trackers:** architecture, project spec

## Context

Open on a File node always opens a new tab. On iPhone Safari, a PDF or image can display in that tab, and a binary type can show a download sheet. On 2026-10-20 Alan decided that a text file shows in the tab and is never downloaded. That decision covers a desktop browser and iPhone Safari. The choice for a binary type stays open.

## What to build

1. [x] **Text files show in the tab** — Open shows a text file in the tab. Open never downloads a text file. The decision covers a desktop browser and iPhone Safari.
2. [ ] **Binary choice** — Alan has not decided whether a binary type downloads or opens in the tab.

### Acceptance Criteria

1. [x] **Written text decision** — [File node Open](plan/file-node-open/spec.md) and [File node Open architecture](plan/file-node-open/arch.md) state that a text file shows in the tab and is never downloaded.
2. [ ] **Written binary choice** — Those files do not yet state a choice for a binary type.
3. [x] **Text response** — A text file is `text/plain; charset=utf-8` with inline disposition. PDF, images, HTML, and SVG stay as they are. A binary file still downloads.

## Amendments

1. **Text decision** — Alan chose display for every text file. The Server decides text from the first 8192 bytes: no NUL byte, and valid UTF-8. An incomplete UTF-8 sequence at the end of that window is text when the file continues.
2. **Inline disposition** — A text response sends `Content-Disposition: inline` and no file name, so a desktop browser and iPhone Safari show the file.
3. **Kept types** — PDF and images keep their media types. `.html` and `.htm` stay plain text. `.svg` stays `image/svg+xml`.
4. **Binary choice stays open** — This ticket does not choose download versus open for a binary type.

## Finish-up

1. [ ] **Piece answers** — Changes during implementation are written on this ticket, or the ticket records that none changed.
2. [ ] **Basis and reference docs** — The ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
3. [ ] **Current and user docs** — Where those sources reference current docs or user docs, update those referents to match the finished state of this ticket. Delete statements that are no longer true. Check off statements that are newly achieved.

## See also

1. [01 — File node: Open button opens the file in the browser](01-open-file-in-browser.md)
2. [06 — File types the browser cannot render on mobile](06-file-types-the-browser-cannot-render-on-mobile.md)
3. [File node Open architecture](plan/file-node-open/arch.md)
4. [File node Open](plan/file-node-open/spec.md)

## Time

- 2026-10-20 2h — text file response and the recorded decision (from chat)
