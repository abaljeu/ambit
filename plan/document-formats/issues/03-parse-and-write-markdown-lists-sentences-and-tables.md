# 03 — Parse and write markdown lists, sentences, and tables

**Status:** done
**Type:** coding
**Actual:** 4h10m
**Blocked by:** None — can start immediately.

## Context

A person keeps a markdown document on a File Node. Parse reads that file into the Graph. Write sends those Nodes back to the file. Today [MdDocument](src/Shared/documents/MdDocument.fs) makes one Node from one file line. A heading is `md-head`. A list marker is only `- `, and the class is `md-list`. Any other file line is plain text at depth active heading + 1. There is no numbered list, no `* ` list, no sentence split, and no table.

[MdReconcile](src/Shared/documents/MdReconcile.fs) builds spans from `MdDocument.flattenText`. It takes one flat entry for each non-blank file line. Sentence split makes more than one Node from one file line. That span path must stay aligned. [DocumentFormat](src/Shared/documents/DocumentFormat.fs) also nests `MdDocument.flattenText` for the Md codec. The extra Nodes must flow through that nest.

[MdDocumentTests](tests/Shared.Tests/MdDocumentTests.fs) is the proof file. The file line `Read this after AGENTS.md.` stays one Node. The period inside `AGENTS.md` is not followed by a space.

The agreed rules are [Workspace .md text format](plan/document-formats/workspace-format-md.md). How cold read, warm write, and Persist bytes stay correct is [Markdown parse and write design](plan/document-formats/arch-md-parse-write.md). Persist file-format processing in that design is the Graph to artifact bytes path. `DocumentPersistWrite.writeDocumentCore` already calls `DocumentWarm.writeArtifact`. The write amendments are `MdDocument.kindOfNode`, `MdDocument.serializeLines`, `MdDocument.formatLine`, and `MdDocument.needsPreBlank`. This ticket implements that file for lists, sentences, tables, and ordinary text. Active heading means the depth of the current heading Node. It is 0 when no heading is open.

Alan locked these rules on 2026-09-29.

## What to build

Parse and write the four shapes below. A cold write followed by a read returns the same Node texts, the same structural css classes, and the same parents and children. Today's ATX heading rules, blank-line omission, and list indent steps stay.

### 1. Numbered lists

A numbered marker is one or more digits, then a period, then one space. `1)` is not a numbered marker. `1.item` is not a numbered marker. The marker may sit after the same leading whitespace as today's `- ` lists. Depth is active heading + 1 + indent steps. Indent steps stay one per tab, plus one per two spaces. Node text is the body after that one space. The structural class is `md-number`.

1. [ ] Digits period space — `1. item` and `12. item` become `md-number` Nodes. The text is the body after the marker.
2. [ ] Reject close-paren — `1)` stays a plain paragraph. It is not `md-number`.
3. [ ] Same depth as dash lists — under a heading, a flush numbered Node sits at the same depth as a flush `- ` Node. Extra indent steps add the same depth as for `- `.

### 2. Unordered lists

An unordered marker is `*` or `-`, and only when a space follows. Depth uses the same formula as numbered lists. `- ` keeps class `md-list`. Write emits `- `. `* ` uses class `md-list-star`. Write emits `* `. The classes stay distinct so write can emit the marker that was read.

1. [ ] Star marker — `* item` is `md-list-star`. The text is `item`.
2. [ ] Dash marker — `- item` stays `md-list`. The text is `item`.
3. [ ] Space required — `*item`, `-item`, `***`, and `---` are not list Nodes.

### 3. Sentences

A paragraph is a flush-left file line that is not a heading, a list, a numbered list, or a pipe line. A paragraph is also the body of a heading, a `md-list` Node, or a `md-number` Node. A sentence ends at `.` or `?` or `!` when a space or the end of the line follows. The first sentence is the paragraph Node text, and that text includes the final `.` `?` or `!`. Each later sentence is a child Node. An unterminated tail is also a child. `One. Two` gives parent `One.` and child `Two`. Do not add a child for an empty tail. A sentence child has no structural class. A period that is not followed by a space or the end of the line is not a break.

On write, join only a prefix of direct children. Take childless children that have no structural class, in order. Stop at the first child that has a structural class or that has children of its own. Join the taken texts onto the parent file line with one space. A plain child that has children is its own plain file line.

1. [ ] Plain split — `One. Two. Three` becomes parent `One.` with children `Two.` and `Three`.
2. [ ] List and number bodies — `- One. Two.` and `1. One. Two.` keep the marker class on the first sentence. `Two.` is a plain child.
3. [ ] Heading body — `# One. Two.` keeps `md-head` on `One.`. `Two.` is a plain child.
4. [ ] No split in tables — a pipe line stays one Node even when the text contains `Hello. World.`
5. [ ] Write joins childless tails — parent `One.` with a childless plain child `Two.` writes one file line whose body is `One. Two.`

### 4. Tables

A table file line starts with `|`. Trailing whitespace does not block that test. Strip trailing whitespace from the Node text. Keep the pipes and the rest of the line. The class is `md-table`. Do not sentence-split the text.

Contiguous pipe lines are one table. A blank file line ends the table. The next pipe line starts a new table. The first pipe line is the parent Node. Each later contiguous pipe line is a child of that first line, one level deeper. Those later children are siblings of each other.

The first pipe line has depth active heading + 2. It sits under a Node at depth active heading + 1. If no Node at that depth is already the open parent, invent a carrier Node there. The carrier text is empty. The carrier has no structural class. Write emits no file line for that empty carrier.

When a sentence child already occupies depth active heading + 1, that child is the open parent. The first pipe line is its child. Do not invent a second carrier.

1. [ ] Three-line table — the file below yields an empty carrier, a header Node, and two children under the header.

```
| first | second | third |
|-----|------|-----|
|first | second | third |
```

2. [ ] Depth with no heading — the carrier is depth 1, the header is depth 2, and the later pipe lines are depth 3. Header text is `| first | second | third |`. The children are `|-----|------|-----|` and `|first | second | third |`, in that order.
3. [ ] Heading and no paragraph — `# H` then a pipe table makes the carrier a child of the heading. The header is a child of the carrier.
4. [ ] Paragraph parent — `# H` then `Para.` then a pipe table makes the header a child of `Para.`. There is no carrier.
5. [ ] Sentence siblings — the file below makes the header a child of `One.` `Two` is also a child of `One.` There is no carrier.

```markdown
One. Two.
| a |
```
6. [ ] Blank line splits tables — two pipe groups with a blank file line between them become two tables.
7. [ ] Trailing spaces — `| a |   ` has Node text `| a |`.

### 5. Write round-trip

Cold write, then read, keeps texts, structural classes, and parent and child shape.

A numbered Node writes `N. ` where `N` is the 1-based index of that Node among sibling `md-number` Nodes. A lone source line `9. item` writes `1. item`.

`md-list` writes `- `. `md-list-star` writes `* `. Indent is two spaces per indent step, as for lists today. A run of `md-list`, `md-list-star`, and `md-number` gets one blank file line before the first Node of the run, and no blank file line between those Nodes. That matches today's list run.

`md-table` writes the Node text as the file line, with no added indent. Pipe lines of one table stay contiguous. When one table's last emitted line is followed by another table's header, write one blank file line between them so the next read starts a new table.

Headings still write `#` repeated to the heading depth, then a space, then the joined text. Sentence tails follow the join rule in Sentences.

1. [ ] Marker round-trip — read, cold write, and read again keep `md-list`, `md-list-star`, and `md-number`. The written file uses `- `, `* `, and `1. ` `2. ` in sibling order.
2. [ ] Table round-trip — the three-line example writes the three pipe lines and no carrier line. Read again rebuilds the carrier and the same three texts.
3. [ ] Two tables — cold write puts one blank file line between them. Read again builds two tables.
4. [ ] Sentence round-trip — `One. Two.` writes as one file line and reads back to parent `One.` and child `Two`.
5. [ ] Tail that owns a table — the read file and the write file are the same pair as in [Workspace .md text format](plan/document-formats/workspace-format-md.md) **Tables**.

```markdown
# One. Two.
| a |
```

```markdown
# One.
Two.
| a |
```

### 6. Tests

Add facts in [MdDocumentTests](tests/Shared.Tests/MdDocumentTests.fs) for each acceptance check under Numbered lists, Unordered lists, Sentences, Tables, Write round-trip, and Ordinary text. Keep the existing facts green. Add one span fact on the Md reconcile path: file line `One. Two.` yields two span Nodes and does not throw.

1. [ ] New facts — cover every acceptance check under Numbered lists, Unordered lists, Sentences, Tables, Write round-trip, and Ordinary text.
2. [ ] Existing facts — [MdDocumentTests](tests/Shared.Tests/MdDocumentTests.fs) facts that already pass still pass, including `Read this after AGENTS.md.` as one Node.
3. [ ] Span fact — `One. Two.` yields two span Nodes.

### 7. Ordinary text

Same rule as **Ordinary text** in [Workspace .md text format](plan/document-formats/workspace-format-md.md). These constructs are not reported and not dropped. Each file line is ordinary text.

```markdown
Heading
=======
```

```markdown
####### deep
```

```markdown
[^1]: note
```

```markdown
<div>hi</div>
```

```markdown
---
title: note
---
```

1. [ ] Keep the lines — each fenced file above becomes plain Nodes, or a sentence parent plus sentence children. None of the lines is a heading. No diagnostic is raised.

## Non-goals

- Blockquote, fenced code, indented code, embeds, and block-ids. Those stay described in the format file and are not this ticket.
- A checkbox state. `[ ]` and `[x]` stay in the Node text.
- A `+ ` list marker.
- Sentence split inside a pipe line.
- A structural css class on sentence children or on the carrier.

## See also

[Document formats](plan/document-formats/map.md), [Markdown parse and write design](plan/document-formats/arch-md-parse-write.md), [Workspace .md text format](plan/document-formats/workspace-format-md.md), [Markdown codec](plan/roadmap/epics/chapters/markdown-codec.md), [MdDocument](src/Shared/documents/MdDocument.fs), [MdReconcile](src/Shared/documents/MdReconcile.fs), [DocumentWarm](src/Shared/documents/DocumentWarm.fs), [DocumentPersistWrite](src/Server/DocumentPersistWrite.fs), [MdDocumentTests](tests/Shared.Tests/MdDocumentTests.fs)

## Comments

- 2026-09-29 — Alan locked sentence scope, unterminated tails, table depth and the carrier, and parse plus write. Structural classes in this ticket are `md-list`, `md-list-star`, `md-number`, and `md-table`. Sentence children and the carrier stay plain.
- 2026-09-29 — Alan accepted; squash-landed. Status `done`.

## Time

- 2026-09-29 1h — chart the ticket from Alan locks (from chat)
- 2026-09-29 1h — trace Persist bytes and amend the design (from chat)
- 2026-09-29 2h — parse and write lists, sentences, and tables (from chat)
- 2026-09-29 10m — Alan accepted; Status `done` before squash-land (from chat)
