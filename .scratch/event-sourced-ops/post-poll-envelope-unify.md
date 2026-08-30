# What unifying post/poll envelopes waits on

Answer for the parent. Sources: [[details/messaging.md]], [[architecture.md]], [[details/client-consume.md]], [[details/open-questions.md]], [[details/decision-log.md]], [[to-tickets-draft.md]].

## 1. What "unify" means here

**Unify post/poll envelopes** means choosing the exact success **response type**: whether a post acknowledgement returns the **same envelope type** as Poll (a shared success shape with a Change list that may be empty), or keeps its **own acknowledgement type**.

It does **not** mean collapsing the two channels. Post and Poll stay **separate paths** (accepted): Post is a signal that external Changes exist and a baseline note; Poll (queue empty) carries the Change list for rewind and replay. The older "Poll is an empty post / one envelope" pin is **superseded** ([[details/messaging.md]], [[details/decision-log.md]] Round 7).

## 2. Quiz pin (current)

**Pinned direction:** share one success **type** for Post and Poll. User preference: smaller footprint / easier to verify as correct. Separate channels remain. Semantic caveat: Post must not be treated as an apply tail even when the type can carry a list.

**Ticket placement:** fold into **Ticket 0** (behavior-identical expand lands the shared type; Post still succeeds with `externalChanges = false` / confirmation-echo behavior until Tickets 2–3). The former optional late unify ticket is **removed**.

Exact field inventory (stamps, readiness, empty-list encoding, persistence message) may still be refined in implementation.

## 3. What it waited on (historical)

| Item | Status |
| --- | --- |
| Two channels (signal on post; list on poll) | **Accepted** |
| Acknowledgement payload: a flag is enough; catch-up is last Server-received revision | **Accepted** |
| Shared success **type** | **Pinned direction** (was proposed) |
| Envelope field details | **Still proposed** (refine under Ticket 0+) |
| Duplicate-retry / lost-ack returning current tail | **Still proposed** |
| Shared type as design safety | Assessed: **no hard obstacle** |

## 4. Paste for the user

Unifying post/poll envelopes means locking a shared success **response type** while keeping two **channels** (post = signal + baseline; poll = Change list). That shared-type direction is now **pinned** and belongs in Ticket 0 expand, not as late optional cleanup. Post still must not apply a list from the ack; Poll remains the apply channel.
