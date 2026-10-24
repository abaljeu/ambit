# 95 — Node JSON stores the persist versions

**Status:** `coded`

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
After [94 — Document state carries persist versions](94-document-state-carries-persist-versions.md), the node JSON writes only the persist word. `DocumentState.changedVersion` and `DocumentState.persistedVersion` are lost on a JSON round trip and come back as `EventId.zero`.

## 5. Changes
- The node encoder writes two fields beside `"persistState"`:
  - `"persistedVersion"`, from `documentState.persistedVersion`, with `EventId.toJson`
  - `"changedVersion"`, from `documentState.changedVersion`, with `EventId.toJson`. It is written for `Persisted` and `Unpersisted` nodes alike.
- `Serialization.nodeFromFields` reads both with `EventId.fromJson`. A missing field reads as `EventId.zero`. The persist word stays `"persisted"` or `"unpersisted"` and is read as today.

## 6. Tests
- Add `document state versions survive a JSON round trip` in `tests/Shared.Tests/SpecialNodeStateAxesTests.fs`. An `Unpersisted` node encoded with non-zero `changedVersion` and `persistedVersion` decodes to the same persist state and versions. A node JSON with neither field decodes both as `EventId.zero`.
- Filters:
  - `scripts/agent-test.sh --filter "document state versions survive a JSON round trip"`
  - `scripts/agent-test.sh --filter "SpecialNodeStateAxesTests"`

## 7. Done
- The named tests pass.
- Upload, import, parse, Load, undo and persist complete.
