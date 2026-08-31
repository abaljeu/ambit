# Relaxed concurrency

Status: superseded

## Handoff

Implementation is the **event-sourced-ops** foundation — not this file.

Slice 1 acceptance (drop the global revision gate, keep per-op compare-and-swap) was delivered by [[plan/event-sourced-ops/issues/02-independent-concurrent-changes-succeed.md]].

Merge, amend, consume, and full-list Replace wire migration were delivered by ESO issues 01–05 and 13–14. See [[plan/event-sourced-ops/overview.md]] and [[plan/event-sourced-ops/architecture.md]].

**Do not implement from this file.** Upstream facts and rejected alternatives live in [[map.md]]; the active standard lives in event-sourced-ops.
