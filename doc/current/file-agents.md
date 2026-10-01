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
