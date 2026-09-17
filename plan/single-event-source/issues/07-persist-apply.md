# 07 — Persist apply

**Status:** done
**Actual:** 3h
**Blocked by:** None — [[05-expand-op-list-apply.md|05 — Expand Op-list apply]] and [[06-compile-preamble.md|06 — Compile preamble]] are done

## Context

Core admits Ev, copies it to leftover Change for FileAgent/DbAgent apply, then appends Ev. Story **Leftover Change and Revision** migrates persist next. Narrowest test seam: persist apply of one Ev.

## What to build

FileAgent and DbAgent admit Ev, apply Ops locally, then `appendEvent`. CoreEventDispatch does not copy Ev to leftover Change for apply. Leftover Change still compiles. CI stays green.

### 1. Persist apply

Modules **FileAgent**, **DbAgent**, **PersistStamp**, **CoreEventDispatch**. Seam **Persist apply**.

- [x] 1.2.2 Persist apply — admit Ev, apply Ops, `appendEvent` Ev. No Ev→Change copy for apply

## Out of scope

1. Core doors dropping `postChange` — [[08-core-doors.md|08 — Core doors]].
2. Contract deletes — [[12-contract-leftover-change-and-revision.md|12 — Contract leftover Change and Revision]].

## See also

[[../arch.md|Single event source architecture]], [[../reports/inventory-non-event-write-paths.md]]

## Time

- 1. Persist apply — 2026-09-16 2h — FileAgent and DbAgent apply Ev Ops then CoreEventDispatch appendEvent; leftover Change still compiles (from chat)
- 2. Persist apply follow-up — 2026-09-16 1h — FileAgent createWithDependencies helpers extracted; PersistApplyTests assert appendEvent (from chat)
