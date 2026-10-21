# 72 — Push parsed nodes to the persist stack

**Status:** `defined`
**Type:** coding
**Blocked by:** [71 — Persist thread takes from the stack](71-persist-thread-takes-from-the-stack.md)
**Trackers:** architecture

## Context

Part 3 of [51 — Stand the persist stack](51-stand-the-persist-stack.md). Today `Notice.NodeParsed` calls `FileAgent.scheduleCatchUp`, which submits a Change job to the collector loop.

## What to build

1. [ ] **Push** — When `Notice.NodeParsed` leaves a node Parsed and Unpersisted, the Core mailbox pushes `PersistRequest { nodeId }`. The mailbox does not wait.
2. [ ] **Remove** — Delete `FileAgent.scheduleCatchUp` and its `noteParsed` wiring.

### Acceptance Criteria

1. [ ] **Test** — `NodeParsed pushes a persist request`. A Parsed and Unpersisted node is pushed. Any other node is not pushed.
2. [ ] **Test** — after the loop writes the node, it is Persisted.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket.

## See also

- [51 — Stand the persist stack](51-stand-the-persist-stack.md)
- [71 — Persist thread takes from the stack](71-persist-thread-takes-from-the-stack.md)
