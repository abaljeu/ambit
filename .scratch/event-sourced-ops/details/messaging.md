# Messaging — post, poll, and what a Reject means

The two Client channels and the response shape. The conceptual split is **accepted**. The exact types and fields are **proposed** and are not implemented.

## Two paths (accepted)

Post and Poll are **not** the same path. An earlier pin — "Poll is a post with an empty Change list", one handler, one envelope — is **superseded**. Do not follow both.

**Post.** The Actor sends Changes. Many posts may be in flight; the Client does not wait for one at a time. The acknowledgement **informs** the Client that **external** Changes exist. The Client **notes the baseline**. The acknowledgement does not carry the sequence and does not apply a tail.

**Poll.** When the posting queue is empty, the Client polls **from that baseline**, rewinds to the baseline, and applies the returned Change list ([[client-consume.md]]).

**Acknowledgement payload (accepted).** A flag is enough. The catch-up point is the last revision the Client already received, so a second number on every pipelined acknowledgement is not needed unless a later increment shows it is.

## Why this shape

The problem it solves: an optimistic Client sends many Changes and does not want to wait for one at a time. Every in-flight post carries the same last-received revision until the first acknowledgement, so if each acknowledgement carried the tail, every one of them would repeat the same list. The Server does not track which Client has what, and inventing that tracking to save a single envelope was refused.

Rejected alternatives: wait for one post at a time; make the Server track Clients; require the Client to batch all pending work into one request.

## Recoverable collision is success (accepted)

An Actor posts a Change planned on a common prior. Another Actor's Change already landed. The posted Ops are not valid as they stand — a stale field value, a stale Replace span, or a merge that must amend.

That is **success**. The Server applies amendment order and answers **HTTP 200** with the Change list. The Client rewinds and replays. The other Actor's data rides the **success** path.

For this case, a success acknowledgement is not a confirmation echo of what was submitted.

## Reject that remains

Authentication, a malformed request, and similar request failures. That is all. A name clash is merge, not Reject ([[conflict-resolution.md]]).

## History (accepted)

**Neither post nor poll clears the Client's History.** Today's behavior — a poll with a non-empty tail clears History — is **software debt**, not the standard.

## Still proposed

- The exact response type. Whether a post returns the poll envelope, or keeps its own acknowledgement type carrying the same kind of Change list.
- Envelope fields. Both types already carry a Change list, but they differ in stamps, readiness, and an optional persistence message. Sharing a kind may still grow fields.
- Duplicate-retry behavior. A stored echo against a freshly computed tail; a lost acknowledgement can mean the retry should return the current tail.

## Obstacles, assessed

There is **no hard obstacle** to a shared Change-list success envelope. The Server already computes the poll tail; a success acknowledgement can be that same tail after a post. What exists today is a different kind of acknowledgement list and a different apply function — a contract and migration problem, not a false design.

Two naive implementations are traps rather than reasons the design cannot work, and both are answered: applying the acknowledgement list on top of the optimistic Local Graph is wrong (rewind first), and reusing the poll History-clear on post is wrong (neither clears).

The contracts that must change are listed as facts in [[as-implemented-facts.md]]. They make today's tests and specs fail on purpose; they do not make the design unsafe.

Not obstacles: Reject (a different status, not the success list); Load packages (Graph transfer, parked); update-time stamps riding inside the Change list; whether revision stays one global number (parked).
