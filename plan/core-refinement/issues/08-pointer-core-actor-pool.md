# 08 — Pointer: Core Actor pool

**Type:** pointer
**Status:** defined
**Blocked by:** None — chart/implement from the source ticket; this file is the ownership home

## Context

Solid core v1 ([[plan/core-creation/project.md]]) is treated as implemented (Alan 2026-10-07). Leftover work continues on this Project (Core v2 / Core-seam).

## Pointer

- **Source (history):** [[plan/core-creation/issues/02-core-actor-pool.md|02 — Core Actor pool]]
- **Owned / continued here:** do not implement further from the source file; chart and code from this pointer + the source body as the detailed spec.
- **Why v2:** Provider-neutral one-mailbox Actor pool rebuild remainder.

## What to build

Follow the source ticket’s remaining unchecked work and Comments. This pointer does not rewrite that spec.

## See also

[[plan/core-refinement/project.md]], [[plan/core-refinement/arch.md]], [[plan/core-creation/issues/02-core-actor-pool.md]]

Pool admission and registry stay on this pointer. The ordered stream calls that bookkeeping from the events list. It does not rebuild the pool. [22 — One ordered event stream](../../single-event-source/issues/22-ordered-event-stream.md).
