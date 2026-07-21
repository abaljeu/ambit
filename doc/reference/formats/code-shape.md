# Current Code Shape

Files for manipulating documents.

Key existing boundaries:

- `src/Shared/documents/AmbDocument.fs` is the per-document codec for `.amb` artifacts.
- `src/Shared/documents/PlainTextDocument.fs` is the per-document codec for non-`.amb` file artifacts that classify as plain text.
- `src/Shared/documents/DocumentFormat.fs` classifies artifact paths to `Amb` or `Plain` and routes `readArtifact` / `writeArtifact` through the matching codec (warm reconcile when `previousText` is present).
- `src/Shared/documents/OutlineLcs.fs` and `src/Shared/documents/OutlineReconcile.fs` provide shared outline LCS and disposition policy for warm reconcile (DiffPlex-backed).
- `src/Shared/dotnet/DocumentAssembly.fs` classifies artifact paths, discovers nested refs, and reads artifacts through `DocumentFormat.readArtifact`.
- `src/Server/DocumentPersistence.fs` resolves artifact paths and writes documents through `DocumentFormat.writeArtifact`.
- `src/Server/DocumentLoader.fs` starts from the artifact set when present and falls back to legacy monolithic `gambol`.
- Closest test homes are `tests/Shared.Tests/AmbDocumentTests.fs`, `tests/Shared.Tests/PlainTextDocumentTests.fs`, `tests/Shared.Tests/DocumentAssemblyTests.fs`, `tests/Server.Tests/DocumentPersistenceTests.fs`, and `tests/Server.Tests/DocumentLoaderTests.fs`.
