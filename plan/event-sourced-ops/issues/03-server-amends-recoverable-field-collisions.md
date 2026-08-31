# 03 — Server amends recoverable field collisions (text, name, classes)

**Context:** After the revision gate, same-field text/name and whole-set classes still Reject via compare-and-swap. The accepted standard sequences by arrival and amends: first text/name kept with loser as `amb-conflict` child; classes as set delta against the common prior. Produce must become amend-and-succeed for those field kinds.

**What to build:** When a posted Change is stale against already-accepted work on Node fields, the Server sequences by arrival, applies amendment order (common prior, other accepted Changes in full, then amended newest), applies as success, and sets `externalChanges = true` when other Actors' work or amendment occurred. Verifiable kinds: same text/name → first arrival kept, loser as `amb-conflict` first child; classes → set delta against the common prior. Same-parent child Replace collision may still Reject until ticket 05. Leave room for optional Change baseline and same-Change fill-in; do not freeze short-tail retention as immovable. End-to-end Browser demo of amended success still needs ticket 04.

**Blocked by:** 01 — Shared success envelope expand (behavior-identical), 02 — Independent concurrent Changes succeed

**See also:** [[../details/merge-invariant.md]], [[../details/conflict-resolution.md]]

**Status:** done

- [x] Stale concurrent field Changes on text/name succeed with first arrival kept and loser as an `amb-conflict` first child.
- [x] Concurrent class edits succeed as a set delta against the common prior without discarding either Actor's intended add/remove.
- [x] Success sets `externalChanges = true` when other Actors' work or amendment occurred; auth and malformed requests remain Reject.
