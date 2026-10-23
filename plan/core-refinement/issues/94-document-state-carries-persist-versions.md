# 94 — Document state carries persist versions

<<<<<<< HEAD
**Status:** `coded`
=======
**Status:** `defined`
>>>>>>> staging

## 1. Type
coding

## 2. Blockers
- [93 — Compile EventId before Model](93-compile-eventid-before-model.md)

## 3. Look at
- Files:
  - `src/Shared/Model.fs`
  - `tests/Shared.Tests/SpecialNodeStateAxesTests.fs`
- Types:
  - `PersistState`, `DocumentState`, `Node`, `EventId`
- Functions:
  - `DocumentState.fromParts`, the only record literal of `DocumentState` in `src` and `tests`
  - `Node.Create`, which builds the record through `DocumentState.fromParts`
  - `Node.withPersistState`
  - `EventId.zero`, `EventId.max`
- Every other writer changes the record with `{ ... with ... }` or through `Node.withPersistState`, `Node.withParseState` and `Node.withDocumentState`. No test builds a `DocumentState` record literal. Tests build nodes through `Node.Create`.

## 4. Context
`PersistState` is `Persisted | Unpersisted`. `DocumentState` holds `parseState`, `persistState` and `pullingFile`. No persist version is recorded. This is the expand step for [Graph change persist version](../arch.md#graph-change-persist-version). The two fields exist at `EventId.zero`, and nothing reads them yet.

## 5. Changes
+ `PersistState` stays `Persisted | Unpersisted`.
+ `DocumentState` gains two fields:
  + `changedVersion: EventId`, the version of the newest graph change that marked the node Unpersisted
  + `persistedVersion: EventId`, the version the node's file was last written at
+ `DocumentState.fromParts`, and so `Node.Create`, set both to `EventId.zero`.
- Add `Node.withUnpersistedAt (version: EventId) (node: Node) : Node`. 
    It sets `persistState` to `Unpersisted` and `changedVersion` to `EventId.max node.documentState.changedVersion version`. A newer mark overwrites an older `changedVersion`, including when the node is already Unpersisted. An older or equal mark keeps the newer one.
- Nothing calls `Node.withUnpersistedAt` yet. [97 — Graph change records changedVersion](97-graph-change-records-changedversion.md) and [98 — Core marks carry the Edit's id](98-core-marks-carry-the-edits-id.md) use it.

## 6. Tests
- Add `unpersisted mark keeps the newer changedVersion` in `tests/Shared.Tests/SpecialNodeStateAxesTests.fs`. `Node.withUnpersistedAt` on a Persisted node at N gives `Unpersisted` and `changedVersion` N. At N+1 it gives N+1. At N again after N+1 it keeps N+1. A new node has both versions at `EventId.zero`.
- Filters:
  - `scripts/agent-test.sh --filter "unpersisted mark keeps the newer changedVersion"`
  - `scripts/agent-test.sh --filter "SpecialNodeStateAxesTests"`
  - `scripts/agent-test.sh --full`

## 7. Done
- Shared, Server and Client compile.
- The named tests pass, and the full suite passes with no other test change.
- Upload, import, parse, Load, undo and persist complete.
