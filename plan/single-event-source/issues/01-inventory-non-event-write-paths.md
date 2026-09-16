# 01 — Inventory non-Event write paths

**Type:** research
**Status:** done
Blocked by: none

## 1. Question

Which remaining code paths mutate the Graph, persist EventLog, or apply Ops without going through an Event? Name every Change record, Revision type or field, `Ev.ofChange` / `Ev.asChange`, `postChange` (Change list) beside `postEvents`, leftover [[src/Shared/EventId.fs]], and any other write path that is not Event.

## Answer

HTTP already posts Ev. Core copies Ev to leftover Change for FileAgent/DbAgent apply, then appends Ev to EventLog. The leftover `{ id; submissionId; ops }` record, `type Revision`, `Change.id` as a second serial, and `postGraphOnlyChange` of Change remain. [[src/Shared/EventId.fs]] is unused. Document load, package install, projection SQL, and file persist still mutate Graph without an Event. Findings: [[plan/single-event-source/reports/inventory-non-event-write-paths.md]].
