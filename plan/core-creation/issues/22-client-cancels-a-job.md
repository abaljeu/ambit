# 22 — Client cancels a job

**Status:** ready-for-agent
**Blocked by:** [[17-cancel-a-job.md|17 Cancel a job]], [[21-client-shows-lock-present.md|21 Client shows lock-present]]

## Context

A person in the Browser stops a running job on a live span. Cancel is not Undo.

## What to build

The user can cancel from the UI. The Browser sends cancel by NodeId for a live span.

- [ ] The Browser can send cancel by NodeId for a live span.
- [ ] After cancel, later Actor output is refused and merged Changes stay.

## See also

[[10-define-actor-cancellation-and-output-admission.md]], [[02-core-actor-pool.md]]
