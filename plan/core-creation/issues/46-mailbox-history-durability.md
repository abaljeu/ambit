# 46 — Mailbox History durability

**Status:** defined
**Blocked by:** [[35b-browser-run-hello.md|35b — Browser Run hello]].

## Context

Split from [35b — Browser Run hello](35b-browser-run-hello.md) §6. The Browser Run hello path can prove Owned child `hello` without History surviving process restart. This ticket makes mailbox-owned History (EventLog audit sequence for Change and Actor lifecycle Events) durable across restart.

Follow Story path / modules named in [[../arch.md|Core creation architecture]] for History durability. Process-lifetime EventLog from 34b/35b stays until this lands.

## What to build

### 1. Persist mailbox History

1. [ ] Persist Change and Actor Events (Change / ActorStart / ActorStop bodies as used post-SES) so the audit sequence survives restart.
2. [ ] Load mailbox History — restore the past/future (or EventLog) sequence on mailbox startup.
3. [ ] Graph / EventLog durability stay distinct where arch already separates them; this slice covers mailbox History survival.
4. [ ] Undo stays Change-only — Actor lifecycle Events are not Undo targets.

## Out of scope

1. Browser Run `?` path and HTTP Adapter Command door — [35b — Browser Run hello](35b-browser-run-hello.md).
2. Outside-Core lifecycle proof — [34b — Outside Core lifecycle proof](34b-outside-core-lifecycle-proof.md).
3. Reopening SES Event-only contract tickets.

## Comments

- 2026-09-18 — Split from 35b §6. Alan: §7 Browser proof can test without this ticket.
