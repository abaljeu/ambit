# 10 — Boot IndexedDB

**Status:** done
**Actual:** 1h30m
**Blocked by:** [05 — Expand Op-list apply](05-expand-op-list-apply.md)

## Context

Boot cache still holds leftover Change list. Poll consume is already Ev. Story **Leftover Change and Revision** migrates the boot log.

## What to build

BootCache and BootCacheStore hold an Ev list. Codec is Ev, not leftover Change JSON. Leftover Change still compiles. CI stays green.

### 1. Boot IndexedDB

Module **BootCache**.

- [x] 1.2.5 Boot IndexedDB — BootCache / BootCacheStore hold Ev list, not leftover Change

## Out of scope

1. Contract deletes — [12 — Contract leftover Change and Revision](12-contract-leftover-change-and-revision.md).

## See also

[Single event source architecture](../arch.md), [Inventory of non-event write paths](../reports/inventory-non-event-write-paths.md)

## Time

- 1. Boot IndexedDB — 2026-09-17 1h30m — BootCache / BootCacheStore hold Ev list; codec is EventJson (from chat)
