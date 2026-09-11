# 26 — Failed Actor stop must still drop

**Status:** ready-for-agent
**Blocked by:** none
Estimate: 45m

## What happened

When an Actor stops because its work failed, Core does not enqueue delete-actor. Query by the public number still succeeds. The send credential stays in the set. lock-present stays on the span.

## What I expected

Any Actor stop enqueues delete-actor, including a failed stop. After delete-actor applies, query fails, the credential is gone, and lock-present is off. Callers still do not get a job Error.

## Steps to reproduce

1. Register an Actor whose work fails after launch.
2. Launch that Actor on a span. Keep the public number.
3. Wait until the Actor has stopped.
4. Query the public number. It still succeeds.
5. Check lock-present on the span and the send credential. Both still look live.

## Additional context

This is leftover from [[18-finish-and-drop.md]]. Wrap the Actor run so a failure still enqueues delete-actor. Successful return already drops. Do not add a job Error.

## See also

[[18-finish-and-drop.md]], [[11-define-actor-finish-and-failure-behavior.md]]

## Time

- 2026-09-11 15m — filed from Core 18 review (from chat)
