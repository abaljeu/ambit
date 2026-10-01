# Gambol.Shared.Documents

Category: Building block

See Also:

[Gambol.Shared](gambol-shared.md)
[Gambol.Client](gambol-client.md)
[Gambol.Shared.DotNet](gambol-shared-dotnet.md)
[Gambol.Shared.Tests](gambol-shared-tests.md)

This library selects a codec for one persisted artifact and reads and writes through that codec.

## Role

[x] [Gambol.Client](gambol-client.md), [Gambol.Shared.DotNet](gambol-shared-dotnet.md), and [Gambol.Shared.Tests](gambol-shared-tests.md) reference this project.

## Interface

[x] [Gambol.Shared.Documents.fsproj](src/Shared/documents/Gambol.Shared.Documents.fsproj): class library
[x] `DocumentFormat` classifies a relative path as Amb, Markdown, C-style, or plain text.
[x] Binary extension: not a codec.
[x] Cold handlers do not use Diff.
[x] Warm reconcile needs Diff from outside this project.

## Depends on

[x] References [Gambol.Shared](gambol-shared.md).

## Explanation

Fable can compile cold handlers.
