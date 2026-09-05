# 02 — Core Actor pool

**Context:** Core owns Actor pool machinery, but the exact pool packaging and API are not yet specified. Actor definitions and advisory soft-lock behavior stay outside Core.

**What to build:** Launch long-running work off the apply queue, assign Core-owned job identity, cancel further output from a job, and finish work through Core Changes and inner apply. The apply queue must remain available while the Actor runs.

**Blocked by:** [[01-generalized-server-actor-produce-path.md]], [[12-define-actor-pool-shutdown-behavior.md|Define Actor-pool shutdown behavior]]

**See also:** [[plan/core-creation/project.md]], [[plan/core-creation/reports/kernel-fsproj.md]], [[plan/event-sourced-ops/details/actors-and-jobs.md]], [[plan/event-sourced-ops/issues/09-job-identity-with-advisory-soft-lock.md]]

**Status:** needs-info

- [ ] Launch returns a Core-owned job identity and does not hold the apply queue while the Actor runs.
- [ ] Cancel prevents further Actor output without undoing Changes that already merged.
- [ ] A finishing Actor submits Change objects through Core Changes and inner apply.
- [ ] The design specifies cancellation and cancel-after-enqueue behavior before implementation.
- [ ] Actor definitions, Browser chrome, and advisory soft-lock policy or indicators are not implemented in this issue.
