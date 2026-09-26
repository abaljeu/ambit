# Current Code Shape

Files for manipulating documents.

## Shared model

All formats share a nested **text-span tree** (`TextSpan` / `SpanNode` in [[src/Shared/documents/DocumentHandler.fs]]): a parent’s span encloses its children’s spans in the artifact. Outline formats (Amb, Plain, Md, CStyle) compute enclosure from depth order; Xml will compute it from element bounds. **Every artifact byte is covered by some node’s `TextSpan`.** There are no unbound interstitial SpanNodes.

Blank / prologue rules:

- **Plain blanks** — each blank line is its own **bound** empty node (`text = ""`), unchanged.
- **Md blanks** — not separate nodes. Absorb blank-line bytes into a **neighboring bound node** by extending that node’s `TextSpan` (prefer the preceding substantive node when one exists; otherwise the following node or the document root) so coverage stays total.
- **CStyle braces** — `{` / `}` attach to the preceding statement (not separate nodes); braced statements use cssClass `code-brace`. See [[doc/roadmap/workspace-format-cstyle-braces.md]].
- **Xml prologue** (doc only until Xml) — fold into the document-root / first content node span, or into complement on the root File node; still in the tree, not unbound.
- **Xml attributes** (doc only) — bound children whose spans sit in the opening tag.

`DocumentHandler` is the dispatch face: `parse` → span tree, `readCold` / `readWarm`, `write`. Warm reconcile is a knob on that tree: outline LCS over preorder bound lines (`OutlineDocument.warmByLcs`) vs future Xml fragment/`NodeId` match — not a second parse model.

## Boundaries

- `src/Shared/documents/AmbDocument.fs` — Amb line grammar, cold read/write; warm via `AmbReconcile.handler`.
- `src/Shared/documents/PlainTextDocument.fs` — Plain indent grammar; warm via `PlainTextReconcile.handler`. Fallback for non-Amb, non-Md, non-CStyle paths.
- `src/Shared/documents/MdDocument.fs` — Simplified markdown (ATX headings, `-` lists, plain lines); warm via `MdReconcile.handler`.
- `src/Shared/documents/CStyleDocument.fs` / `CStyleBrace.fs` — two-pass C-style brace codec (grammar tree → newline explode + attach); warm via `CStyleReconcile.handler`. First extension: `.cs`.
- `src/Shared/documents/DocumentFormat.fs` — classifies `.amb` → Amb, `.md` → Md, `.cs` → CStyle, else → Plain; routes through the codec→handler table.
- `src/Shared/documents/OutlineLcs.fs` / `OutlineReconcile.fs` / `OutlineDocument.fs` — DiffPlex LCS, disposition policy, `nestFlatLines` / `readWarmByLcs` / `makeOutlineHandler`. Amb/Plain reconcile files are thin format knobs. **Fable/paste target:** DiffPlex/warm LCS moves DotNet-only; Documents stays cold + Fable-safe — see [[doc/roadmap/paste-document-codec-import.md]] (`DocumentColdParse`).
- `src/Shared/dotnet/DocumentAssembly.fs` — path classify, nested refs, `DocumentFormat.readArtifact`.
- `src/Shared/dotnet/DocumentParseOps.fs` — warm+cold artifact → ops (DotNet); cold-only Shared entry is planned `DocumentColdParse.planApplyCold` for Client paste.
- `src/Shared/dotnet/ImportDocument.fs` — Parse / Upload file import via document reader (`buildFilePackage`); see [[doc/roadmap/parsefile-document-codec-import.md]]. Current warm: [[doc/roadmap/parse-file-reconcile-current.md]].
- `src/Server/DocumentPersistence.fs` / `DocumentLoader.fs` — path resolve, write, load.

Closest tests: `tests/Shared.Tests/AmbDocumentTests.fs`, `PlainTextDocumentTests.fs`, `MdDocumentTests.fs`, `CStyleDocumentTests.fs`, `DocumentAssemblyTests.fs`, `OutlineReconcileTests.fs`; `tests/Server.Tests/DocumentPersistenceTests.fs`, `DocumentLoaderTests.fs`.
