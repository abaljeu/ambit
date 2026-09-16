# Single event source

Chart map for [[project.md]]. Stage `chart`: locks below are knowns, not a spec.

## 1. Problem

Graph history and apply still have two durable stories in the air: an Event sequence and a parallel Change log (file and/or `changes` table). Dual persist means two sources of truth for the same apply, restore, and Poll path.

## 2. Destination

Event is the sole durable source of truth. Persist is EventLog only: `gambol.events` on file and the `events` table on Database. Change is an EventBody subtype (Ops on an Event), not a second log. `postGraphOnlyChange` is the same Event flow as `postChange` minus the file write.

## 3. Knowns

From recent work already on the Core Event stories and persist contract:

1. Shared Event / EventLog / ClientHistory exist; `EventBody` includes `Change` of Ops.
2. Persist of that EventLog is Event JSON via EventLogFile (`gambol.events`) and the `events` table. Arch: not a second log; drop the ChangeLog name.
3. CoreMailbox `postEvent` is the Changes door. `postGraphOnlyChange` uses the same Event flow and skips file persistence only (not EventLog).
4. Those Event stories are coded on [[plan/core-creation/project.md]] / [[plan/core-creation/arch.md]]. This Project does not retick or rewrite those checkboxes.

## 4. Open questions

1. Which leftover Change-shaped persist, Poll, or restore paths still write or read a parallel log after the coded Event stories.
2. What a later spec must still contract (names, HTTP shapes, leftover files) versus what Core creation already closed.
3. How Graph-only (no file write) stays distinct from document persist without becoming a second durable history.

## 5. Out of scope

1. Actor Change merge semantics — [[plan/event-sourced-ops/project.md]].
2. Rewriting [[plan/core-creation/arch.md]] Event-story checkboxes.
3. Issues or implementation tickets (chart only).
4. Chapter-only ACID apply work that is already on [[plan/roadmap/epics/chapters/acid-apply.md]] (db-authority, file mode). This Project is Epic Required for done, not that Chapter’s exclusive beat.
