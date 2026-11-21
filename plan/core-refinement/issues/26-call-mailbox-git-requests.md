# 26 — Call mailbox git requests

**Status:** `defined`
**Type:** coding
**Blocked by:** 25 — Enable mailbox git requests
**Trackers:** architecture, user manual

## Context

The mailbox accepts git requests. Callers can still use the old git path.

## What to build

Callers use the mailbox git requests. Core performs that work. The old path remains until the stop beat.

### Call

1. [ ] **Callers use the requests** — Callers send git work through the mailbox git requests.
2. [ ] **Core performs the work** — Core performs that git work in the background.
3. [ ] **Old path remains** — The old git path remains until the stop beat.

### Obligations

1. [ ] **Core claim** — The Core claim shows callers using the mailbox git requests.
2. [ ] **Caller test** — A test shows a caller uses a mailbox git request.

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement map](plan/core-refinement/map.md)
- [core-refinement](plan/core-refinement/project.md)
- [25 — Enable mailbox git requests](plan/core-refinement/issues/25-enable-mailbox-git-requests.md)
- [Core](doc/current/core.md)
