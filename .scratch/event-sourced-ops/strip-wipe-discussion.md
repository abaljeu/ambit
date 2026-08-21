# Strip wipe discussion

Worker slang (`true wipe`, pending-queue wipe, `rejectPending` as a Merge topic) is **removed**. Not a design case. Stage still `charting`.

## Remaining Reject

Auth, malformed POST, and similar request failures. Not concurrency, not name clash, not a wipe concept.

## What was stripped

- Deleted [[rejectPending-origin.md]]
- Trimmed wipe / `rejectPending` / `ServerRejected` asides from [[name-clash-amb-conflict.md]], [[slice-2-obsoleted.md]], [[reject-vs-success-ack.md]], [[unified-messaging.md]], [[more-general-relaxed-concurrency.md]], [[unified-messaging-obstacles.md]]
- Later pointer on [[.scratch/relaxed-concurrency/map.md]] no longer lists wipe. The map's own older slice 2 blocker line (its facts) was left.

Amendment order, rewind+replay, POST-ACK History-clear constraint: untouched.

## WORK.md mutations

`remove` related link [[rejectPending-origin.md]] from the Active [[project.md]] line. Do not remove the project item. Add this report if useful; not required as lasting design.
