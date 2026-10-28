# File Load event order

Branch: dev

## Fact

- File: tests/Server.Tests/ParseThreadLoadTests.fs
- Name: File Load logs Load, NodeUnparsed, NodeParsing, then NodeParsed
- Kind: File

## Assertion

The fact reads the event log after `loadRig` sends Load on a File and the parse thread finishes.

It keeps events on that File whose body is Load, NodeUnparsed, NodeParsing, or NodeParsed.

`hasLoadOrder` walks that list and requires those four names in that order.

Events of other kinds stay in the log. Events before, after, and between the four stay allowed.

## Result

Command: `dotnet test tests/Server.Tests/Gambol.Server.Tests.fsproj --no-build --filter "FullyQualifiedName~File Load logs Load"`

The test project was built with `BuildProjectReferences=false` because `netcoredbg.exe` held the Server output files.

Failed: 1. Passed: 0. Duration: 1 s.

Actual event sequence:

- Change
- Change
- Load file
- NodeParsing file
- Change
- NodeParsed file

NodeUnparsed is absent between Load and NodeParsing. The order check failed on that gap. The fact stays in place.

## Scope

This agent edited tests/Server.Tests/ParseThreadLoadTests.fs.

src/Shared/History.fs is unchanged.

tests/Shared.Tests/SpecialNodeStateAxesTests.fs is unchanged by this agent.

Facts skipped with "pending documentState/Load spec" stay skipped.
