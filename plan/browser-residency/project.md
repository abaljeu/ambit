# Browser residency

Stage: done
Summary: The Browser starts with a small bootstrap Graph and grows from ViewModel-derived Wants: Unloaded Included Nodes, then their Children, then the grandchildren. An Unloaded Node shows a hollow-circle Bullet until its Children arrive. Every post-Event and Poll carries Changes plus the current Want.
Updated: 2026-10-07
Started: 2026-09-26
Finished: 2026-10-07
Actual: 7h 42m

**Part of:** [[plan/roadmap/epics/chapters/incremental-operations.md]]

## Notes

- Successor to [[plan/selective-client-loading/project.md]] (Stage done; prior whole-Workspace slice).
- Auto wants need no click and no command. Commands that name Nodes come later.
- File transit stays on [[plan/transport-layer/project.md]]. Parse thread home stays on [[plan/parse-thread/project.md]].
- Map: [[map.md]].
- Spec: [[spec.md]].
- Arch: [[arch.md]].
- Implementation: [07 — Expand Want and edges/Nodes package](issues/07-expand-want-and-edges-nodes-package.md), [08 — Migrate Shared wire](issues/08-migrate-shared-wire.md), [09 — Migrate Server Sync doors](issues/09-migrate-server-sync-doors.md), [10 — Migrate Browser Poll, post-Event, and Boot](issues/10-migrate-browser-poll-post-event-and-boot.md), and [11 — Migrate Bullet and Included readers](issues/11-migrate-bullet-included-and-bootstrap-wants.md) are done. [12 — Contract old Load Fetch packages](issues/12-contract-old-load-fetch-packages.md) is coded.
- Server Find ([05 — Chart server-mode Find](issues/05-chart-server-mode-find.md)) is parked toward [[plan/online-search/project.md]] (Chapter [[plan/roadmap/epics/chapters/find-what-i-wrote.md]]).
