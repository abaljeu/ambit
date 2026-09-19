# Standards — Amb extract-walk

Range: three-dot `origin/staging...HEAD` (`85673a5b`). Subject: `AmbDocument` extract-walk, `Graph.focus` / `withFocus`. Plan-file replan noise is secondary except documented-standard hits.

## Hard violations

### [AmbDocument.fs](src/Shared/documents/AmbDocument.fs)

[fsharp-source.md](.agents/rules/fsharp-source.md): 800 lines or less per file; when this occurs, split into three pieces each under 400; if a file is already longer, split when the change increases it. Scan: FILE 644→737, already over 400, change increased it.

Same rule: 40 lines or less per function. `serializeExtractLines` is lines 229–270 (42). Nested `writePresent` counts in that binding.

### [11 — Pack extract with Amb (supplied-fragment walk)](plan/llm-connector/issues/11-simple-extract-format.md)

[refer-by-name.md](.agents/rules/refer-by-name.md): never refer by only the id or number; always include the name. Comments line 40: `GitHub PRs #53 and #54 close without land.` Time line 46: `replan ticket 11 to Amb extract-walk`.

[markdown-writing.md](.agents/rules/markdown-writing.md): labeled links must be `[label](path)`; do not write Obsidian `[[path|label]]`. See also is four labeled wikilinks.

Secondary (same range, not in the mechanical scan): [project.md](plan/llm-connector/project.md) `blocked by 11`; [map.md](plan/llm-connector/map.md) labels `|09]` and `|10]` with no names.

## Judgement-call smells

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

## No hit

No [core-api.md](.agents/rules/core-api.md), [no-retrofit.md](.agents/rules/no-retrofit.md), or [project-stage.md](.agents/rules/project-stage.md) hit. Project `Stage: build` matches first implement. Extraction of `childOccurrenceCount` and `renderLines` matches surgical edit in [core-agent-behavior.md](.agents/rules/core-agent-behavior.md). Custom extract walk is not a [GraphQuery](src/Shared/GraphQuery.fs) substitute (`ownedArtifactsInDirectory` stops at artifacts).

Counts: 6 hard, 2 secondary, 4 smells.
