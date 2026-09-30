# Unify PersistFilling

Opinion of one agent, 2026-09-30. Persist means send the graph to storage. File and Db are two backends for that same filling. There is no File-vs-Db distinction in the mailbox host contract.

## Shared source

[FileAgent](src/Server/Core/FileAgent.fs) and [DbAgent](src/Server/Core/DbAgent.fs) already held the same persist fields: `handlers`, `onError`, `formatError`, `isReady`, `flushSnapshot`, `dispose`. [PersistFilling](src/Server/Core/CoreMsg.fs) is that record plus `until` and `bindSnapshot`. Both `persist` functions copied those fields into a PersistFilling literal. File set `until = None` and `bindSnapshot = ignore`. Db wrapped `until` as `Some` and passed `bindSnapshot`. Same source, same meaning.

The named record is PersistFilling itself. I did not add a tuple or a second mega-record.

## What collapsed

Each agent type now stores one `filling: PersistFilling`. Create still builds that record once, from the agent’s closures. `FileAgent.persist` and `DbAgent.persist` return `agent.filling`. They do not expand the same fields again.

File vs Db still differ at create: File has no startup wait; Db has `until` and snapshot bind. That is lifecycle of the storage backend, not a second persist mapping.

Mailbox, parse/load, and handler internals did not change.

FileAgent still keeps `initialState` beside `filling`. `FileAgent.initialState` has no callers. I did not delete it.

## Tests

Command:

```
dotnet test tests/Server.Tests/Gambol.Server.Tests.fsproj -c Debug --filter "FullyQualifiedName~ParseThreadLoadTests|FullyQualifiedName~PersistHandlersRestoreTests|FullyQualifiedName~PersistApplyTests"
```

Result: passed. Failed 0, passed 14, skipped 0. I added PersistApplyTests to the filter because that module calls both persist functions and reads `.handlers`.
