# Event-sourced ops

Stage: charting
Summary: One semantic standard for how any Actor's Change enters a Graph — global order by Server arrival, Server amendment, Client rewind and replay, conflict as an `amb-conflict` child instead of a Reject; three layers now published as overview, architecture, and details.
Updated: 2026-08-21

Start at [[overview.md]] — objective and semantic means. Then [[architecture.md]] — roles, the life of a Change, the two channels.

Details, by topic:

- [[details/vocabulary.md]] — locked terms, and the words this project refuses
- [[details/merge-invariant.md]] — critical information, amendment order, single owner
- [[details/conflict-resolution.md]] — the conflict kinds and what merge does with each
- [[details/client-consume.md]] — rewind and replay, baseline, leftover pending
- [[details/messaging.md]] — post against poll, success against Reject, History
- [[details/completing-ops.md]] — Server fill-in and its timing
- [[details/soft-lock.md]] — the advisory subtree reservation
- [[details/actors-and-jobs.md]] — long-running Actors, Parse File, shell commands, launch and cancel
- [[details/undo.md]] — what Undo inverts, and the retained open question
- [[details/relation-to-relaxed-concurrency.md]] — sibling project, what stays, what is obsolete
- [[details/as-implemented-facts.md]] — today's behavior, including behavior to beat
- [[details/open-questions.md]] — accepted, proposed, open, parked
- [[details/decision-log.md]] — how each point was reached, and what was superseded

Related, not a replacement: [[.scratch/relaxed-concurrency/]]. That map examined Event Modeling and Event Sourcing and rejected full Event Sourcing with replay from genesis. This framework is a more general relaxed concurrency. Do not archive or cancel the older project.
