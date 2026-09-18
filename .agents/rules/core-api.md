Core API for this increment:

- [[plan/core-creation/project.md]] Agent instruction
- [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]
- [[CONTEXT.md]] Core API

## EventId serial

EventId is Zero or a positive Int. `EventId.next` of Zero is Zero. EventLog assigns stored serials. Get-all and drafts use `EventId.zero`. There is no stored event id 0, so `since` of Zero is every stored Int. Wire 0 is Zero. A positive wire int is that stored Int. Shape: [Core creation architecture](plan/core-creation/arch.md) Module **Ev** and Module **EventLog**.

Client and Shared mint a draft Event with `EventId.zero`, or rebuild a received Event from the wire. They do not assign stored serials.
