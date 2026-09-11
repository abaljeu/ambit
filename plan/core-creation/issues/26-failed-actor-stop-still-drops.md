# 26 — Failed Actor stop must still drop

**Status:** cancelled
**Blocked by:** none

## What happened

When an Actor stops because its work failed, Core does not enqueue delete-actor. Query by the public number still succeeds. The send credential stays in the set. lock-present stays on the span.

## What I expected

Any Actor stop enqueues delete-actor, including a failed stop. After delete-actor applies, query fails, the credential is gone, and lock-present is off. Callers still do not get a job Error.

## Why cancelled

This was a patch ticket on the current pool mailbox. The Core 18 review will reset the working head before those commits and reimplement from a tighter spec. The finding lives in [[../reports/actor-pool-rewind-review.md]].

## See also

[[18-finish-and-drop.md]], [[11-define-actor-finish-and-failure-behavior.md]]

## Time

- 2026-09-11 15m — filed from Core 18 review (from chat)
- 2026-09-11 — cancelled; rewind/redo, not a wrap patch (from chat)
