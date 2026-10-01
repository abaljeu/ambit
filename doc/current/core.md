# Core

Category: Building block

See Also:

[Server](server.md)
[Mailbox](mailbox.md)
[Parse and persist](parse-persist.md)
[File agent](file-agents.md)
[Db agent](db-agents.md)
[Actors](actors.md)
[Gambol.Server](gambol-server.md)

Core is the first Subsystem of the Server.

## Role

[x] Subsystem: a named body inside a project, F# modules and the types for those modules, with one Interface.
[x] First Subsystem of Gambol.Server.
[ ] Git use.

## Interface

[x] Contract: Mailbox.

## Parts

[x] Mailbox
[x] Parse and persist
[x] File agent
[x] Db agent
[x] Actors

## Depends on

[x] Part of Gambol.Server. Behavior of the HTTP server stays Server.
