# Event-sourced ops — overview

The top layer of this project. It gives the objective and the semantic means. The protocol is in [[architecture.md]]. Everything else is in [[details/]]. Project card: [[project.md]].

## Status words

Every claim in this project carries one word.

| Word | Sense |
| --- | --- |
| **accepted** | The user pinned it. Build on it. |
| **proposed** | Written, not pinned. Do not build on it. |
| **open** | A named question with no answer. Retained on purpose. |
| **parked** | Deliberately not discussed in this increment. |
| **fact** | How the software behaves today. Some facts are behavior to beat, not the standard. |

The project stage is `charting`. No part of this is software. No part of it is a lock on an algorithm.

## Objective

Give one semantic standard for how a mutation enters a Graph, so that every producer of mutations uses the same path, and so that concurrent work merges instead of being refused.

[[plan/core-creation/project.md]] owns implementation of Core as the sole Server Graph writer and Actor pool. ESO owns the merge standard, the Parse definition in [[issues/08-parse-file-realignment-tracer.md]], and advisory soft-lock behavior in [[issues/09-job-identity-with-advisory-soft-lock.md]].

Three aims:

1. **One mutation path.** An Op is the only mutation. A Change is a set of Ops. An Actor is anything that produces a Change — a person editing in the Browser, the Parse File job, a later shell command or agent. There is no second writer.
2. **Merge, not refuse.** Concurrency is normal, not an error. The Server sequences Changes and amends the newest one against what already landed. A recoverable collision is a success, not a Reject.
3. **Async work is not a separate product.** A long-running job is an Actor of the same kind. Its result arrives as Changes on the same path, and Clients consume it the same way.

## Goal outcome (proposed)

A **new server process or version** must not demand a Browser reload when the post protocol is unchanged. The Server resumes with **consistent state** — same graph and revision the Clients already knew — as before the reset. **Old Clients are accepted** unless we **explicitly code a fail point** (`CodeOutdated`, malformed wire, auth, and similar). The Server still generates Browser code, so very old Clients do not exist in practice; we only code for **short-term transition states**, and that coding is basically **keep state and protocols consistent** so the previous Client does not break. See [[details/permanent-history-and-genesis.md]].

## What this is not

This is **not** full Event Sourcing in the relaxed-concurrency sense: log-as-truth, retained historic parsers, or **routine** replay from empty through re-parse. That rejection from [[plan/relaxed-concurrency/map.md]] stands.

**Proposed extension:** make the **global Change log permanent** so a new server process does not discard history and orphan open Browsers. Current state still loads from the DB projection; genesis — the state when the permanent log began — is **derivable** by inverting every Change back to the first entry, not by re-parsing files from empty. Routine operation still uses a short poll tail, not genesis replay. Post-protocol changes still force reload. See [[details/permanent-history-and-genesis.md]].

Load packages stay a Graph transfer, not a replay. [[plan/relaxed-concurrency/map.md]] is a **build-upon layer** on this foundation — verified facts, shared rejections, frontier D–F — not a competing implementation. See [[details/relation-to-relaxed-concurrency.md]].

**Out of scope for ESO** (not Gambol-wide; see [[doc/agents/scope-vs-commitment.md]]): a plug-in bus, a job-framework product, or an offline editor. ESO is a small framework for how a Change merges into a Local Graph.

## Semantic means

These are the ideas that carry the objective. Each has a home in [[details/]].

**Common prior.** Every Change is planned against some Local Graph. That graph is the base the merge reasons from. Nothing is compared against isolated node fields.

**Global order.** Server arrival sequences Changes. First arrival is first. There is no vote, no timestamp race, and no last-write-wins.

**Amendment.** The Server takes the common prior, applies the other Actors' accepted Changes, then rewrites — amends — the newest Actor's Change so it fits that combined state. A correction that is true only of the touched Nodes, and that omits the other Actors' data, is invalid. See [[details/merge-invariant.md]].

**Never lose critical information.** Changing text, adding a class, and adding a child edge are critical. Edge order is important but not critical. Merge may drop information only when an Actor removed it. See [[details/merge-invariant.md]].

**Conflict becomes data.** When two Actors set the same text or the same name, the first arrival stays on the Node and the loser becomes a new first child, a Normal Node with the class `amb-conflict` and the losing value as its text. Nothing is discarded and nobody is refused. See [[details/conflict-resolution.md]].

**Independence.** Changes to different Nodes' fields, or to different parents' child lists, do not conflict. Deleting a Node is independent of editing it for critical information — a delete writes the old parent's child list — but leaving the edit under TRASH is a bad end state; a `deleted` wrapper recovery is a future consideration. Independence makes merge simple; it does not let a Client skip the other Actors' Changes. See [[details/conflict-resolution.md]].

**Rewind and replay.** An optimistic Client converges by rewinding its Local Graph to the baseline and replaying the Server's sequence. It does not patch its own state in place. This is a short tail from a shared base, not a replay from empty. See [[details/client-consume.md]].

**Advisory reservation.** A long job may soft-lock its subtree. That recommends work elsewhere. It does not make edits there illegal, and merge still runs. See [[details/soft-lock.md]].

## Reading order

1. This file — objective and means.
2. [[architecture.md]] — the protocol: roles, the life of a Change, the two channels.
3. [[details/vocabulary.md]] — the locked terms.
4. Then any detail file by topic.
5. [[details/open-questions.md]] — what is still proposed, open, or parked.
6. [[details/decision-log.md]] — how each accepted point was reached, and what was superseded.
