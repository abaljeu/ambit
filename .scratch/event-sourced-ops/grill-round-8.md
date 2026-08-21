# Grill round 8

Hangs on the pipelined ACK **signal**. Home: [[pipelined-post.md]]. **"Poll = empty POST" superseded** (two paths) — not this Q; Poll already exists as GET `/ambit/poll`.

## User-facing (speak unchanged)

❓ **Q1** - **ACK payload**: Pipelined ACK informs that external Changes exist; the Client notes the **baseline**, then Polls from it when the queue is empty. Does that ACK include the baseline, or only a flag?

A) Flag only. Baseline is the last Server Revision the Client already received. ACK says "external Changes exist."

B) ACK includes the **baseline** Revision (the catch-up-from point).

C) ACK includes the Server's **current** Revision after this POST (not the same as catch-up-from unless no others landed).

➡️ **A.** Catch-up-from is last-received. Do not invent a second number on every pipelined ACK unless B is needed.

## WORK.md

Add this file to the Active related list. Stage `charting`.
