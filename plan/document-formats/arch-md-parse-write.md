# Markdown parse and write design

Ticket: [03 — Parse and write markdown lists, sentences, and tables](issues/03-parse-and-write-markdown-lists-sentences-and-tables.md)
Format: [Workspace .md text format](workspace-format-md.md)
Updated: 2026-09-29
Sequence: module-build

This file is the design for that ticket only. It is not the architecture of the whole Document formats project.

The Graph record of a markdown shape is the structural css class on the Node. A private line kind inside [MdDocument](src/Shared/documents/MdDocument.fs) may mirror that class. The class is the source of truth on write.

## 1. Story paths

1. **Cold read builds the Graph** — A person opens a markdown File Node that has not been read yet. Parse reads the file bytes into Nodes.
   1. [ ] Flatten — `MdDocument.flattenText` emits one row per Node. A sentence child is its own row. A pipe line is one row. The empty table carrier is not a flatten row.
   2. [ ] Tree — `MdDocument.read` folds those rows by depth into Owned children. Numbered and star lists use depth active heading + 1 + indent steps. The first pipe line is at active heading + 2. When no Node at active heading + 1 is the open parent, the fold inserts the empty carrier at that depth.
   3. [ ] Classes — The fold sets `md-list`, `md-list-star`, `md-number`, or `md-table` from the file line. Sentence children and the carrier get no structural class.
   4. [ ] Spans — [MdReconcile](src/Shared/documents/MdReconcile.fs) builds a span tree from the same flatten rows. Every Node that came from one file line shares that line's span. Blank lines after the file line still extend the last Node of that line. The span path does not zip one row to one file line by index.
2. **Warm write keeps bytes when the person edits** — The person changes the file or the Graph. Write sends the Graph back through `MdDocument.writeWarm` and the LCS in `OutlineDocumentWarm`.
   1. [ ] Same rows on both sides — The LCS diffs flatten rows of the previous file against flatten rows of the Graph. Both sides include sentence children. Neither side includes the carrier.
   2. [ ] File lines are joined — Emission joins a childless plain child that has no structural class onto its parent line with one space, and emits no second line for that child. A plain child that has children is its own file line.
   3. [ ] Markers come from the class — `md-list` writes `- `. `md-list-star` writes `* `. `md-number` writes `N. ` with `N` the 1-based index among sibling `md-number` Nodes. `md-table` writes the Node text with no added indent.
   4. [ ] Unchanged lines keep raw bytes — When the formatted line equals the previous substantive text, write keeps that line's raw bytes, its ending, and the blank lines absorbed after it. A list-run blank is inserted only on a new list run, not on a kept line.
   5. [ ] Carrier id — The carrier is not an LCS row. When the table is unchanged, reuse the empty child that already sits at the carrier depth under that parent. Otherwise mint a new id.
3. **Repersist is outside this ticket** — A Graph edit of these Nodes marks the nearest owning special node Unpersisted. That special node is the File Node for a markdown file. Ancestors are not marked. Persist is the Core async stack, not an Actor. Persist of that File Node waits while the node is Unparsed. This ticket does not set those axes and does not run Load. The dependency is [Here→There](plan/github-transport/here-to-there.md) and [19 — Parsed/Unparsed and Persisted/Unpersisted](plan/github-transport/issues/19-file-newer-graph-newer.md).

Shared segments:

1. [ ] Flatten rows — Cold read, span build, and warm write all use `MdDocument.flattenText`.
2. [ ] Class on the Node — Read sets it. Write reads it. The file does not store the class.

Test seam: [MdDocumentTests](tests/Shared.Tests/MdDocumentTests.fs) calls `MdDocument.read` and `MdDocument.writeWarm`. One MdReconcile fact calls parse on a two-sentence line. Persist and Load are not in the seam.

## 2. Module map

1. **MdDocument** — [MdDocument](src/Shared/documents/MdDocument.fs)
   1. State
      1. [ ] Structural classes in the set are `md-head`, `md-list`, `md-list-star`, `md-number`, and `md-table`.
      2. [ ] Flatten rows are depth, text, and class. They omit the empty carrier.
   2. Interface
      1. [ ] `read` returns the Graph for the fenced files in the format spec, including the carrier Node.
      2. [ ] `writeWarm` returns file text whose read matches that Graph, and keeps raw bytes on an unchanged line.
   3. Uses
      1. [ ] `DocumentOutlineOps` for line split and the depth fold.
      2. [ ] `OutlineDocumentWarm` for the LCS plan.
2. **MdReconcile** — [MdReconcile](src/Shared/documents/MdReconcile.fs)
   1. State
      1. [ ] Each span Node points at the file line it came from, not at a one-to-one flat index.
   2. Interface
      1. [ ] Parse of a two-sentence line returns two span Nodes and does not throw.
      2. [ ] Warm reconcile still reuses Node ids when the flatten rows match.
   3. Uses
      1. [ ] `MdDocument.flattenText` and `MdDocument.read`.
3. **Md door** — [DocumentFormat](src/Shared/documents/DocumentFormat.fs)
   1. State
      1. [ ] The Md codec has no second flatten.
   2. Interface
      1. [ ] Cold parse and artifact write call MdDocument only.
   3. Uses
      1. [ ] `MdDocument.flattenText`, `MdDocument.read`, and `MdDocument.writeArtifact`.

## 3. Seams

1. [ ] Flatten seam — Interface on **MdDocument**. Tests and MdReconcile cross it.
2. [ ] Warm emission seam — Interface on **MdDocument** `writeWarm`. Tests cross it with `OutlineLcs.diffTexts`.
3. [ ] Persist seam — Not this ticket. Core calls write only after Unparsed is clear. The Server git Actor may Save while the File Node is Unparsed or Unpersisted.

## 4. Alternative considered

1. **One Node per file line** — Sentence breaks would stay inside the Node text. That keeps today's one-row span zip. Alan locked a child Node per later sentence, so the span zip changes instead.
2. **Carrier as an LCS row** — An empty carrier row would make parse longer than the file-backed serialize, and warm rebuild would fail the length check. The carrier stays out of the LCS.

## 5. Unsettled

1. None. Per-sentence character offsets are not a second span. Nodes from one file line share that line's span.
