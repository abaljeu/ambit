# 27 — Stop the old git path

**Status:** `defined`
**Type:** coding
**Blocked by:** 26 — Call mailbox git requests
**Trackers:** architecture, user manual

## Context

Callers use the mailbox git requests. The old git path can still run.

## What to build

The old git path stops. Git requests go through the mailbox. Core performs that work.

### Stop

1. [ ] **Old path stops** — The old git path stops.
2. [ ] **Mailbox remains** — Git requests remain mailbox requests.
3. [ ] **Core performs the work** — Core performs that git work.

### Obligations

1. [ ] **Core claim** — The Core claim shows the old git path as stopped.
2. [ ] **Absence test** — A test or a repo search shows callers do not use the old git path.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement map](plan/core-refinement/map.md)
- [core-refinement](plan/core-refinement/project.md)
- [26 — Call mailbox git requests](plan/core-refinement/issues/26-call-mailbox-git-requests.md)
- [Core](doc/current/core.md)
