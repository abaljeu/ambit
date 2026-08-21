# POST `/changes` and Poll — two paths

**Unification superseded.** Earlier pin "Poll = POST `/changes` with empty Changes" (same handler / same envelope) is **withdrawn**. They are **not** the same. Home for the split: [[pipelined-post.md]]. Do not follow both.

Still accepted (not unification): recoverable kick-back is 200 Merge ([[slice-2-obsoleted.md]]); Client consume of a **Change list** is undo-to-baseline + replay ([[merge.md#Client correction]]); **neither** POST nor Poll **clears History** (today's Poll clear is debt); leftover pending sent **unamended**; POST carries last-received Server Revision; no Client-tracking.

## Two paths (accepted)

**POST `/changes` (or `/change`)** while in flight: ACK **informs** that **external** Changes exist. Client **notes the baseline**. Does **not** apply the full Change list on each ACK.

**Poll** (own route today: GET `/ambit/poll`): when the posting queue is empty, Poll **from the baseline**, undo to baseline, apply the Change list.

Software and exact type fields are **not** implemented. ACK payload (flag vs included baseline) not pinned.

## What must change

- Pipelined ACK is a **signal**, not a repeated `getChangesSince` tail.
- Queue-empty Poll returns the Server sequence; Client undo-to-baseline then apply. Do not clear History.
- Do not treat Poll as empty POST.

## Still true vs today (facts)

- Poll (`PollResponse`) already has a Change list. Non-empty tail **clears** History today — **debt**.
- POST-ACK (`ChangeBatchAck`) is still confirmation + `SetUpdateTime` suffixes.
- Load packages stay Graph transfer ([[poll-load-conveyance.md]]). `/state` parked.

## Distinct from

Amendment order produces. Rewind+replay consumes (on **Poll**). Fill-in completes one Change. Load packages parked.

Worker reports: [[rewind-replay-unified-msg.md]], [[unified-messaging-obstacles.md]], [[post-ack-history.md]], [[slice-2-obsoleted.md]], [[pipelined-post.md]].
