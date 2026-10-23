# File agent

Category: Capability

See Also:

- [Core](core.md)
- [Mailbox](mailbox.md)
- [Db agent](db-agents.md)
- [Persistence model](persistence-model.md)

`FileAgent` reads disk information into the graph and writes graph information to files.

## Job

[x] When the database is offline, [File agent](../../src/Server/Core/FileAgent.fs) reads file data into a partial graph.

[x] File agent: not an Actor.
[x] [File agent](../../src/Server/Core/FileAgent.fs) reads the graph from the `.amb` files.
[x] A meta checkpoint past the log tip truncates the log.
[x] In every other case the event id becomes the log tip.
[x] [File agent](../../src/Server/Core/FileAgent.fs) does not write `SYSTEM/gambol.meta`.
[x] File agent: an Edit posts no Notice. `notePersisted` is `ignore`. The persist thread posts `Notice.NodePersisted` after a write. [Mailbox](mailbox.md)
