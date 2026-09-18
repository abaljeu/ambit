# 48 — EventId Zero and positive Int

**Status:** done
**Actual:** 2h
**Blocked by:** None — can start immediately. Follows [47 — Server rejected Change: duplicate event id](47-server-rejected-change-duplicate-event-id.md) (Status `done`).

## 1. Context

A person edits a Node in the App after Load against a live Ambit host that persists EventLog in the Database. The Browser submits a Change. The Server must persist that Change as a new Event with a unique event id. After [47 — Server rejected Change: duplicate event id](47-server-rejected-change-duplicate-event-id.md), EventLog catch-up from a persisted event id stopped one duplicate-key reject. EventId is still one private int: 0 is both the draft Event id and the basis before the first stored Event, and `EventId.next` of 0 is 1. That shape let the Server assign a stored event id that EventLog already held. Catch-up is a patch. The type must make Zero distinct from a positive stored serial.

## 2. What to build

Retool EventId to the shape in [Core creation architecture](../arch.md) Module **Ev** and Module **EventLog**, and [EventId serial](.agents/rules/core-api.md). A draft Event stays at Zero until EventLog stamps it. A Change persists as a new Event with a unique event id. The Graph stays in sync. The App does not ask for a reload that discards the edit.

This ticket's F# DU (from chat):

```
EventId = Zero | Int of positive int
EventId.next EventId.zero = EventId.zero
```

Client and Shared mint a draft Event with `EventId.zero`. They do not assign stored serials. Get-all uses `EventId.zero`. There is no BeforeAll.

### 1. EventId

Shared EventId is Zero or a positive Int. State, Interface, and Uses: [Core creation architecture](../arch.md) Module **Ev**. Serial policy: [EventId serial](.agents/rules/core-api.md).

1. [x] Zero and positive Int — EventId is `Zero` or `Int` of a positive int. Zero is the draft event id. A stored event id is a positive Int. There is no stored event id 0.
2. [x] next of Zero is Zero — `EventId.next EventId.zero` equals `EventId.zero`. Next of a draft does not mint a stored serial.
3. [x] next of a stored Int — `EventId.next` of a positive Int is the next positive Int.
4. [x] Get-all is Zero — `EventLog.since EventId.zero` returns every stored Int. There is no BeforeAll case. Zero is the draft id and the basis before the first stored Event.
5. [x] Wire 0 and positive — Wire 0 is Zero. A positive wire int is that stored Int. EventId builder tests prove `fromJson` / `toJson` for those cases.

### 2. EventLog

EventLog is the only assigner of stored event ids. State, Interface, and Uses: [Core creation architecture](../arch.md) Module **EventLog**.

1. [x] First stored id is a positive Int — An empty EventLog holds the first assignable stored Int as `nextId`. It does not take that value from `EventId.next EventId.zero`.
2. [x] Append stamps a stored Int — `EventLog.append` writes a unique positive Int on the Event and advances `nextId`. It never persists Zero as a stored event id.
3. [x] Restore past max — `EventLog.restore` (and catch-up from a persisted event id) leaves `nextId` past every merged stored Int so the next append cannot reuse an id EventLog already holds.

### 3. Draft Events

Client and Shared mint drafts. They do not assign stored serials.

1. [x] Draft id is Zero — Command builders, pending ChangeRequest, and wire submit keep `EventId.zero` until EventLog stamps a stored Int.
2. [x] Received Events keep stored Int — A received Event rebuilt from the wire keeps its stored Int. Callers do not assign a stored serial to make that id.

### 4. Tests

Prove the builders. Other tests do not lock stored serial values.

1. [x] EventId builder tests — The EventId builder tests in the program check Zero, positive Int, `next` of Zero, `next` of a stored Int, wire 0, a positive wire int, and display. Those tests may check event id numbers.
2. [x] Other tests do not lock serials — Tests that are not EventId builder tests may require `EventId.zero` on a draft, uniqueness, restore/append order, and `since` tails. They obtain stored ids from EventLog. They do not lock 1, 2, 3, or another stored serial.

### 5. Unique persist

The [47 — Server rejected Change: duplicate event id](47-server-rejected-change-duplicate-event-id.md) user path still holds after the type change.

1. [x] Change persists unique event id — After Load of a mapped Workspace Node, a Browser Change persists as a new Event with a unique event id. The Database unique constraint on event id does not reject it. The App does not show **Server rejected change** for a duplicate event id.

## 3. Out of scope

1. Revert 47 persist wording — Public messages still say Event, never Ev. This ticket does not undo that wording.
2. Poll and Load product — Poll still requests Events since a known event id. Load still Fetches and Polls. This ticket does not add Browser chrome or change those commands.
3. EventLog product redesign — This ticket retools EventId and the serial that EventLog assigns. It does not add a new Event sequence or replace EventLog.

## 4. See also

[Core creation architecture](../arch.md), [EventId serial](.agents/rules/core-api.md), [47 — Server rejected Change: duplicate event id](47-server-rejected-change-duplicate-event-id.md)

## 5. Comments

- 2026-09-18 — Filed as a follow-on to [47 — Server rejected Change: duplicate event id](47-server-rejected-change-duplicate-event-id.md). Locked type: Zero or positive Int; `EventId.next EventId.zero` equals `EventId.zero`. Tests check event id numbers only in EventId builder tests.
- 2026-09-18 — Alan: BeforeAll is not necessary. Zero is the get-all basis because a stored event id is a positive Int.
- 2026-09-18 — Plan-or-doc-change: EventId serial lives on [Core creation architecture](../arch.md) Module **Ev** / **EventLog** and [EventId serial](.agents/rules/core-api.md). This ticket consumes that shape. Caller bans (`EventId.next` / `fromJson` as constructors) and “do not check event id numbers” were stripped from architecture and thinned here. Other tests do not lock stored serials; they obtain stored ids from EventLog.
- 2026-09-18 — Implement: EventId `Zero | Int` was already in Shared. Remaining work was 4.2 Other tests do not lock serials. Non-builder tests now take stored ids from EventLog via [EventIdFixtures](tests/Shared.Tests/EventIdFixtures.fs). EventId builder tests still use `fromJson`. Status `coded`.
- 2026-09-18 — Alan approved the 4.2 review ([code-review-48-other-tests-do-not-lock-serials](plan/core-creation/reports/code-review-48-other-tests-do-not-lock-serials.md)). SerializationTests wire 3/4 and DbAgentTests growth accepted as-is. Status `done`.

## Time

- 2026-09-18 2h — 4.2 Other tests do not lock serials: EventIdFixtures and non-builder serial unlock (from chat)
