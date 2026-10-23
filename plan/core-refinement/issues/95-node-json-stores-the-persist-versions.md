# 95 — Node JSON stores the persist versions

**Status:** `defined`

## 1. Type
coding

## 2. Blockers
- [94 — Document state carries persist versions](94-document-state-carries-persist-versions.md)

## 3. Look at
- Files:
  - `src/Shared/Serialization.fs`
  - `tests/Shared.Tests/SpecialNodeStateAxesTests.fs`
- Types:
  - `DocumentState`, `PersistState`, `EventId`
- Functions:
  - `Serialization.encodePersistState`, `Serialization.decodePersistState`, `Serialization.nodeFromFields`, and the node encoder that writes `"persistState"`
  - `EventId.toJson`, `EventId.fromJson`

## 4. Context
After [94 — Document state carries persist versions](94-document-state-carries-persist-versions.md), the node JSON writes only the persist word. `persistedVersion` and the `Unpersisted` version are lost on a JSON round trip and come back as `EventId.zero`.

## 5. Changes
- The node encoder writes two fields beside `"persistState"`:
  - `"persistedVersion"`, from `documentState.persistedVersion`, with `EventId.toJson`
  - `"changedVersion"`, from `Unpersisted changedVersion`, with `EventId.toJson`. A `Persisted` node writes no `changedVersion`.
- `Serialization.nodeFromFields` reads both with `EventId.fromJson`. A missing field reads as `EventId.zero`. An `"unpersisted"` word with no `changedVersion` reads as `Unpersisted EventId.zero`.

## 6. Tests
- Add `document state versions survive a JSON round trip` in `tests/Shared.Tests/SpecialNodeStateAxesTests.fs`. A node encoded with `persistedVersion` and `Unpersisted changedVersion` decodes to those versions. A node JSON with neither field decodes both as `EventId.zero`.
- Filters:
  - `scripts/agent-test.sh --filter "document state versions survive a JSON round trip"`
  - `scripts/agent-test.sh --filter "SpecialNodeStateAxesTests"`

## 7. Done
- The named tests pass.
- Upload, import, parse, Load, undo and persist complete.
