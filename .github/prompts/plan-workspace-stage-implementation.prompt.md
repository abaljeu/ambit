## Plan: Workspace-Only Stage Implementation

Implement workspace lifecycle and mapping support by extending existing graph invariants, change ops, persistence projection, desktop local config, and command/UI surfaces, while explicitly excluding directory/file lifecycle. Reuse the existing Trash invariant pattern and change-replay architecture so workspace behavior remains deterministic under sync/replay.


**Steps**
1. Phase 1: Graph Model + Invariants (blocks all downstream)
2. Add a canonical WORKSPACES container concept in [src/Shared/Model.fs](src/Shared/Model.fs) alongside ROOT/TRASH constants.
3. Extend graph bootstrap/rebuild paths in Graph.fromNodes to guarantee exactly one owner WORKSPACES child under ROOT, similar to ensureTrashNode. Add normalization helper(s) for workspace labels and a graph-level workspace lookup index (label -> NodeId).
4. Add workspace identity rules: case-insensitive uniqueness for lookup, display casing retained on node metadata (recommend storing display label in Node.name).
5. Extend root/replace validation logic in Graph.replace and History ownership validation to reject invalid WORKSPACES structure and duplicate labels. Depends on step 2.
6. Phase 2: Workspace Ops + Replay (depends on Phase 1)
7. Extend Op union and apply/undo pipeline in [src/Shared/History.fs](src/Shared/History.fs): add create-workspace and rename-workspace operations with deterministic error messages for duplicate/unknown labels.
8. Ensure Change.apply/undo and History.applyChange preserve idempotency/conflict behavior under replay, including duplicate submissions from server-side dedup.
9. Add command-level operation constructors in client-side controller/update flow so palette commands call workspace ops rather than ad hoc graph edits.
10. Phase 3: Shared Serialization + Projection Persistence (depends on Phases 1-2)
11. Fix SpecialKind serialization coverage in [src/Shared/Serialization.fs](src/Shared/Serialization.fs): encode/decode must handle Workspace, Directory, File, Trash (currently trash-only).
12. Extend Graph projection rows in [src/Shared/GraphProjection.fs](src/Shared/GraphProjection.fs) to persist node kind and rebuild it on load (current projection loses kind except TRASH-by-id).
13. Ensure snapshot read/write in [src/Shared/Snapshot.fs](src/Shared/Snapshot.fs) preserves WORKSPACES/workspace special nodes similarly to TRASH canonical handling so FileAgent mode and DB mode behave consistently.
14. Phase 4: Server Database + Load/Save (depends on Phase 3)
15. Add node kind persistence column to nodes table in [src/Server/Database.fs](src/Server/Database.fs) and update inserts/selects.
16. Database changes required:
17. Add nodes.kind TEXT NOT NULL with constrained values: normal, workspace, directory, file, trash.
18. Backfill existing rows with normal, then set canonical trash row kind=trash by id.
19. Update read/write DTOs (NodeDbRow and GraphProjection conversions) to round-trip kind.
20. No separate workspace mapping table needed for this stage if mapping is derived from Special Workspace nodes and label metadata in shared graph. Keep decision explicit.
21. Validate startup load fails fast for duplicate/case-conflicting workspace labels after graph reconstruction.
22. Phase 5: Desktop Local Workspace Mapping JSON (parallel with Phase 6 after Phase 2)
23. Add a desktop local mapping store module in [src/Desktop](src/Desktop), following [src/Desktop/AuthStore.fs](src/Desktop/AuthStore.fs) load/save/protect pattern.
24. Persist one mapping per workspace label: label -> absolute local root path, with JSON validation (malformed JSON, duplicate labels, non-absolute path).
25. Wire config loading into desktop request handling in [src/Desktop/LocalProxy.fs](src/Desktop/LocalProxy.fs).
26. Add a desktop endpoint for open-workspace-in-explorer that resolves workspace label to local path and returns explicit unmapped/invalid diagnostics.
27. Phase 6: Client Command/UI Surface (parallel with Phase 5 after Phase 2)
28. Add workspace commands (create, rename, list) in [src/Client/Controller.fs](src/Client/Controller.fs) commandRegistry and filteredCommands flow.
29. Add command dispatch affordances in [src/Client/View.fs](src/Client/View.fs) and [src/Client/App.fs](src/Client/App.fs) where command palette/buttons are wired.
30. Add unresolved workspace indicator path in shared VM indicator pipeline by extending file-reference-like flow in [src/Shared/DesktopCapabilities.fs](src/Shared/DesktopCapabilities.fs) and [src/Shared/ViewModelOps.fs](src/Shared/ViewModelOps.fs).
31. Keep local mapping edit/list commands out of scope this stage; only consume desktop config and surface diagnostics.
32. Phase 7: Workspace Resolver Base Support (depends on Phase 1, consumed by Phase 6)
33. Add workspace-only resolver for @workspace:label in shared resolver path (new functions in existing shared reference module area, reusing current parse/indicator architecture).
34. Unknown labels must return unresolved state, not silent no-op.
35. Do not implement directory/file path steps yet.
36. Phase 8: Verification + Stage Exit (depends on all phases)
37. Extend [tests/Shared.Tests/HistoryTests.fs](tests/Shared.Tests/HistoryTests.fs) with create/rename workspace apply/undo/conflict/replay cases.
38. Extend [tests/Shared.Tests/GraphProjectionTests.fs](tests/Shared.Tests/GraphProjectionTests.fs) with workspace kind + label mapping round-trip cases.
39. Extend [tests/Shared.Tests/ViewModelTests.fs](tests/Shared.Tests/ViewModelTests.fs) with WORKSPACES bootstrap invariants and unresolved workspace indicator behavior.
40. Extend [tests/Server.Tests/DbAgentTests.fs](tests/Server.Tests/DbAgentTests.fs) and [tests/Server.Tests/DatabaseSetupTests.fs](tests/Server.Tests/DatabaseSetupTests.fs) for DB schema migration/backfill/startup-validation.
41. Add desktop-focused tests under [tests](tests) for mapping config load/save/validation and open-explorer endpoint behavior.
42. Run fullstack and targeted tests: Shared tests, Server tests, desktop build/run smoke.

**Relevant files**
- [src/Shared/Model.fs](src/Shared/Model.fs) — Graph record/index additions, canonical WORKSPACES node, invariant helpers, Graph.fromNodes bootstrap logic.
- [src/Shared/History.fs](src/Shared/History.fs) — Op union, apply/undo, validateOwnershipSemantics extension for workspace invariants.
- [src/Shared/Serialization.fs](src/Shared/Serialization.fs) — SpecialKind encode/decode coverage and op codec additions.
- [src/Shared/GraphProjection.fs](src/Shared/GraphProjection.fs) — persistence row shape (node kind), graphFromPersistence reconstruction and validation.
- [src/Shared/Snapshot.fs](src/Shared/Snapshot.fs) — file snapshot round-trip for special workspace/container nodes.
- [src/Shared/DesktopCapabilities.fs](src/Shared/DesktopCapabilities.fs) — workspace reference parse/result type extensions.
- [src/Shared/ViewModelOps.fs](src/Shared/ViewModelOps.fs) — unresolved indicator and desktop request effects for workspace actions.
- [src/Client/Controller.fs](src/Client/Controller.fs) — commandRegistry entries and workspace command operations.
- [src/Client/View.fs](src/Client/View.fs) — palette rendering/dispatch behavior for new workspace commands.
- [src/Client/App.fs](src/Client/App.fs) — command button wiring (if exposing workspace commands there).
- [src/Desktop/LocalProxy.fs](src/Desktop/LocalProxy.fs) — desktop endpoints, mapping lookup, explorer action, diagnostics.
- [src/Desktop/AuthStore.fs](src/Desktop/AuthStore.fs) — implementation template for local encrypted config storage.
- [src/Server/Database.fs](src/Server/Database.fs) — schema migration (nodes.kind), read/write DTOs, load validation.
- [src/Server/DbAgent.fs](src/Server/DbAgent.fs) — relies on updated persistence/read model; verify dedup/replay unchanged.
- [src/Server/FileAgent.fs](src/Server/FileAgent.fs) — consistency path with snapshot/replay when in file mode.
- [tests/Shared.Tests/HistoryTests.fs](tests/Shared.Tests/HistoryTests.fs) — workspace op behavior tests.
- [tests/Shared.Tests/GraphProjectionTests.fs](tests/Shared.Tests/GraphProjectionTests.fs) — workspace persistence round-trip tests.
- [tests/Shared.Tests/ViewModelTests.fs](tests/Shared.Tests/ViewModelTests.fs) — workspace special-node and unresolved-indicator tests.
- [tests/Server.Tests/DbAgentTests.fs](tests/Server.Tests/DbAgentTests.fs) — DB persistence/restart tests for workspace data.
- [tests/Server.Tests/DatabaseSetupTests.fs](tests/Server.Tests/DatabaseSetupTests.fs) — schema/init validation tests.

**Verification**
1. Shared unit tests: workspace op apply/undo/replay conflict matrix passes.
2. Shared persistence tests: graph -> projection -> graph preserves workspace special kinds and labels.
3. Serialization tests: all SpecialKind values and workspace ops round-trip JSON.
4. DB initialization test: existing DB upgrades without data loss; nodes.kind backfill succeeds.
5. DB restart test: workspace labels resolve identically before/after restart.
6. File mode parity test: Snapshot.write/read retains workspace container + workspace nodes.
7. Client command tests/manual: create/rename/list commands show expected results and validation errors.
8. Desktop tests/manual: open-workspace-in-explorer returns success for mapped labels and explicit unmapped diagnostics for unknown/unmapped labels.
9. Resolver tests: @workspace: known label resolves, unknown label yields explicit unresolved state.

**Decisions**
- Included scope: workspace identity, create/rename/list, shared mapping to workspace root node, desktop local mapping config consumption, unresolved diagnostics.
- Excluded scope: directory/file lifecycle, workspace-relative path traversal, local mapping edit/list command surface, filesystem sync.
- Recommended storage decision: use graph-native workspace mapping (Special Workspace node + label metadata) and graph-level computed index; avoid separate workspace_labels SQL table this stage.
- Required DB delta remains: persist Node.kind in nodes projection because workspace special nodes must survive DB/file round-trips.

**Further Considerations**
1. Label normalization algorithm: recommend invariant lowercase + trimmed for identity, preserve original casing for display.
2. Reserved labels: recommend blocking ROOT, WORKSPACES, TRASH (case-insensitive) for user-defined workspace names.
3. Explorer invocation behavior: recommend fail-fast JSON diagnostic over silent no-op when mapping missing or path invalid.