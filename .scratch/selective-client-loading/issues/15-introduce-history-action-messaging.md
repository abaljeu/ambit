# 15 — Introduce HistoryAction messaging

**What to build:** Make Change, Undo, and Redo explicit HistoryActions throughout synchronization and durable history so users receive the same ordered behavior during ordinary full-graph submit, Poll, bootstrap, Load catch-up, replay, and projected transitions. Undo and Redo must travel as actions rather than becoming inverted or reissued Changes.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] Submitting, polling, bootstrapping, and loading exchange ordered Change, Undo, and Redo HistoryActions with revisions preserved from request through response.
- [ ] Durable history records and replays all three action kinds, and an existing Change-only durable entry decodes and replays as a Change HistoryAction.
- [ ] Invoking Undo or Redo places that explicit action in the client pending queue and produces the corresponding canonical server History entry without creating an inverted or reissued Change.
- [ ] Server application and the resident-transition boundary accept each HistoryAction kind and produce the expected graph and History result.
- [ ] Existing full-graph editing, synchronization, replay, Undo, and Redo behavior remains green when all child lists are resident.
