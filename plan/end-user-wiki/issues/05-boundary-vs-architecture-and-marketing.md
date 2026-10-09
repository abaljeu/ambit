# Boundary vs Architecture and Marketing wiki

**Type:** grilling
**Status:** done
Blocked by:

## Question

What is in the end-user wiki vs [[plan/architecture/map.md]] (internals) vs [[plan/marketing-wiki/map.md]] (uses)? Draw the boundary. Do not write pages in this ticket.

## Comments

- 2026-09-02: Filed unclaimed from WORK.md. Map: [[../map.md]].
- 2026-10-09 — Answer recorded from Alan. The marketing boundary stays open on [06 — Boundary vs marketing wiki](06-boundary-vs-marketing-wiki.md).

## Answer

Alan, 2026-10-09.

The end-user wiki is how to operate Gambol. It uses concepts as the user sees them, and tasks in the App and in the Browser.

Plan and architecture are how Gambol is built and run. That covers Actors, the event source, the HTTP contract, and storage.

User pages may link to architecture pages. User pages must not depend on architecture pages.

The boundary versus the marketing wiki is not decided. That question is [06 — Boundary vs marketing wiki](06-boundary-vs-marketing-wiki.md).
