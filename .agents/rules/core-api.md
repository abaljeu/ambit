Core API for this increment:

- [[plan/core-creation/project.md]] Agent instruction
- [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]
- [[CONTEXT.md]] Core API

## EventId serial

Only EventLog may call `EventId.next`. EventId has private id. EventId.fromJson/toJson bypasses. Only serializing should use the fromJson/toJson functions. Any event not from these sources should have id 0.

Client and Shared callers mint a draft Event with `EventId.zero`, or rebuild a received Event from the wire (`Ev.fromJson` / `Ev.toJson`). They do not advance the serial.
