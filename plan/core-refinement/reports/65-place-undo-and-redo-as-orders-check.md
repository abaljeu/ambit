pass

## 1. Suites

- 1. Server.Tests — 733 passed, 0 failed, 1 skipped
- 2. Shared.Tests — 1919 passed, 0 failed, 3 skipped

## 2. Mapped run

`scripts/agent-test.sh` with no arguments passed 38 Server tests and 137 Shared tests.

## 3. Named filters

- 1. `posted empty-op Undo logs Order.Undo and restores the Graph` — passed
- 2. `posted empty-op Redo logs Order.Redo and restores the Change` — passed
- 3. `postEvents places Order.Change with origin Edit` — passed
- 4. `name-only Undo and Redo store completed Events with submission ids` — passed
- 5. `Undo after restart refuses a target from before the restart` — passed
- 6. `Ev JSON round-trips each EventBody.Order case` — passed
- 7. `Undo sets persistState Unpersisted and leaves persistedVersion` — passed
