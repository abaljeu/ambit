# Gambol.Server

Category: Building block

See Also:

[Server](server.md): HTTP behavior
[Core](core.md)
[Gambol.Shared](gambol-shared.md)
[Gambol.Shared.DotNet](gambol-shared-dotnet.md)
[Gambol.CloudAgents](gambol-cloud-agents.md)
[Gambol.Client](gambol-client.md)

The spoken name is Server.

## Interface

[x] [Gambol.Server.fsproj](src/Server/Gambol.Server.fsproj): ASP.NET Core web project
[x] Publish runs Fable on [Gambol.Client](gambol-client.md) and writes the JavaScript into `wwwroot`.

## Parts

[x] [Core](core.md): the backend ensuring everything is managed consistently through events

## Depends on

[x] References [Gambol.Shared](gambol-shared.md), [Gambol.Shared.DotNet](gambol-shared-dotnet.md), and [Gambol.CloudAgents](gambol-cloud-agents.md).
