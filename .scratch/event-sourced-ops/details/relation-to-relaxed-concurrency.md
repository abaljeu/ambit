# Relation to relaxed concurrency

This project is a **more general relaxed concurrency** than [[.scratch/relaxed-concurrency/]]. They are siblings. This one does **not** replace or cancel that one, and its knowns and rejections still stand.

## The two pictures

The older destination is small: drop the **global revision gate**, accept a Change when each Op's compare-and-swap matches, and reject a genuine collision. Its later slices carried the remote Changes back in a Reject body, and the Client merged them and replanned the failed item. It stated no model change and no wire change for the first slice.

This project is a **merge** of Changes from a common prior, for any Actors — not only two Browsers:

- Server arrival gives a **global order**.
- The Server **produces** a sequence: common prior, other accepted Changes, newest amended.
- The Client **consumes** it by rewind and replay.
- Independence is still **conveyed**, not skipped.
- A collision is not always a Reject: same text and same name keep the first value and add an `amb-conflict` child; child lists Accept Both; classes are a set delta.
- A recoverable kick-back returns **success with a Change list**, not a Reject.

The older picture is per-path: apply if the compare-and-swap matches, otherwise refuse. This one is **one sequence** that also covers the cases a compare-and-swap would refuse, by amending the newest Change.

## What stays from the older map

- Full Event Sourcing with **replay from genesis through historic parsers** stays **rejected**. Parse already logs Op diffs. Rewind and replay for Clients is a short tail from a shared base, not a replay from empty.
- **Proposed nuance (this project):** a **permanent** global Change log makes genesis **derivable** by inverting the retained sequence to its first entry — not routine, not log-as-truth, not a reopening of parser replay ([[permanent-history-and-genesis.md]]). The DB projection remains how current state loads.
- Ops stay per-Node field or per-parent child list. There is no graph-wide Op.
- No silent server-side relocation inside the replace path.
- The first slice — drop the revision gate, keep per-Op compare-and-swap on apply — is **not cancelled**. Rejection stays legal where this framework still rejects.
- What was out of scope there — order CRDTs, offline editing, genesis replay — stays out of scope here.

## What this project obsoletes

The older slice-2 plan — Reject, carry the remote Changes in the Reject body, let the Client merge and replan the failed pending item, and post again — is **obsolete for recoverable kick-back**. That case is now success with a Change list.

Slice 1 stands. The no-silent-relocation decision stands. The remaining Reject is authentication, malformed requests, and the like.

## Tensions that must not be papered over

- **Model and wire.** The older first slice claimed no model change and no wire change. This project **is** a merge model, and, when the messaging change lands, a change in what an acknowledgement means. They are not the same increment.
- **Behavior to beat.** The older slice keeps span compare-and-swap and attribute compare-and-swap as the refusal boundary. This standard treats those apply paths as behavior to beat for the cases that Accept Both or keep both texts.
- **Leftover pending.** Consume of a posted Change is rewind and replay of the success list. Leftover pending stays unamended and is sent on the next post ([[client-consume.md]]). Replanning the *failed posted item* is what became obsolete.
- **This is still not log-as-truth Event Sourcing.** Permanent retention and derivable genesis do not make the log the sole record or reopen parser replay from empty.
