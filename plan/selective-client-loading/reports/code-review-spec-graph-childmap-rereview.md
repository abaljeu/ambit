# Spec rereview: Graph.childMap overlay

Range: `git diff c76009ee...origin/cursor/graph-childmap-ea38` (`06b8e2fb` Walk document overlay members on the read childMap). Findings only. Spec: [Selective client loading spec](plan/selective-client-loading/spec.md). Prior must-fix: [Spec review: Graph.childMap](code-review-spec-graph-childmap.md) finding 1.

## Findings

No spec findings.

## Not findings

[mergeReadResult](src/Shared/documents/DocumentFormat.fs) sets `graphWithRead.childMap = readResult.childMap` before [memberNodeIds](src/Shared/DocumentPartition.fs). Overlay ids follow the read Children. [readArtifact cold Amb overlays nested outline nodes](tests/Shared.Tests/DocumentAssemblyTests.fs) installs the nested parent and child Nodes. User story 9 and Testing Decisions (an unloaded child list is never an authoritative empty list) hold for that overlay walk.

Absent read key uses `Map.remove`. [AmbDocument.read](src/Shared/documents/AmbDocument.fs) and [foldRowsIntoTree](src/Shared/documents/DocumentOutlineOps.fs) seed `[]` for every context Node, then prepend. A Loaded-empty leaf that already exists in context keeps a present key in the read, so overlay does `Map.add []`, not `Map.remove`. `Map.remove` does not unload that leaf.

New leaves minted in the parse can lack a `childMap` key. Overlay `Map.remove` is then a no-op. Those Nodes stay Unloaded after a complete document read. [Snapshot.read](src/Shared/Snapshot.fs) fills `[]` for every present Node; document read does not. That gap is residual from `c76009ee`, not a regression of this follow-up. The nested outline test does not assert Loaded on the new leaf.

Claimed should-fixes are present and match the spec contract: Graph.replace Unloaded test (`"parent children not loaded"`); LoadResponse Unloaded codec (package Node id absent from `packageChildMap`); Implementation Decision now names `Graph.childMap` (absent key Unloaded; present key including `[]` Loaded); line wraps in [Api.fs](src/Server/Api.fs) and [SerializationTests.fs](tests/Shared.Tests/SerializationTests.fs). [14 — Simplify selective client loading](plan/selective-client-loading/issues/14-simplify-selective-loading.md) still says `Node.children` and `childrenStatus`; that grill is historical. [spec.md](plan/selective-client-loading/spec.md) is current. No scope creep in the follow-up diff.
