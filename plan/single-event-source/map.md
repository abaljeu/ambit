# Single event source

Labels: wayfinder:map

## 1. Destination

Every unit of work that today goes through a Change record, a Revision, or a parallel persist/command path is an Event with one event id. Other divergences from that rule left by [[plan/core-creation/project.md]] are in scope as they are found. [[plan/core-creation/project.md]] is suspended until that architecture is created. Then [[plan/core-creation/arch.md]] is corrected to match what was created. Do not edit that arch before then.

## 2. Notes

- Domain: Event (`Ev`), EventLog, EventBody.Change, event id, Op. Consult [[.agents/skills/wayfinder/SKILL.md]], [[.agents/skills/grilling/SKILL.md]], [[.agents/skills/domain-modeling/SKILL.md]], [[.agents/skills/project-work/SKILL.md]], [[CONTEXT.md]].
- Sister of [[plan/core-creation/project.md]]. That Project is suspended until this architecture exists. Do not rewrite its map or tickets. Do not add implementation issues there. [[plan/core-creation/arch.md]] is written last, to match what this Project created.
- Event (`Ev`), EventId, Op, and EventBody stay in [[src/Shared/History.fs]]. There is no `module History`. EventLog remains the sequence. ClientHistory stays the Emacs Action view.
- Change is `EventBody.Change` of an Op list, not a `{ id; submissionId; ops }` record.
- Two Core doors remain, both Event: `postEvents` and `postGraphOnly` (graph-only still skips file persist).
- Ev is transported. Ops are not. Apply, validation, invert, amend, and PersistStamp take an Op list locally.
- A command holds an Event with `EventId.zero` until admit fills `Ev.id`.
- After this map is clear, this Project implements Event-only in code per [[plan/single-event-source/arch.md]]. Then [[plan/single-event-source/issues/04-write-core-creation-arch-md-last.md|04 — Write core-creation arch.md last]] updates [[plan/core-creation/arch.md]]. [[plan/core-creation/project.md]] stays suspended until that is done.

## 3. Decisions so far

1. [[plan/single-event-source/issues/01-inventory-non-event-write-paths.md|01 — Inventory non-Event write paths]] — HTTP posts Ev; Core copies Ev to leftover Change for persist apply, then appends Ev; Revision and Change.id remain a second serial. [[plan/single-event-source/reports/inventory-non-event-write-paths.md]]
2. [[plan/single-event-source/issues/02-files-query-and-command-as-event-work.md|02 — Files, Query, and Command as Event work]] — Files and Query are not EventLog appends; file-upload Actor is later; Run is ActorStart or a Change Event with `commandName`.
3. [[plan/single-event-source/issues/03-cleanup-seam-order.md|03 — Cleanup seam order]] — Ev transported, Ops local; compile preamble; persist and command in any order; `Change.id` is `EventId` (no Revision stop); leftover Change dies last.

## 4. Not yet specified

<!-- see "Fog of war": in-scope fog you can't ticket yet; graduates as the frontier advances; numbered list when items exist -->

## 5. Out of scope

1. Rewriting [[plan/core-creation/]] map or tickets. Editing [[plan/core-creation/arch.md]] before the architecture is created.
2. Adding implementation issues to [[plan/core-creation/project.md]].
3. Implementing the cleanup during charting.
4. File upload as an Actor. Deferred; not this Project. Graph-mutating work from Upload still becomes Events when that Actor exists.
