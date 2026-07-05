# Workspace Plain Text Format

Status: Draft
Authority: Target design for text workspace files that are not `.amb` or `.md`.
See also: [[doc/roadmap/workspace-text-outline-conversion.md]], [[doc/roadmap/reference-expressions.md]], [[doc/roadmap/workspace-format-amb.md]]

Import/export workflow and the generic conversion contract live in [[doc/roadmap/workspace-text-outline-conversion.md]]. This format applies to plain text files that are not `.amb` or `.md`.

## Export

Export is **operations-driven**, not whole-cloth: `file_next = f_out(file_prev, op)` ([[doc/roadmap/workspace-text-outline-conversion.md]]). An op rewrites only the lines it touches; all other bytes stay as they were, including blank lines, line endings, indent style, and untouched node lines.

The rules below define one node's line projection. They do not imply full-file regeneration.

## Lines and indentation

One non-blank line creates one node; blank lines create none and are never projected. Node text never contains embedded newlines. Line endings on touched lines follow the replaced line.

Depth comes only from leading whitespace. There are no headings, list markers, or other structural prefixes. Node text is the line after stripping structural indent and any identity suffix.

Plain text has no indent declaration, so each import infers style from non-blank lines:

1. Any leading tab means **tabs**: one tab per level. Spaces before the first tab are **mixed indent**; flag them, but compute depth from the tab count after those spaces.
2. Otherwise use **spaces**. `spaces per level` is the GCD of non-zero leading space counts, minimum 1. Depth is leading spaces divided by that step.
3. Store the inferred style on the file artifact complement. Re-infer only when importing a changed file.

Mixed tab/space hierarchy is flagged but still deterministic. Export uses the stored uniform style: one tab per level, or `spaces per level` × depth spaces. Do not convert tabs/spaces unless import re-infers a changed file.

The file root is depth 0. Shallower lines pop the stack. Deeper lines may increase depth by at most one level; if raw depth exceeds active + 1, flag it, use active + 1, and move each surplus indent unit into node text as leading spaces. Projection writes structural indent plus those text spaces, so skipped indentation round-trips.

## Identity

Stable `NodeId` is authoritative in the graph. The readable file anchor is the node `name`, projected as a trailing ` #name-token`.

A `name-token` follows [[src/Shared/Filename.fs]] `Ok` rules: letters, digits, `.`, `-`, `_`; max 255; not `.` or `..`. Invalid or duplicate tokens in one file are flagged.

On export, named nodes append ` #name-token`. On import, trailing ` #name-token` (whitespace before `#` required) is stripped from node text and sets the created node's `name`. Ref targets use the same token form; position distinguishes suffix from target.

Subtree reconciliation (`NodeId` matching, unnamed lines, deletion) follows **Reconciliation** below and [[doc/roadmap/workspace-text-outline-conversion.md]] § Deletion on import.

## Reconciliation

Import reconciles **(previous file text, current graph document, edited/new file text)**. Export is operations-driven: only lines touched by an op change; untouched bytes — blank lines, line endings, indent style, and unmodified node lines — are preserved.

| Change kind | Import behavior | Export behavior |
| --- | --- | --- |
| Unchanged imported text | No graph ops | Byte-identical write |
| Unchanged exported outline | Graph-identical import for representable content; complement restores metadata plain text cannot encode | No file change |
| Line text edit | Match by ` #name-token` / `NodeId`; update node text | Rewrite matched line only |
| Line add | Mint new `NodeId`; insert Owner edge at inferred depth | Append or insert new line at correct depth |
| Line delete | External deletion — reuse graph delete/ownership-migration semantics ([[doc/roadmap/workspace-text-outline-conversion.md]] § Deletion on import) | Remove matched line only |
| External move (reorder/re-indent) | Default delete plus add unless id matching preserves identity | N/A — import-side |
| Outline move (graph op) | N/A — graph-side | Preserve `NodeId`; rewrite moved node's line at new depth |

Unnamed lines (no ` #name-token`) mint fresh `NodeId` on first import. Once exported with a suffix, subsequent reconciliation matches by stable id.

## Move asymmetry

External editors may reorder or re-indent lines without stable ids. Plain text has no durable move marker, so import treats ambiguous relocations as delete plus add unless trailing ` #name-token` matching is strong enough to preserve `NodeId`. Graph-driven moves (reparent, reorder via ops) preserve identity on export — the codec rewrites only the moved node's line at the new depth and indent.

## References

Plain text has no native embed or wikilink syntax. **Ref lines** use `.amb` grammar:

```
<indent> "-> " <ref-target>
<ref-target> ::= "#" <name-token>
              | <workspace-relative-path> "#" <name-token>
```

Import resolves `ref-target` to `NodeId` and emits a Ref edge under the current parent. Export projects a Ref edge as `-> …` at the correct depth. Inline refs are plain text; use a ref-only child line instead.

Use `.md` for markdown-readable references and `.amb` for metadata prefixes. Reconcile paths with [[doc/roadmap/reference-expressions.md]] where applicable.

## Metadata

`cssClasses` are graph-only. Export never writes `{.class}` prefixes. Import preserves user classes on reconciled nodes; hand-authored `{.class}` remains node text.

## Verification Targets

Test home: `tests/Shared.Tests/PlainTextDocumentTests.fs` (register after `AmbDocumentTests.fs` in `Gambol.Shared.Tests.fsproj`). Extend `DocumentAssemblyTests.fs` for path → Plain codec dispatch; extend `DocumentPersistenceTests.fs` for `readme.txt` round-trip on disk.

Codec and reconciliation (`PlainTextDocumentTests`):

- Arbitrary LF/CRLF text imports to the expected tree; unchanged import writes byte-identically.
- Exported outline imports with the same `NodeId` values for representable content; complement restores `cssClasses`.
- Line edit, add, and delete preserve identity for unaffected nodes; export leaves untouched bytes unchanged.
- External text move defaults to delete plus add; outline move preserves `NodeId` on write.

Format rules:

- Untouched file bytes stay unchanged, including blanks, line endings, indent style, and untouched node lines.
- Tab and space files infer correctly; export preserves the inferred style without silent tab/space conversion.
- Mixed and skipped indentation are flagged; depth remains deterministic and skipped units round-trip as text spaces.
- One non-blank line creates one node; blank lines create none.
- ` #name-token` sets `name`; invalid or duplicate suffixes are flagged.
- Ref-only lines round-trip at the correct depth; inline refs stay plain text.
- Reconciled import preserves user `cssClasses`; unsupported constructs produce diagnostics.

Dispatch and persistence (assembly/persistence tests): see [[doc/roadmap/workspace-format-dispatch.md]].

- `.amb` paths classify to Amb codec; non-`.amb`, non-`.md` file paths classify to Plain codec; `.md` remains unimplemented.
- A `Special File` named `readme.txt` writes plain text, not `.amb` stable-id syntax; `readAllDocuments` reads it back through the plain codec.
- Workspace and directory `.amb` artifacts are unchanged by plain-format dispatch.

