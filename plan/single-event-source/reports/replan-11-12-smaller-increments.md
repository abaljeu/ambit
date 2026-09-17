# Replan — smaller increments for SES 11 and 12

Prepared: 2026-09-17. Not a commit decision. Alan: EventId numbers mismatch after `/state` fix; suspect broke after 10; may redo 11–12.

## Evidence from commits

Reset tip if redoing: `c1e2c1b2` — *Land Boot IndexedDB Ev log; fold client pending notes on 11* (end of 10 + ticket notes for client pending).

| Land on staging | SHA | What it was |
| --- | --- | --- |
| One serial event id (11) | `9630cf23` | Squash of work-branch tip |
| Contract leftover (12) | `38e1ef6d` | Squash of work-branch tip |
| Write core-creation arch (04) | `b226cb97` | After 12 |
| `/state` decode fix | `a5fd0b91` | After 04 |

Work branches had **almost no intermediate commits** to reuse as increments:

- `cursor/one-serial-event-id-2e7c`: one implement commit `6016a926` (*Implement one serial EventId and client pending by submissionId.*) — **116 files**, ~1660/1450.
- `cursor/repair-11-serial-event-id-2455`: one repair `75738008` (*stamp merge History, Undo targets, EventId.beforeAll*).
- `cursor/standards-11-serial-event-id-eca8`: LONG-line / naming follow-ups on top of repair.
- `cursor/contract-leftover-change-revision-b373`: one implement `b5ed59ce` — **80 files**, ~1257/1252.

So finer sets must be **carved by seam**, not by replaying existing commit series.

Ticket 11 already named two concerns that landed as one batch ([11 — One serial event id](../issues/11-one-serial-event-id.md)):

1. Server/Shared **one serial** (Revision → event id, private EventId, only EventLog `next`).
2. Client **pending model** (`EventId.zero`, `submissionId`, drop `nextEventId` / `PendingTransition`).

Repair then fixed approve/stamp / `beforeAll` — proof that approve/stamp was a third seam.

Ticket 12 ([12 — Contract leftover Change and Revision](../issues/12-contract-leftover-change-and-revision.md)) already lists three deletes that can land as separate green steps.

## Proposed redo sequence (after 10)

Keep expand/migrate 05–10. Replace 11 and 12 with:

### 11z — Revision always 0 (diagnostic; before EventId)

**Alan (2026-09-17):** a stage where **Revision is always 0**, to surface the numbers-mismatch bug. This is **before** Revision becomes EventId. **EventId is not always 0** — that comes later with real serial minting.

**Goal:** Stay on the post-10 **Revision** shape (no EventId rename yet). Force **new client work to always post Revision 0**. No local client Revision serial. Tests that mint non-zero Revision for new client events must fail and be fixed. Optional: server rejects non-zero Revision on new inbound posts until admit (if admit already assigns) — or prove on the wire that posts are 0.

**Why here / why not EventId:** Mega-11 renamed to EventId and introduced EventLog serial in the same cut as “client posts zero.” 11z isolates the minting bug on the **old Revision name**. When 11a introduces EventId, EventId follows the arch: EventLog mints serials; pending client events start at `EventId.zero` until admit — **not** “EventId always 0.”

**In:** ClientHistory / record / pending post path still speaking Revision; test helpers that invent Revision for new client posts; interactive `:5215` edit loop.

**Out:** Any EventId type/rename; EventLog-only `EventId.next`; submissionId approve/stamp (11a–11c).

**Green bar:**
1. Interactive: basic edit posts **Revision 0** (or fails honestly).
2. Tests: non-zero Revision fixtures for *new* client events are fixed.
3. Temporary — superseded when 11a moves Revision → EventId with real serial rules.

**Maps to:** diagnostic precursor; not present as its own land in the mega-11 squash.

### 11a — EventId serial on Shared + Server (no client pending change)

**Goal:** One serial id type everywhere wire/API/State already talks revision; EventId private; `EventId.next` only in EventLog; leftover `Change.id` is EventId; JSON `"eventId"`; `getEventId`.

**In:** [History.fs](../../../src/Shared/History.fs) EventId API, [EventLog.fs](../../../src/Shared/EventLog.fs), [EventJson.fs](../../../src/Shared/EventJson.fs), Server Api/Core/Database peels that still say Revision, core-api EventId serial rule.

**Out:** ClientHistory pending queue shape; SyncPlanner/SyncLogic pending list; Browser `record` / approve.

**Green bar:** Shared + Server tests that do not depend on client pending-by-submissionId.

### 11b — Client pending = zero + submissionId

**Goal:** Client has no EventId serial. Pending events are Ev with `EventId.zero`. Match/approve by `submissionId`. Drop `nextEventId`, `PendingTransition`, `PendingChange.transition`, `record` local id. SyncInfo pending is an event list.

**In:** [ClientHistory.fs](../../../src/Shared/ClientHistory.fs), [SyncLogic.fs](../../../src/Shared/SyncLogic.fs), [SyncPlanner.fs](../../../src/Shared/SyncPlanner.fs), [ViewModelSync.fs](../../../src/Shared/ViewModelSync.fs), Client Update/App/Program paths that post pending.

**Out:** Nested Undo/Redo target stamping and merge-stream rewind edge cases if they can wait (prefer 11c).

**Green bar:** ClientHistory / SyncLogic tests for zero pending + submissionId match. **Wire assert:** posted new client events have `eventId: 0` (this is the likely live reject).

### 11c — Approve / merge stamp + beforeAll

**Goal:** On approve, replace zero with server id. On revised stream (server ops inserted ahead), rewind and stamp zeros from matching `submissionId`. Stamp nested Undo/Redo targets. `EventId.beforeAll` instead of `fromJson -1`. BootCache SnapshotRecord `eventId` if still open after 11a.

**In:** ClientHistory `approve` / `stampEvent` / `stampBody`, EventLog `all`/`since` cursor, BootCache store field names if needed.

**Maps to:** repair commit `75738008` content.

**Green bar:** merge/revise and Undo/Redo target tests from the repair review.

### 12a — Delete Revision aliases

**Goal:** Delete `type Revision` and `EventId.ofRevision` / `toRevision`.

**Green bar:** no Revision type left.

### 12b — Delete leftover Change wrapping

**Goal:** Delete leftover `{ id; submissionId; ops }` Change record, `module Change` apply wrapping, `Ev.ofChange` / `Ev.asChange`, `eventFromChange`. Callers already on Ev/Ops from 05–11.

**Green bar:** production + tests compile with no `asChange`/`ofChange`.

### 12c — Delete unused EventId.fs (optional fold into 12b)

**Goal:** Delete unused [EventId.fs](../../../src/Shared/EventId.fs) if still present after 12a/12b (EventId lives in History.fs).

### 04 — Write core-creation arch last

Unchanged; runs after the new 11/12 sequence.

## Tickets

Alan approved this replan. Implementation tickets (redo path; existing [11 — One serial event id](../issues/11-one-serial-event-id.md) and [12 — Contract leftover Change and Revision](../issues/12-contract-leftover-change-and-revision.md) stay as historical `coded` lands):

| Ticket | Maps to |
| --- | --- |
| [13 — Revision always 0 (diagnostic)](../issues/13-revision-always-zero.md) | 11z |
| [14 — EventId serial on Shared + Server](../issues/14-eventid-serial-shared-server.md) | 11a |
| [15 — Client pending = zero + submissionId](../issues/15-client-pending-zero-submissionid.md) | 11b |
| [16 — Approve / merge stamp + beforeAll](../issues/16-approve-merge-stamp-beforeall.md) | 11c |
| [17 — Delete Revision aliases](../issues/17-delete-revision-aliases.md) | 12a |
| [18 — Delete leftover Change wrapping](../issues/18-delete-leftover-change-wrapping.md) | 12b |
| [19 — Delete unused EventId.fs](../issues/19-delete-unused-eventid-fs.md) | 12c |

## What not to split further

- Do not land “rename locals only” as its own ticket — fold into 11a/11b.
- Do not separate Server vs Shared for 11a unless compile forces it; one serial type wants one green bar.
- Standards LONG-line cleanups stay follow-ups on the same ticket tip, not new tickets.

## Suspected break vs this split

Alan’s numbers-mismatch on basic edit points at **client posting a non-zero Revision**. **11z** forces Revision always 0 *before* the EventId rename so the bug surfaces on the old name. **EventId is not always 0** — 11a adds EventLog serial; 11b pending starts at zero until admit. Then 11c stamp; then 12* deletes (Revision aliases, then leftover Change wrapping, then unused EventId.fs).

## Redo mechanics (when Alan says go)

1. Reset workplace to `c1e2c1b2` (or cherry land 04/state-fix decisions separately — `/state` decode may still be wanted on the redo line).
2. Charted issue files: tickets [13](../issues/13-revision-always-zero.md)–[19](../issues/19-delete-unused-eventid-fs.md) (11z, 11a–11c, 12a–12c). Existing 11/12 remain historical `coded` (no-retrofit).
3. Implement one increment per cloud agent; publish to staging only on Good.
4. Keep interactive web repro (local `:5215` + `/ambit?debug=1`) as the green bar for **11z** and 11b.

## Open choices for Alan

1. Keep `/state` decode fix (`a5fd0b91`) when resetting past 11/12, or drop and re-apply after redo.
2. Keep 04 arch rewrite on the redo tip or re-run 04 after new 12c.
3. Whether 11c is required before declaring 11b done for live edits (approve/stamp may be needed for round-trip even if zero-id post succeeds).
4. Run **11z on current staging tip** as a quick diagnostic patch, or only on a redo line reset to `c1e2c1b2`.
5. In 11z (still Revision): does the server still assign a non-zero Revision on admit while the client must post 0, or does the whole Revision pipeline stay 0 until 11a introduces EventId serial? Default assumption if unset: **client posts 0; server may still assign on admit**.
