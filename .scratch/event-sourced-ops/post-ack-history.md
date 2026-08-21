# POST-ACK History and rewind

Worker report. Home: [[unified-messaging.md]]. Success envelope **accepted**. Poll History-clear **resolved**.

## What is now accepted

1. **Rewind, then apply the response Change list.** Do not apply the list onto the optimistic unamended Local Graph. This is [[merge.md#Client correction]] on the unified success envelope.
2. **Neither POST nor Poll clears History.** **"Poll = empty POST" superseded** — two paths ([[pipelined-post.md]]). Today's Poll-with-tail clear (`applySyncResponse`) is **software debt**.

Amendment order (Server produces) and rewind+replay (Client consumes) stay distinct. Fill-in still completes one Change. Load packages stay Graph transfer (parked).

## What stays proposed / open

- Exact POST return type (`PollResponse` vs `ChangeBatchAck` with the same list kind). Wire still today's ACK confirmation + `SetUpdateTime` suffixes.
- After POST, History inverts the amended own Change (thoughts in [[undo.md]]) — not re-asked here.

## Files changed

- [[unified-messaging.md]] — unification superseded; neither clears History
- [[unified-messaging-obstacles.md]] — Poll History-clear resolved
- [[merge.md]] — neither POST nor Poll clears History
- [[undo.md]] — neither clears; today's Poll clear is debt
- [[poll-load-conveyance.md]] — today's Poll clear is software debt
- [[vocab.md]], [[collab-vocab.md]], [[goal.md]], [[project.md]] — speak / summary
- This file

No software. No Stage change. No [[WORK.md]] edit here.

## WORK.md mutations

Update the Active [[project.md]] related list: add [[post-ack-history.md]] (POST rewind+apply and no History-clear accepted; envelope still proposed). No `add` / `move` / `block` / `remove`.
