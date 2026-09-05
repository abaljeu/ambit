# 09 — Advisory soft-lock and Browser job access

**Context:** Soft-lock meaning is accepted, and its lifecycle coupling to a job is an accepted direction. Core owns job launch, identity, cancellation, and finish through Changes. ESO owns the advisory semantics and Browser-facing access. Issuance, expiry, and Browser chrome are still proposed. Parse (08) proves the Actor path without this Browser surface.

**What to build:** Use Core job identity as the Browser access point for an advisory subtree reservation. The lock belongs to the job, job completion clears it, and its indicator lets the person inspect or cancel the job through the Core pool surface. Edits under the lock remain legal and merge. Do not duplicate Core pool implementation.

**Blocked by:** [[plan/core-creation/issues/02-core-actor-pool.md]], [[08-parse-file-realignment-tracer.md]]

**See also:** [[../details/soft-lock.md]], [[../details/actors-and-jobs.md]], [[plan/core-creation/project.md]]

**Status:** needs-info

- [ ] Advisory soft-lock semantics remain advisory: edits under the reservation are legal and merge.
- [ ] The reservation belongs to a Core job identity, and job completion clears it.
- [ ] The Browser indicator provides access to inspect or cancel the job through the Core pool surface.
- [ ] Issuance, expiry, and exact Browser chrome are specified before implementation.
- [ ] This issue does not implement job launch, identity, cancellation, or inner apply machinery.
