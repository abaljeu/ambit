# 08 — Core doors

**Status:** coded
**Actual:** 2h
**Blocked by:** None — [[05-expand-op-list-apply.md|05 — Expand Op-list apply]] and [[07-persist-apply.md|07 — Persist apply]] are done

## Context

**CoreChanges** still has `postChange` (Change list) and `postGraphOnlyChange` of leftover Change beside `postEvents`. Persist apply already takes Ev.

## What to build

`postEvents` and `postGraphOnly` both take Ev. `postChange` (Change list) and `PostGraphOnlyChange` of leftover Change are gone. Graph-only still skips file persist, not EventLog. Leftover Change still compiles at command mint. CI stays green.

### 1. Core doors

Modules **CoreChanges**, **CoreMailbox**. Seams **postEvents**, **postGraphOnly**.

- [x] 1.2.3 Core doors — `postEvents` and `postGraphOnly` both take Ev. `postChange` (Change list) and `PostGraphOnlyChange` of leftover Change are gone

## Out of scope

1. Command mint — [[09-command-mint.md|09 — Command mint]].
2. Contract deletes — [[12-contract-leftover-change-and-revision.md|12 — Contract leftover Change and Revision]].

## See also

[[../arch.md|Single event source architecture]], [[../map.md]]

## Time

- 1. Core doors — 2026-09-17 2h — CoreChanges and CoreMailbox take Ev only; leftover Change converts at command mint call sites (from chat)
