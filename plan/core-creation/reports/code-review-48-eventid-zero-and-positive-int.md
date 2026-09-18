# Code review — [48 — EventId Zero and positive Int](plan/core-creation/issues/48-eventid-zero-and-positive-int.md)

Range: uncommitted vs `HEAD` (`git diff HEAD`). Axis files: [code-review-48-standards](code-review-48-standards.md), [code-review-48-spec](code-review-48-spec.md). A report is not approval.

## Standards

Range: uncommitted vs `HEAD` (`git diff HEAD`). This axis does not score Spec. This review did not edit product code. The mechanical scan printed findings.

### 1. Mechanical scan

Stdout treated as documented-standard hits:

1. File growth — `src/Shared/History.fs` `FILE 733->741` already over 400 or new file over 400; change increased it. Rule: [fsharp-source.md](.agents/rules/fsharp-source.md) 400 lines or less per file; if a file is already longer, only restructure to split if the change would increase it.

### 2. Hard violations

2. EventId.next on a stored Int outside EventLog — [core-api.md](.agents/rules/core-api.md) EventId serial: only EventLog may call `EventId.next` on a stored Int. [EventTests.fs](tests/Shared.Tests/EventTests.fs) restore and advancePast mint persist ids with `EventId.next EventLog.empty.nextId` and `EventId.next (EventId.next EventLog.empty.nextId)`. Production `src/` next stays in EventLog. Builder tests that call `EventId.next` to prove next itself are not this finding.
3. fromJson used as a constructor in a non-codec test — [core-api.md](.agents/rules/core-api.md) EventId serial: only serializing uses fromJson/toJson. [PersistHandlersRestoreTests.fs](../../../tests/Server.Tests/PersistHandlersRestoreTests.fs): `Assert.NotEqual(EventId.fromJson 1, stored.id)`.

### 3. Judgement smells

Per [SMELLS.md](.agents/skills/code-review/SMELLS.md), smells are heuristics, not hard violations.

1. Shotgun Surgery — One EventId retool plus “do not assert serial numbers” lands in sixteen Server test files plus Shared EventTests. Same swap: `Assert.Equal(EventId.fromJson N, …)` to `Assert.NotEqual(EventId.zero, …)`.
2. Duplicated Code — That NotEqual-zero shape repeats. EventTests extracted `assertStored`; the Server files did not share it.

### 4. Checks without findings

New EventId bindings stay under 40 lines and 100 columns. `firstStored` is two words. Get-all and drafts use `EventId.zero`. [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) seed uses `getEventLog`. Plan links for [48 — EventId Zero and positive Int](plan/core-creation/issues/48-eventid-zero-and-positive-int.md) use `[label](path)` and name [47 — Server rejected Change: duplicate event id](plan/core-creation/issues/47-server-rejected-change-duplicate-event-id.md). Ignored: [gambol.md](.agents/rules/gambol.md) plan-or-doc-change line and untracked skill noise.

Worst hard issue: History.fs growth (scan) plus EventTests advancing stored serials with `EventId.next`.

## Spec

Diff: `git diff HEAD`. Spec: [48 — EventId Zero and positive Int](plan/core-creation/issues/48-eventid-zero-and-positive-int.md). Alan: BeforeAll is not necessary; EventId is Zero or positive Int; get-all uses EventId.zero; [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) `seedEventLog` uses `persist.getEventLog ()`. Policy: [EventId serial](.agents/rules/core-api.md). Arch: [Core creation architecture](plan/core-creation/arch.md) Module **Ev** and Module **EventLog**.

Product EventId (`Zero` / `Int`), `next` of Zero, EventLog empty/append/restore, get-all via `since EventId.zero`, drafts at `EventId.zero`, production `EventId.next` only in [EventLog.fs](src/Shared/EventLog.fs), `seedEventLog` = `getEventLog ()`, and EventId builder tests match the ticket.

### 1. Missing or partial

1. **Other tests still name serials** — Spec: “Tests that are not EventId builder tests do not check event id numbers.” and “without asserting 1, 2, 3, or other serial values.” Diff: [PersistHandlersRestoreTests.fs](tests/Server.Tests/PersistHandlersRestoreTests.fs) keeps `Assert.NotEqual(EventId.fromJson 1, stored.id)`. [DbAgentTests.fs](tests/Server.Tests/DbAgentTests.fs) still `Assert.Equal(EventId.fromJson 9, revision)` and fromJson 4/6. Unchanged Shared suites still `Assert.Equal(EventId.fromJson N, …)`.
2. **Seed-fail test still drives getEventsSince** — Alan: “CoreMailboxBackend seedEventLog must use persist.getEventLog () — not persist.getEventsSince EventId.zero (or a basis).” Seed matches. [Issue42PersistHandlersTests.fs](tests/Server.Tests/Issue42PersistHandlersTests.fs) and [PersistHandlersRestoreTests.fs](tests/Server.Tests/PersistHandlersRestoreTests.fs) `getEventsSince Error does not seed empty EventLog as success` still stub `getEventsSince` and expect closed writes.
3. **EventLog tests call next** — Spec: “No caller outside EventLog calls `EventId.next` on a stored Int.” [EventTests.fs](tests/Shared.Tests/EventTests.fs) restore/advancePast mint fixtures with `EventId.next EventLog.empty.nextId`.

### 2. Scope creep

1. **gambol.md skill pointer** — Spec What-to-build is EventId, EventLog, drafts, tests, unique persist. Diff adds [plan-or-doc-change](.agents/skills/plan-or-doc-change/SKILL.md) to [gambol.md](.agents/rules/gambol.md).
2. **EventId.firstStored** — Spec type shape: “EventId = Zero | Int of positive int” and “EventId.next EventId.zero = EventId.zero.” Diff adds public `EventId.firstStored = Int 1` in [History.fs](src/Shared/History.fs). Empty `nextId` must be a stored Int not next of Zero; that value did not need a new EventId name.

### 3. Looks implemented but wrong

1. **fromJson maps every n <= 0 to Zero** — Spec: “Wire 0 is Zero. A positive wire int is that stored Int.” [History.fs](src/Shared/History.fs) `fromJson` uses `if n > 0 then Int n else Zero`, so wire -1 becomes Zero (get-all). There is no BeforeAll case; a negative wire int is not wire 0 and is not a stored Int.

### 4. Summary

(a) 3, (b) 2, (c) 1. Worst in Spec: non-builder tests still name serials, and the seed-fail tests still target `getEventsSince` after Alan required `getEventLog ()`.

## Summary

Standards: 3 hard, 2 judgement; worst is [History.fs](src/Shared/History.fs) growth plus EventTests `EventId.next` on stored Ints. Spec: 3 missing/partial, 2 creep, 1 wrong; worst is non-builder serial asserts and seed-fail tests still stubbing `getEventsSince`.
