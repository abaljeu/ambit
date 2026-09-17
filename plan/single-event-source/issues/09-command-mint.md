# 09 — Command mint

**Status:** defined
**Blocked by:** [05 — Expand Op-list apply](05-expand-op-list-apply.md), [08 — Core doors](08-core-doors.md)

## Context

Browser command builders and Parse still mint leftover Change. Wire already posts Ev. `postGraphOnly` now takes Ev.

## What to build

Browser command builders and Parse mint Ev (`EventId.zero`, `commandName`). Run is ActorStart or a Change Event with that Run command in `commandName`. Leftover Change still compiles until contract. CI stays green.

### 1. Command mint

Modules **GraphOnlyChangePost**, **ClientHistory**. Command builders in Browser. Seam **postEvents** / **postGraphOnly**.

- [ ] 1.2.4 Command mint — Browser and Parse mint Ev (`EventId.zero`, `commandName`). Run is ActorStart or a Change Event with `commandName`

## Out of scope

1. Boot IndexedDB — [[10-boot-indexeddb.md|10 — Boot IndexedDB]].
2. Contract deletes — [[12-contract-leftover-change-and-revision.md|12 — Contract leftover Change and Revision]].

## See also

[[../arch.md|Single event source architecture]], [[../issues/02-files-query-and-command-as-event-work.md|02 — Files, Query, and Command as Event work]]
