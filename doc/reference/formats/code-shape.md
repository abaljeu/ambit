# Current Code Shape

Files for manipulating documents.

Key existing boundaries:

- `src/Shared/AmbDocument.fs` is the per-document codec for `.amb` artifacts.
- `src/Shared/PlainTextDocument.fs` is the per-document codec for non-`.amb` file artifacts that classify as plain text.
- `src/Shared/DocumentFormat.fs` classifies artifact paths to `Amb` or `Plain` and routes `readArtifact` / `writeArtifact` through the matching codec.
- `src/Shared/DocumentAssembly.fs` classifies artifact paths, discovers nested refs, and reads artifacts through `DocumentFormat.readArtifact`.
- `src/Server/DocumentPersistence.fs` resolves artifact paths and writes documents through `DocumentFormat.writeArtifact`.
- `src/Server/DocumentLoader.fs` starts from the artifact set when present and falls back to legacy monolithic `gambol`.
- Closest test homes are `tests/Shared.Tests/AmbDocumentTests.fs`, `tests/Shared.Tests/PlainTextDocumentTests.fs`, `tests/Shared.Tests/DocumentAssemblyTests.fs`, `tests/Server.Tests/DocumentPersistenceTests.fs`, and `tests/Server.Tests/DocumentLoaderTests.fs`.
