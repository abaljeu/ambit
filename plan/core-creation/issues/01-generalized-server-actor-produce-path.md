# 01 — Generalized Server Actor produce path

**Context:** Browser Changes and Server-side producers must share one Core-owned mutation path. Today Server producers are not first-class on the same amend, log, and Poll-visible sequence. Entry seam and packaging remain proposed; merge and consume after a Change arrives are accepted.

**What to build:** One Core Changes path that Server-side Actors hand Changes into without HTTP self-post. A non-Browser producer applies through the same amend, log, and Poll-visible sequence as a Browser Change. This path establishes Core as the sole Server Graph writer. Prefer to lock the entry seam lightly, then implement it.

**Blocked by:** [[plan/event-sourced-ops/issues/03-server-amends-recoverable-field-collisions.md]], [[plan/event-sourced-ops/issues/04-client-consumes-merge-success-without-reload.md]], [[06-ready-the-initial-core-changes-increment.md|Ready the initial Core Changes increment]]

**See also:** [[plan/core-creation/project.md]], [[plan/core-creation/initial-core-changes-implementation.md|Initial Core Changes implementation (enables this issue)]], [[plan/core-creation/reports/kernel-fsproj.md]], [[plan/event-sourced-ops/details/actors-and-jobs.md]], [[plan/event-sourced-ops/architecture.md]]

**Status:** needs-info

- [ ] A Server-side producer can submit a Change through Core Changes without HTTP self-post.
- [ ] That Change is amended, logged, and visible on Poll like a Browser-posted Change.
- [ ] Core is the sole Server Graph writer.
- [ ] Auth and malformed failures remain Reject; this issue does not invent multi-job identity or soft-lock UI.
