# NewSpecialNode starts Parsed

## Branch

`dev`

## Test

- Name: `NewSpecialNode starts Parsed and Persisted`
- File: `tests/Shared.Tests/SpecialNodeStateAxesTests.fs`
- Create path: `Op.NewSpecialNode` for Workspace, Directory, and File

Assertions:

- `Assert.Equal(ParseState.Parsed, node.documentState.parseState)`
- `Assert.Equal(PersistState.Persisted, node.documentState.persistState)`

The second assertion is the persist half of the fact that was already there.

## Parsed and Current

`ParseState.Parsed` is `documentState.parseState`.

`Current` is `DocumentStateEnum`. `documentState.AsEnum` returns it through `DocumentState.toLegacy`.

`Op.NewSpecialNode` passes `documentState = Current` into `Node.Create`. That constructor sets `parseState` with `ParseState.ofDocumentState`, and `Current` maps to `ParseState.Parsed`.

The fact asserts `ParseState.Parsed` only. It does not assert `Current`.

## Result

`dotnet test tests/Shared.Tests/Gambol.Shared.Tests.fsproj --filter "DisplayName~NewSpecialNode starts Parsed and Persisted"`

Passed: 1. Failed: 0. Skipped: 0.

## Scope

`src/Shared/History.fs` is unchanged.

The other facts in `tests/Shared.Tests/SpecialNodeStateAxesTests.fs` stay skipped or untouched.
