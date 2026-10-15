# 06 — File types the browser cannot render on mobile

**Status:** `needs-info`
**Type:** grilling
**Blocked by:** None — can start immediately
**Trackers:** architecture, project spec

## Context

Open assumes the browser shows every file type usefully. On iPhone Safari, a type such as an Office document can fail to render and can show a download sheet instead. Alan deferred how those types should behave on mobile. This effort does not add per-type handling.

## What to build

1. [ ] **Record the handling** — Alan decides what Open does for a file type the mobile browser cannot render.
2. [ ] **Known case** — The decision covers Office documents on a phone, and it states whether other non-rendering types share that handling.

### Acceptance Criteria

1. [ ] **Written decision** — [File node Open](plan/file-node-open/spec.md) and [File node Open architecture](plan/file-node-open/arch.md) state the handling.
2. [ ] **No code until the decision** — This ticket does not add per-type handling until that decision is recorded.

## Finish-up

1. [ ] **Piece answers** — Changes during implementation are written on this ticket, or the ticket records that none changed.
2. [ ] **Basis and reference docs** — The ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
3. [ ] **Current and user docs** — Where those sources reference current docs or user docs, update those referents to match the finished state of this ticket. Delete statements that are no longer true. Check off statements that are newly achieved.

## See also

1. [01 — File node: Open button opens the file in the browser](01-open-file-in-browser.md)
2. [05 — Download versus new tab](05-download-versus-new-tab.md)
3. [File node Open architecture](plan/file-node-open/arch.md)
4. [File node Open](plan/file-node-open/spec.md)
