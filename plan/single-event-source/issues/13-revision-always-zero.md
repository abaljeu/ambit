# 13 — Revision always 0 (diagnostic)

**Status:** done
**Blocked by:** [10 — Boot IndexedDB](10-boot-indexeddb.md)

## Context

Alan approved the SES replan ([Replan — smaller increments for SES 11 and 12](../reports/replan-11-12-smaller-increments.md)). EventId numbers mismatch after the `/state` fix; suspect broke after 10. Mega ticket [11 — One serial event id](11-one-serial-event-id.md) remains a historical `coded` land; this ticket is the redo diagnostic precursor (**11z**), **before** Revision → EventId rename.

**Alan locks:** Revision always 0 on new client posts to surface the mint bug. **EventId is NOT always 0** (that comes later with real serial minting). Default: client posts 0; server may still assign on admit. Interactive green bar.

## What to build

Stay on the post-10 **Revision** shape (no EventId rename yet). Force **new client work to always post Revision 0**. No local client Revision serial. Tests that mint non-zero Revision for new client events must fail and be fixed. Optional: server rejects non-zero Revision on new inbound posts until admit (if admit already assigns) — or prove on the wire that posts are 0.

**In:** ClientHistory / record / pending post path still speaking Revision; test helpers that invent Revision for new client posts; interactive edit loop.

**Green bar:**
1. Interactive: basic edit posts **Revision 0** (or fails honestly).
2. Tests: non-zero Revision fixtures for *new* client events are fixed.
3. Temporary — superseded when [14 — EventId serial on Shared + Server](14-eventid-serial-shared-server.md) moves Revision → EventId with real serial rules.

## Out of scope

1. Any EventId type/rename; EventLog-only `EventId.next`; submissionId approve/stamp — [14 — EventId serial on Shared + Server](14-eventid-serial-shared-server.md), [15 — Client pending = zero + submissionId](15-client-pending-zero-submissionid.md), [16 — Approve / merge stamp + beforeAll](16-approve-merge-stamp-beforeall.md).
2. Contract deletes — [17 — Delete Revision aliases](17-delete-revision-aliases.md) and later.

## See also

[Single event source architecture](../arch.md), [Replan — smaller increments for SES 11 and 12](../reports/replan-11-12-smaller-increments.md), [11 — One serial event id](11-one-serial-event-id.md) (historical coded land; superseded for redo by 13–19)

## Comments

- 2026-09-17 — Maps to replan **11z**. Redo path after 10; does not replace the historical land of [11 — One serial event id](11-one-serial-event-id.md).

## Time
