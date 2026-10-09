# 16 — Pointer: Drain Actor lifecycle on host stop

**Type:** pointer
**Status:** defined
**Blocked by:** None — this file is the implement home

## Context

Solid core v1 ([core-creation](../../core-creation/project.md)) is implemented (Alan 2026-10-07).

## Pointer

- **Source (history):** [28 — Drain Actor lifecycle on host stop](../../core-creation/issues/28-drain-actor-lifecycle-on-host-stop.md)
- **Owned / continued here:** implement only from this pointer.
- **Why v2:** Host `StopAsync` must refuse new Posts, drain the mailbox, and request cancellation without waiting.

## What to build

Host `StopAsync` refuse and drain are still open.

## See also

[core-refinement](../project.md), [core-refinement architecture](../arch.md), [28 — Drain Actor lifecycle on host stop](../../core-creation/issues/28-drain-actor-lifecycle-on-host-stop.md)
