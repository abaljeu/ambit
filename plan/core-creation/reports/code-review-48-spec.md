# Spec review — 48 EventId Zero and positive Int

Range: uncommitted vs `HEAD` (`git diff HEAD`), plus untracked [EventIdFixtures](tests/Shared.Tests/EventIdFixtures.fs). Spec: [48 — EventId Zero and positive Int](plan/core-creation/issues/48-eventid-zero-and-positive-int.md). This axis does not score Standards. Task spawn for this axis failed (`resource_exhausted`); this file is the Spec pass on that same brief.

## 1. Missing or partial

1. **Wire JSON fixtures still name serials** — Spec: “They obtain stored ids from EventLog. They do not lock 1, 2, 3, or another stored serial.” [SerializationTests](tests/Shared.Tests/SerializationTests.fs) still decodes `{"r":3,...}` and `{"r":4,...}` and asserts `storedId 3` / `storedId 4`. Those literals lock wire 3 and 4. The F# EventId now comes from EventLog, but the JSON does not.

## 2. Scope creep

1. **core-agent-behavior.md** — Spec What-to-build is EventId, EventLog, drafts, tests, unique persist. Diff includes [core-agent-behavior.md](.agents/rules/core-agent-behavior.md) dropping `dotnet build` from the toolchain gate. That file is not this ticket.

## 3. Looks implemented but wrong

1. **storedId ordinal equals today’s serial** — Spec: “They obtain stored ids from EventLog.” [EventIdFixtures.storedId](tests/Shared.Tests/EventIdFixtures.fs) starts at `EventLog.empty.nextId` and walks `EventId.next`. That is EventLog’s assigner. While empty `nextId` is `Int 1`, `storedId n` is still `Int n`, so tests that pass ordinal 5 still behave as if they named serial 5. The source of the id is EventLog; the ordinal argument remains a serial stand-in.

## 4. In spec and present

EventId `Zero | Int`, `next` of Zero, EventLog empty/append/restore, get-all via `since EventId.zero`, drafts at `EventId.zero`, and EventId builder tests with `fromJson` are already in Shared and [EventTests](tests/Shared.Tests/EventTests.fs). Non-builder `EventId.fromJson N` constructors are gone from the test tree except those builder facts. Persist SQL and projection revisions use `EventId.toJson` of EventLog-derived ids. Ticket item 4.2 is coded on that basis.

## 5. Summary

(a) 1, (b) 1, (c) 1. Worst in Spec: SerializationTests JSON still locks wire 3 and 4.
