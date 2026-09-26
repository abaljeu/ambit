# Workspace XML Format

Status: Draft
Authority: Target design for XML workspace files (well-formed XML; HTML excluded).
See also: [[doc/roadmap/workspace-text-outline-conversion.md]], [[doc/roadmap/workspace-format-dispatch.md]], [[doc/roadmap/reference-expressions.md]], [[doc/reference/formats/xml-round-trip-plan.md]]

Import/export workflow and the generic conversion contract live in [[doc/roadmap/workspace-text-outline-conversion.md]]. This format applies to `File` artifacts whose persisted body is XML.

## Classification

Dispatch to the Xml codec uses a **heading scan** of artifact text (leading bytes; skip UTF-8 BOM and insignificant leading whitespace). Path extension is not authoritative. See [[doc/roadmap/workspace-format-dispatch.md]].

- **HTML-shaped** — first markup is `<!DOCTYPE html` or an `html` element (ASCII case-insensitive) → Plain codec; HTML is out of scope.
- **XML-shaped** — first markup is XML (e.g. `<?xml`, `<!--`, `<!DOCTYPE`, or `<` starting an element) and not HTML-shaped → Xml codec when a well-formed parse succeeds.
- **Otherwise** — Plain codec.

Malformed XML after Xml classification uses Plain on read/write (flagged).

## Export

Export is **operations-driven**, not whole-cloth: `file_next = f_out(file_prev, op)` ([[doc/roadmap/workspace-text-outline-conversion.md]]). An op rewrites only the XML fragments it touches; all other bytes stay as they were. Projection rules live in **Tree mapping**; they define one node's fragment shape and do not imply full-document regeneration.

## Structural classes

Gambol stores class names on each node in `cssClasses`. Three disjoint kinds share that field:

- **System** — `amb-*` prefix; UI/DOM row state; reserved and not user-editable.
- **User** — styling classes such as `.blue` or `.h1`; assigned by the user; never written into external file bodies by any codec.
- **Structural** — codec-owned; record syntactic role in the persisted format; never written into external file bodies.

Each file format defines its own structural class vocabulary behind a format-specific prefix so kinds stay distinctive across codecs. Markdown uses `md-*` ([[doc/roadmap/workspace-format-md.md]]). XML uses the `xml-*` vocabulary in **Tree mapping**.

Import sets or refreshes the structural class from the parsed construct. Export reads it to choose fragment shape. Reconciled import preserves user classes on matched nodes.

## Tree mapping

**Import shape.** The `File` node is the document root (filename lives on that node). Import does not create a node for the file itself — it attaches **Owner** children under that `File` from the parsed file body. Prologue bytes and any trailing opaque tail live on the artifact complement; per-node wrapper bytes live on node complements.

**Prologue** (XML declaration, `DOCTYPE` with internal DTD subset and entity declarations, comments, and processing instructions before the first top-level body construct): skip on read — do not create outline nodes. Store prologue bytes on the artifact complement. On write, emit them before the projected body unchanged.

Within the file body (after prologue through end of file), each projection-table row becomes one outline node; **text** (character data) imports as **regular** Owner children with no `xml-*` class (see **Text**). **Top-level** constructs — elements, comments, CDATA sections, and processing instructions, including comments after the last element — are depth-0 Owner children under the `File` node in document order. Nested elements add one depth level per ancestor.

**Epilogue complement** — trailing bytes after the last representable top-level construct, if any (e.g. final whitespace not attributed to a node). Skip on read as nodes; preserve on write.

Under a parent `xml-element`, Owner children appear in this order: all `xml-attribute` children first (source attribute order), then all other child rows in document order.

Import strips syntactic wrappers into per-node complement bytes and drops them from node text where the table says so. Export reads structural class and node fields from the table; re-emit wrappers from complement, not from text. Export projects an `xml-element` by emitting its `xml-attribute` children before other child content.

**Projection.** Export uses the row's structural class — parent affects wrapper and sibling order only (`xml-element`: attributes before other children; `xml-pi`: `<?` … `?>` around target and opaque `text` only). See **Parentage rules**.

**Parentage rules.** Gambol edit operations that would violate these rules **fail or no-op** (invalid parent is a common case). The codec does not rely on misplaced `xml-*` rows for round-trip.

- `xml-attribute` — no Owner children.
- `xml-pi` — no `xml-*` Owner children (PI data stays in `text`; multi-line tails may be **regular** Owner children only).
- `xml-comment`, `xml-cdata` — **regular** Owner children only (text nodes; see **Text**).
- Other `xml-*` rows — only under `File` or `xml-element` (`xml-attribute` only under `xml-element`).

**Text.** Character data has no `xml-*` structural class. Import creates **regular** Owner children (`text` field holds the content) under `File`, `xml-element`, `xml-comment`, or `xml-cdata`. `xml-comment` and `xml-cdata` rows also have a `text` field (see projection table).

Under `xml-element` or `File`, text children export in sibling order (standard entity rules). Under `xml-comment` and `xml-cdata` only (no nested XML delimiters inside the wrapper), use **outline layout**: row `text` is the first line; each **sibling** text child starts a new line; each **deeper** text child adds one indent step on that line. Export joins with newline and spaces accordingly inside the wrapper. Import inverts the split where complements do not already fix the bytes.

**Parser limits (cold import).** Import uses a standard XML parser. It assigns rows and parentage only for constructs XML defines. PI data is one opaque string in `xml-pi` `text` (markup-like characters stay literal, not element rows). Multi-line PI tails import as regular Owner children.

**Warm import.** Reconciled import has `(previous file text, current graph, new file text)`. Match nodes by **`NodeId` first**, using complement bounds and each node's exported fragment from the prior round. Preserve graph ownership and structural classes for matched nodes; update fields from the file slice each node owns. Parentage follows **Parentage rules** — invalid graphs are prevented at edit time, not reconciled here.

**Empty elements.** A childless `xml-element` stores empty-element closing syntax in complement (`<e/>` vs `<e></e>`). Export preserves complement when present. New empty elements without complement use either well-formed form (implementation default).

| Construct | Structural class | Node fields | Complement bytes |
| --- | --- | --- | --- |
| Element | `xml-element` | `text` = element name (see **Names**) | Opening-tag syntax beyond `text`; empty-element closing syntax when childless |
| Attribute | `xml-attribute` | `name` = attribute name (see **Names**); `text` = attribute value | Quoting and surrounding attribute syntax |
| CDATA section | `xml-cdata` | `text` = character data (may be empty when children hold content) | `<![CDATA[` / `]]>` delimiters and surrounding spacing |
| Comment | `xml-comment` | `text` = comment body (may be empty when children hold content) | Delimiters and surrounding spacing |
| Processing instruction | `xml-pi` | `name` = PI target; `text` = first PI data line after the target (may be empty); later lines are regular text children | Delimiters, target/data spacing, and closing syntax |

**Processing instructions** in the file body import as one `xml-pi` row. The target goes in `name`; PI data is opaque text (see **Parser limits**). The first data line goes in row `text`; later data lines import as **regular** Owner children so outline layout can preserve multi-line data without pretending it is XML structure. Export emits `<?`, sanitized target, opaque data lines, and `?>` from `name`, `text`, regular text children, and complement — no `xml-*` children (see **Parentage rules**). Prologue PIs stay in the artifact complement only (see **Prologue**).

**Names.** Element `text` and attribute `name` hold the tag or attribute name **as written**, with an optional single `:` (`title`, `meta:note`). The codec does **not** resolve namespace URIs, infer prefix bindings, or treat names as equivalent across prefixes. `xmlns` and `xmlns:*` import as ordinary `xml-attribute` rows; `text` is the quoted attribute value only — never processed as a URI. Import reconstructs `prefix:local` from the parser when prefix is non-empty; otherwise `local`. Complement bytes preserve literal opening-tag and attribute syntax for byte-identical unchanged export. Export writes graph name fields into markup (after **Name sanitization**); no prefix rebinding or URI-driven rewrites.

Structure preservation targets generic XML well-formedness only. No schema validation, namespace URI semantics, or format-profile rules (SVG, XSLT, etc.) are in scope for this slice.

## Identity

Stable `NodeId` is authoritative in the graph. XML must not gain Ambit-only syntax (`^` stable ids, `->` ref lines, `#name-token` suffixes) just to preserve graph round trips.

The graph **`name`** field is an optional, non-globally-unique ref identifier (see [[doc/roadmap/workspace-format-plain.md]]). It is **not** an XML element, attribute, or PI name and is **never** written into XML markup. Codec naming uses other node fields: element name in **`text`**; attribute name in **`name`** on `xml-attribute` rows only; PI target in **`name`** on `xml-pi` rows only. Ref **`name`** and those codec fields are unrelated — do not match or merge them on import or export.

Durable file anchors (`xml:id`, `id`, `name` attributes, element paths) are **out of scope** for this slice. Reconciliation uses graph `NodeId` and complement context on warm round trips; cold import from XML alone mints fresh ids for parser-representable structure only.

Subtree reconciliation (`NodeId` matching, deletion) follows **Reconciliation** below and [[doc/roadmap/workspace-text-outline-conversion.md]] § Deletion on import.

## Reconciliation

Import reconciles **(previous file text, current graph document, edited/new file text)**.

| Change kind | Import behavior | Export behavior |
| --- | --- | --- |
| Unchanged imported text | No graph ops | Byte-identical write |
| Unchanged exported tree | Graph-identical import for matched nodes; complement restores metadata XML cannot encode; parser-only shapes per **Parser limits** | No file change |
| Content edit | Match by `NodeId` where available; update node fields and complements per **Tree mapping** | Rewrite matched fragment only |
| Content add | Mint new `NodeId`; insert Owner edge at inferred position in sibling order | Insert new fragment at correct position |
| Content delete | External deletion — reuse graph delete/ownership-migration semantics ([[doc/roadmap/workspace-text-outline-conversion.md]] § Deletion on import) | Remove matched fragment only |
| External move (reorder/reparent in file) | Default delete plus add unless id matching preserves identity | N/A — import-side |
| Outline move (graph op) | N/A — graph-side | Preserve `NodeId`; rewrite moved subtree at new position |

## Move asymmetry

External editors may reorder or restructure elements without stable anchors. XML has no universal move marker, so import treats ambiguous relocations as delete plus add unless matching is strong enough to preserve `NodeId`. Graph-driven moves preserve identity on export — the codec rewrites only the moved subtree.

## Irregularities

The Xml codec does not fail import or export on parseable irregular content; target **round-trip equivalence** through graph fields and complements. Mal-formed XML (parse fails) still uses plain-text fallback (**Rejected** below).

### Mal-formed XML

Use a regular .NET XML-to-DOM parser with chosen `XmlReaderSettings` (including `CheckCharacters = false` for control characters). If parse fails, fall back to plain-text import per [[doc/roadmap/workspace-format-plain.md]]; irregularities below are moot for that file until content is well-formed again.

A missing XML declaration is valid XML, not an irregularity.

#### Rejected

Parse fails; import uses plain-text fallback (flagged).

- Mismatched or unclosed tags
- Duplicate attributes on one element
- Invalid element or attribute names
- Unescaped `<`, `&`, or `]]>` in text
- `--` inside comment body
- Unclosed or malformed comment
- Malformed processing instruction
- Invalid or unclosed entity reference
- Malformed XML declaration
- Encoding declaration inconsistent with actual encoding when decode or parse fails

#### Accepted

Parse succeeds; Xml codec imports.

- Multiple top-level elements — [ ONLY if it doesn't complicate coding ] sibling depth-0 `xml-element` Owner children under the `File` node (outline target; how the reader yields them is implementation)  
- Prologue `<?xml …?>`  [ ONLY if it doesn't complicate coding ] - semantically wrong but parseable (e.g. wrong `encoding=` while bytes still decode) — prologue bytes preserved via complement
- Control characters in content — encode in graph as `\uuuu` (see **Control characters**); export writes literal character

### Editing XML-source nodes

Graph **edit operations** (add, move, reparent, structural class assignment) that violate **Parentage rules** fail or no-op. Field edits on valid nodes still project on export; invalid name or body values use sanitization rules below (UI may highlight).

#### Name sanitization

Element name (`text`), attribute name (`name`), and PI target (`name` on `xml-pi` — only the first word before a space). Names may contain **at most one** `:`; see **Names**.

- **`sanitizedName x`**
  1. If `x` contains a space, keep only the substring before the first space (suffix is not preserved on round-trip).
  2. Walk left to right; each character that is not legal at that position in an XML Name becomes `_`, except allow a single `:` (a second `:` becomes `_`). Do not collapse consecutive `_`.
  3. If the result is empty, use `_`.
- **Export** — always write `sanitizedName x`.
- **Reconciled import** — parsed name `y` matches an existing node with graph value `x` when `y = sanitizedName x` and `NodeId` / reconciliation context agree. When `x` contains a space, set the graph field to the part before the first space; otherwise keep **`x`** (not `y`) when `y = sanitizedName x`.
- **Cold import** — no prior `x`; node field is the parsed name from the file.

Example: `café` → `caf_`; `block this…` → `block`; `2items` → `_2items`; `meta:note` → `meta:note`; `a:b:c` → `a_b:c`.

#### Comment body

`xml-comment` `text` plus optional regular text children. Layout inside the wrapper follows **Text** (sibling → newline, deeper → indent). Comments do not expand entities; `--` cannot appear literally in the file.

- **`sanitizedCommentBody x`** — replace each non-overlapping `--` substring with `-_-` (left to right).
- **Export** — `sanitizedCommentBody` on each body slice (row `text`, then text children per layout); delimiters from complement.
- **Reconciled import** — parsed body slices match the row `text` or a text child by `NodeId` / reconciliation context; keep graph values when `y = sanitizedCommentBody x`.
- **Cold import** — row `text` and text children are filled from the parsed comment body per layout.

Example: row `text` ` TODO:`, one child `    fix this` → `<!-- TODO:\n    fix this-->`. `a--b` → `a-_-b`. Literal `--` in the file remains **Rejected** on parse (plain fallback).

#### Duplicate attribute names

Under one `xml-element`, two or more `xml-attribute` children with the same graph `name`.

- **`disambiguatedAttrName x i`** — let `base = sanitizedName x`. For duplicate index `i` among same-graph-name siblings (0-based, source order): `i = 0` → `base`; `i > 0` → `base` plus `i` underscore characters (`a`, `a_`, `a__`, …).
- **Export** — walk attribute children in source order; assign `i` per distinct graph `name`; write `disambiguatedAttrName x i`.
- **Reconciled import** — parsed attribute name `y` matches an attribute node with graph `name` `x` at duplicate index `i` when `y = disambiguatedAttrName x i` and `NodeId` / reconciliation context agree; keep graph **`name` `x`**, not `y`.
- **Cold import** — node `name` is the parsed name from the file.

A file that still contains duplicate unprefixed attribute names remains **Rejected** on parse (plain fallback).

#### Processing instructions

`xml-pi` in the file body. **`name`** holds the PI target; **`text`** holds opaque PI data (see **Parser limits**). Further data lines are regular Owner children only (see **Parentage rules**). Prologue PIs are complement-only and do not use this shape.

- **Invalid target** — only name irregularity; project with **`sanitizedName`** on **`name`** (see **Name sanitization**). Only the first word before a space is the target; suffix is not preserved on round-trip.
- **Export** — `<?`, sanitized target, opaque `text`, `?>`; complement preserves literal syntax.
- **Reconciled import** — same asymmetric **`sanitizedName`** match on target as for element/attribute names.

#### Escaping

Standard XML entity rules on **regular text children under `xml-element`** and on **attribute values**: encode on export (`&amp;`, `&lt;`, `&quot;`, `&apos;` as required); decode on import into graph fields. **CDATA** and **comment** text children stay literal aside from **Control characters** (see **Comment body**). PI `text` stays literal aside from **Control characters**.

#### Control characters

In graph-bound text fields (regular text children, `xml-attribute` `text`, `xml-comment` / `xml-cdata` `text` and their text children, `xml-pi` `text`, and regular PI continuation-line children). TAB, LF, and CR stay literal in the graph.

- **`encodeControlChars x`** — each other control-character code point in `x` becomes `\` + `u` + four lowercase hex digits.
- **Export** — write `decodeControlChars x` (inverse mapping) as the literal character in the file (`CheckCharacters = false` when the writer requires it).
- **Reconciled import** — parsed literal `y` matches existing graph `x` when `y = decodeControlChars x` and `NodeId` / reconciliation context agree; keep **`x`**.
- **Cold import** — graph field is `encodeControlChars` of parsed content.

Example: literal U+0001 in `hello` + U+0001 + `world` → graph `hello\u0001world`.

## References

XML has no native Ambit ref syntax in this slice. Ref edges export as the referred node's visible content projection only (same consequence as plain text: the file does not encode the target edge). Cold rebuild from XML alone cannot recover Ref edges.

Use `.amb` for ref lines and `.md` for markdown-readable references where applicable. Reconcile paths with [[doc/roadmap/reference-expressions.md]] where the target is addressable that way.

## Verification Targets

Test home: `tests/Shared.Tests/XmlDocumentTests.fs` (register after `PlainTextDocumentTests.fs` in `Gambol.Shared.Tests.fsproj`). Extend `DocumentAssemblyTests.fs` for path → Xml codec dispatch; extend `DocumentPersistenceTests.fs` for an XML `File` round-trip on disk.

Codec and reconciliation (`XmlDocumentTests`):

- Well-formed XML imports to the outline shape and sibling order defined in **Tree mapping**; unchanged import writes byte-identically.
- Exported tree imports with the same `NodeId` values for representable content when reconciliation has current graph context.
- Content edit, add, and delete preserve identity for unaffected nodes; export leaves untouched bytes unchanged.
- Reconciled import preserves user `cssClasses`; structural classes are never written to the file.
- `xmlns` / `xmlns:*` round-trip as ordinary attributes; attribute values and prefixed names are literal text, not URI-resolved.
- Empty-element syntax (`<e/>` vs `<e></e>`) round-trips via element complement when unchanged.
- Prologue (including `DOCTYPE` and DTD) is not imported as nodes; unchanged export preserves it byte-identically from complement. `xml-cdata` and `xml-comment` round-trip with text children in document order. Top-level post-element comments import as depth-0 nodes; epilogue complement holds only trailing opaque bytes.
- External structural move defaults to delete plus add; outline move preserves `NodeId` on write.
- No Ambit-only identity syntax is emitted into XML.
- HTML-shaped headings classify to Plain; XML-shaped headings classify to Xml regardless of path extension.
- Control characters in text fields round-trip via `\uuuu` graph encoding and literal character on export.
- Reparenting an `xml-*` row under `xml-pi` or `xml-attribute`, or adding any Owner under `xml-attribute`, fails or no-ops.

Dispatch and persistence: see [[doc/roadmap/workspace-format-dispatch.md]].
