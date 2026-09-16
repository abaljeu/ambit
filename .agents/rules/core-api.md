Core API for this increment:

- [[plan/core-creation/project.md]] Agent instruction
- [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]
- [[CONTEXT.md]] Core API

## EventId serial

Only the mailbox may call `EventId.next` (or an equivalent tip bump). Mailbox here is the Core EventLog / admit path.

Client and Shared callers mint a draft Event with `EventId.zero`, or rebuild a received Event from the wire (`Ev.fromJson` / decode). They do not advance the serial.
