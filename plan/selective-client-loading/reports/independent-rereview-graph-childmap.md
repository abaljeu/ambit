# Independent rereview — Graph.childMap overlay

Reviewer did not write the implementation. This report is not approval. No ticket Status changes.

**Range:** follow-up `c76009ee...origin/cursor/graph-childmap-ea38` at `06b8e2fb` Walk document overlay members on the read childMap. Full PR vs `origin/staging` is two commits. Command: `git diff c76009ee...origin/cursor/graph-childmap-ea38`. Diff is non-empty (6 files, +97 / −8).

**Prior review:** [independent review — Graph.childMap](independent-review-graph-childmap.md). Must-fix was [mergeReadResult](src/Shared/documents/DocumentFormat.fs) walking the context `childMap`.

**Spec:** [Selective client loading spec](plan/selective-client-loading/spec.md) Implementation Decision now names `Graph.childMap`.

**Axis reports:** [Standards rereview](code-review-standards-graph-childmap-rereview.md), [Spec rereview](code-review-spec-graph-childmap-rereview.md).

Mechanical scan at `06b8e2fb` (`python3 .agents/skills/code-review/scripts/standards-scan.py --diff c76009ee`): none.

## Standards

### 1. mergeReadResult exceeds 40 lines (hard)

[.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) requires 40 lines or less per function. [mergeReadResult](src/Shared/documents/DocumentFormat.fs) is lines 125–184. This follow-up grew it from 58 to 60 when it wrapped `graphWithRead` and assigned `readResult.childMap`. The binding was already over the limit in `c76009ee`. Prior 100-character findings in [Api.fs](src/Server/Api.fs) and [SerializationTests.fs](tests/Shared.Tests/SerializationTests.fs) do not remain.

## Spec

No spec findings.

Overlay now sets `graphWithRead.childMap = readResult.childMap` before [memberNodeIds](src/Shared/DocumentPartition.fs). Absent read keys use `Map.remove`. [AmbDocument.read](src/Shared/documents/AmbDocument.fs) seeds `[]` for every context Node, so a Loaded-empty leaf already in context stays Loaded. New parse-minted leaves can stay Unloaded; that is residual from `c76009ee`, not this follow-up.

## Independent checks

Must-fix is fixed. `dotnet test` in a worktree at `06b8e2fb`:

- [readArtifact cold Amb ignores previous when None](tests/Shared.Tests/DocumentAssemblyTests.fs) — pass (was `KeyNotFoundException`)
- [readArtifact cold Amb overlays nested outline nodes](tests/Shared.Tests/DocumentAssemblyTests.fs) — pass
- [Graph.replace rejects Unloaded parent](tests/Shared.Tests/ModelTests.fs) — pass
- [LoadResponse round-trip keeps Unloaded package header absent](tests/Shared.Tests/SerializationTests.fs) — pass
- [planParseFile md reorder updates child order](tests/Shared.Tests/ImportDocumentTests.fs) and DocumentColdParseTests — pass

Claimed should-fixes are present: Unloaded replace test, Unloaded LoadResponse codec, [spec.md](plan/selective-client-loading/spec.md) childMap wording, line wraps.

## Verdict

**Good.**

No new must-fix. The prior overlay must-fix is closed. Standards: 1 finding (worst: [mergeReadResult](src/Shared/documents/DocumentFormat.fs) still over 40 lines). Spec: 0 findings.
