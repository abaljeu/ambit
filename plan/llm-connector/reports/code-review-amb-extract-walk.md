# Code review: Amb extract-walk

Independent two-axis review of [11 — Pack extract with Amb (supplied-fragment walk)](plan/llm-connector/issues/11-simple-extract-format.md). Range: three-dot `origin/staging...HEAD` (merge-base `35df2319`; tip `85673a5b`). Spec: that ticket plus [llm-connector architecture](plan/llm-connector/arch.md) module **Document (Amb pack + Reference Paste)** and Locked items **Focus on extract Graph** and **First pack is Amb extract-walk**. Subject: `AmbDocument` extract-walk (`AmbWriteWalk.SuppliedExtract` / `writeWith`), `Graph.focus` / `withFocus`, GraphBuild/serialization omit, [AmbExtractWalkTests.fs](tests/Shared.Tests/AmbExtractWalkTests.fs), [GraphFocusTests.fs](tests/Shared.Tests/GraphFocusTests.fs). Plan-file replan noise is secondary unless it contradicts the ticket. Axis drafts: [Standards axis](code-review-standards-amb-extract-walk.md), [Spec axis](code-review-spec-amb-extract-walk.md). Mechanical scan: `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging`. Focused Shared tests: 9 passed (`AmbExtractWalkTests`, `GraphFocusTests`). **Status:** stays `coded`. A report is not approval.

## Verdict

**Approve with nits.** Ticket What-to-build items are present. Extract walk recurses Owned and Ref into present Nodes, omits missing ids, does not persist, does not stop at File/document bounds, and does not call owning-document partition. `Graph.withFocus` sets `Graph.focus`; `encodeGraph` and History/projection omit it; Amb text has no Focus sentinel. Parse is unchanged. Worst Standards hit on subject code is `serializeExtractLines` at 42 lines (limit 40). Worst Spec hit is extract line form for nested File/Directory Nodes: default Amb owner-line (`^` stable id plus Filename) is dropped for a unique File such as `note.txt`, so the pack keeps `file body` and `inside-file` and loses Filename. Ticket text requires reuse of `AmbDocument` and a different walk, not an explicit owner-line at File children; confirm that grammar before [08 — Agent ask from what I see](plan/llm-connector/issues/08-agent-ask-from-what-i-see.md) consumes the pack. File-size growth of [AmbDocument.fs](src/Shared/documents/AmbDocument.fs) (644→737) is a documented hit; the same rule defers split to a later standalone commit. Plan BARE_ID / Destination wording is secondary.

## Standards

Range: three-dot `origin/staging...HEAD` (`85673a5b`). Subject: `AmbDocument` extract-walk, `Graph.focus` / `withFocus`. Plan-file replan noise is secondary except documented-standard hits.

### Hard violations

#### [AmbDocument.fs](src/Shared/documents/AmbDocument.fs)

[fsharp-source.md](.agents/rules/fsharp-source.md): 800 lines or less per file; when this occurs, split into three pieces each under 400; if a file is already longer, split when the change increases it. Scan: FILE 644→737, already over 400, change increased it.

Same rule: 40 lines or less per function. `serializeExtractLines` is lines 229–270 (42). Nested `writePresent` counts in that binding.

#### [11 — Pack extract with Amb (supplied-fragment walk)](plan/llm-connector/issues/11-simple-extract-format.md)

[refer-by-name.md](.agents/rules/refer-by-name.md): never refer by only the id or number; always include the name. Comments line 40: `GitHub PRs #53 and #54 close without land.` Time line 46: `replan ticket 11 to Amb extract-walk`.

[markdown-writing.md](.agents/rules/markdown-writing.md): labeled links must be `[label](path)`; do not write Obsidian `[[path|label]]`. See also is four labeled wikilinks.

Secondary (same range, not in the mechanical scan): [project.md](plan/llm-connector/project.md) `blocked by 11`; [map.md](plan/llm-connector/map.md) labels `|09]` and `|10]` with no names.

### Judgement-call smells

**Duplicated Code.** `extractNodeLine` repeats the owning-document owner-vs-plain choice in `serializeLines` `writeChild`:

```
if isShared || Set.contains nodeId refTargets || ambiguousPlain then
    ownerLineContent nodeId node body
else
    plain
```

**Mysterious Name.** Durable `Graph.focus` collides with command/selection `focusId` across Shared:

```
/// Ephemeral extract-pack Focus. JSON and History omit this field.
focus: NodeId option
```

**Shotgun Surgery.** Ephemeral extract Focus edits [Model.fs](src/Shared/Model.fs), three [GraphBuild.fs](src/Shared/GraphBuild.fs) constructors (`focus = None` on `fromNodes` / `fromExtracted`; `focus = graph.focus` on `appendChildren`), and [GraphOps.fs](src/Shared/GraphOps.fs) `withFocus`. JSON omit is implicit: `encodeGraph` never lists the field.

**Middle Man.** Public `serializeLinesWith` only dispatches:

```
| AmbWriteWalk.OwningDocument -> serializeLines graph documentRootId
| AmbWriteWalk.SuppliedExtract -> serializeExtractLines graph documentRootId
```

`writeWith` is the specified write option.

### No hit

No [core-api.md](.agents/rules/core-api.md), [no-retrofit.md](.agents/rules/no-retrofit.md), or [project-stage.md](.agents/rules/project-stage.md) hit. Project `Stage: build` matches first implement. Extraction of `childOccurrenceCount` and `renderLines` matches surgical edit in [core-agent-behavior.md](.agents/rules/core-agent-behavior.md). Custom extract walk is not a [GraphQuery](src/Shared/GraphQuery.fs) substitute (`ownedArtifactsInDirectory` stops at artifacts).

Counts: 6 hard, 2 secondary, 4 smells.

## Spec

Spec: [11 — Pack extract with Amb (supplied-fragment walk)](plan/llm-connector/issues/11-simple-extract-format.md); architecture module **Document (Amb pack + Reference Paste)** and Locked items **Focus on extract Graph** and **First pack is Amb extract-walk** in [llm-connector architecture](plan/llm-connector/arch.md). Range: `origin/staging...HEAD`. Subject: [AmbDocument.fs](src/Shared/documents/AmbDocument.fs) extract-walk, [Graph.focus](src/Shared/Model.fs) / [withFocus](src/Shared/GraphOps.fs), JSON/History omit, tests [AmbExtractWalkTests.fs](tests/Shared.Tests/AmbExtractWalkTests.fs) and [GraphFocusTests.fs](tests/Shared.Tests/GraphFocusTests.fs).

### (a) Missing or partial

None. Ticket 11 write, walk, omit-missing, no partition, no persist, default parse, `Graph.focus` / `withFocus`, JSON/History omit, and no Amb Focus sentinel are present. Actor consume stays on [08 — Agent ask from what I see](plan/llm-connector/issues/08-agent-ask-from-what-i-see.md).

### (b) Behaviour the spec did not ask

`AmbDocument.serializeLinesWith` is extra public surface. Ticket Comments: "Coded: `AmbWriteWalk.SuppliedExtract` on `AmbDocument.writeWith`." The ticket names `writeWith`, not a second serialize entry.

Product code does not add mixed-format owning-codec or Md extract serialize (tabled). [map.md](plan/llm-connector/map.md) Destination still says "mixed-format Graph extract, mark Focus in the outbound document," which contradicts ticket 11; that is plan text, not product behaviour.

### (c) Implemented but wrong

Nested File Node / Directory Node line form. Spec: "Reuse the Amb (Ambit `.amb` / `AmbDocument`) codec." Spec: "Do not stop at nested document or File Node boundaries." Default Amb write emits an owner-line (`^` stable id plus Filename) at a nested document boundary, then stops. Extract walk recurses (correct; `inside-file` appears) but `extractNodeLine` writes plain `node.text` unless the node is shared, a Ref target, or has ambiguous text. Nested File Node `note.txt` therefore loses stable id and Filename. The walk change is continue after the boundary, not drop Amb owner-line identity. Locked **First pack is Amb extract-walk**: "Reuse `AmbDocument`." Recurse-and-plain is a different line grammar for artifact Nodes, not only a different walk.

## Summary

Standards: 6 hard + 2 secondary + 4 smells; worst within axis: `serializeExtractLines` 42 lines (and file 644→737). Spec: 2 findings; worst within axis: nested File/Directory extract line form drops Filename / stable id. Verdict: Approve with nits. Status stays `coded`.
