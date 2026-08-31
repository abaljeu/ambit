# Relation to relaxed concurrency

[[plan/relaxed-concurrency/]] is a **build-upon layer** on this project. Event-sourced-ops is the foundation: active concurrency standard, protocol, and implementation. Relaxed concurrency retains verified Graph/Ops facts, audit documents, shared rejections, and frontier open questions D–F — not slice specs, merge-sync protocol, or active implementation plans.

## Roles

**This project** owns delivery: global revision gate removal, merge, amend, consume, and full-list Replace wire (issues 01–05, 13–14).

**Relaxed concurrency** records upstream evidence that informed that work:

- Verified knowns ([[plan/relaxed-concurrency/map.md]])
- Audits: [[plan/relaxed-concurrency/replace-span-cas-feasibility.md]], [[plan/relaxed-concurrency/child-occurrence-uniqueness.md]]
- Shared rejections (genesis replay, id-anchored Replace, convergence without rejection)
- Frontier questions D–F (hybrid authority, identity across reparse, undo under command/event split)

It is not a competing implementation project or a parallel delivery track.

## What the relaxed-concurrency map still contributes

- Full Event Sourcing with **replay from genesis through historic parsers** stays **rejected**. Parse already logs Op diffs. Client catch-up is rewind and replay on a short tail, not replay from empty.
- **Proposed nuance (this project):** a **permanent** global Change log makes genesis **derivable** by inverting the retained sequence to its first entry — not routine, not log-as-truth, not parser replay ([[permanent-history-and-genesis.md]]). The DB projection remains how current state loads.
- Ops stay per-Node field or per-parent child list. There is no graph-wide Op.
- No silent server-side relocation inside the replace path.
- Gate removal and per-Op compare-and-swap on apply were **delivered here** (issue 02). Rejection stays legal where this framework still rejects.
- What was out of scope in that map — order CRDTs, offline editing, genesis replay — stays out of scope here.

## What this project superseded from relaxed-concurrency history

The old relaxed-concurrency **slice 2–3** plan — Reject carrying remote Changes, Client merge-sync and replan, post again — is **obsolete** for recoverable kick-back. That behavior is ESO merge success with a Change list (issues 01–05).

The old **slice 1** (drop the global revision gate) was also delivered here (issue 02), not as a parallel build in relaxed-concurrency.

The no-silent-relocation decision stands. The remaining Reject is authentication, malformed requests, and the like.

## How this project extends the map picture

The relaxed-concurrency map described per-path apply if compare-and-swap matches, otherwise refuse. This project is a **merge** of Changes from a common prior, for any Actors — not only two Browsers:

- Server arrival gives a **global order**.
- The Server **produces** a sequence: common prior, other accepted Changes, newest amended.
- The Client **consumes** it by rewind and replay.
- Independence is still **conveyed**, not skipped.
- A collision is not always a Reject: same text and same name keep the first value and add an `amb-conflict` child; child lists Accept Both; classes are a set delta.
- A recoverable kick-back returns **success with a Change list**, not a Reject.

## Tensions that must not be papered over

- **Model and wire.** The map's gate-removal increment claimed no model change and no wire change. This project **is** a merge model, and, when the messaging change lands, a change in what an acknowledgement means. They are not the same increment.
- **Behavior to beat.** The map kept span compare-and-swap and attribute compare-and-swap as the refusal boundary. This standard treats those apply paths as behavior to beat for the cases that Accept Both or keep both texts.
- **Leftover pending.** Consume of a posted Change is rewind and replay of the success list. Leftover pending stays unamended and is sent on the next post ([[client-consume.md]]). Replanning the *failed posted item* is what became obsolete.
- **This is still not log-as-truth Event Sourcing.** Permanent retention and derivable genesis do not make the log the sole record or reopen parser replay from empty.
