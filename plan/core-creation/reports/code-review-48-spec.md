# Spec review — [48 — EventId Zero and positive Int](plan/core-creation/issues/48-eventid-zero-and-positive-int.md)

Diff: `git diff HEAD`. Spec: [48 — EventId Zero and positive Int](plan/core-creation/issues/48-eventid-zero-and-positive-int.md). Alan: BeforeAll is not necessary; EventId is Zero or positive Int; get-all uses EventId.zero; [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) `seedEventLog` uses `persist.getEventLog ()`. Policy: [EventId serial](.agents/rules/core-api.md). Arch: [Core creation architecture](plan/core-creation/arch.md) Module **Ev** and Module **EventLog**.

Product EventId (`Zero` / `Int`), `next` of Zero, EventLog empty/append/restore, get-all via `since EventId.zero`, drafts at `EventId.zero`, production `EventId.next` only in [EventLog.fs](src/Shared/EventLog.fs), `seedEventLog` = `getEventLog ()`, and EventId builder tests match the ticket.

## 1. Missing or partial

1. **Other tests still name serials** — Spec: “Tests that are not EventId builder tests do not check event id numbers.” and “without asserting 1, 2, 3, or other serial values.” Diff: [PersistHandlersRestoreTests.fs](tests/Server.Tests/PersistHandlersRestoreTests.fs) keeps `Assert.NotEqual(EventId.fromJson 1, stored.id)`. [DbAgentTests.fs](tests/Server.Tests/DbAgentTests.fs) still `Assert.Equal(EventId.fromJson 9, revision)` and fromJson 4/6. Unchanged Shared suites still `Assert.Equal(EventId.fromJson N, …)`.
2. **Seed-fail test still drives getEventsSince** — Alan: “CoreMailboxBackend seedEventLog must use persist.getEventLog () — not persist.getEventsSince EventId.zero (or a basis).” Seed matches. [Issue42PersistHandlersTests.fs](tests/Server.Tests/Issue42PersistHandlersTests.fs) and [PersistHandlersRestoreTests.fs](tests/Server.Tests/PersistHandlersRestoreTests.fs) `getEventsSince Error does not seed empty EventLog as success` still stub `getEventsSince` and expect closed writes.
3. **EventLog tests call next** — Spec: “No caller outside EventLog calls `EventId.next` on a stored Int.” [EventTests.fs](tests/Shared.Tests/EventTests.fs) restore/advancePast mint fixtures with `EventId.next EventLog.empty.nextId`.

## 2. Scope creep

1. **gambol.md skill pointer** — Spec What-to-build is EventId, EventLog, drafts, tests, unique persist. Diff adds [plan-or-doc-change](.agents/skills/plan-or-doc-change/SKILL.md) to [gambol.md](.agents/rules/gambol.md).
2. **EventId.firstStored** — Spec type shape: “EventId = Zero | Int of positive int” and “EventId.next EventId.zero = EventId.zero.” Diff adds public `EventId.firstStored = Int 1` in [History.fs](src/Shared/History.fs). Empty `nextId` must be a stored Int not next of Zero; that value did not need a new EventId name.

## 3. Looks implemented but wrong

1. **fromJson maps every n <= 0 to Zero** — Spec: “Wire 0 is Zero. A positive wire int is that stored Int.” [History.fs](src/Shared/History.fs) `fromJson` uses `if n > 0 then Int n else Zero`, so wire -1 becomes Zero (get-all). There is no BeforeAll case; a negative wire int is not wire 0 and is not a stored Int.

## 4. Summary

(a) 3, (b) 2, (c) 1. Worst in Spec: non-builder tests still name serials, and the seed-fail tests still target `getEventsSince` after Alan required `getEventLog ()`.
