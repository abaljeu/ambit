# Grill round 7

Open problem only. Do not lock. Home: [[pipelined-post.md]].

## User-facing (speak unchanged)

❓ **Q1** - **Many in-flight POSTs vs one tail**: Optimistic Client sends many Changes and does not want to wait one-in-flight. Each POST carries the same last-received Server Revision until the first ACK, so each 200 would repeat the same Change list. The Server does not track Clients. Which way?

A) Wait — one POST in flight; leftover pending stays local until ACK, then send unamended.

B) ACK names only what this POST applied (ids / that Change as applied). Tails come from Poll (or a later POST). Envelope's Change list is not repeated on every pipelined ACK.

C) Batch many pending Changes into **one** HTTP POST (not many POSTs).

D) Server tracks Clients (who already has through rev R). Not built; they noted it has not been tracking.

E) Leave open; name the clash only.

➡️ **E** until one of A–C is clearly cheaper. Do not invent Client tracking to save the envelope.

## Answer

**Pinned** (not A wait-one, not D Client-tracking, not E leave-open). User: ACK **informs** that there are **external** Changes; Client **notes the baseline**; **when the posting queue is empty**, **Poll** from that baseline; **undo to baseline** + apply the new Change list.

Not B as asked (ACK = applied ids only) — closer in spirit (ACK is not the full tail) but the signal is **external Changes exist**, then queue-empty Poll. Not C (one batched HTTP POST required).

Home: [[pipelined-post.md]]. Later: **"Poll = empty POST" superseded** — two paths ([[unified-messaging.md]]). GET `/ambit/poll` already exists.

## WORK.md

Move [[pipelined-post.md]] toward accepted. Stage `charting`.
