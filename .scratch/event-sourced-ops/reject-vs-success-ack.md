# Slice 2 Reject vs success ACK

**Resolved.** Recoverable concurrent kick-back uses the **200 path**. Slice 2 Reject+replan is **obsoleted** for that case. Report: [[slice-2-obsoleted.md]]. Unified success envelope **accepted** ([[unified-messaging.md]]).

## The shared situation

Actor B POSTs a Change planned on a common prior. Actor A's Change already landed on the Server. B's posted Ops are not valid as-is against the Server Local Graph (stale `old*`, stale Replace span, or a merge that must amend).

## Decision

**Merge success (accepted):** Server applies amendment order (A accepted, then B amended). POST returns **HTTP 200** with that Change list (same kind as Poll). B rewind+replays. Other Actors' data rides the **success** path. Success ACK is **not** a confirmation echo for this case.

**Slice 2 obsoleted** for this case: Reject + remote Changes + Client merge + replan `pendingChanges` + POST again ([[.scratch/relaxed-concurrency/map.md]]). Software does not do 200-Merge yet.

## Reject that remains

Auth, malformed POST, and similar request failures. Name is Merge (`amb-conflict`), not Reject.

## What the two pictures were (history)

**Slice 2 (obsolete for recoverable kick-back).** Apply uses per-Op CAS. Intervening fail → Reject body includes remotes; B replans and resubmits. Success ACK was confirmation echo.

**200 Merge (accepted).** Server Merges; 200 + sequence; B rewind+replays.

## Second tension (accepted — grill-round-6; Q1 B superseded)

After rewind+replay, leftover pending stays **unamended**; next POST sends it; Server amends as newest. Not Client-amend (old B), not drop (C). POST/Poll carry last-received Server Revision only. [[more-general-relaxed-concurrency.md]].
