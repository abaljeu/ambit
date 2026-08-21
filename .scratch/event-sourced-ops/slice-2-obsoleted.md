# Slice 2 obsoleted (recoverable kick-back)

Worker report. Design only. Software does **not** do 200-Merge yet. Stage still `charting`.

## What is now accepted

Recoverable concurrent kick-back (B POSTs on a stale common prior; A already landed) is **Merge success**: HTTP **200**, Change list (A accepted, then B amended), Client **rewind+replay**. Success ACK is that list, not a confirmation echo.

**Success envelope accepted** ([[unified-messaging.md]]): POST and Poll share a Change-list response; POST sends Changes, Poll does not; rewind then apply; POST does not clear History.

Slice 2 of [[.scratch/relaxed-concurrency/map.md]] (Reject + remote Changes + Client merge + replan `pendingChanges` + POST again) is **obsolete** for that case. G's "no server weak-form Replace" **stands**. Slice 1 (drop the revision gate) **stands**.

## What Reject remains

Auth, malformed POST, and similar request failures. Name is Merge (`amb-conflict`), not Reject ([[name-clash-amb-conflict.md]]).

## Still open

- Poll History-clear **resolved**: neither POST nor Poll clears History. Today's Poll clear is debt. "Poll = empty POST" **superseded** ([[pipelined-post.md]]).
- Exact response type/fields (`PollResponse` vs `ChangeBatchAck` + persist message).
- Remaining local **pending after** a 200 POST: **accepted** (grill-round-6; Q1 B superseded) — send unamended leftover pending; Server amends on apply. Slice 2 replan of the *failed posted item* is obsolete.
- Wire/software: ACK is still confirmation + `SetUpdateTime` suffixes.

## Files changed

- [[reject-vs-success-ack.md]] — resolved
- [[unified-messaging.md]] — success envelope accepted
- [[merge.md]] — envelope accepted; 200 path
- [[more-general-relaxed-concurrency.md]] — first tension resolved
- [[.scratch/relaxed-concurrency/map.md]] — pointer; slice 2 row marked obsolete; spec not rewritten
- [[.scratch/relaxed-concurrency/project.md]] — pointer
- [[project.md]], [[vocab.md]], [[collab-vocab.md]], [[goal.md]], [[unified-messaging-obstacles.md]]
- This file

No software. No [[WORK.md]] edit here.

## WORK.md mutations

- Update Active [[project.md]] related: add [[slice-2-obsoleted.md]]; move [[unified-messaging.md]] from proposed to accepted (success envelope).
- `block` Pending [[.scratch/relaxed-concurrency/map.md]] (slices 2–3 reject+replan) — superseded for recoverable kick-back by event-sourced-ops 200 Merge (blocker: [[slice-2-obsoleted.md]]). G / no server weak-form still stands.
- Do **not** `block` or `remove` Pending [[.scratch/relaxed-concurrency/spec.md]] (slice 1 drop gate).
