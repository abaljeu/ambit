# To-tickets draft — critical flaw elimination and later program sequence

Draft only. Do not publish issue files until the user approves the breakdown. Stage stays `charting`.

Source: [[overview.md]], [[architecture.md]], [[project.md]], [[details/]], especially [[details/as-implemented-facts.md]], [[details/messaging.md]], [[details/merge-invariant.md]], [[details/conflict-resolution.md]], [[details/client-consume.md]], [[details/soft-lock.md]], [[details/actors-and-jobs.md]], [[details/completing-ops.md]], [[details/undo.md]], [[details/open-questions.md]], [[details/decision-log.md]], [[details/relation-to-relaxed-concurrency.md]], [[details/vocabulary.md]]. Build-upon layer: [[plan/relaxed-concurrency/]]. Envelope note: [[post-poll-envelope-unify.md]]. Quiz reconcile: [[reconcile-architecture-sequence-report.md]].

Publication map: conceptual Ticket N publishes as `issues/0(N+1)-…` when N < 9; Ticket 9 → `10`, Ticket 10 → `11`, Ticket 11 → `12`.

## 1. Critical flaws today

Inferred from the project docs plus a short Client/Server skim. These are **behavior to beat**, not the semantic standard.

1. **Global revision gate.** Any concurrent Change that names a stale base revision is refused, even when the Ops touch unrelated Nodes or parents. Identified in [[plan/relaxed-concurrency/map.md]] known 3; **delivered** in issue 02.
2. **Per-Op compare-and-swap refuse.** After the gate, same-field text/name, whole-set classes, and same-parent Replace span mismatch still Reject. The standard says amend: `amb-conflict` for text/name, set delta for classes, occurrence-bag Accept Both for children.
3. **Reject forces reload and drops work.** Client `ServerRejected` shows a blocking alert: reload to resync; **unsaved Changes will be lost**. Ack reconciliation also Rejects when the acknowledgement is not a confirmation echo of the submitted Ops. So a recoverable concurrent kick-back — and any future amended success list — is treated like a terminal failure. User judgment: this Reject/reload path is **indirectly a critical information loss**.
4. **No Server amendment path.** Produce today is apply-or-refuse. There is no common-prior → other accepted Changes → amend newest sequence. Recoverable collision is not merge success.
5. **No Client rewind-and-replay consume for that case.** Poll with a non-empty tail **clears History** (debt). Submit success still uses confirmation-echo reconcile, not baseline note + later poll replay. Leftover pending cannot survive a Reject wipe.
6. **After the initial critical-flaw tickets.** Actor produce path, Parse realignment, job identity **with** advisory soft-lock (one vertical), early recovery decisions (delete-against-edit / orphan), child-list polish, completing-ops beyond timing, Undo desirability.

## 2. Initial tickets — critical-flaw elimination

High-level on purpose: each ticket is meant to become its own later project. Scope of this block is **eliminate critical flaws**, not full event-sourced-ops delivery.

### Extension constraints (so later decisions do not fight frozen wire)

Tickets 0–4 must leave these open without implementing them:

- **Optional Change baseline field** (or equivalent) for delete-against-edit history scan — do not ship a Change schema that cannot grow it.
- **Short-tail log retention policy** remains adjustable — do not hard-code “discard immediately after poll” if delete-against-edit needs scan-since-baseline.
- **History** after Ticket 3 retains Server-originated Changes — do not freeze History as own-posts-only (Undo decision may need other Actors’ amended Changes visible).
- **Amendment / complete** seam allows same-Change fill-in (timing already **accepted** in [[details/completing-ops.md]]).

### Ticket 0 — Shared success envelope expand (behavior-identical) → publish `01`

- **Blocked by:** None — can start immediately; may run in parallel with Ticket 1.
- **What it delivers:** One shared success **response type** used by both Post and Poll encode/decode paths. Fields cover: last Server-received revision, readiness/stamps as today require, `externalChanges` (or equivalent signal), and a Change list that may be **empty**. Behavior-identical phase: Post still confirmation-echo succeeds with `externalChanges = false` and Client ignores the new fields for apply; Poll still returns its list in the same type. Channels stay **separate** (Post = signal + baseline note; Poll = list for rewind/replay).
- **Architectural shift:** Expand–contract plus **pinned direction**: shared envelope type for fewer concepts / easier verification. Does **not** collapse the two channels.
- **Semantic caveat:** Sharing a type does **not** mean Post applies a Change list. Until Ticket 3, Client must not treat a Post body as an apply tail; Poll remains the apply channel.
- **Status basis:** Two channels and “flag is enough” are **accepted**; shared envelope type is now the **pinned direction** (was proposed; user prefers one footprint). Remaining field details may stay proposed until implement.
- **See also:** [[details/messaging.md]], [[details/as-implemented-facts.md]], [[architecture.md]], [[post-poll-envelope-unify.md]]

### Ticket 1 — Independent concurrent Changes succeed → publish `02`

- **Blocked by:** None — can start immediately. **Delivered** in issue 02. Parallel with Ticket 0.
- **What it delivers:** Two Actors may post Changes against a stale global revision when their Ops do not collide on per-Op preconditions; both succeed. Unrelated attribute edits and structural edits under different parents no longer Reject solely for revision lag. Same-target CAS Reject still exists until Tickets 2 and 4.
- **Architectural shift:** Revision ceases to be the conflict boundary for unrelated work. Not yet the merge model.
- **Status basis:** Gate removal **delivered** (issue 02). One **global** Server sequence/revision is **accepted** (see §5).
- **See also:** [[plan/relaxed-concurrency/map.md]], [[details/relation-to-relaxed-concurrency.md]], [[details/as-implemented-facts.md]]

### Ticket 2 — Server amends recoverable field collisions (text, name, classes) → publish `03`

- **Blocked by:** Ticket 0, Ticket 1.
- **What it delivers:** When a posted Change is stale against already-accepted work on **Node fields**, the Server sequences by arrival, applies amendment order, applies as **HTTP 200**, and sets `externalChanges = true` when other Actors' work or amendment occurred. Verifiable kinds: same text/name → first arrival kept, loser as `amb-conflict` first child; classes → set delta against the common prior. Same-parent child Replace collision still Rejects until Ticket 4. Auth and malformed requests remain Reject. End-to-end Browser demo still needs Ticket 3.
- **Architectural shift:** **Significant.** Produce becomes amend-and-succeed for field kinds; confirmation echo is no longer the success truth for those cases.
- **Status basis:** Amendment order, text/name/`amb-conflict`, class set-delta are **accepted**; merge document as a whole and per-Op tables stay **proposed**.
- **See also:** [[details/merge-invariant.md]], [[details/conflict-resolution.md]], [[architecture.md]], [[details/decision-log.md]]

### Ticket 3 — Client consumes merge success without reload → publish `04`

- **Blocked by:** Ticket 0, Ticket 2.
- **What it delivers:** When `externalChanges` is true (or the ack is not a confirmation echo), the Browser does **not** enter `ServerRejected` / forced reload. It notes the baseline, and when the posting queue is empty Polls, **rewinds to baseline and replays** the Change list from the shared envelope. Neither post nor poll clears History. Leftover pending stays planned and unamended for the next post.
- **Architectural shift:** **Significant.** Client consume is rewind-and-replay of a short Server tail; post is signal-only (even though the envelope type is shared); History survives both channels.
- **Status basis:** Rewind/replay, leftover pending, History retention, two channels are **accepted**.
- **See also:** [[details/client-consume.md]], [[details/messaging.md]], [[details/as-implemented-facts.md]], [[architecture.md]]

### Ticket 4 — Child-list Accept Both (same-parent merge) → publish `05`

- **Blocked by:** Ticket 2, Ticket 3.
- **What it delivers:** Same-parent concurrent inserts/removes merge without Reject. Occurrence-bag Accept Both against the common prior; order may be approximate (algorithm polish later — Ticket 9). Server amends the newest Replace; Client already consumes via Ticket 3.
- **Architectural shift:** Structural concurrency leaves span-CAS Reject for the Accept Both case; critical child edges are not discarded.
- **Status basis:** Occurrence-bag Accept Both and positional Replace default are **accepted**; approximation algorithm is **later / not locked**.
- **See also:** [[details/conflict-resolution.md]], [[details/merge-invariant.md]]

**Dependency (initial):** `0 ∥ 1 → 2 → 3 → 4`

## 3. Later program enhancements

Each item is project-sized (likely its own `plan/<slug>/`). Status words follow [[overview.md]]. Prefer fewer concepts: merged tickets where lifecycle is one surface.

### Early decisions (decision first; implement later)

#### Ticket 5 — Recovery safety decisions (delete-against-edit + orphan) → publish `06`

- **Blocked by:** Tickets 2–3 for a meaningful prototype substrate; may start analysis in parallel with Ticket 4. **Product schedule:** decide before Tickets 6–8 freeze log retention or Change schema further. **Technical gate for implementation of recovery only** — does **not** block Tickets 0–4 if extension constraints above are honored.
- **What it delivers:** A **decision/prototype** that (a) accepts, revises, or rejects the tentative `deleted` Owned-wrapper recovery and whether a Change must carry an explicit baseline for history scan; (b) names orphan-collection policy vs proving hard Orphaning cannot arise. Implementation of recovery is a **follow-on** only after accept.
- **Architectural shift:** **Significant if delete-against-edit is accepted** (wrapper Owns recovered Node; possible baseline + history scan). Orphan policy is janitor/safety, not a second mutation path.
- **Status basis:** Delete-against-edit independence **proposed**; wrapper **tentative/open**; orphan outcome **open** ([[details/conflict-resolution.md]], [[details/open-questions.md]]).
- **Why early:** Late accept of baseline/history-scan after Ticket 0–3 freeze would force painful wire/log rework. Decide early; build recovery later.
- **See also:** [[details/conflict-resolution.md]], [[details/merge-invariant.md]], [[details/open-questions.md]], [[details/completing-ops.md]]

### Actor spine (fewer concepts)

Ownership changed on 2026-09-05. Former ticket 07 moved to [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]]. Core pool machinery from ticket 09 moved to [[plan/core-creation/issues/02-core-actor-pool.md]]. ESO retains Parse in [[issues/08-parse-file-realignment-tracer.md]] and advisory soft-lock behavior in [[issues/09-job-identity-with-advisory-soft-lock.md]]. The sections below preserve the approved publish history.

#### Ticket 6 — Generalized Server Actor produce path → publish `07`

- **Blocked by:** Tickets 2 and 3.
- **What it delivers:** One inner apply path Server-side Actors hand Changes into (no HTTP self-post required). Verifiable: a non-Browser producer applies through the same amend/log/poll-visible sequence as a Browser Change.
- **Architectural shift:** **Significant.** “One mutation path” becomes real for Server producers.
- **Status basis:** Merge/consume once a Change arrives are **accepted**; entry seam/packaging **proposed**.
- **See also:** [[details/actors-and-jobs.md]], [[architecture.md]], [[details/completing-ops.md]]

#### Ticket 7 — Parse File realignment (tracer bullet) → publish `08`

- **Blocked by:** Ticket 6.
- **What it delivers:** Parse plans off the apply queue, concludes through inner apply, returns merge success (not revision CAS refuse / bare ack). Other Browsers learn by poll + rewind/replay. **Does not** require multi-job identity, cancel, or soft-lock chrome (Parse stays request-scoped for this ticket).
- **Architectural shift:** **Significant.** First real non-Browser Actor on the framework; proves Ticket 6 without inventing the job runtime.
- **Status basis:** Fit **accepted** as observation; realignment **proposed**.
- **See also:** [[details/actors-and-jobs.md]], [[details/relation-to-relaxed-concurrency.md]], [[details/as-implemented-facts.md]]

#### Ticket 8 — Job identity with advisory soft-lock (one vertical) → publish `09`

- **Blocked by:** Ticket 6; preferably after Ticket 7 so the produce path is proven by Parse first.
- **What it delivers:** Client-held job identity, launch that returns before apply, cancel that stops further Changes (not Undo), **and** advisory soft-lock as part of the same surface: the lock is owned by the job; job completion clears it; the lock indicator is an access point to the job. Edits under the lock remain legal and merge.
- **Architectural shift:** **Significant.** One concept footprint: reservation lifecycle is not a second product beside jobs. Not the plug-in-bus pattern (ESO scope; see [[overview.md]] § What this is not).
- **Status basis:** Soft-lock **meaning accepted**; job↔lock lifecycle coupling is **accepted direction** (user quiz); issuance/expiry/chrome details **proposed**. Job identity/launch/cancel **proposed** / none exists yet.
- **Why not soft-lock before job, or two tickets:** User: lock linked to job, completion unlocks, indicator opens job. Separate tickets would invent two surfaces that must immediately couple. Parse is the tracer for produce path **without** this footprint.
- **See also:** [[details/soft-lock.md]], [[details/actors-and-jobs.md]], [[details/undo.md]], [[architecture.md]]

### Polish and retained decisions

#### Ticket 9 — Child-list approximation polish → publish `10`

- **Blocked by:** Ticket 4.
- **What it delivers:** Better ordered-list approximation while preserving occurrence-bag Accept Both.
- **Architectural shift:** None new.
- **Status basis:** Algorithm **later / not locked**.
- **See also:** [[details/conflict-resolution.md]], [[details/merge-invariant.md]]
- **Parallelism:** After 4; parallel with 6–8 and with Ticket 5 implementation follow-ons.

#### Ticket 10 — Completing-ops pattern beyond timing → publish `11`

- **Blocked by:** Ticket 6.
- **What it delivers:** Locked fill-in pattern (not only timing): Server completes missing Ops **in the same Change**; Clients see them on History with that Change.
- **Architectural shift:** Server completion as Actor ops inside the poster’s Change (distinct from amendment and rewind/replay).
- **Status basis:** Timing **accepted**; rest **proposed**. Timing already constrains Tickets 2–3 (same-Change fill-in) — no late wire surprise if that constraint is kept.
- **See also:** [[details/completing-ops.md]], [[details/actors-and-jobs.md]], [[details/client-consume.md]]

#### Ticket 11 — Unrestricted Undo desirability (decision only) → publish `12`

- **Blocked by:** Ticket 3 (History must survive consume). Soft constraint: Ticket 3 must not freeze History as own-posts-only.
- **What it delivers:** Answer the retained **open** question; invent no Undo protocol until answered.
- **Architectural shift:** **Significant if yes** (cross-Actor Undo).
- **Status basis:** **Open** on purpose. Cancel-is-not-Undo **accepted**.
- **See also:** [[details/undo.md]], [[details/open-questions.md]], [[details/client-consume.md]]
- **Why not earlier:** Does not force Post/Poll envelope or amendment kinds. Only History model must stay extensible (Ticket 3 constraint). Product can answer after critical flaws; no false block on 0–4.

### Accepted architecture (not parked; not separate tickets)

- **Load packages = Graph / state transfer** for Nodes/children the Client does not yet hold — **accepted** (Round 4; overview). Not Ops-driven residency; not genesis replay. Do not implement Load in this program from these tickets.
- **One global Server arrival order / revision sequence** — **accepted**. Posts/polls carry last revision **received from the Server**. Not per-Workspace revisions.

### Still parked (not tickets)

- State-endpoint producer duty / what a partial view may believe (user paused Round 3).
- Server-partial Local Graph as a designed mode.
- Action-against-Change framing (no stake).
- Job residency packaging details (what a job emits vs what Browser must Load) — still proposed/parked detail, not Load-as-Ops.

## 4. Compact dependency shape

Initial (critical flaws):

- `0 ∥ 1 → 2 → 3 → 4`

Later:

- `2+3 → 5` (recovery **decisions** early; implement recovery only after accept)
- `2+3 → 6 → 7` (Actor path, then Parse tracer)
- `6 (+7 preferred) → 8` (job **with** soft-lock, one vertical)
- `4 → 9` (child-list polish)
- `6 → 10` (completing-ops pattern)
- `3 → 11` (Undo decision)

Critical-flaw block first. Ticket 5 decides early without blocking 0–4 when extension constraints hold. Actor spine is `6 → 7 → 8` with soft-lock folded into 8. Envelope unification is **in Ticket 0**, not a later optional ticket.

## 5. Significant architectural shifts (summary)

- **Tickets 0–4:** shared success envelope type (channels still distinct); Server amendment; Client rewind/replay; Reject/reload removed for recoverable merge; History retained.
- **Ticket 5:** possible Owned `deleted` recovery + orphan policy (decision early; build later).
- **Tickets 6–7:** one Server Actor produce path; Parse leaves revision-CAS special case.
- **Ticket 8:** job runtime and advisory reservation as **one** surface (lock owned by job).
- **Ticket 11:** possible cross-Actor Undo (only if open question answers yes).
- **Pinned accepted constraints:** Load = Graph transfer; one global Server revision sequence; no genesis replay.

## 6. Quiz answers (this round and prior)

Prior:

1. Granularity OK with child-list separate.
2. Child-list own ticket (Ticket 4).
3. Scaffold early = Ticket 0; linear `0 ∥ 1 → 2 → 3 → 4`.
4. Soft-lock/jobs/delete-against-edit after critical flaws — now sequenced with merges below.
5. Early expand — now includes shared envelope type in Ticket 0.

This round:

1. **Later grouping.** Accepted as probably OK; soft-lock/job merged to shrink footprint.
2. **Soft-lock vs job.** Neither first as separate products. **Parse (7) tracers the produce path without jobs.** Then **Ticket 8** delivers job identity **and** soft-lock together (lock owned by job; completion clears; indicator opens job).
3. **Envelope.** Separate channels OK; **shared type preferred**. Folded into Ticket 0; optional later unify ticket **removed**. Caveat: Post still does not apply the list.
4. **Decision-first OK.** Delete-against-edit/orphan decisions moved to Ticket 5 (early); implementation later. Extension constraints on 0–4 prevent late wire pain. Completing-ops timing already accepted; Undo stays later with History extensibility constraint.
5. **Load.** User claim verified: Graph/state transfer for unloaded Nodes/children is **accepted**, not parked. **Global revision.** User claim verified: one global Server sequence is **accepted**; not per-Workspace.

## 7. Remaining user choice (narrow)

1. Approve merged Ticket 8 (job+soft-lock) and Ticket 0 shared-envelope pin for publish.
2. Confirm Ticket 5 may decide delete-against-edit **without** implementing recovery in the same project.
3. Anything else to merge for a still-smaller footprint (e.g. fold Ticket 10 into 6)?

## 8. WORK.md mutations for parent (do not apply here)

- **refine** Active [[plan/event-sourced-ops/to-tickets-draft.md]] — outcome: quiz answers reconciled; approve revised sequence (Tickets 0–11) then publish `issues/01`–`12`; architectural pins recorded in detail docs.
- **add** Pending (optional): [[plan/event-sourced-ops/reconcile-architecture-sequence-report.md]] — delegated reconcile report for parent synthesis.
- **note** [[plan/relaxed-concurrency/]] is a build-upon layer (Stage done) — gate removal delivered in issue 02; old slice 2–3 protocol superseded by [[details/relation-to-relaxed-concurrency.md]].

No Stage change. No `issues/` files written. No software edits. No commit.
