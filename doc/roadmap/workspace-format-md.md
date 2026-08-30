# Workspace `.md` Text Format

Status: Target design
Authority: Target design for markdown workspace files. Obsidian markdown structure is assumed; only Gambol-specific mapping is defined here.
See also: [[doc/roadmap/workspace-text-outline-conversion.md]], [[doc/roadmap/reference-expressions.md]]

Import/export workflow and the generic conversion contract live in [[doc/roadmap/workspace-text-outline-conversion.md]].

## Export

Export is **operations-driven**, not whole-cloth. Each graph op projects only the file lines it affects onto the prior artifact: `file_next = f_out(file_prev, op)` ([[doc/roadmap/workspace-text-outline-conversion.md]]). Unchanged regions of `file_prev` — including blank lines, line endings, and lines for untouched nodes — are not rewritten.

Format rules below define how a **single node** projects to one file line when an op requires it. They do not imply regenerating the full file from the subtree.

## Line breaks

The outline graph has **no line breaks** in node text: one substantive file line yields one node. Import splits the file into lines (how `\n` and `\r\n` are recognized is implementation). Node text never contains embedded newlines.

**Blank lines** are not imported and are not operated on; they remain in `file_prev` until an external edit removes them. Line endings on touched lines follow the ending style of the line replaced in `file_prev`.

## Hierarchy

Each substantive line becomes an outline node. Depth is not "heading text + body"; structure comes from heading level, line order, and list indent.

Example (labels show outline depth):

```markdown
# level 1
level 2
## level 2
level 3
level 3
level 3
- level 3
  - level 4
  - level 4
    - level 5
level 3
### level 3
## level 2
level 3
```

Rules:

- **Heading** — ATX heading: `#` count is outline depth (1–6), then a space before heading text, **or** two or more `#` with no space required before text. Opens a node at that depth; pop the stack to that depth first. May pop to any shallower depth (or equal, for a sibling). When going **deeper** than the active heading, depth must be active + 1. If the source skips levels (e.g. `## hello` then `#### world`), import **flags** it and treats the heading as depth active + 1 (`### world`); projection writes the corrected level. A lone `#tag` (single `#` with no following space) is **not** a heading — see **Tags**.
- **Plain line** — node at depth = active heading depth + 1. Multiple consecutive plain lines are sibling nodes, not joined text on the heading. Includes tag-only lines, inline `#tag` text, and horizontal rules (`---`, `***`, `___`) — full line text, no structural class.
- **List item** — unordered (`-` / `*`) or ordered (`N.`). Depth = active heading depth + 1 + indent steps (two spaces or one tab per step on import). Nested items are deeper nodes; dedent returns to the shallower depth. Export writes two spaces per indent step; tab indents are normalized to spaces on import. **Task lists** (`- [ ]` / `- [x]`) are `md-list` items; `[ ]` / `[x]` stay in node text as plain text (not parsed as state).
- **Blockquote** — `>` after indent. Same depth rule as list items. Lines such as `> - item` are `md-quote`, not `md-list`; the `-` stays in node text.
- **Fenced code block** — opening/closing lines are `` ``` `` after indent; lines between are **fenced inner** lines. All are `md-code-block`. Opener and closer sit at the same depth as a plain line would (active heading depth + 1 + indent steps). **Fenced inner lines are depth + 1** under the opener. Opener node text is the optional language tag after `` ``` ``; inner lines keep full text; closer node text is empty. While inside a fence, normal line-kind rules and embed parsing do not apply. Unclosed fence at EOF is flagged.
- **Indented code block** — one or more consecutive lines with ≥4 leading spaces (outside a `` ``` `` fence). Each line is `md-code-block` with **no opener or closer**. Depth = active heading depth + 1 + one per 4-space indent level (same +1 inner offset as fenced inner). Node text is the line after stripping leading spaces. Normal line-kind rules do not apply within the run. On projection, indented lines emit four leading spaces per indent level; fenced inner lines emit no extra indent.
- **Blank line** — no node; not imported; not projected on export. Ignored for parentage; may separate siblings in the file only.

**Line-kind precedence** (outside a `` ``` `` fence): `` ``` `` opener/closer (after indent); then ≥4 leading spaces (indented code); then Obsidian tag-only line (`#tag`); then ATX heading; then blockquote (`>`); then list (`-` / `*` / `N.`).

When an op projects a node to a line, line kind comes from structural `cssClasses` (see **Metadata**): `md-head` → ATX heading; `md-quote` → blockquote; `md-code-block` → fence line; `md-list` / `md-list-star` / `md-list-ordered` → list item; neither → plain line.

## Identity

No `amb:` HTML comments. Follow [[doc/roadmap/workspace-text-outline-conversion.md]]: stable `NodeId` is authoritative in the graph; the durable readable anchor in the file is `#name` on the node's `name` field. Ambit ensures `name` is unique among reference targets within a file.

**Every substantive line creates a node.** Import always emits one outline row per non-blank line.

**Name token.** `name` and `^name` suffixes use the same `name-token` as `.amb`: an identifier satisfying [[src/Shared/Filename.fs]] `Ok` rules (letters, digits, `.`, `-`, `_`; max 255; not `.` or `..`). Invalid tokens are flagged on import.

**Block-id suffix.** When an op projects a line, append Obsidian-style ` ^name-token` if the node has a `name`. Import parses a trailing `^name-token` (whitespace before `^` required), strips it from line text, and **sets `name` on the node created from that line**. Do not promote plain or list nodes to ATX headings solely to carry identity. Gambol refs in the file use embed form `![[…#name-token]]`, not Obsidian's `[[#^name-token]]` block-fragment link form.

**Heading text** is node display text only; it is not required to equal `name`. A projected heading line is `#` × depth plus text; when the node has a `name`, append ` ^name` (e.g. `## Section title ^section-name`).

**Duplicate block-ids.** Two lines in one file with the same `^name` suffix are flagged on import.

Subtree reconciliation (`NodeId` matching, unnamed lines, deletion) is defined in [[doc/roadmap/workspace-text-outline-conversion.md]]; this format only defines how a line yields node text, `name`, and structural class.

## References

**Embeds (`![[…]]`) are Ref edges.** Import parses Obsidian embed syntax as Ref edges; `#name` fragment resolves to `NodeId` via the target's `name` field — not Obsidian's auto-generated heading slug. When an op projects a Ref, write embed form `![[…]]`.

| Form | Meaning |
|------|---------|
| `![[file.md]]` | Ref to file subtree root (or file node) |
| `![[path/file.amb]]` | Cross-format; same, workspace-relative path |
| `![[#name]]` | Same-file ref to node with `name` |
| `![[file.md#name]]` | Cross-file ref to node with `name` |
| `![[file.md#name\|display]]` | Ref with Obsidian display alias |

**Wikilinks (`[[…]]`) are plain text.** Import does not parse `[[…]]` as Ref edges; the brackets stay in node text. Obsidian link navigation in external editors is unaffected; graph refs use `![[…]]` only.

**Ref-only child.** A child row that is only a Ref — the `.amb` counterpart of `-> #name` — projects to a **plain line** at the correct depth whose entire line text is one embed `![[…]]`. Import treats such a line as a child node with a Ref edge. When the wrapper row has a `name`, append ` ^name` like any other named node.

**Inline embeds.** `![[…]]` in lines that also have other text still parse as Ref edges; the full line text (including the embed) is node text. Only a line that is **solely** an embed is a ref-only child.

Reconcile paths with [[doc/roadmap/reference-expressions.md]] where applicable. Cross-file edits must not force unrelated files to be rewritten.

## Tags

**Obsidian tags are plain text.** `#tag` inline and tag-only lines stay in node text; import does not create Ref edges or graph tags from them. A **tag-only line** is one `#` immediately followed by tag characters (no space) — a plain line, not `md-head`. Inline `#tag` in other lines is likewise plain text.

## Metadata

**CSS classes (`cssClasses`) are graph-only for `.md`.** Ops do not write `{.class}` prefixes or other class syntax into markdown lines. Obsidian has no native per-line class marker; keeping classes out of the file preserves clean external editing.

**User classes** (e.g. `.blue`) are never written to the file. On import, when a line reconciles to an existing node, user classes on that node are **preserved**. Hand-authored `{.class}` text in an external file is node text, not parsed as metadata.

**Structural classes** record line kind for export. Set from the parsed line on every import; never written to the file. A node has at most one structural class.

| Line prefix (import) | Structural class | Node text |
|----------------------|------------------|-----------|
| `#`… (1–6 ATX hashes) + heading text | `md-head` | Text after the heading prefix (hashes and optional space per ATX rules) |
| `#tag` only (single `#`, no space) | none (plain) | Full `#tag` line text (minus block-id suffix) |
| `>` after indent | `md-quote` | Text after `>` and one optional space |
| `-` after indent | `md-list` | Text after the list marker and one optional space (includes `[ ]` / `[x]` task prefixes) |
| `*` after indent | `md-list-star` | Text after the list marker and one optional space |
| `N.` after indent (`N` = digits) | `md-list-ordered` | Text after the list marker and one optional space |
| `` ``` `` opener after indent | `md-code-block` | Optional language tag after `` ``` `` |
| `` ``` `` closer after indent | `md-code-block` | Empty |
| Line inside open `` ``` `` fence | `md-code-block` (fenced inner) | Full line text (minus block-id suffix) |
| ≥4 leading spaces (outside fence) | `md-code-block` (indented) | Text after stripping leading spaces |
| Neither | neither class | Full line text (minus block-id suffix) |

Import strips line-kind prefixes into the class entry and drops them from node text (fenced inner code lines are verbatim). When an op projects a line, read the class to choose line kind; prefix is re-emitted, not stored in text. `md-quote` → `>`; `md-code-block` opener → `` ``` `` plus language tag when non-empty; `md-code-block` closer → `` ``` ``; fenced inner → verbatim text; indented → four spaces per indent level + text; `md-list` → `-`; `md-list-star` → `*`; `md-list-ordered` → `N.` where `N` is the 1-based index among `md-list-ordered` siblings under the same parent (flag on import when the source number differs; projection writes the corrected index). List, quote, and fence opener/closer indent is two spaces per step (depth beyond the base depth under the active heading).

For arbitrary class syntax in the on-disk artifact, use `.amb` ([[doc/roadmap/workspace-format-amb.md]]).

## Text to outline / outline to text

Each rule pairs with its counterpart in [[doc/roadmap/workspace-text-outline-conversion.md]] **Content Conversion**. Export column rules apply when an op projects that node to its file line.

| Import | Export (per op) |
|--------|-----------------|
| Heading at depth D → node + `md-head`; pop stack to D | `md-head` at depth D → `#` × D line |
| Heading skips a level when nesting deeper → flag; use active + 1 | Writes consecutive heading levels on that line |
| Plain line under heading D → node at D + 1; no structural class | No structural class at D + 1 → plain line |
| `` ``` `` opener → node at D + 1 + indent + `md-code-block` | `md-code-block` opener → two-space indent + `` ``` `` line |
| Fenced inner line → opener depth + 1 + `md-code-block` | Fenced inner → verbatim line (no extra indent) |
| `` ``` `` closer → opener depth + `md-code-block` | `md-code-block` closer → two-space indent + `` ``` `` line |
| ≥4 spaces (outside fence) → `md-code-block` at inner depth | Indented → four spaces per level + text |
| `>` blockquote → node at D + 1 + indent + `md-quote` | `md-quote` → two-space indent + `>` line |
| `-` list item → node at D + 1 + indent + `md-list` | `md-list` → two-space indent + `-` line |
| `*` list item → node at D + 1 + indent + `md-list-star` | `md-list-star` → two-space indent + `*` line |
| `N.` list item → node at D + 1 + indent + `md-list-ordered` | `md-list-ordered` → two-space indent + `N.` line |
| Ordered number ≠ sibling index → flag; use corrected index | Projection writes consecutive ordinals |
| Tab list indent → same depth as equivalent spaces | Project list indent as spaces |
| Sole-embed plain line → ref-only child + Ref edge | Ref-only child → `![[…]]` embed-only plain line |
| `![[…]]` embed in text → Ref edge; `#name` → `NodeId` | Ref edge → `![[…]]` in line text |
| `[[…]]` wikilink in text → plain text only | `[[…]]` stays in node text; not a Ref |
| Trailing `^name` → strip suffix; set `name` on line's node | Named node → ` ^name` suffix on its line (any kind) |
| No `^name` | Unnamed node → no block-id suffix |
| Reconciled node → keep user `cssClasses`; set structural class from line | Never write any `cssClasses` to the file |

## Unsupported (report, do not silently drop)

- Setext headings, ATX headings deeper than six levels, footnotes, tables, HTML blocks, front matter.

## Verification Targets

- An op that does not touch a file leaves `file_prev` bytes unchanged (including blank lines and line endings).
- A node-projection op writes one line per format rules; omits all `cssClasses` from the file.
- Import of an unchanged file preserves user classes on reconciled nodes.
- `md-head`, `md-quote`, `md-code-block`, `md-list`, `md-list-star`, and `md-list-ordered` project to the correct line kind, marker, and depth.
- Fenced inner `md-code-block` nodes are children (+1 depth) of the opener; opener and closer are siblings at the same depth.
- Indented `md-code-block` runs have no opener/closer; each line is at inner depth with four-space file indent on projection.
- Task-list lines (`- [ ]` / `- [x]`) import as `md-list` with checkbox text preserved; projection round-trips the checkbox in node text.
- `^name-token` attaches `name` to the line's node; invalid or duplicate suffixes in one file are flagged.
- Skipped heading levels are flagged on import and normalized to active depth + 1.
- Cross-file embed in file A unchanged when file B is edited elsewhere.
- Ref-only children project as embed-only plain lines at the correct depth.
- `[[…]]` wikilinks remain plain text; only `![[…]]` creates Ref edges.
- `#tag` lines and inline tags remain plain text; `#project` is not parsed as a level-1 heading.
- Horizontal rules (`---`, `***`, `___`) import as plain lines with full text preserved.
- Unsupported constructs produce explicit conversion diagnostics.
