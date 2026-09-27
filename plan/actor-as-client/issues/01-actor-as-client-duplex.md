# 01 — Actor-as-client duplex

**Type:** grilling
**Status:** defined
Blocked by: None

## 1. Question

What Core door does an Actor use, and is that door Poll or a future push?

Provisional aim (Alan 2026-09-27 chat, not yet a Decision): an Actor is a Core client in the same relation as the Browser. It owns internal behavior. It talks Core for Graph, Events, and Changes. Today that talk is mostly Poll, not push. It is not a special mailbox citizen with a private Graph extract.

Grill:

1. **Same door as Browser?** — Does the Actor call the same Core mailbox doors the Browser uses (Changes, Query, Poll / events since an event id), or a sibling door that is still Core API?
2. **Poll vs push** — Does the remake stay Poll-only like the Browser, or must it add push from Core to a live Actor? What would push unblock that Poll does not?
3. **Not a mailbox citizen** — What mailbox privileges, if any, remain after the Actor is a client (lifecycle admit, cancel, finish) versus Graph and Event traffic?

Do not implement.

## Comments

- 2026-09-27 — Filed with the [Actor as client](../map.md) chart. Status `defined`.
