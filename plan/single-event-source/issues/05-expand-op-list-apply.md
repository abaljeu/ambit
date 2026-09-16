# 05 — Expand Op-list apply

**Status:** ready-to-implement
**Blocked by:** None — can start immediately

## Context

HTTP already posts Ev. Apply still wraps leftover Change. Story **Leftover Change and Revision** on [[../arch.md|Single event source architecture]] expands Op-list apply beside that wrapping so nothing breaks.

## What to build

Add Op-list apply, invert, amend, and PersistStamp beside leftover Change wrapping. Leftover Change still compiles. CI stays green. Do not migrate persist, command, or doors. Do not delete leftover Change.

### 1. Op-list apply

Modules **Ev**, **ChangeValidation**, **PersistStamp**. Seam **Op apply**. State / Interface / Uses: [[../arch.md|Single event source architecture]].

- [ ] 1.1.2 Op-list apply — `Ev.apply` / invert / ChangeValidation / amend / PersistStamp take `Op list` (or Ops on EventBody). They do not wrap leftover Change. `Change.apply` still compiles.

## Out of scope

1. Change.id EventId — [[11-one-serial-event-id.md|11 — One serial event id]].
2. Persist, doors, command, boot, compile — migrate batches 06–10.
3. Contract deletes — [[12-contract-leftover-change-and-revision.md|12 — Contract leftover Change and Revision]].

## See also

[[../arch.md|Single event source architecture]], [[../map.md]]
