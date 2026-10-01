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
