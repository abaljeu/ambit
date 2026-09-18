# 09 — Command mint

**Status:** done
**Actual:** 2h
**Blocked by:** [05 — Expand Op-list apply](05-expand-op-list-apply.md), [08 — Core doors](08-core-doors.md)

## Context

Browser command builders and Parse still mint leftover Change. Wire already posts Ev. `postGraphOnly` now takes Ev.

## What to build

Browser command builders and Parse mint Ev (`EventId.zero`, `commandName`). Run is ActorStart or a Change Event with that Run command in `commandName`. Leftover Change still compiles until contract. CI stays green.

### 1. Command mint

Modules **GraphOnlyChangePost**, **ClientHistory**. Command builders in Browser. Seam **postEvents** / **postGraphOnly**.

- [x] 1.2.4 Command mint — Browser and Parse mint Ev (`EventId.zero`, `commandName`). Run is ActorStart or a Change Event with `commandName`

## Out of scope

1. Boot IndexedDB — [10 — Boot IndexedDB](10-boot-indexeddb.md).
2. Contract deletes — [12 — Contract leftover Change and Revision](12-contract-leftover-change-and-revision.md).

## See also

[Single event source architecture](../arch.md), [02 — Files, Query, and Command as Event work](../issues/02-files-query-and-command-as-event-work.md)

## Time

- 1. Command mint — 2026-09-17 2h — Browser command builders and Parse mint Ev with EventId.zero and commandName (from chat)
