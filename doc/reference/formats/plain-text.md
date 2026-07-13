# Plain Text Round Trip Plan

## Assumptions:
- Generic text means the existing `other text` / plain format path, not `.amb` or `.md`.
- Cold read/write and flatten/project live in [[src/Shared/PlainTextDocument.fs]]. Import/warm reconcile uses shared outline LCS in Shared/DotNet (`OutlineLcs` / `OutlineReconcile` via DiffPlex); Client does not own file reconcile.
- Every file line (including blank) is an outline node with `text = ""` for blanks; empty nodes project as blank lines both ways.
- Match key for warm reconcile is line text only; edited depth wins for LCS-matched lines (block re-indent keeps `NodeId`). Export remains operations-driven with `previousText` byte preservation for untouched lines.
- Tests drive the work. Filesystem integration stays in `src/Server/`.
- Plain text must not gain Ambit-only syntax for hidden graph identity. No `NodeId`, `#name-token`, or ref target is emitted into arbitrary text just to preserve graph round trips. Plain leaves `hardKey` unset by design; no ` #name-token` hard-match is planned.
- A Ref occurrence inside a plain text document exports as the target node's visible `text` line. Reimport from text alone loses that it was a Ref and creates/reconciles ordinary content unless an out-of-band current graph context preserves it.

## Scope

Plan and document a narrow implementation slice:

- Preserve existing `.amb` document behavior.
- Add generic text as the codec for `Special File` artifacts whose path is not `.amb` and not `.md`.
- Keep workspace and directory documents on `.amb`.
- Do not add markdown behavior in this slice.
- Do not make filesystem observation authoritative; DB/graph identity remains the source of truth.

## Code Shape
The implementation should introduce a small document-format dispatch boundary instead of threading plain text through `.amb` functions.
Fit according to [[code-shape.md]].

## Planned Doc Changes

1. In `[doc/roadmap/workspace-file-model.md](doc/roadmap/workspace-file-model.md)`, promote the generic text bullet from deferred to a planned/read-write-layer slice and link to `[doc/roadmap/workspace-format-plain.md](doc/roadmap/workspace-format-plain.md)` plus `[doc/roadmap/workspace-text-outline-conversion.md](doc/roadmap/workspace-text-outline-conversion.md)`.

2. In [[doc/roadmap/workspace-text-outline-conversion.md]], record shared outline LCS reconcile (Shared/DotNet) and that generic text persistence is reconciled as `(previous file text, current graph document, edited/new file text)`:

- unchanged imported text exports byte-identically;
- unchanged exported outline imports graph-identically for representable content, with complement-backed recovery for metadata plain text cannot encode;
- line edit/add/delete preserve unaffected node identity and untouched file bytes;
- external re-indent keeps id when text LCS-matches; duplicate/blank runs use positional tie-break.

3. In [[doc/roadmap/workspace-format-plain.md]], revise the plain-format specifics:

- Keep indentation inference, blanks both ways, byte preservation expectations, and LCS reconcile outcomes.
- Remove or sharply qualify ` #name-token` and ref-only line syntax; those make an Ambit-flavored text format, not arbitrary text.
- State that a Ref child projects as its visible `Node.text` line only. The text file does not encode the target edge.
- State the consequence: cold rebuild from plain text alone cannot recover refs, ids, classes, or other graph-only metadata. Live reconciliation may preserve graph-only facts only from the current graph/complement, not from the text file.

4. Update verification targets in the same docs so they correspond directly to the test modules below.

## Test-First Implementation Plan

1. Add pure Shared tests for a new plain text codec.
   - Suggested file: `tests/Shared.Tests/PlainTextDocumentTests.fs`.
   - Register it after `AmbDocumentTests.fs` or near `DocumentAssemblyTests.fs` in `tests/Shared.Tests/Gambol.Shared.Tests.fsproj`.
   - Initial cases: arbitrary LF/CRLF text imports to expected tree; unchanged import writes byte-identically; exported outline imports with same node identity when reconciliation has current graph context; mid insert / block re-indent / blank round-trip; edit/add/delete; external unique-line swap may keep ids via LCS; outline move preserves node id on write. Also `OutlineReconcileTests` for DiffPlex disposition policy.
   - Ref cases: exporting a Ref child writes the referred node's visible text with no target marker; cold reimport treats that line as ordinary content; live reconciliation may preserve the Ref edge only if the current graph/complement still supplies that identity.

2. Add Shared tests for format classification and assembly dispatch.
   - Extend `tests/Shared.Tests/DocumentAssemblyTests.fs`.
   - Cases: `.amb` paths classify to Amb codec; non-`.amb`, non-`.md` file paths classify to Plain codec; `.md` remains unsupported/deferred or classified distinctly but not implemented; nested document refs still assemble through `.amb` parent docs.

3. Add Server tests for persisted artifact read/write.
   - Extend `tests/Server.Tests/DocumentPersistenceTests.fs`.
   - Cases: a `Special File` named `readme.txt` writes plain text, not `.amb` stable-id syntax; `readAllDocuments` reads that artifact back through the plain codec; workspace/directory `.amb` artifacts are unchanged; discovery still rejects/ignores stray `foo.amb` as today.
   - Ref regression: a file document containing a Ref occurrence persists the target text in `readme.txt`, with no in-band edge target. A file-only rebuild cannot recover that Ref edge.

4. Add loader/backup regression tests only if dispatch changes startup behavior.
   - Extend `tests/Server.Tests/DocumentLoaderTests.fs` if `DocumentLoader.tryLoadState` or `writeStateBackup` needs new coverage.
   - Keep legacy monolithic `gambol` fallback behavior unchanged.

5. Implement in smallest slices after tests exist:
   - Add `src/Shared/PlainTextDocument.fs` for pure parse/write/reconcile helpers.
   - Add a shared `DocumentFormat`/codec dispatch in `src/Shared/DocumentAssembly.fs` or a small adjacent module.
   - Route `DocumentAssembly.readArtifact` through dispatch.
   - Route `DocumentPersistence.writeDocument` through dispatch.
   - Keep `AmbDocument` behavior unchanged except for shared helper extraction if needed.

## Verification

Targeted foreground checks when implementing:

```bash
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~PlainTextDocumentTests|FullyQualifiedName~DocumentAssemblyTests"
dotnet build tests/Server.Tests -c Debug
dotnet test tests/Server.Tests -c Debug --no-build --filter "FullyQualifiedName~DocumentPersistenceTests|FullyQualifiedName~DocumentLoaderTests"
```

Run the broader suite in the background after the focused tests pass:

```bash
./scripts/test.sh all
```

## Out Of Scope

- Implementing code in this planning pass.
- Markdown `.md` persistence.
- Full concurrent merge/conflict behavior.
- Replacing `.amb` for workspace or directory documents.
- A background filesystem watcher or automatic local filesystem reconciliation.
- Preserving arbitrary graph refs through plain text artifacts without current graph/complement context.