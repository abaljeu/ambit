# Current Code Shape

Files for manipulating documents.

## Shared model

All formats share a nested **text-span tree** (`TextSpan` / `SpanNode` in [[src/Shared/documents/DocumentHandler.fs]]): a parent’s span encloses its children’s spans in the artifact. Outline formats (Amb, Plain; later Md) compute enclosure from depth order; Xml will compute it from element bounds. **Every artifact byte is covered by some node’s `TextSpan`.** There are no unbound interstitial SpanNodes.

Blank / prologue rules:

- **Plain blanks** — each blank line is its own **bound** empty node (`text = ""`), unchanged.
- **Md blanks** (not implemented yet) — not separate nodes. Absorb blank-line bytes into a **neighboring bound node** by extending that node’s `TextSpan` (prefer the preceding substantive node when one exists; otherwise the following node or the document root) so coverage stays total.
- **Xml prologue** (doc only until Xml) — fold into the document-root / first content node span, or into complement on the root File node; still in the tree, not unbound.
- **Xml attributes** (doc only) — bound children whose spans sit in the opening tag.

`DocumentHandler` is the dispatch face: `parse` → span tree, `readCold` / `readWarm`, `write`. Warm reconcile is a knob on that tree: outline LCS over preorder bound lines (`OutlineDocument.warmByLcs`) vs future Xml fragment/`NodeId` match — not a second parse model.

## Boundaries

- `src/Shared/documents/AmbDocument.fs` — Amb line grammar, cold read/write; warm via `AmbReconcile.handler`.
- `src/Shared/documents/PlainTextDocument.fs` — Plain indent grammar; warm via `PlainTextReconcile.handler`. Fallback for all non-Amb paths (including `.md` / XML-shaped) until dedicated handlers exist.
- `src/Shared/documents/DocumentFormat.fs` — classifies `.amb` → Amb, else → Plain (unchanged); routes through the codec→handler table.
- `src/Shared/documents/OutlineLcs.fs` / `OutlineReconcile.fs` / `OutlineDocument.fs` — DiffPlex LCS, disposition policy, `nestFlatLines` / `readWarmByLcs` / `makeOutlineHandler`. Amb/Plain reconcile files are thin format knobs.
- `src/Shared/dotnet/DocumentAssembly.fs` — path classify, nested refs, `DocumentFormat.readArtifact`.
- `src/Server/DocumentPersistence.fs` / `DocumentLoader.fs` — path resolve, write, load.

Closest tests: `tests/Shared.Tests/AmbDocumentTests.fs`, `PlainTextDocumentTests.fs`, `DocumentAssemblyTests.fs`, `OutlineReconcileTests.fs`; `tests/Server.Tests/DocumentPersistenceTests.fs`, `DocumentLoaderTests.fs`.
