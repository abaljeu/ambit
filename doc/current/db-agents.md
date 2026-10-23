# Db agent

Category: Capability

See Also:

- [Core](core.md)
- [Mailbox](mailbox.md)
- [File agent](file-agents.md)
- [Persistence model](persistence-model.md)

`DbAgent` keeps graph and event info in the database.

## Job

[x] `DbAgent`: owns database reads, database writes, startup, and failed-loop behavior. [Db agent](../../src/Server/Core/DbAgent.fs).

[x] Startup load, projection repair, change apply, and maintenance failure: the store procedure.

[x] Db agent: not an Actor.

[x] `Notice.NodePersisting`: [Db agent](../../src/Server/Core/DbAgent.fs) `replaceGraph` stores that graph. [Database](../../src/Server/Database.fs) `writeNoticeRow` writes that node's `persist_state`.
[x] `notePersisted`: the `Notice.NodePersisted` handler calls it after the Persisted write returns Ok. When `diffWorkspaces` is waiting and no file diff is in progress, [Db agent](../../src/Server/Core/DbAgent.fs) calls `requestFileDiff`. A diff already in progress sets `diffNeeded`.
[x] `startFileDiff`: the pushed ids are the waiting workspaces outside `opsNotifiedIds` and outside `PersistThread.unparsedNode`.
[x] `pushPersist` is `rememberPushed`. It adds the pushed id to `opsNotifiedIds` and pushes that id onto the persist request stack. The next `startFileDiff` skips that id. One Edit of ROOT stores one `Notice.NodePersisted`.
