# 08 — Pointer: Core Actor pool

**Type:** pointer
**Status:** defined
**Blocked by:** None — this file is the implement home

## Context

Solid core v1 ([core-creation](../../core-creation/project.md)) is implemented (Alan 2026-10-07).

## Pointer

- **Source (history):** [02 — Core Actor pool](../../core-creation/issues/02-core-actor-pool.md)
- **Owned / continued here:** implement only from this pointer.
- **Why v2:** Verify-close any residual second-pool launch or query door.

## What to build

Open work is that verify-close. The events door is [22 — One ordered event stream](../../single-event-source/issues/22-ordered-event-stream.md) in [Single event source architecture](../../single-event-source/arch.md).

## See also

[core-refinement](../project.md), [core-refinement architecture](../arch.md), [02 — Core Actor pool](../../core-creation/issues/02-core-actor-pool.md)

Pool admission and registry stay on this pointer. The ordered stream calls that bookkeeping from the events list. It does not rebuild the pool. [22 — One ordered event stream](../../single-event-source/issues/22-ordered-event-stream.md).
