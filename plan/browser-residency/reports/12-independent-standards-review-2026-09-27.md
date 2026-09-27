# 12 — Contract old Load Fetch packages independent Standards review

Range: `origin/staging...HEAD`. Axis: Standards only.

## 1. Documented-standard violations

1. **Leftover Load Fetch package helper (must-fix)** — [core-agent-behavior](.agents/rules/core-agent-behavior.md) Simplicity First: "Don't replicate code — put shared logic in a reusable place and call it." "Consider removing code to achieve a goal." Honor [CONTEXT.md](CONTEXT.md) **Fetch**: Load Fetch uses the current edges-plus-Nodes answer. Production [Api](src/Server/Api.fs) `loadWantAnswer` and [ResidentProjection](src/Shared/ResidentProjection.fs) `captureLoadResponse` call `wantAnswerForTargets`. [ResidentProjection](src/Shared/ResidentProjection.fs) still exports `packagesForTarget`, which returns an owning-Workspace Node list through `workspaceSubgraphNodes`. That helper has no `src/` caller. [LoadCaptureTests](tests/Shared.Tests/LoadCaptureTests.fs) still asserts it. This is the leftover dual-run package path under `src/` and `tests/`. Mechanical scan sizes (17 and 14 lines) do not break the 40-line limit in [fsharp-source](.agents/rules/fsharp-source.md).

2. **Retired Revision wording in edited names (nice-to-have)** — [CONTEXT.md](CONTEXT.md) **event id**: avoid **Revision**. [ClientHistoryRuntimeTests](tests/Shared.Tests/ClientHistoryRuntimeTests.fs) titles change `package-only` to `answer-only` and still say `Revision`. [Update.fs](src/Client/Update.fs) comment still says "advance revision".

## 2. Smells (judgment)

1. **Speculative Generality / Mysterious Name** — public `packagesForTarget` keeps the deleted package API name with no production caller.

```
    let packagesForTarget
        (graph: Graph)
        (targetId: NodeId)
        (includeWorkspace: bool)
        : Node list =
        if not includeWorkspace then
            []
        elif not (Map.containsKey targetId graph.nodes) then
            []
        else
            match GraphQuery.enclosingWorkspace graph targetId with
            | None -> []
            | Some wsId -> workspaceSubgraphNodes graph wsId
```

## 3. Outcome

2 documented-standard findings (1 must-fix, 1 nice-to-have) and 1 smell.
