# 1. Standards review — 48 EventId Zero and positive Int

Range: uncommitted vs `HEAD` (`git diff HEAD`). This axis does not score Spec. This review did not edit product code. The mechanical scan printed findings.

## 2. Mechanical scan

Stdout treated as documented-standard hits:

1. File growth — `src/Shared/History.fs` `FILE 733->741` already over 400 or new file over 400; change increased it. Rule: [fsharp-source.md](.agents/rules/fsharp-source.md) 400 lines or less per file; if a file is already longer, only restructure to split if the change would increase it.

## 3. Hard violations

1. File growth on History.fs — Same scan hit. [History.fs](src/Shared/History.fs) EventId grew from one private int case to `Zero | Int`, plus `next` / `fromJson` matches. The file was already over 400 lines. The change increased it and did not split. Cite [fsharp-source.md](.agents/rules/fsharp-source.md) 400 lines or less per file.

2. EventId.next on a stored Int outside EventLog — [core-api.md](.agents/rules/core-api.md) EventId serial: only EventLog may call `EventId.next` on a stored Int. [EventTests.fs](tests/Shared.Tests/EventTests.fs) restore and advancePast mint persist ids with `EventId.next EventLog.empty.nextId` and `EventId.next (EventId.next EventLog.empty.nextId)`. Production `src/` next stays in EventLog. Builder tests that call `EventId.next` to prove next itself are not this finding.

3. fromJson used as a constructor in a non-codec test — [core-api.md](.agents/rules/core-api.md) EventId serial: only serializing uses fromJson/toJson. [PersistHandlersRestoreTests.fs](tests/Server.Tests/PersistHandlersRestoreTests.fs): `Assert.NotEqual(EventId.fromJson 1, stored.id)`.

## 4. Judgement smells

Per [SMELLS.md](.agents/skills/code-review/SMELLS.md), smells are heuristics, not hard violations.

1. Shotgun Surgery — One EventId retool plus “do not assert serial numbers” lands in sixteen Server test files plus Shared EventTests. Same swap: `Assert.Equal(EventId.fromJson N, …)` to `Assert.NotEqual(EventId.zero, …)`.

2. Duplicated Code — That NotEqual-zero shape repeats. EventTests extracted `assertStored`; the Server files did not share it.

## 5. Checks without findings

New EventId bindings stay under 40 lines and 100 columns. `firstStored` is two words. Get-all and drafts use `EventId.zero`. [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) seed uses `getEventLog`. Plan links for [48 — EventId Zero and positive Int](plan/core-creation/issues/48-eventid-zero-and-positive-int.md) use `[label](path)` and name [47 — Server rejected Change: duplicate event id](plan/core-creation/issues/47-server-rejected-change-duplicate-event-id.md). Ignored: [gambol.md](.agents/rules/gambol.md) plan-or-doc-change line and untracked skill noise.

Worst hard issue: History.fs growth (scan) plus EventTests advancing stored serials with `EventId.next`.
