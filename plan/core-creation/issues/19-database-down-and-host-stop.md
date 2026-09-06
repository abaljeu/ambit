# 19 — Database down and host stop

**Status:** ready-for-agent
**Blocked by:** [[17-cancel-a-job.md|17 Cancel a job]], [[18-finish-and-drop.md|18 Finish and drop]]

## Context

The Database can fail while Actors run. The host can also stop. Crash isolation is out of this issue.

## What to build

A mutating Post that fails TCP is a system-error Reject. Core marks the Database down. Sibling mailbox items still apply. The next mutating Post or launch is the probe. Reads stay admitted. Launch Rejects while the Database is down. On host StopAsync, Core refuses Posts, drains the mailbox including delete-actor, cancels Actors, and exits when idle or at the host default timeout.

- [ ] A mutating Post that fails TCP is a system-error Reject, the Database is marked down, and already-enqueued siblings still apply.
- [ ] While down, reads are admitted and launch Rejects; the next mutating Post or launch is the probe.
- [ ] Host StopAsync refuses Posts, drains the mailbox including delete-actor, cancels Actors, and exits when idle or at host default timeout.
- [ ] Crash isolation is not in this issue.

## See also

[[12-define-actor-pool-shutdown-behavior.md]], [[plan/core-creation/reports/to-build-09-12-actor-pool.md]]
