# Gambol.CloudAgents.Console

Category: Building block

See Also:

[Gambol.CloudAgents](gambol-cloud-agents.md)
[Gambol.CloudAgents.Tests](gambol-cloud-agents-tests.md)

This program starts one Cursor Agent from the command line.

## Interface

[x] [Gambol.CloudAgents.Console.fsproj](src/CloudAgents.Console/Gambol.CloudAgents.Console.fsproj): output type Exe
[x] [Program.fs](src/CloudAgents.Console/Program.fs) calls `AgentRunner`.
[x] Arguments, appsettings, and user secrets supply the run. API key: `--api-key`, user secrets, or `CURSOR_API_KEY`.

## Depends on

[x] References [Gambol.CloudAgents](gambol-cloud-agents.md).
