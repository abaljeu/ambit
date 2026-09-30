# Markdown parse and write design

Ticket: [03 — Parse and write markdown lists, sentences, and tables](issues/03-parse-and-write-markdown-lists-sentences-and-tables.md)
Format: [Workspace .md text format](workspace-format-md.md)
Updated: 2026-09-29
Sequence: module-build

This file is the design for that ticket only. It is not the architecture of the whole Document formats project.

The Graph record of a markdown shape is the structural css class on the Node. A private line kind inside [MdDocument](src/Shared/documents/MdDocument.fs) may mirror that class. The class is the source of truth on write.

Persist of a markdown File Node already turns the Graph into artifact bytes through [DocumentWarm](src/Shared/documents/DocumentWarm.fs) and [MdDocument](src/Shared/documents/MdDocument.fs). This ticket changes the MdDocument write functions on that path.

## 1. Story paths

1. **Cold read builds the Graph** — A person opens a markdown File Node that has not been read yet. Parse reads the file bytes into Nodes.
   1. [ ] Flatten — `MdDocument.flattenText` emits one row per substantive file line. `parseOutlineLines` is that same grain. A pipe line is one row. Sentence children and the empty table carrier stay out of these rows.
   2. [ ] Tree — `MdDocument.read` folds those rows by depth into Owned children, then splits sentences. The first sentence stays on the file-line Node. Each later sentence is a child Node. Numbered and star lists use depth active heading + 1 + indent steps. The first pipe line is at active heading + 2. When no Node at active heading + 1 is the open parent, the fold inserts the empty carrier at that depth.
   3. [ ] Classes — The fold sets `md-list`, `md-list-star`, `md-number`, or `md-table` from the file line. Sentence children and the carrier get no structural class.
   4. [ ] Spans — [MdReconcile](src/Shared/documents/MdReconcile.fs) `toSpanTree` indexes one flatten row to one substantive file line. Later sentences are child span nodes on that line's span. Blank lines after the file line still extend that span.
   5. [ ] Carrier id — The carrier is absent from flatten rows. `MdDocument.read` and `MdDocument.rebuildFromAligned` insert it. When an empty child with no structural class already sits at the carrier depth under that parent, the fold reuses that child id. Otherwise the fold mints a new id.
2. **Warm write keeps bytes when the person edits** — Write sends the Graph through `MdDocument.writeWarm` and the LCS in `OutlineDocumentWarm`.
   1. [ ] File-line rows — `writeWarmImpl` diffs one row per substantive file line. The previous side is `parseOutlineLines` of `previousText` inside `buildPrevEntities`. The graph side is `serializeLines` after the sentence join, then `isSubstantive`. Sentence children and the empty carrier stay out of these rows.
   2. [ ] File lines are joined — `serializeLines` joins a prefix of direct children onto the parent line. Take childless children that have no structural class, in order. Stop at the first child that has a structural class or that has children of its own. Join the taken texts with one space. Those children have no row of their own. A plain child that has children is its own file line.
   3. [ ] Markers come from the class — `formatLine` reads the class. `md-head` writes `#` repeated to the heading depth, then a space, then the joined text. `md-list` writes `- `. `md-list-star` writes `* `. `md-number` writes `N. ` with `N` the 1-based index among sibling `md-number` Nodes. `md-table` writes the Node text with no added indent. List indent is two spaces per indent step.
   4. [ ] Unchanged lines keep raw bytes — `writeWarmImpl` EmitKeep keeps the previous line's raw bytes, its ending, and the blank lines absorbed after it when the formatted line equals the previous substantive text. A list-run blank is inserted on EmitInsert. A kept line stays as stored.
   5. [ ] List run and table gap — `needsPreBlank` treats `md-list`, `md-list-star`, and `md-number` as one run: one blank file line before the run, and no blank file line inside the run. It inserts one blank file line before an `md-table` whose parent is not `md-table` when the previous emitted line is `md-table`. Pipe lines under an `md-table` parent stay contiguous.

Shared segments:

1. [ ] File-line flatten — Cold read and span build use `MdDocument.flattenText`. Warm write uses `serializeLines` and `parseOutlineLines` at that same grain. Sentence split is the tree fold. Sentence join is `serializeLines`.
2. [ ] Class on the Node — Read sets it. Write reads it. The file does not store the class.

Test seam: [MdDocumentTests](tests/Shared.Tests/MdDocumentTests.fs) calls `MdDocument.read`, `MdDocument.write`, and `MdDocument.writeWarm`. One MdReconcile fact calls parse on a two-sentence line and expects two span nodes. Those tests cover the bytes Persist writes. Cold persist bytes are `write`. Warm persist bytes are `writeWarm`. The tests leave `DocumentPersistWrite` uncalled.

## 2. Persist file-format processing

This is Graph to artifact bytes when Persist runs. The section names the functions that run, then the functions this ticket changes.

`DocumentPersistWrite.writeDocument` and `DocumentPersistWrite.writeDocumentsSoft` call `writeDocumentCore`.

1. **previousText** — `previousFileText` reads the artifact file. A file on disk gives `Some` text. A missing file gives `None`. An empty file gives `Some ""`.
2. **Router** — `writeDocumentCore` calls `DocumentWarm.writeArtifact` with `OutlineLcs.diffTexts`, the Graph, the File Node id, the relative path, and `previousText`.
3. **Cold** — `previousText` is `None`. `DocumentWarm.writeArtifact` calls `DocumentFormat.writeArtifact`. For a `.md` path, `classifyCodecForWrite` selects Md. The Md `coldHandlerFor` write hook is `MdDocument.writeArtifact` with `None`. That calls `MdDocument.write`, then `writeFresh`. `stableUpdateFailed` stays false.
4. **Warm** — `previousText` is `Some`. `DocumentWarm.writeWarm` calls `MdReconcile.handler` write. That calls `MdDocument.writeArtifactWarm`, then `MdDocument.writeWarm`, then `writeWarmImpl`.
5. **Warm failure** — An Error or an exception from that warm call makes `DocumentWarm.writeArtifact` call `DocumentFormat.writeArtifact` with `None`. `stableUpdateFailed` becomes true. `persistArtifactText` writes the cold bytes. The ack message is `stableFileUpdateFailed`.
6. **Same bytes** — When the artifact text equals `previousText`, `persistArtifactText` leaves the file unchanged.
7. **MdReconcile on this path** — Persist calls `MdReconcile.handler` write. `toSpanTree`, `readWarm`, and `reconcile` stay on warm read.
8. **Complement** — `write` and `writeWarm` ignore `MdComplement`. Markers come from `Node.cssClasses` on the Graph.

### Validated, no code change

These functions already do the job once `serializeLines`, `formatLine`, `kindOfNode`, and `needsPreBlank` emit the Ticket 03 lines.

- `DocumentPersistWrite.writeDocumentCore`, `previousFileText`, and `persistArtifactText`
- `DocumentWarm.writeArtifact` and its cold fallback
- `DocumentFormat.writeArtifact` and the Md `coldHandlerFor` write hook
- `MdReconcile.handler` write (`None` calls `writeArtifact`, `Some` calls `writeArtifactWarm`)
- `MdDocument.write`, `writeWarm`, `writeArtifact`, and `writeArtifactWarm` as the dispatch
- `MdDocument.isSubstantive` drops the empty carrier after `serializeLines` has walked its children. The carrier text is empty, so the carrier has no file line. The table lines still emit.
- `writeWarmImpl` EmitKeep keeps raw bytes when the formatted line equals the previous substantive text
- `OutlineDocumentWarm.writePlan` and `executeWritePlan`

`DocumentWarm.writeArtifact` is the persist caller. The cold branch passes `None` into `MdDocument.write`. The warm branch calls `MdDocument.writeWarm`. `MdDocument.write` still returns an error when `previousText` is `Some`. That error stays the guard for a direct warm call.

### Amendments inside MdDocument

Today `kindOfNode` treats `md-head` and `md-list` as structural. `md-list-star`, `md-number`, and `md-table` take the plain path. `formatLine` writes `- ` for every list. `serializeLines` emits one row per Owned child and leaves sentence tails as their own rows. `needsPreBlank` starts a list run only from `md-list`. A warm write of the new shapes would emit the wrong markers and would split `One. Two.` into two file lines. That warm call would still return Ok, so the cold fallback would keep those bytes.

1. [ ] `kindOfNode` — `md-list-star`, `md-number`, and `md-table` select list or table emission. `md-head` and `md-list` stay as they are. A sentence child and the carrier stay plain.
2. [ ] `structuralNames` — The set is `md-head`, `md-list`, `md-list-star`, `md-number`, and `md-table`. Read merge keeps that set. Write reads the class from the Node.
3. [ ] `serializeLines` — Apply the sentence join before `writeFresh` and `writeWarmImpl` build rows. Use the list depth formula for `md-list`, `md-list-star`, and `md-number`. Walk children of the empty carrier.
4. [ ] `formatLine` — Emit the markers and the table line in story path 2.3. `N` is the sibling index among `md-number` Nodes.
5. [ ] `needsPreBlank` — Apply the list run and the table gap in story path 2.5. `writeFresh` and warm EmitInsert both call it.

`parseOutlineLines` stays one row per substantive file line. Sentence rows there would make `buildPrevEntities` bind one raw file line to the wrong outline row. EmitKeep would then keep or replace the wrong bytes.

## 3. Module map

1. **MdDocument** — [MdDocument](src/Shared/documents/MdDocument.fs)
   1. State
      1. [ ] Structural classes are `md-head`, `md-list`, `md-list-star`, `md-number`, and `md-table`. `structuralNames` holds that set.
      2. [ ] Flatten rows and write rows are one substantive file line. Sentence children and the empty carrier stay out of those rows.
   2. Interface
      1. [ ] `read` returns the Graph for the fenced files in the format spec, including sentence children and the carrier Node.
      2. [ ] `write` returns cold file text whose read matches that Graph.
      3. [ ] `writeWarm` returns file text whose read matches that Graph, and keeps raw bytes on an unchanged line.
      4. [ ] `kindOfNode`, `serializeLines`, `formatLine`, and `needsPreBlank` implement the amendments in Persist file-format processing.
   3. Uses
      1. [ ] `DocumentOutlineOps` for line split and the depth fold.
      2. [ ] `OutlineDocumentWarm` for the LCS plan.
2. **MdReconcile** — [MdReconcile](src/Shared/documents/MdReconcile.fs)
   1. State
      1. [ ] A file-line span covers that line. Sentence child spans share it.
   2. Interface
      1. [ ] Parse of a two-sentence line returns two span nodes and does not throw.
      2. [ ] Warm reconcile reuses Node ids when the flatten rows match. Sentence children hang on the reused file-line node.
      3. [ ] `handler` write stays the persist door. `None` calls `MdDocument.writeArtifact`. `Some` calls `MdDocument.writeArtifactWarm`.
   3. Uses
      1. [ ] `MdDocument.flattenText`, `MdDocument.read`, `MdDocument.writeArtifact`, and `MdDocument.writeArtifactWarm`.
3. **Md door** — [DocumentFormat](src/Shared/documents/DocumentFormat.fs)
   1. State
      1. [ ] The Md codec has no second flatten.
   2. Interface
      1. [ ] Cold parse calls `MdDocument.read`.
      2. [ ] Cold artifact write calls `MdDocument.writeArtifact` with `None`.
   3. Uses
      1. [ ] `MdDocument.flattenText`, `MdDocument.read`, and `MdDocument.writeArtifact`.
4. **Persist router** — [DocumentWarm](src/Shared/documents/DocumentWarm.fs)
   1. Interface
      1. [ ] `writeArtifact` sends `None` to `DocumentFormat.writeArtifact` and `Some` to the codec handler write. This ticket leaves that function as it is.
   2. Uses
      1. [ ] `DocumentFormat.writeArtifact` and `MdReconcile.handler`.
5. **Persist entry** — [DocumentPersistWrite](src/Server/DocumentPersistWrite.fs)
   1. Interface
      1. [ ] `writeDocumentCore` loads `previousText` and calls `DocumentWarm.writeArtifact`. This ticket leaves that function as it is.
      2. [ ] `persistArtifactText` writes the bytes. Equal text leaves the file unchanged.

## 4. Seams

1. [ ] Flatten seam — Interface on **MdDocument** `flattenText`. Tests and MdReconcile cross it. The grain is one substantive file line.
2. [ ] Warm emission seam — Interface on **MdDocument** `writeWarm`. Tests cross it with `OutlineLcs.diffTexts`.
3. [ ] Cold emission seam — Interface on **MdDocument** `write`. Tests cross it with `previousText` `None`.
4. [ ] Persist seam — `DocumentWarm.writeArtifact`. Production Persist crosses it. Tests stay on the warm emission seam and the cold emission seam.

## 5. Alternative considered

1. **One Node per file line** — Sentence breaks would stay inside the Node text. That keeps one span node per file line. Alan locked a child Node per later sentence, so the span tree gains child nodes on that line's span.
2. **Carrier as an LCS row** — An empty carrier row would make parse longer than the file-backed serialize, and warm rebuild would fail the length check. `isSubstantive` keeps the carrier out of the write rows.
3. **A second persist writer** — `DocumentPersistWrite` would format markdown itself. `writeDocumentCore` already calls `DocumentWarm.writeArtifact`, and that call already reaches `MdDocument.write` and `MdDocument.writeWarm`.
4. **Sentence children as write LCS rows** — `buildPrevEntities` pairs outline index `i` with substantive file line `i`. EmitKeep keeps that file line's raw bytes. Extra sentence rows would attach those bytes to the wrong row. `serializeLines` joins the tails, and the existing LCS stays.

## 6. Unsettled

1. None. Persist bytes use the file-line row. The file stores no Node id. Sentence child ids stay on the read fold under the reused file-line Node.
