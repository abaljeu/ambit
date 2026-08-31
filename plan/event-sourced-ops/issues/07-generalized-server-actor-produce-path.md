# 07 — Generalized Server Actor produce path

**Context:** Browser Changes and Server-side producers should share one mutation path. Today Server producers are not first-class on the same amend/log/poll-visible sequence. Entry seam and packaging remain proposed; merge/consume once a Change arrives are accepted.

**What to build:** One inner apply path that Server-side Actors hand Changes into without HTTP self-post. A non-Browser producer applies through the same amend, log, and poll-visible sequence as a Browser Change. Prefer locking the entry seam lightly, then implementing.

**Blocked by:** 03 — Server amends recoverable field collisions (text, name, classes), 04 — Client consumes merge success without reload

**See also:** [[../details/actors-and-jobs.md]], [[../architecture.md]]

**Status:** ready-for-agent

- [ ] A Server-side producer can submit a Change through an inner apply path without HTTP self-post.
- [ ] That Change is amended, logged, and visible on Poll like a Browser-posted Change.
- [ ] Auth and malformed failures remain Reject; this ticket does not invent multi-job identity or soft-lock UI.
