# Document Format Dispatch

See also: [[doc/roadmap/workspace-file-model.md]] Stage 7 Step 5,
[[doc/roadmap/workspace-format-plain.md]], [[doc/roadmap/workspace-text-outline-conversion.md]]

## What it gives you

- `.amb` directory and workspace artifacts keep using `AmbDocument`.
- Generic text file artifacts (neither `.amb` nor `.md`) use `PlainTextDocument`.
- One shared dispatch boundary in `src/Shared/`; `DocumentPersistence` and `DocumentAssembly` call it instead of hard-coding `AmbDocument`.
- Server read/write/discovery tests prove both codecs on disk.

## What it avoids for now

- Markdown (`.md`) — classify distinctly, return error on read/write.
- Changing workspace or directory persistence away from `.amb`.
- Persisting plain-text complement separately on disk.
- Filesystem watcher or automatic external reconciliation.

## Dispatch API (Shared)

Add `DocumentFormat.fs` (or extend `DocumentAssembly.fs` if it stays small):

```fsharp
type DocumentCodec =
    | Amb
    | Plain

let classifyCodec (relativePath: string) : Result<DocumentCodec, string>
let readArtifact (text: string) (docId: NodeId) (graph: Graph) (codec: DocumentCodec) : Result<Graph, string>
let writeArtifact (graph: Graph) (docId: NodeId) (codec: DocumentCodec) (previousText: string option) : Result<string, string>
```

Classification rules (reuse `classifyArtifactRelative`):

| Path pattern | Codec |
|--------------|-------|
| `.amb`, `*/.amb`, `@*/*/.amb` | Amb |
| `*.md` | Error (deferred) |
| Any other file path | Plain |
| Plain file with nested document-root children | Amb on write (graph-aware) |
| `.txt` file whose content has `->` or `^` lines | Amb on read (content-aware) |

`readArtifact` for Amb: existing `AmbDocument.read` + `mergeReadResult`.
For Plain: `PlainTextDocument.read` + parallel merge (same overlay/conflict rules).

`writeArtifact` for Amb: `AmbDocument.write` (no previous-text incremental path today).
For Plain: `PlainTextDocument.write graph docId complement previousText` where complement is
`buildComplement` from the current graph; `previousText` is the on-disk artifact when present.

Nested doc refs in `.amb` parents still queue child artifacts; plain file children assemble through Plain codec.

## Wiring

1. `DocumentAssembly.readArtifact` → `DocumentFormat.readArtifact` after `classifyCodec relativePath`.
2. `DocumentPersistence.writeDocument` → read existing file bytes when present, then `DocumentFormat.writeArtifact`.
3. `DocumentPersistence.readAllDocuments` unchanged except assembly now dispatches per path.
4. `AmbDocument` untouched except shared helper extraction if needed.

## Implementation steps

1. Shared tests: extend `DocumentAssemblyTests` for `classifyCodec` and dispatch read/write on in-memory artifact maps.
2. Server tests: extend `DocumentPersistenceTests` (plain write/read, amb regression, discovery, ref cold loss) — **this slice**.
3. Implement `DocumentFormat` module and route `DocumentAssembly` + `DocumentPersistence`.
4. Fix any loader tests only if startup behavior changes (legacy `gambol` fallback stays).

## Tests

Shared: `PlainTextDocumentTests` (done), extended `DocumentAssemblyTests` (codec dispatch).

Server (`DocumentPersistenceTests`):

- Plain `readme.txt` writes outline text without `^` stable-id syntax.
- `.amb` directory artifacts still contain stable-id syntax.
- `readAllDocuments` round-trips plain file member text.
- `discoverArtifactRelatives` lists plain and amb paths together.
- Ref child in plain file exports target visible text only; cold `readAllDocuments` loses Ref edge.

Verification:

```bash
dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~PlainTextDocumentTests|FullyQualifiedName~DocumentAssemblyTests"
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~DocumentPersistenceTests"
```
