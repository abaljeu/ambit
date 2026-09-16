# 10 — Boot IndexedDB

**Status:** blocked
**Blocked by:** [05 — Expand Op-list apply](05-expand-op-list-apply.md)

## Context

Boot cache still holds leftover Change list. Poll consume is already Ev. Story **Leftover Change and Revision** migrates the boot log.

## What to build

BootCache and BootCacheStore hold an Ev list. Codec is Ev, not leftover Change JSON. Leftover Change still compiles. CI stays green.

### 1. Boot IndexedDB

Module **BootCache**.

- [ ] 1.2.5 Boot IndexedDB — BootCache / BootCacheStore hold Ev list, not leftover Change

## Out of scope

1. Contract deletes — [[12-contract-leftover-change-and-revision.md|12 — Contract leftover Change and Revision]].

## See also

[[../arch.md|Single event source architecture]], [[../reports/inventory-non-event-write-paths.md]]
