# 15 — Introduce ChangeRequest submission

**Context:** Browser edits get send to the server as Change objects.  We introduce Undo and Redo as distinct objects so the server can evaluate those in context of the full graph.
**What to build:** Widen the existing client pending queue, mixed `/changes` batch, and server command application from Change to ChangeRequest. Keep Undo and Redo immediate and optimistic: submit the explicit action, apply it to canonical server History, then materialize its graph effect as an ordinary Change through the unchanged ChangeLog, Poll, bootstrap, Load, persistence, acknowledgement, revision, and retry paths. See the superseding plan in [[plan/selective-client-loading/undo-spec.md]].

**Status:** agent-done

Delivered in commit `4255c48` on `w/rename-upload-to-load`. Focused Shared History/Serialization/SyncPlanner/SyncLogic and StateEndpoint Undo/Redo tests remain green.

- [x] Change, Undo, and Redo share the existing pending queue and atomic `/changes` batch; every action carries the existing revision id and `changeId`, while only Change carries operations.
- [x] Invoking Undo or Redo updates local History immediately and queues that explicit action; server application performs the same canonical History transition or fails the batch.
- [x] Every accepted action produces one materialized Change with the action's same revision id and `changeId`; ChangeLog, persistence, acknowledgements, Poll, bootstrap, and Load remain Change-only.
- [x] Applying a non-empty upstream Change tail clears local History and applies the projected Changes without recording them; empty Polls and acknowledgements of this client's own actions preserve local History.
- [x] Server History starts empty on each process start, database and file startup do not replay ChangeLog, and a changed server start time puts existing pages into the stale-client refresh flow without requiring a new login.
- [x] No compatibility codec or migration is added, and existing full-graph editing, mixed batching, retry identity, synchronization, optimistic Undo, and optimistic Redo behavior remains green.
