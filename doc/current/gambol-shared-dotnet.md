# Gambol.Shared.DotNet

Category: Building block

See Also:

[Workspace file sync](workspace-file-sync.md)
[Gambol.Shared](gambol-shared.md)
[Gambol.Shared.Documents](gambol-shared-documents.md)
[Gambol.Server](gambol-server.md)
[Gambol.Desktop](gambol-desktop.md)
[Gambol.Shared.Tests](gambol-shared-tests.md)
[Gambol.Client](gambol-client.md)

This library holds shared F# that Fable must not compile.

## Role

[x] [Gambol.Server](gambol-server.md), [Gambol.Desktop](gambol-desktop.md), and [Gambol.Shared.Tests](gambol-shared-tests.md) reference this project.
[x] [Gambol.Client](gambol-client.md) does not reference this project.

## Interface

[x] [Gambol.Shared.DotNet.fsproj](src/Shared/dotnet/Gambol.Shared.DotNet.fsproj): class library

## Parts

[x] Files: auth tokens, process and git execution, [Workspace file sync](workspace-file-sync.md), document assembly, and lazy-load reconciliation.

## Depends on

[x] References [Gambol.Shared](gambol-shared.md) and [Gambol.Shared.Documents](gambol-shared-documents.md).
