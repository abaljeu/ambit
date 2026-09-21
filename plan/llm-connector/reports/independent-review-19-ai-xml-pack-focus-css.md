# Independent review — 19 — AI extract pack is XML with Focus cssClass

Range: `origin/staging...HEAD` at tip `61fb021bafe46c963e5dbc79b905aa2e641c70b0` (after css rename focus→prompt and systemPrompt alignment). Mechanical scan: `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` (`python` was not on PATH). Scan printed binding sizes only; no LONG / TAB / MUTABLE / BARE_ID / FILE hits. `writePresent` is 26 lines. Spec: [19 — AI extract pack is XML with Focus cssClass](plan/llm-connector/issues/19-ai-xml-pack-focus-css.md) plus [llm-connector architecture](plan/llm-connector/arch.md) / [llm-connector map](plan/llm-connector/map.md) notes. Implementer self-review is not authority. Status stays `coded`.

## Standards

Mechanical scan: no LONG / TAB / MUTABLE / BARE_ID / FILE hits. `writePresent` is 26 lines (under 40).

### Documented standards (hard)

- [llm-connector spec](plan/llm-connector/spec.md) story 4 (edited) still uses `[[issues/08-agent-ask-from-what-i-see.md|08]]` and `[[issues/06-define-command-run-agent-redesign.md|06]]`. That breaks [.agents/rules/markdown-writing.md](.agents/rules/markdown-writing.md) (no Obsidian labeled wikilinks; use `[label](path)`) and [.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md) (display text is the id only). The same sentence already uses correct `[19 — …](…)` / `[11 — …](…)` links.
- [llm-connector map](plan/llm-connector/map.md) Implementation item for [18 — AI Actor stream](plan/llm-connector/issues/18-ai-actor-stream.md): `Blocked by 17.` Breaks [.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md) (number without the name).
- Unrelated hunks in this three-dot range break [.agents/rules/core-agent-behavior.md](.agents/rules/core-agent-behavior.md) Surgical Changes (“Touch only what you must”; every line traces to the request):
  - [.agents/skills/code-review/SKILL.md](.agents/skills/code-review/SKILL.md) — “Markdown lists.”
  - [src/Server/appsettings.Development.json](src/Server/appsettings.Development.json) — fills `AiKeys` `ApiKey` (credential; not quoted).
  - [plan/core-creation/reports/changed-files-directional-links.md](plan/core-creation/reports/changed-files-directional-links.md) — live-Actor chrome graph, not the XML pack.

No F# hits vs [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) (line/fn/file, no `mutable`, `Result` not exceptions, `AiExtractPack.write` matches `AmbDocument.write`). [.agents/rules/core-api.md](.agents/rules/core-api.md) and [.agents/rules/no-retrofit.md](.agents/rules/no-retrofit.md) are clean. [19 — AI extract pack is XML with Focus cssClass](plan/llm-connector/issues/19-ai-xml-pack-focus-css.md) names issues by title.

### Baseline smells (judgement)

- **Duplicated Code** (suppressed by [19 — AI extract pack is XML with Focus cssClass](plan/llm-connector/issues/19-ai-xml-pack-focus-css.md) §1.3–1.5: Server write-only walk, no Shared parse): `writePresent` repeats `AmbDocument.serializeExtractLines`’s present-id / cycle-`path` / `node.children` walk.

```
let rec private writePresent
    (graph: Graph)
    (path: Set<NodeId>)
    (nodeId: NodeId)
```

- **Middle Man**: [src/Server/RunAgentActor.fs](src/Server/RunAgentActor.fs) `packExtract` only forwards three `ActorInput` fields.

```
let private packExtract (input: ActorInput) =
    AiExtractPack.packExtract
        input.graph
        input.zoomId
        input.focusId
```

- **Speculative Generality**: public `AiExtractPack.write` has no caller except `packExtract`; tests use `packExtract` / `markFocus`.

## Spec

Checked [19 — AI extract pack is XML with Focus cssClass](plan/llm-connector/issues/19-ai-xml-pack-focus-css.md) vs `origin/staging...HEAD`. `Graph.focus` / `focusId` / `withFocus` unchanged. No Fable.SimpleXml. [AmbDocument.fs](src/Shared/documents/AmbDocument.fs), [AmbExtractWalkTests.fs](tests/Shared.Tests/AmbExtractWalkTests.fs), [FocusChildrenReplace.fs](src/Shared/documents/FocusChildrenReplace.fs) not in the diff. [AiExtractPack.fs](src/Server/AiExtractPack.fs) walk matches `SuppliedExtract` (Owned+Ref via `child.id`, omit missing, cycle `path`, no `DocumentPartition` / file write). `systemPrompt` is XML / class `prompt`, not mixed Amb.

### (a) Missing or partial

- None on the locked pack/prompt/proofs. Nested-File / no-partition is in the walk code; XML tests only cover Ref + missing id, not a File boundary (ticket 1.5: “Walk the same as `AmbWriteWalk.SuppliedExtract` … no owning-document partition”).

### (b) Scope creep

- [.agents/skills/code-review/SKILL.md](.agents/skills/code-review/SKILL.md) (“Markdown lists.”) is not in What to build / Non-goals.
- [changed-files-directional-links.md](plan/core-creation/reports/changed-files-directional-links.md) is unrelated Core chrome mapping.
- [src/Server/appsettings.Development.json](src/Server/appsettings.Development.json) fills `AiKeys` `ApiKey` (secret in the diff; not quoted). Ticket: “Persist nothing”; Non-goals do not include credentials.
- [code-review-ai-xml-pack-focus-css.md](code-review-ai-xml-pack-focus-css.md) is extra and still says class `focus`, against “Focus mark css class token is `prompt` (not `focus`).”

### (c) Looks implemented but wrong

- None. `packExtract` does `withFocus` then merge `prompt` on the copy; XML is `System.Xml.Linq` `<node>` / `class`; original `cssClasses` stay; reply still `FocusChildrenReplace.plan`.

## Summary

Standards 5 hard + 3 judgement (worst: Development `AiKeys` `ApiKey` in the three-dot range). Spec 1 partial + 4 scope-creep (worst: same `appsettings.Development.json` credential).
