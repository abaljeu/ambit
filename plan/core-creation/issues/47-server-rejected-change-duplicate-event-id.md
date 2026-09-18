# 47 — Server rejected Change: duplicate event id

**Status:** done
Actual: 1h15m

## 1. What happened

The App showed **Server rejected change**. The reason was `Ev persist error: One or more errors occurred. (23505: duplicate key value violates unique constraint "events_pkey")`. The Change did not persist. The App said to reload to resync and that unsaved changes will be lost.

## 2. What I expected

The Change would persist as a new Event with a unique event id. The Graph would stay in sync. The App would not ask for a reload that discards the edit.

Also, public messages should say Event, never Ev.

## 3. Steps to reproduce

1. Open the Desktop App against a live Ambit host that persists EventLog in the Database.
2. Load a mapped Workspace Node.
3. Edit a Node so the Browser submits a Change.
4. The App shows **Server rejected change** with Ev persist error 23505 on `events_pkey`.

## 4. Additional context

Reported during QA after [46 — Workspace Load prepare-push 401](46-workspace-run-prepare-push-401.md). The Database EventLog unique constraint rejected the new Event because that event id was already stored. The Server assigned an event id that EventLog already holds.

## 5. Comments

1. 2026-09-18 — Alan: screenshot of the blocking alert after a Change.

## Time

- 2026-09-18 1h15m — EventLog nextId catch-up from persisted event_id; Event persist wording (from chat)
