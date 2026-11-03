# 06 — Server sends each Node with its child list build

Ticket: [06 — Server sends each Node with its child list](06-server-sends-child-list-ticket.md)

## Decisions

[ ] Alan on 2026-11-01 adds `tests/Server.Tests/LoadSaveCommandClientTests.fs` to Inputs so `gitResponse` carries the child list.

[ ] Alan on 2026-11-01 names two exceptions in `tests/Shared.Tests/WantTests.fs`. Those tests expect the child list.

- ``visibleClosureGraph loads reserved Children and Zoom ancestors``
- ``visibleClosureGraph bad Zoom stays inside reserved set``

## Stop

[ ] The first stop record stays [06 — Server sends each Node with its child list stop](plan/client-never-fails/reports/06-server-sends-child-list-stop.md).

[ ] The resume stops before source edits. The record is [06 — Server sends each Node with its child list stop 2](plan/client-never-fails/issues/06-server-sends-child-list/reports/06-server-sends-child-list-stop-2.md).

## Status

[ ] Ticket status is `build`.

[ ] Source files changed: 0. The branch is rebased onto `origin/staging` at `26483028`.
