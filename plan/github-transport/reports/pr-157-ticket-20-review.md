# Independent review of [PR 157](https://github.com/abaljeu/ambit/pull/157) — [20 — State axes on special nodes](plan/github-transport/issues/20-state-axes-on-special-nodes.md)

Range: `git diff origin/staging...HEAD` (`99d017ef` on `cursor/state-axes-on-special-nodes-889e`). Ticket-only commit: `91e51fad`. Ready-only vs staging: `git diff origin/staging...origin/ready`. Mechanical scan: no binding or line over the limits in [fsharp-source.md](.agents/rules/fsharp-source.md). Status on the ticket stays `defined`. This report is not approval.

Axis reports: [PR 157 Standards](pr-157-standards.md), [PR 157 Spec](pr-157-spec.md).

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

## Spec

### (a) Missing or partial

1. **Client Load Unparsed is not written.** Spec: “8. **Client Load on Directory** — Mark the Directory Node Unparsed (re-process).” and “9. **Client Load on File** — Mark the File Node Unparsed. Push onto the Parse stack is deferred.” [20 — State axes on special nodes](plan/github-transport/issues/20-state-axes-on-special-nodes.md) does not add those writes. The ready Load path that would land with this PR ([gitLoadAfterOp](src/Client/UpdateWorkspaceLoad.fs), [parseFocusFile](src/Server/GithubTransportActor.fs)) directory-matches and Parses. It does not mark the Directory Node or File Node Unparsed.

### (b) Scope creep

2. **Ready-only Load/reconciliation must not land with [20 — State axes on special nodes](plan/github-transport/issues/20-state-axes-on-special-nodes.md).** Spec: “1.4 Leave workers unbuilt — Do not stand up the Parse actor or stack, the Core Persist stack, git Load retarget, Upload or selection Parse, path-control migration, or retirement of old hops.” Same lock: [Here→There — step 1 locked](plan/github-transport/here-to-there.md) §5 items 3–4 and 6. This range is `origin/ready` vs `origin/staging`, not the ticket. Strip it so the PR is the ticket only. Files: [LoadSaveCommandClient.fs](src/Client/LoadSaveCommandClient.fs), [UpdateWorkspaceLoad.fs](src/Client/UpdateWorkspaceLoad.fs), [GithubTransportActor.fs](src/Server/GithubTransportActor.fs), [LazyLoadReconciliationServer.fs](src/Server/LazyLoadReconciliationServer.fs), [Want.fs](src/Shared/Want.fs), [IncludedDescendantIds.fs](src/Shared/IncludedDescendantIds.fs), [WorkspaceSyncScope.fs](src/Shared/WorkspaceSyncScope.fs), [GraphBuild.fs](src/Shared/GraphBuild.fs), [CommandRequest.fs](src/Shared/CommandRequest.fs), [LazyLoadReconciliation.fs](src/Shared/dotnet/LazyLoadReconciliation.fs), [load-missing-nodes.md](plan/github-transport/reports/load-missing-nodes.md), [load-no-upstream-diagnosis.md](plan/github-transport/reports/load-no-upstream-diagnosis.md), [browser-residency](plan/browser-residency/) `arch.md` `map.md` `project.md` `spec.md` [03 — Lock ongoing-want priority](plan/browser-residency/issues/03-lock-ongoing-want-priority.md) [07 — Expand Want and edges/nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md), [incremental-operations.md](plan/work-with-text-files-from-anywhere/epics/chapters/incremental-operations.md), and tests [LoadSaveCommandClientTests.fs](tests/Server.Tests/LoadSaveCommandClientTests.fs) [LoadSaveCommandTests.fs](tests/Server.Tests/LoadSaveCommandTests.fs) [AppGithubTransportBoundaryTests.fs](tests/Server.Tests/AppGithubTransportBoundaryTests.fs) [WantTests.fs](tests/Shared.Tests/WantTests.fs) [IncludedDescendantIdsTests.fs](tests/Shared.Tests/IncludedDescendantIdsTests.fs) [WorkspaceSyncScopeTests.fs](tests/Shared.Tests/WorkspaceSyncScopeTests.fs) [CommandRequestTests.fs](tests/Shared.Tests/CommandRequestTests.fs).

3. **That Load path stands up selection Parse.** Spec (same 1.4) and [Here→There — step 1 locked](plan/github-transport/here-to-there.md) §5.4 “Upload and selection Parse … Both stay deferred.” [GithubTransportActor.parseFocusFile](src/Server/GithubTransportActor.fs) calls `DocumentPersistWrite.planParseFile` after pull. That is selection Parse, not ticket markers.

### (c) Implemented but wrong

4. **Create special dual-write is inconsistent.** Spec: “7. **Create special** — Unparsed + Persisted.” Mikado: “Set both the new axes and the old `DocumentState` wherever state changes.” [Op.NewSpecialNode](src/Shared/History.fs) sets `parseState = Unparsed` and `persistState = Persisted` and leaves `documentState = Current` (Parsed). Old and new parse poles disagree.

5. **SQL Graph drop of the new axes.** Spec: “This ticket adds the axes as Graph state that can be set and read.” [GraphProjection](src/Shared/GraphProjection.fs) rows and `nodeEquals` omit `parseState` and `persistState`. Reload derives parse from `documentState` and persist as Persisted. Graph-edit Unpersisted does not survive persist.

Standards: 4 findings (2 hard, 2 judgement). Worst: `runLoad` / `runOperation` lengthen an argument list that already has a dependencies type. Spec: 5 findings. Worst: ready-only Load/reconciliation (including selection Parse) would land on staging with this ticket.

## Must-fix vs nice-to-have

Parent judgement. The axes above stay unmerged.

### Must-fix before this PR lands on staging

1. **Strip the ready-only Load/reconciliation delta.** Do not land `origin/staging...origin/ready` with [20 — State axes on special nodes](plan/github-transport/issues/20-state-axes-on-special-nodes.md). The ticket commit `91e51fad` does not need those files. [LazyLoadReconciliation.currentDiscoveredAsModified](src/Shared/dotnet/LazyLoadReconciliation.fs) only reads `DocumentState` Current. Dual-write already lives on `Op.SetDocumentState`. Landing the ready slice on staging also brings [GithubTransportActor.parseFocusFile](src/Server/GithubTransportActor.fs), which is selection Parse and is deferred by item 1.4. If that Load work should be on staging, give it its own PR.

2. **Create special must dual-write the old parse pole.** [Op.NewSpecialNode](src/Shared/History.fs) must set `documentState = Unparsed` (or another Unparsed pole) when it sets `parseState = Unparsed`. Today `Node.Create` defaults `documentState` to `Current`. [SpecialNodeStateTests](tests/Shared.Tests/SpecialNodeStateTests.fs) does not assert the old field.

### Nice-to-have (not Ticket 20 acceptance)

1. **Client Load Unparsed writes** (Notes 8–9). Acceptance on the ticket stays markers set and read only. This review’s acceptance list did not require those writes or a stack push.

2. **SQL / [GraphProjection](src/Shared/GraphProjection.fs) columns for the new axes.** The PR states SQL still stores `document_state` only. That matches Mikado expand-contract. `persistState` has no old column, so Unpersisted is lost on SQL reload. JSON encode/decode already round-trips both axes.

3. **Reword the ticket Comment** that names `origin/ready` as delivery status.

4. **Dedup** `setParseState` / `setPersistState`.

5. **`runLoad` parameter type** and rename `held` — both sit in the ready-only slice and go away if that slice is stripped.

## Ticket 20 acceptance (ticket commit only)

Against `91e51fad`, not the ready merge:

- Parsed|Unparsed and Persisted|Unpersisted on Workspace, Directory, and File: present on `Node`; setters reject Normal and Workspaces.
- Axes settable and readable: `Graph.setParseState` / `Graph.setPersistState` plus [SpecialNodeStateTests](tests/Shared.Tests/SpecialNodeStateTests.fs).
- Dual-write with old `DocumentState`: `Op.SetDocumentState` and [DocumentAssembly.seedUnparsedStub](src/Shared/dotnet/DocumentAssembly.fs) write the parse axis. Create special does not (must-fix 2).
- Create → Unparsed + Persisted on the new axes: yes. Graph edit → nearest owning special Unpersisted only: `setText` / `setName` / `setClasses` / `replace` call [SpecialNodeState.markUnpersisted](src/Shared/SpecialNodeState.fs); ancestors stay Persisted.
- No Parse actor, Persist stack, or stack push in the ticket commit.

No fixes implemented. Ticket Status left `defined`.
