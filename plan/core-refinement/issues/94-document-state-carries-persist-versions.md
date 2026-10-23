# 94 — Document state carries persist versions

**Status:** `defined`

> **Size: stop and ask Alan before building.** This step touches about 7 source files and about 14 test files, which is over 20 files. Changing `PersistState.Unpersisted` from a case with no fields to `Unpersisted of changedVersion: EventId` breaks every construction of it, and every `= PersistState.Unpersisted` comparison, in tests (14 files; `SpecialNodeStateAxesTests.fs` alone has 21 sites). The alternative is to keep `Unpersisted` with no fields and add `changedVersion: EventId` as a field of `DocumentState`, next to `persistedVersion`. That needs almost no test edits. The design in this ticket is Alan's, from his rewrite of [46 — Record the persist version](46-record-the-persist-version.md). Build it only after Alan picks.

## 1. Type
coding

## 2. Blockers
- [93 — Compile EventId before Model](93-compile-eventid-before-model.md)

## 3. Look at
- Files:
  - `src/Shared/Model.fs`
  - `src/Shared/GraphMutate.fs`
  - `src/Shared/GraphProjection.fs`
  - `src/Shared/Serialization.fs`
  - `src/Server/Core/CoreEventDispatch.fs`
  - `src/Server/Core/CoreMailboxLoad.fs`
  - `src/Server/Core/DbAgent.fs`
  - `src/Server/Core/PersistThread.fs`
  - `src/Server/DocumentPersistPath.fs`
- Types:
  - `PersistState`, `DocumentState`, `Node`, `EventId`
- Functions, each a construction or match of `PersistState`:
  - `DocumentState.fromParts`, `Node.Create`, `Node.withPersistState` in `Model.fs`
  - `GraphMutate.markOwningSpecialUnpersisted`
  - `GraphProjection.storedPersistWord`, `GraphProjection.persistFromStoredWord`, `GraphProjection.nodeEquals`
  - `Serialization.encodePersistState`, `Serialization.decodePersistState`, `Serialization.nodeFromFields`
  - `CoreEventDispatch.markForPersist` (builds `Unpersisted`)
  - `CoreEventDispatch.persistedNode` (matches `Persisted` only; unchanged)
  - `PersistThread.runRequest` `stillPersisted` skip, through `persistedNode` (unchanged)
  - `CoreMailboxLoad.writePullingFileUnpersisted`, `CoreMailboxLoad.parsedAndUnpersisted`, and the `Notice.NodePersisting` arm of `CoreMailboxLoad.applyNotice`
  - `DbAgent.noteParsed` (compares with `PersistState.Unpersisted`)
  - `DocumentPersistPath.stampNode` (writes `Persisted`; unchanged)
- Tests that build or compare `PersistState.Unpersisted`, by file and count:
  - `tests/Shared.Tests/SpecialNodeStateAxesTests.fs`, 23
  - `tests/Server.Tests/CoreLoopNoticeTests.fs`, 19
  - `tests/Server.Tests/DocumentOpPersistenceTests.fs`, 7
  - `tests/Server.Tests/DocumentPersistenceTests.fs`, 5
  - `tests/Server.Tests/PersistThreadTests.fs`, 3
  - `tests/Shared.Tests/HistoryTests.fs`, 2
  - `tests/Shared.Tests/LargeChangeApplyTests.fs`, 2
  - `tests/Shared.Tests/GraphProjectionTests.fs`, 2
  - `tests/Server.Tests/DbAgentFailureTests.fs`, 1
  - `tests/Server.Tests/DbAgentNoticeTests.fs`, 1
  - `tests/Shared.Tests/DocumentOpImpactTests.fs`, 1
  - `tests/Server.Tests/DatabaseProjectionTests.fs`, 1
  - `tests/Server.Tests/FileAgentFailureTests.fs`, 1
  - `tests/Shared.Tests/LazyLoadReconciliationTests.fs`, 1

## 4. Context
`PersistState` is `Persisted | Unpersisted`. `DocumentState` holds `parseState`, `persistState` and `pullingFile`. No persist version is recorded anywhere. This is the expand step for [Graph change persist version](../arch.md#graph-change-persist-version): the shapes exist, every writer passes `EventId.zero`, and nothing reads the versions yet.

## 5. Changes
- Add `persistedVersion: EventId` to `DocumentState`. `DocumentState.fromParts`, and so `Node.Create`, set it to `EventId.zero`.
- Make `PersistState` this:
  - `Persisted`
  - `Unpersisted of changedVersion: EventId`
- Every construction listed in Look at writes `Unpersisted EventId.zero`. Every match uses `Unpersisted _`. Equality checks against `PersistState.Unpersisted` become a match on `Unpersisted _`.
- `GraphMutate.markOwningSpecialUnpersisted` keeps its current rule and writes `Unpersisted EventId.zero`.
- `GraphProjection.persistFromStoredWord` and `Serialization.decodePersistState` read `"unpersisted"` as `Unpersisted EventId.zero`. The stored word stays `"unpersisted"`.
- `GraphProjection.nodeEquals` compares `persistedVersion` too. Its `persistState` comparison includes the `Unpersisted` version.
- Each test site in Look at passes `EventId.zero`.

## 6. Tests
- No test is added. The test sites in Look at change mechanically to `Unpersisted EventId.zero` or `Unpersisted _`.
- Filters:
  - `scripts/agent-test.sh --filter "SpecialNodeStateAxesTests"`
  - `scripts/agent-test.sh --filter "CoreLoopNoticeTests"`
  - `scripts/agent-test.sh --filter "PersistThreadTests"`
  - `scripts/agent-test.sh --full`

## 7. Done
- Shared, Server and Client compile.
- The full suite passes with no expectation change other than the added `EventId.zero`.
- Upload, import, parse, Load, undo and persist complete.
