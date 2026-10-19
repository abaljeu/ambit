# 05 — Download versus new tab

**Status:** `needs-info`
**Type:** grilling
**Blocked by:** None — can start immediately
**Trackers:** architecture, project spec

## Context

Open on a File node always opens a new tab. On iPhone Safari, a PDF or image can display in that tab, and another type can show a download sheet. Alan deferred the choice between downloading and opening in a new tab. This effort does not make that choice.

## What to build

1. [ ] **Record the choice** — Alan decides whether Open stays a new tab for every file type, or whether some types download.
2. [ ] **Both surfaces** — The decision covers iPhone Safari and desktop browsers.

### Acceptance Criteria

1. [ ] **Written decision** — [File node Open](plan/file-node-open/spec.md) and [File node Open architecture](plan/file-node-open/arch.md) state the choice.
2. [ ] **No code until the choice** — This ticket does not change Open until that decision is recorded.

## Finish-up

1. [ ] **Piece answers** — Changes during implementation are written on this ticket, or the ticket records that none changed.
2. [ ] **Basis and reference docs** — The ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
3. [ ] **Current and user docs** — Where those sources reference current docs or user docs, update those referents to match the finished state of this ticket. Delete statements that are no longer true. Check off statements that are newly achieved.

## See also

1. [01 — File node: Open button opens the file in the browser](01-open-file-in-browser.md)
2. [06 — File types the browser cannot render on mobile](06-file-types-the-browser-cannot-render-on-mobile.md)
3. [File node Open architecture](plan/file-node-open/arch.md)
4. [File node Open](plan/file-node-open/spec.md)
