# 70 — Add the persist request stack

**Status:** `coded`
**Actual:** 30m
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture

## Context

Part 1 of [51 — Stand the persist stack](51-stand-the-persist-stack.md). This ticket only adds the type and the stack. Nothing pushes to it or takes from it yet.

## What to build

1. [x] **Request** — Add `PersistRequest = { Graph; NodeId }` in [Core](../../../src/Server/Core). `Graph` is the graph at the time of the push.
2. [x] **Stack** — Add the persist stack: a `BlockingCollection<PersistRequest>` over a `ConcurrentStack<PersistRequest>`, with `push` and a blocking `take`. Use the concrete type, not a generic one. Add no `ref`, `mutable` or lock in our code.

### Acceptance Criteria

1. [x] **Test** — `persist stack takes the newest request`. Push two requests. `take` returns the last one pushed.
2. [x] **Test** — `persist stack take waits for a push`. `take` blocks until a request is pushed.

## Finish-up

[x] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[x] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[x] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket.

## Amendments

1. **None** — No rule changed during the build. `PersistRequest` fields are `graph: Graph` and `nodeId: NodeId`. [doc/current](../../../doc/current) needed no line. [Graph change persist stack](../arch.md#graph-change-persist-stack) stays open.

## Time

- 2026-10-20 30m — persist request stack and tests (from chat)

## See also

- [51 — Stand the persist stack](51-stand-the-persist-stack.md)
- [Graph change persist stack](../arch.md#graph-change-persist-stack)
