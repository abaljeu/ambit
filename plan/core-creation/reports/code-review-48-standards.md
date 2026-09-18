# Standards review — 48 EventId Zero and positive Int

## 1. Range

Uncommitted vs HEAD (`git diff HEAD`), plus untracked [EventIdFixtures](tests/Shared.Tests/EventIdFixtures.fs). [core-agent-behavior.md](.agents/rules/core-agent-behavior.md) in the tree is out of this ticket.

## 2. Hard violations

### 1. File over 400 lines — DatabaseProjectionContractTests

[fsharp-source.md](.agents/rules/fsharp-source.md): 400 lines or less per file. If a file is already longer, only restructure to split up the code if your changes would increase it. Scan: [DatabaseProjectionContractTests](tests/Server.Tests/DatabaseProjectionContractTests.fs) FILE 624->625. The wrap added a line:

```
do! Database.replaceGraphProjectionWithTx tx graph (EventId.toJson eventId)
    |> Async.AwaitTask
```

### 2. File over 400 lines — DbAgentTests

Same rule. [DbAgentTests](tests/Server.Tests/DbAgentTests.fs) FILE 597->602. Line wraps for `storedId` and SQL interpolation increased the file, for example:

```
do! Database.replaceGraphProjectionWithTx tx graph
        (EventId.toJson (EventIdFixtures.storedId 9))
    |> Async.AwaitTask
```

### 3. Labeled link not project-root relative

[markdown-writing.md](.agents/rules/markdown-writing.md): other local files use a path relative to the project root. New comment on [48 — EventId Zero and positive Int](plan/core-creation/issues/48-eventid-zero-and-positive-int.md) uses `[EventIdFixtures](../../../tests/Shared.Tests/EventIdFixtures.fs)` instead of `tests/Shared.Tests/EventIdFixtures.fs`.

## 3. Judgement calls

### 1. Shotgun Surgery

One serial unlock edits many test files ([SMELLS.md](.agents/skills/code-review/SMELLS.md)). Typical hunk: `EventId.fromJson 4` replaced by `EventIdFixtures.storedId 4`. The fixture gathers the walk; the scatter is the ticket surface.

### 2. Duplicated Code

Wire asserts repeat `EventId.toJson (EventIdFixtures.storedId n)`, for example in [DatabaseProjectionContractTests](tests/Server.Tests/DatabaseProjectionContractTests.fs): `Assert.Equal(EventId.toJson (EventIdFixtures.storedId 2), revision)`.

## 4. Clear in this range

[EventIdFixtures.storedId](tests/Shared.Tests/EventIdFixtures.fs) starts at `EventLog.empty.nextId` and walks with `EventId.next`. EventId builder tests in [EventTests](tests/Shared.Tests/EventTests.fs) still use `fromJson`. No added F# line over 100 characters. No product Shared or Server source in this range. [core-api.md](.agents/rules/core-api.md) mint and assign rules apply to Client and Shared product code, not this test helper. [refer-by-name.md](.agents/rules/refer-by-name.md) holds on the Status `coded` list item that names the issue.
