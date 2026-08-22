# Event-sourced ops

Stage: active
Summary: One semantic standard for how any Actor's Change enters a Graph — fourteen implementation issues (`issues/01`–`14`) from critical-flaw elimination through Actor spine, recovery decisions, wire migration, and polish; charting docs in overview, architecture, and details.
Updated: 2026-08-22

Start at [[overview.md]] — objective and semantic means. Then [[architecture.md]] — roles, the life of a Change, the two channels.

Implementation issues (dependency order): [[issues/01-shared-success-envelope-expand.md]] through [[issues/14-drop-replace-index-wire-migration.md]]. Done wire slices: [[issues/13-migrate-producers-full-list-replace-wire.md]], [[issues/14-drop-replace-index-wire-migration.md]]. Draft and quiz history: [[to-tickets-draft.md]].

Details, by topic:

- [[details/vocabulary.md]] — locked terms, and the words this project refuses
- [[details/merge-invariant.md]] — critical information, amendment order, single owner
- [[details/conflict-resolution.md]] — the conflict kinds and what merge does with each
- [[details/replace-amendment.md]] — full-list Replace shape, three-way resolve, acceptBoth algorithm
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
