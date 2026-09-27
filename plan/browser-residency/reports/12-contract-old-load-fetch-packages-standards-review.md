# 12 — Contract old Load Fetch packages — Standards review

## 1. Documented-standard violations

1. **Replicated Load-target Want answer (hard)** — [core-agent-behavior](.agents/rules/core-agent-behavior.md) Simplicity First: "Don't replicate code — put shared logic in a reusable place and call it." After [Api.fs](src/Server/Api.fs) dropped `packagesForTargets`, `loadWantAnswer` and [ResidentProjection](src/Shared/ResidentProjection.fs) `captureLoadResponse` both map `LoadTarget` ids, refuse multi-Workspace, choose `includeWorkspace` ids, and call `wantAnswer`. Mechanical scan of `loadWantAnswer` (34 lines) does not break the 40-line limit in [fsharp-source](.agents/rules/fsharp-source.md).

## 2. Smells

1. **Duplicated Code (judgement)** — Same logic shape in two hunks of the change.

```
+                let targetIds =
+                    targets |> List.map (fun target -> target.targetId)
+                if
+                    ResidentProjection.selectionSpansMultipleWorkspaces
+                        state.graph
+                        targetIds
+                then
+                    return Ok(Error ResidentProjection.LoadRefuse.MultiWorkspace)
+                else
+                    let want =
+                        targets
+                        |> List.choose (fun target ->
+                            if target.includeWorkspace then
+                                Some target.targetId
+                            else
+                                None)
+                    let childMap, nodes =
+                        ResidentProjection.wantAnswer state.graph want
```

```
+        let targetIds = targets |> List.map (fun target -> target.targetId)
+        if selectionSpansMultipleWorkspaces graph targetIds then
+            Error LoadRefuse.MultiWorkspace
+        else
+            let want =
+                targets
+                |> List.choose (fun target ->
+                    if target.includeWorkspace then Some target.targetId else None)
+            let childMap, nodes = wantAnswer graph want
```

## 3. Outcome

1 hard documented-standard violation and 1 smell, both the replicated Load-target Want answer.
