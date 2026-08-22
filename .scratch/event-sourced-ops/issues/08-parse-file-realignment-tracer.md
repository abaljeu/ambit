# 08 — Parse File realignment (tracer bullet)

**Context:** Parse already fits the long-running Actor shape as an observation, but still compare-and-swaps on revision and returns a bare ack. Realignment is proposed. This ticket is the tracer that proves ticket 07 without inventing multi-job identity, cancel, or soft-lock chrome.

**What to build:** Parse plans off the apply queue, concludes through inner apply, and returns merge success (not revision CAS refuse / bare ack). Other Browsers learn by poll plus rewind/replay. Parse stays request-scoped for this ticket.

**Blocked by:** 07 — Generalized Server Actor produce path

**See also:** [[../details/actors-and-jobs.md]], [[../details/as-implemented-facts.md]]

**Status:** ready-for-agent

- [ ] Parse concludes through the shared inner apply path and returns merge success rather than revision CAS refuse or bare ack.
- [ ] Concurrent Browser work during Parse is amended as merge success visible on Poll.
- [ ] No multi-job identity, cancel surface, or soft-lock chrome is required for this tracer.
