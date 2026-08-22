# 09 — Job identity with advisory soft-lock (one vertical)

**Context:** Soft-lock meaning is accepted; job↔lock lifecycle coupling is an accepted direction. Issuance, expiry, chrome, and job launch/cancel mechanics are proposed and do not exist yet. Shipping lock and job as two products would create two surfaces that must immediately couple. Parse (08) is the tracer without this footprint.

**What to build:** Client-held job identity, launch that returns before apply, cancel that stops further Changes (not Undo), and advisory soft-lock as the same surface: the lock is owned by the job; job completion clears it; the lock indicator is an access point to the job. Edits under the lock remain legal and merge. Still not a plug-in bus.

**Blocked by:** 07 — Generalized Server Actor produce path, 08 — Parse File realignment (tracer bullet)

**See also:** [[../details/soft-lock.md]], [[../details/actors-and-jobs.md]]

**Status:** ready-for-agent

- [ ] A Client can launch a long job with an identity, receive return before apply completes, and cancel further Changes without undoing merged work.
- [ ] Advisory soft-lock belongs to that job: completion clears it; the indicator opens the job; edits under the lock remain legal and merge.
- [ ] Job identity and soft-lock ship as one vertical surface, not two independent products.
