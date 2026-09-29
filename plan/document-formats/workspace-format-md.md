# Workspace `.md` Text Format

Status: Target design
Authority: Target design for markdown workspace files. Obsidian markdown structure is assumed; only Gambol-specific mapping is defined here.
See also: [[doc/roadmap/workspace-text-outline-conversion.md]], [[doc/roadmap/reference-expressions.md]]

Import/export workflow and the generic conversion contract live in [[doc/roadmap/workspace-text-outline-conversion.md]].

Lists, sentences, and tables in this file are the same rules as [03 — Parse and write markdown lists, sentences, and tables](plan/document-formats/issues/03-parse-and-write-markdown-lists-sentences-and-tables.md).

## Export

Export is **operations-driven**, not whole-cloth. Each graph op projects only the file lines it affects onto the prior artifact: `file_next = f_out(file_prev, op)` ([[doc/roadmap/workspace-text-outline-conversion.md]]). Unchanged regions of `file_prev` — including blank lines, line endings, and lines for untouched nodes — are not rewritten.

Format rules below define how a **single node** projects to one file line when an op requires it. They do not imply regenerating the full file from the subtree.

## Line breaks

Node text has no embedded newline. Import splits the file on line breaks. A plain paragraph, a heading body, a list body, or a numbered body yields the first sentence as that Node and each later sentence as a child. A pipe line yields one Node. Any other substantive file line yields one Node.

**Blank lines** are not imported and are not operated on; they remain in `file_prev` until an external edit removes them. Line endings on touched lines follow the ending style of the line replaced in `file_prev`.

## Hierarchy

Depth comes from heading level, file order, and list indent. A heading body is not stored as one Node with the following lines. Sentence children sit under the first sentence. Table children sit under the first pipe line.

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
- **Plain line** — first sentence at depth = active heading + 1. Later sentences are children. See **Sentences**. Consecutive plain lines are siblings, not joined onto the heading. Tag-only lines and horizontal rules stay one Node with the full line text and no structural class.

```markdown
---
***
___
```

- **List item** — a marker is `- ` or `* ` or digits, a period, and a space. The space is required. `1)` is not a numbered marker. `1.item` is not a numbered marker. Depth = active heading + 1 + indent steps. One indent step is one tab or two spaces. `- ` is `md-list` and writes `- `. `* ` is `md-list-star` and writes `* `. The numbered marker is `md-number` and writes `N. ` where `N` is the 1-based index among sibling `md-number` Nodes. A lone `9. item` writes `1. item`. Export indent is two spaces per step. `[ ]` and `[x]` stay in the Node text. They are not a separate state.

```markdown
1. item
12. item
```

```markdown
1)
1.item
```

```markdown
* item
- item
```

```markdown
*item
-item
```

```markdown
9. item
```

- **Blockquote** — `>` after indent. Same depth rule as list items. Lines such as `> - item` are `md-quote`, not `md-list`; the `-` stays in node text.
- **Fenced code block** — opening/closing lines are `` ``` `` after indent; lines between are **fenced inner** lines. All are `md-code-block`. Opener and closer sit at the same depth as a plain line would (active heading depth + 1 + indent steps). **Fenced inner lines are depth + 1** under the opener. Opener node text is the optional language tag after `` ``` ``; inner lines keep full text; closer node text is empty. While inside a fence, normal line-kind rules and embed parsing do not apply. Unclosed fence at EOF is flagged.
- **Indented code block** — one or more consecutive lines with ≥4 leading spaces (outside a `` ``` `` fence). Each line is `md-code-block` with **no opener or closer**. Depth = active heading depth + 1 + one per 4-space indent level (same +1 inner offset as fenced inner). Node text is the line after stripping leading spaces. Normal line-kind rules do not apply within the run. On projection, indented lines emit four leading spaces per indent level; fenced inner lines emit no extra indent.
- **Blank line** — no node; not imported; not projected on export. Ignored for parentage; may separate siblings in the file only.

**Line-kind precedence** (outside a `` ``` `` fence): `` ``` `` opener/closer (after indent); then ≥4 leading spaces (indented code); then Obsidian tag-only line (`#tag`); then ATX heading; then blockquote (`>`); then list (`- ` / `* ` / digits, period, space); then a file line that starts with `|`.

When an op projects a node to a line, line kind comes from structural `cssClasses` (see **Metadata**): `md-head` → ATX heading; `md-quote` → blockquote; `md-code-block` → fence line; `md-list` / `md-list-star` / `md-number` → list item; `md-table` → the Node text as the file line; neither → plain line.

## Sentences

A paragraph is a flush-left file line that is not a heading, a list, a numbered list, or a pipe line. A paragraph is also the body of a heading, a list Node, or a numbered Node. A sentence ends at `.` or `?` or `!` when a space or the end of the line follows. The first sentence is the paragraph Node text, and that text includes the closing mark. Each later sentence is a child Node. An unterminated tail is a child. An empty tail is not a child. A sentence child has no structural class. A period that is not followed by a space or the end of the line is not a break.

```markdown
One. Two. Three
```

Parent text is `One.` Children are `Two.` and `Three`.

```markdown
One. Two
```

Parent text is `One.` The child text is `Two`.

```markdown
- One. Two.
```

```markdown
1. One. Two.
```

The marker class stays on the first sentence. `Two.` is a child with no structural class.

```markdown
# One. Two.
```

`md-head` stays on `One.` `Two.` is a child with no structural class.

```markdown
Read this after AGENTS.md.
```

This stays one Node. The period inside `AGENTS.md` is not followed by a space.

```markdown
| a | Hello. World. |
```

This stays one Node. The class is `md-table`. Pipe text is not sentence-split.

On write, take the parent's direct children that are childless and have no structural class, in order. Stop at the first child that has a structural class or that has children of its own. Join the taken texts onto the parent file line with one space. A plain child that has children is its own plain file line.

```markdown
One. Two.
```

That file is the write of parent `One.` and a childless child `Two.`

## Tables

A table file line starts with `|`. Trailing whitespace does not block that test. Strip trailing whitespace from the Node text. Keep the pipes and the rest of the line. The class is `md-table`.

Contiguous pipe lines are one table. A blank file line ends the table. The next pipe line starts a new table. The first pipe line is the parent. Each later contiguous pipe line is a child of that first line, one level deeper. Those later children are siblings.

The first pipe line has depth active heading + 2. It sits under a Node at depth active heading + 1. If no Node at that depth is the open parent, invent a carrier Node there. The carrier text is empty. The carrier has no structural class. Write emits no file line for the carrier. When a sentence child already occupies depth active heading + 1, that child is the open parent. Do not invent a second carrier.

```markdown
| first | second | third |
|-----|------|-----|
|first | second | third |
```

With no heading, the carrier is depth 1, the header is depth 2, and the later pipe lines are depth 3. Header text is the first line. The children are the separator line and the body line, in that order. Write emits the three pipe lines and no carrier line.

```markdown
# H
| a |
|---|
```

The carrier is a child of the heading. The header is a child of the carrier.

```markdown
# H
Para.
| a |
```

The header is a child of `Para.` There is no carrier.

```markdown
One. Two.
| a |
```

The header is a child of `One.` `Two` is a child of `One.` They are siblings. There is no carrier.

```markdown
# One. Two.
| a |
```

`Two.` is a child of the heading. The header is a child of `Two.` Write emits the heading and `Two.` as separate file lines, then the pipe line, because `Two.` has a child:

```markdown
# One.
Two.
| a |
```

```markdown
| a |

| b |
```

A blank file line makes two tables. Cold write puts one blank file line between them.

```markdown
| a |   
```

The Node text is `| a |`. Trailing spaces are removed.

`md-table` writes the Node text as the file line, with no added indent. Pipe lines of one table stay contiguous. A run of `md-list`, `md-list-star`, and `md-number` gets one blank file line before the first Node of the run, and no blank file line between those Nodes.

## Identity

No `amb:` HTML comments. Follow [[doc/roadmap/workspace-text-outline-conversion.md]]: stable `NodeId` is authoritative in the graph; the durable readable anchor in the file is `#name` on the node's `name` field. Ambit ensures `name` is unique among reference targets within a file.

**A non-blank file line creates one or more Nodes.** Sentence children are the extra Nodes from that line. A pipe line stays one Node.

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
| `- ` after indent | `md-list` | Text after the marker and the required space (includes `[ ]` / `[x]` in the text) |
| `* ` after indent | `md-list-star` | Text after the marker and the required space |
| Digits, period, space after indent | `md-number` | Text after the marker and the required space |
| File line starts with `\|` | `md-table` | Full line, pipes kept, trailing whitespace removed |
| Sentence after the first | none | That sentence, including a tail with no closing mark |
| `` ``` `` opener after indent | `md-code-block` | Optional language tag after `` ``` `` |
| `` ``` `` closer after indent | `md-code-block` | Empty |
| Line inside open `` ``` `` fence | `md-code-block` (fenced inner) | Full line text (minus block-id suffix) |
| ≥4 leading spaces (outside fence) | `md-code-block` (indented) | Text after stripping leading spaces |
| Neither | neither class | Full line text (minus block-id suffix) |

Import strips line-kind prefixes into the class entry and drops them from node text (fenced inner code lines are verbatim). When an op projects a line, read the class to choose line kind; prefix is re-emitted, not stored in text. `md-quote` → `>`; `md-code-block` opener → `` ``` `` plus language tag when non-empty; `md-code-block` closer → `` ``` ``; fenced inner → verbatim text; indented → four spaces per indent level + text; `md-list` → `- `; `md-list-star` → `* `; `md-number` → `N. ` where `N` is the 1-based index among sibling `md-number` Nodes (a source number that differs is written as that index); `md-table` → the Node text with no added indent. A sentence child has no structural class. A table carrier has empty text and no structural class. List, quote, and fence opener/closer indent is two spaces per step (depth beyond the base depth under the active heading).

For arbitrary class syntax in the on-disk artifact, use `.amb` ([[plan/document-formats/workspace-format-amb.md]]).

## Text to outline / outline to text

Each rule pairs with its counterpart in [[doc/roadmap/workspace-text-outline-conversion.md]] **Content Conversion**. Export column rules apply when an op projects that node to its file line.

| Import | Export (per op) |
|--------|-----------------|
| Heading at depth D → node + `md-head`; pop stack to D | `md-head` at depth D → `#` × D line |
| Heading skips a level when nesting deeper → flag; use active + 1 | Writes consecutive heading levels on that line |
| Plain line under heading D → first sentence at D + 1; later sentences are children; no structural class | Childless sentence children join onto the parent line with one space |
| `` ``` `` opener → node at D + 1 + indent + `md-code-block` | `md-code-block` opener → two-space indent + `` ``` `` line |
| Fenced inner line → opener depth + 1 + `md-code-block` | Fenced inner → verbatim line (no extra indent) |
| `` ``` `` closer → opener depth + `md-code-block` | `md-code-block` closer → two-space indent + `` ``` `` line |
| ≥4 spaces (outside fence) → `md-code-block` at inner depth | Indented → four spaces per level + text |
| `>` blockquote → node at D + 1 + indent + `md-quote` | `md-quote` → two-space indent + `>` line |
| `- ` list → node at D + 1 + indent + `md-list` | `md-list` → two-space indent + `- ` line |
| `* ` list → node at D + 1 + indent + `md-list-star` | `md-list-star` → two-space indent + `* ` line |
| Digits, period, space → node at D + 1 + indent + `md-number` | `md-number` → two-space indent + `N. ` line, `N` = sibling index |
| `1)` or a marker with no space → plain paragraph, not a list | Writes as a plain line |
| Pipe line → `md-table`; first line at active heading + 2; later lines are children | `md-table` → the Node text; no carrier line; blank line between tables |
| No parent at active heading + 1 → empty carrier at + 1 | Carrier writes no file line |
| Tab list indent → same depth as equivalent spaces | Project list indent as spaces |
| Sole-embed plain line → ref-only child + Ref edge | Ref-only child → `![[…]]` embed-only plain line |
| `![[…]]` embed in text → Ref edge; `#name` → `NodeId` | Ref edge → `![[…]]` in line text |
| `[[…]]` wikilink in text → plain text only | `[[…]]` stays in node text; not a Ref |
| Trailing `^name` → strip suffix; set `name` on line's node | Named node → ` ^name` suffix on its line (any kind) |
| No `^name` | Unnamed node → no block-id suffix |
| Reconciled node → keep user `cssClasses`; set structural class from line | Never write any `cssClasses` to the file |

## Ordinary text

These constructs are not reported and not dropped. Each file line is ordinary text. Plain-line and sentence rules apply. There is no conversion diagnostic.

Setext heading:

```markdown
Heading
=======
```

ATX heading deeper than six hashes:

```markdown
####### deep
```

Footnote:

```markdown
[^1]: note
```

HTML:

```markdown
<div>hi</div>
```

Front matter:

```markdown
---
title: note
---
```

None of these lines is a heading. A line with more than one sentence still splits under **Sentences**.

## Verification Targets

- An op that does not touch a file leaves `file_prev` bytes unchanged (including blank lines and line endings).
- A node-projection op writes one line per format rules; omits all `cssClasses` from the file.
- Import of an unchanged file preserves user classes on reconciled nodes.
- `md-head`, `md-quote`, `md-code-block`, `md-list`, `md-list-star`, `md-number`, and `md-table` project to the correct line kind, marker, and depth.
- The fenced files in **Sentences** and **Tables** read and cold-write back to the same texts, classes, and parents.
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
- The fenced files in **Ordinary text** stay in the Graph as plain lines or sentence children. They are not dropped and they raise no diagnostic.
