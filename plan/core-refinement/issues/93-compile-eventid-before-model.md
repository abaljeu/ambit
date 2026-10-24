# 93 — Compile EventId before Model

**Status:** `defined`

## 1. Type
coding

## 2. Blockers
None.

## 3. Look at
- Files:
  - `src/Shared/EventId.fs`
  - `src/Shared/Gambol.Shared.fsproj`
  - `src/Shared/Model.fs`
- Types:
  - `EventId`
- Functions:
  - `EventId.zero`, `EventId.value`, `EventId.toJson`, `EventId.fromJson`

## 4. Context
Alan's commit `cd873b0b` ("move EventId out.") moved the `EventId` type and its `EventId` module, unchanged, from `src/Shared/History.fs` into `src/Shared/EventId.fs` in `namespace Gambol.Shared`. `Gambol.Shared.fsproj` lists `EventId.fs` just before `History.fs`, which is after `Model.fs`. So `Model.fs` cannot name `EventId` yet. [94 — Document state carries persist versions](94-document-state-carries-persist-versions.md) needs `EventId` in `Model.fs`. `EventId.fs` uses nothing from other Shared files.

## 5. Changes
- In `src/Shared/Gambol.Shared.fsproj`, move the `<Compile Include="EventId.fs" />` line to just before `<Compile Include="Model.fs" />`.
- No other file changes.

## 6. Tests
- No test is added or changed. Existing tests prove the build.
- Filters:
  - `scripts/agent-test.sh --filter "EventJsonTests"`
  - `scripts/agent-test.sh --filter "PersistThreadTests"`

## 7. Done
- Shared, Server and Client compile.
- `EventId.fs` is listed before `Model.fs`.
- The named tests pass.
