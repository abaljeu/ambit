# Gambol.CloudAgents

Category: Building block

See Also:

[Actors](actors.md)
[Gambol.Server](gambol-server.md)
[Gambol.CloudAgents.Console](gambol-cloud-agents-console.md)
[Gambol.CloudAgents.Tests](gambol-cloud-agents-tests.md)

This library talks to an external Agent.

## Role

[x] An Agent is not an Ambit Actor. Detail: [Actors](actors.md).
[x] [Gambol.Server](gambol-server.md) and [Gambol.CloudAgents.Console](gambol-cloud-agents-console.md) reference this project.

## Interface

[x] [Gambol.CloudAgents.fsproj](src/CloudAgents/Gambol.CloudAgents.fsproj): class library
[x] `AgentRunner` starts, polls, streams, and cancels a Cursor Agent.
[x] `GrokBotRunner` wakes, streams, and cancels a Grok bot.

## Parts

[x] Each runner has a fake for tests.
