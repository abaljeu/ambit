## Standards

**Hard.** [planning-docs.md](.agents/rules/planning-docs.md) — Plan text records what is implemented by ticket, section, or Point, not git branch names. Do not discuss `ready` as delivery status in plan docs. [20 — State axes on special nodes](plan/github-transport/issues/20-state-axes-on-special-nodes.md) Comments:

```
- 2026-09-28: Merged `origin/ready` (`fb83e3ce`). Ready added git Load after-step / directory-match helpers and `LazyLoadReconciliation.currentDiscoveredAsModified` (reads `DocumentState` Current only). No new DocumentState write sites. Dual-write stays on `Op.SetDocumentState` apply.
```

**Hard.** [fsharp-source.md](.agents/rules/fsharp-source.md) — Group related function parameters into a named, reused type. When adding a parameter that belongs with existing ones, extend that type instead of lengthening the argument list. `runLoad` / `runOperation` in [GithubTransportActor.fs](src/Server/GithubTransportActor.fs) added `dataDir` and `focusId`. `GithubTransportActorDependencies` already exists; `productionDependencies` already takes `dataDir`. `focusId` is already on `ActorInput` in `runBody`.

```
    let private runLoad
        dependencies
        dataDir
        changes
        focusId
        label
        workspaceRoot
```

`runOperation` is the same list plus `operation` (7 parameters).

**Judgement (Duplicated Code).** [setParseState](src/Shared/GraphMutate.fs) and [setPersistState](src/Shared/GraphMutate.fs) copy the same six-arm match as each other (node missing, Workspaces, Normal, old-state mismatch, no-op, touch-and-add). Only the field, type, and error string change.

```
        match graph.nodes |> Map.tryFind nodeId with
        | None -> Error "node not found"
        | Some node when node.kind = Special Workspaces ->
            Error "workspaces is not a graph document"
        | Some node when node.kind = Normal ->
            Error "normal nodes do not have parse state"
        | Some node when node.parseState <> oldState ->
            Error "old parse state does not match"
        | Some node when oldState = newState ->
            Ok graph
        | Some node ->
            let updated = NodeUpdateTime.touch { node with parseState = newState }
            Ok { graph with nodes = graph.nodes |> Map.add nodeId updated }
```

`setPersistState` repeats that ladder with `persistState`.

**Judgement (Mysterious Name).** `held` in [matchDiscoveredDirectory](src/Server/LazyLoadReconciliationServer.fs) does not say these are discovered paths already Current, remapped as Modified.

```
                    let held =
                        LazyLoadReconciliation.currentDiscoveredAsModified
                            stateResponse.graph
                            workspaceLabel
                            discovered
```
