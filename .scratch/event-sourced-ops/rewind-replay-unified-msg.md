# Rewind+replay and unified messaging

Worker report. Topic homes: [[merge.md#Client correction]], [[unified-messaging.md]].

## Verdict

**Client correction — accepted.** Optimistic Client must match Server state by **rewind + replay**: rewind to the common prior (or the base the Server sequence is based on), then replay the Server-produced sequence (other accepted Changes, then the newest Actor's amended Change). In-place transform is **not** an equal alternative. `SetText C→B` is not the strategy. This is **not** genesis replay.

**Unified POST-ACK / Poll — proposed, fits, not locked.** Same response kind (Change list). POST sends Changes; Poll does not. One receive/apply path (rewind+replay) after that. It fits amendment order (Server produces) and rewind+replay (Client consumes). Wire today is **not** that.

## What must change on ACK vs Poll

- **Poll** already is a Change list + `Op.apply`. Keep that kind.
- **ACK** today is a confirmation echo: submitted prefix exact; suffix **`SetUpdateTime` only**; suffixes do not enter History; `reconcileAck` exact-match. Types both have `changes`, but ACK's list is not a Server sequence. Unified messaging changes ACK's **kind** to Poll's kind (sequence to apply), including other accepted Changes and the amended own Change.
- Load packages stay Graph transfer (parked). `/state` stays parked.

## Tensions with today's wire

- undo-spec ACK contract (exact prefix + stamp-only suffix) **breaks** if ACK carries an amended Change.
- Poll-with-tail **clears** History today (**debt**). Design: neither POST nor Poll clears ([[unified-messaging.md]]).
- [[.scratch/relaxed-concurrency/map.md]] slice 2 is a **Reject** path (remote changes + merge + replan pending). Unified messaging is the **success** envelope. Do not resolve slice 2. Reject stays a different status. Recoverable reject-then-merge may shrink if success ACK already carries the sequence — named, not decided.

## Distinct

| Thing | Role | Status |
| --- | --- | --- |
| Amendment order | Server **produces** the sequence | accepted |
| Rewind+replay | Client **consumes** the sequence | accepted |
| Unified POST-ACK / Poll | One response kind; POST posts, Poll does not | proposed |
| Fill-in | Completes **one** Change | timing accepted |
| Load packages | Graph transfer | parked |

## Files changed

- [[merge.md]] — Client correction accepted; same-text no longer `SetText C→B`; unified messaging pointer
- [[unified-messaging.md]] — new topic (proposed)
- [[vocab.md]] — Merge row; Next increment
- [[conflict-kinds.md]] — text kind uses rewind+replay
- [[collab-vocab.md]] — speak
- [[goal.md]] — draft + grill record
- [[project.md]] — summary + links (Stage still `charting`)
- [[undo.md]] — History vs Poll-clear
- [[server-fill-ops.md]] — fill-in ≠ rewind+replay
- [[poll-load-conveyance.md]] — facts stay facts; pointer to proposal
- [[amendment-order.md]] — Q1/Q2 closed
- This file

No software. No [[CONTEXT.md]]. No [[WORK.md]]. No Stage change; [[.scratch/index.md]] not regenerated.

## WORK.md mutations

Update the Active [[project.md]] related list: add [[rewind-replay-unified-msg.md]] (accepted rewind+replay; proposed unified messaging) and [[unified-messaging.md]]. No `add` / `move` / `block` / `remove` of a work item. Grill stays Active.

## Open questions

1. After rewind+replay, does this Browser's History invert only the **amended own** Change, while other-actor Changes in the list are not undo entries? Today's Poll-clear fights that.
2. If the Browser has **more pending** after the POSTed batch, does rewind+replay then replan remaining pending on the new base (slice 2–3 speech), or is that out of this increment?
