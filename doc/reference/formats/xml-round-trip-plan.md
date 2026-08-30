# XML Round Trip Plan

## Assumptions:
- This conversation is still planning only; implementation code is out of scope until explicitly requested.
- XML means any file based on XML.  HTML is out of scope.
- The main implementation work is adding a persisted file type beside `.amb` in the document read/write layer.
- Tests drive the work. Pure format and reconciliation behavior should live in `src/Shared/` where possible; filesystem integration stays in `src/Server/`.
- Standard diff/LCS-style algorithms may be used internally, but the spec and tests should name required outcomes.
- XML text must not gain Ambit-only syntax for hidden graph identity. No `NodeId`, `#name-token`, or ref target is emitted into arbitrary text just to preserve graph round trips.
- Preserve structure when possible. However, this only applies to the base XML spec, not any particular document format.
- Preserve content.  

## Scope

Plan and document a narrow implementation slice:

- Preserve existing `.amb` document behavior.
- Preserve existing plain-text codec behavior for File artifacts that are not XML.
- Add an XML codec for File artifacts whose file is XML (path and/or content classification TBD; HTML excluded).
- Map the XML tree (elements, attributes, text nodes) to the graph outline model; preserve element order, attribute names/values, and text content on unchanged round trips when the base XML structure is representable.
- Structure preservation targets generic XML well-formedness, not validation against any particular document schema or format profile.
- Reconcile edits as `(previous file text, current graph document, edited/new file text)`; unchanged imported XML exports byte-identically when the graph has not changed representable content.
- Do not add HTML behavior in this slice.
- Do not make filesystem observation authoritative; DB/graph identity remains the source of truth.


## Code Shape

Extend the existing `DocumentFormat` dispatch with an XML codec; keep `AmbDocument` and `PlainTextDocument` unchanged.

- Planned codec module: `src/Shared/XmlDocument.fs` (parse, write, reconcile).
- Extend `DocumentFormat.classifyCodec` (and read/write routing) for XML-shaped file artifacts; path vs content sniffing TBD.
- Fit according to [[code-shape.md]].

## Documentation

Roadmap specs for this slice :

- [[doc/roadmap/workspace-format-xml.md]] — tree mapping, identity, reconciliation, verification targets.
- [[doc/roadmap/workspace-format-dispatch.md]] — `Xml` codec in classification and routing.
- [[doc/roadmap/workspace-text-outline-conversion.md]] — generic conversion contract (**Settled**); XML slice pointer in § Generic XML reconciliation.
- [[doc/roadmap/workspace-file-model.md]] Stage 7 Step 6.

## Test-First Implementation Plan

Behavioral targets: [[doc/roadmap/workspace-format-xml.md]] § Verification Targets. Dispatch: [[doc/roadmap/workspace-format-dispatch.md]].

1. **Codec tests** — `tests/Shared.Tests/XmlDocumentTests.fs` (after `PlainTextDocumentTests.fs` in `Gambol.Shared.Tests.fsproj`). Parse, write, reconcile per **Tree mapping**; byte-identical unchanged paths; structural classes; prologue/epilogue complement.
2. **Dispatch tests** — extend `DocumentAssemblyTests.fs`: XML-shaped paths → Xml codec; `.amb`, plain, and `.md` rules unchanged.
3. **Persistence tests** — extend `DocumentPersistenceTests.fs`: a `File` backed by e.g. `config.xml` round-trips through read/write; `.amb` artifacts unchanged.
4. **Loader tests** — extend `DocumentLoaderTests.fs` only if Xml dispatch changes startup or backup behavior.
5. **Implement** — `src/Shared/XmlDocument.fs`; extend `DocumentFormat` classification and `readArtifact` / `writeArtifact` routing; leave `AmbDocument` and `PlainTextDocument` unchanged.

## Out Of Scope

- HTML documents (even when XML-shaped).
- Schema validation and format-profile rules (SVG, XSLT, etc.); well-formed XML only.
- Markdown `.md` persistence.
- Full concurrent merge/conflict behavior.
- Replacing `.amb` for workspace or directory documents.
- A background filesystem watcher or automatic local filesystem reconciliation.
- Preserving Ref edges through XML artifacts without current graph/complement context.
- Emitting Ambit identity or ref syntax into XML markup.
- Durable file anchors (`xml:id`, `id`, `name`, XPath) for import identity matching.
