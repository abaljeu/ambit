# 25 — Enable mailbox git requests

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture, user manual

## Context

Actors post to the mailbox. Core does not yet accept git requests as mailbox work. Today's doors that already reach Core for Load and Save stay.

## What to build

The mailbox accepts git requests. Actors stay defined outside Core and post to the mailbox. Core performs that git work in the background and posts an end Event when one is needed.

### Enable

1. [ ] **Mailbox accepts git** — The mailbox accepts git requests.
2. [ ] **Actor posts** — An Actor stays outside Core and posts the request to the mailbox.
3. [ ] **Core performs the work** — Core performs that git work in the background. It posts an end Event when one is needed.
4. [ ] **Load and Save stay** — Today's doors that already reach Core for Load and Save stay.
5. [ ] **Beats stay on the architecture** — These three beats stay on the architecture note. They are not new claims on the current docs. The locked target keeps its claim homes.

### Obligations

1. [ ] **Core and Actors claims** — The Core claim and the Actors claim still say git use is Core and the Actor posts.
2. [ ] **Request test** — A test shows the mailbox accepts a git request and Core performs that work.

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement map](plan/core-refinement/map.md)
- [core-refinement](plan/core-refinement/project.md)
- [Core](doc/current/core.md)
- [Actors](doc/current/actors.md)
