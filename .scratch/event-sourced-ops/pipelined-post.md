# Pipelined POST vs redundant tails

**Pinned (grill-round-7).** **POST and Poll are not the same.** Earlier "Poll = empty POST" is **superseded**. Do not follow both.

## Clash (was)

Optimistic many POSTs. Wanted: no wait / no redundant tails / no Client tracking. Same last-received Revision on in-flight POSTs would have repeated the tail on every 200.

## Pin (user words)

ACK **informs** the Client that there are **external** Changes. The Client **notes the baseline**. **When the posting queue is empty**, **Poll** for Changes **from the baseline**, then **undo to baseline** plus **apply the new Change list**.

1. Many POSTs may be in flight. Not wait-one. Not Server Client-tracking.
2. POST ACK does **not** carry or apply the full Change tail. It **signals** that external Changes exist.
3. Client **notes the baseline** (catch-up-from; last-received Server Revision when that is the same thing).
4. Queue empty → **Poll** from that baseline (Poll is its own path, not empty POST).
5. **Undo to baseline** + apply the Poll list. That undo is consume rewind, **not** unrestricted Undo ([[undo.md#Unrestricted Undo desirability]]).

Leftover unamended pending: queue-empty means those have been POSTed and ACKed; Poll is the Server sequence.

ACK payload (flag vs included baseline): not pinned. [[grill-round-8.md]].

## WORK.md

Keep this file **accepted**. Move [[unified-messaging.md]] off "POST and Poll identical" (proposed / superseded unification). Stage `charting`.
