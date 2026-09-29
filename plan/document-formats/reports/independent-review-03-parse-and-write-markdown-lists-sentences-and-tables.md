# Independent review — Ticket 03 — Parse and write markdown lists, sentences, and tables

- Date: 2026-09-29
- PR: [Parse and write markdown lists, sentences, and tables](https://github.com/abaljeu/ambit/pull/178)
- Tip: `a265cb1b` on `cursor/md-lists-sentences-tables-6374`. Confirmed.
- Range: `128a97d6...a265cb1b`. `128a97d6` is the chart tip and the PR base (`cursor/md-parse-write-chart-c407`). `origin/staging` is `88436e6a`, an ancestor of that chart. The PR diff is the one commit `a265cb1b`.
- Command: `git diff 128a97d6...HEAD`
- Spec: [Ticket 03 — Parse and write markdown lists, sentences, and tables](plan/document-formats/issues/03-parse-and-write-markdown-lists-sentences-and-tables.md) and [Markdown parse and write design](plan/document-formats/arch-md-parse-write.md).

This report is not approval. [Ticket 03 — Parse and write markdown lists, sentences, and tables](plan/document-formats/issues/03-parse-and-write-markdown-lists-sentences-and-tables.md) stays `coded`.

## Standards

### 1. MdDocument.fs crosses the file limit (hard)

[.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) says "800 lines or less per file" and "If a file is already longer, only restructure to split up the code if your changes would increase it." [MdDocument.fs](src/Shared/documents/MdDocument.fs) goes from 630 lines to 924. The same rule says the split is its own later commit. The scan line is a finding.

### 2. writeWarmImpl stays over 40 lines and grows (hard)

The same rule says "40 lines or less per function." `writeWarmImpl` was 99 lines at the chart tip (lines 482–580) and is 102 lines now (lines 773–874). The warm-write edits sit in that binding. `kindOfEdit` now returns `parentKind`. `emitStep` passes it to `needsPreBlank`. The measurer prints new `let` bindings only, so it did not print this function. Every printed function in the scan is under 40 lines. Those lines are not findings.

### 3. New parameter lists are not a named type (hard)

The same rule says "Group related function parameters into a named, reused type" and "When adding a parameter that belongs with existing ones, extend that type instead of lengthening the argument list." `classifyContent` was `(activeHeading, content)`. It is now the cursor clump plus content:

```fsharp
let private classifyContent
    (active: int)
    (afterBlank: bool)
    (openTable: int option)
    (content: string)
```

`sentenceSpanLines` takes nine arguments: `expand`, `fileIndex`, `spanned`, `depth`, `span`, `body`, `kind`, `nodeId`.

### 4. Serialize and sentence reuse scan the same children again (hard)

The same rule says a per-item scan of one list is quadratic, and says to update an index once. `serializeLines` calls `numberAmong` for every `NumberItem`. That function filters the parent's children and searches again:

```fsharp
let private numberAmong (graph: Graph) (parentId: NodeId) (nodeId: NodeId) =
    GraphChildren.get graph parentId
    |> List.filter (fun child -> kindOfNode graph child.id = NumberItem)
    |> List.tryFindIndex (fun child -> child.id = nodeId)
```

`addSentenceTails` calls `tryReusePlain` once per sentence tail. That `List.tryFind` walks the parent's children again. Cold read (`parseCold` then `foldOutline`) and warm read (`rebuildFromAligned` then `foldOutline`) both do this. `serializeLines` feeds `writeFresh` and `writeWarmImpl`. Persist reaches those through `MdReconcile.handler` write, then `writeArtifact` or `writeArtifactWarm`.

### 5. Sentence split and the outline fold are copied (smell)

[SMELLS.md](.agents/skills/code-review/SMELLS.md) Duplicated Code is a judgement call. [DocumentFormat.fs](src/Shared/documents/DocumentFormat.fs) inlines the same split as `MdReconcile.sentenceSpanLines`: parent text at `depth`, each tail at `depth + 1`. `pushNode` and `foldOutline` also repeat the `DocumentOutlineOps.foldRowsIntoTree` steps (`popStack`, `prependChild`, clear `childMap` with `Map.map` over `contextGraph.nodes`) after `parseCold` and `rebuildFromAligned` stop calling that helper.

## Spec

### 1. Warm read binds a file line to the wrong Node id (wrong)

`mapPreviousLines` zips `parseOutlineLines` to `serializeLines` by index. It does not apply `isSubstantive`. `previousOutlineIds` returns that zip. `MdReconcile.rebuildUnchanged` sends it to `rebuildFromAligned` when the file flatten matches. `prevIds` has one entry per file line, so the length check passes while `serializeLines` still holds the empty carrier and an unjoined sentence child.

A cold read of `| a |` then `| b |` keeps the carrier id on warm read of the same file. The header id changes.

A cold read of `# One. Two.` then `| a |` keeps the heading id. The pipe line is bound to the sentence child's id. That Node's text becomes `| a |`.

[Markdown parse and write design](plan/document-formats/arch-md-parse-write.md) says "When an empty child with no structural class already sits at the carrier depth under that parent, the fold reuses that child id. Otherwise the fold mints a new id." It says "Warm reconcile reuses Node ids when the flatten rows match. Sentence children hang on the reused file-line node." It says "Sentence children and the empty carrier stay out of these rows."

`writeFresh` and `writeWarmImpl` do filter `isSubstantive`, so the empty carrier has no write row. The bad zip is the warm read path. No fact reconciles a table. No fact reconciles a sentence tail that owns a table.

### 2. Heading and list sentence spans cover the wrong characters (wrong)

`sentenceSpanLines` sets the child `TextSpan` to `contentStart + offset`. `contentStart` is the start of the raw file line. `offset` comes from `splitLineSentences` on the marker-stripped body. On `# One. Two.` the child text is `Two.` and the span slice is `. Tw`. The same shift hits `- `, `* `, and `N. `.

The fact `parse span of two sentences yields two span nodes` uses the plain line `One. Two.`, where the body is the whole line.

[Markdown parse and write design](plan/document-formats/arch-md-parse-write.md) says "Later sentences are child span nodes on that line's span." [Ticket 03 — Parse and write markdown lists, sentences, and tables](plan/document-formats/issues/03-parse-and-write-markdown-lists-sentences-and-tables.md) says "A paragraph is also the body of a heading, a `md-list` Node, or a `md-number` Node."

### 3. A following paragraph joins onto a sentence-final heading (wrong)

`plainTails` joins a childless plain child only while the joined text already ends with `.` `?` or `!`. The spec names two other stops: a structural class, or a child that has children. Cold write of `# Title.` plus a newline plus `Paragraph` returns `# Title. Paragraph`. The next read builds the same parent text and the same child text. The paragraph is no longer its own file line.

[Ticket 03 — Parse and write markdown lists, sentences, and tables](plan/document-formats/issues/03-parse-and-write-markdown-lists-sentences-and-tables.md) says "A paragraph is a flush-left file line that is not a heading, a list, a numbered list, or a pipe line." The join examples are tails of one file line, such as `One. Two.`. The code comment on `plainTails` says a following paragraph under a heading stays its own file line. The `endsSentence` guard does not keep that line when the heading already ends with `.`.

## Verdict

**Needs work.**

Standards: 5 findings (4 hard, 1 smell). Worst issue: [MdDocument.fs](src/Shared/documents/MdDocument.fs) is 924 lines.
Spec: 3 findings. Worst issue: warm read binds a table header, and a pipe line under a sentence tail, to the wrong Node id.
