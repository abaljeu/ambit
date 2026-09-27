# Standards findings for [14 — Route Load and Save by pre-pick](plan/github-transport/issues/14-route-load-save-by-pre-pick.md)

Range: `git diff e8a46fff^...HEAD`. Mechanical scan printed ten bindings; none exceed the 40-line or 100-character limits in [fsharp-source.md](.agents/rules/fsharp-source.md). No file in the range exceeds 800 lines.

## Hard violations

1. **Orphan `completeUploadInventory`.** [core-agent-behavior.md](.agents/rules/core-agent-behavior.md) Surgical Changes: remove functions that this change made unused. [UpdateWorkspaceSync.fs](src/Client/UpdateWorkspaceSync.fs) adds `completeUploadInventory` as a wrapper of `completeUploadInventoryWith`. [DeskLoadSaveEffectClient.fs](src/Client/DeskLoadSaveEffectClient.fs) calls `completeUploadInventoryWith` only. No other caller exists. Delete the wrapper.

## Documented-standard judgment calls

1. **`Dependencies` mixes unrelated fields.** [fsharp-source.md](.agents/rules/fsharp-source.md): do not invent a mega-record of unrelated values; split by cohesion. [DeskLoadSaveEffectClient.fs](src/Client/DeskLoadSaveEffectClient.fs) `Dependencies` holds workspace inventory (`encodeWorkspaceInventory`, `decodeWorkspaceInventory`), workspace push (`prepareWorkspacePush`), and desk save (`postEmpty`, `fileName`) together. Split by operation.

## Smells (judgment)

1. **Duplicated Code** in [DeskLoadSaveEffectClient.fs](src/Client/DeskLoadSaveEffectClient.fs) `runWorkspaceStubsThenPushWith` and `runWorkspacePushWith`:

```
(fun status text ->
    dispatch (
        ApplyOp (
            failWorkspacePushHttp status text)))
(fun () ->
    dispatch (
        ApplyOp (
            failWorkspacePush
                "workspace-inventory request failed")))
```

The push binding repeats the same callbacks with `"workspace-push request failed"`.

2. **Middle Man** in [DeskLoadSaveEffectClient.fs](src/Client/DeskLoadSaveEffectClient.fs):

```
let runDeskSaveWith dependencies =
    UpdateSave.runDeskSaveWith
        dependencies.postEmpty
        dependencies.fileName
```

3. **Speculative Generality** — the unused `completeUploadInventory` wrapper in [UpdateWorkspaceSync.fs](src/Client/UpdateWorkspaceSync.fs) (same hunk as hard violation 1).
