# 19 — Database down and probe

**Status:** blocked
**Blocked by:** [[plan/core-creation/issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md]]

## Context

The Database can fail while work is active. Database availability, system-error Reject, and probe behavior belong to the persistence boundary. Host-stop Actor drain is separate work in [[plan/core-creation/issues/28-drain-actor-lifecycle-on-host-stop.md]]. Crash isolation is out of this issue.

## What to build

Implement the Database-down policy locked by [[12-define-actor-pool-shutdown-behavior.md]]. Host-stop drain is [[28-drain-actor-lifecycle-on-host-stop.md]]. Restart Interrupted is [[18-finish-and-drop.md]].

- [ ] A mutating Post that fails TCP is a system-error Reject, the Database is marked down, and already-enqueued siblings still apply.
- [ ] While down, reads are admitted and launch Rejects; the next mutating Post or launch is the probe.
- [ ] Crash isolation is not in this issue.

## See also

[[plan/core-creation/issues/12-define-actor-pool-shutdown-behavior.md]], [[plan/core-creation/issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md]], [[plan/core-creation/issues/28-drain-actor-lifecycle-on-host-stop.md]]

## Comments

- 2026-09-11 — Reconciled after the Actor rewind. Database-down detection and the mutating Post/launch probe stay here with [[plan/core-creation/issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md]]. Host StopAsync drain moved to [[plan/core-creation/issues/28-drain-actor-lifecycle-on-host-stop.md]].
