# Relaxed concurrency — design

Status: superseded

## Handoff

Gate removal and error semantics: see ESO issue 02 build report and [[.scratch/event-sourced-ops/architecture.md]].

Merge, amend, and consume: [[.scratch/event-sourced-ops/details/client-consume.md]], [[.scratch/event-sourced-ops/details/messaging.md]].

**Do not implement from this file.**

## No server weak-form Replace (still valid)

Server-side silent relocation in `Graph.replace` stays **rejected**. The client applies optimistically before POST; server relocate would leave UI and graph diverged until catch-up. Relocation is a planner choice, not server compare-and-swap. The server matches or rejects; recoverable same-parent collisions merge via ESO rewind and replay, not hidden relocate. See [[map.md#shared-rejections]] and [[.scratch/event-sourced-ops/details/relation-to-relaxed-concurrency.md]].
