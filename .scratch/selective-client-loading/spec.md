# Selective client loading

Status: ready-for-agent
See also: [[map.md]], [[issues/14-simplify-selective-loading.md]], [[doc/roadmap/on-demand-graph-residency.md]]

## Problem Statement

Gambol currently sends the complete graph to every browser and keeps it fully resident for the webpage session. As workspaces grow, startup, synchronization, search, and browser memory therefore scale with graph content that the user may never visit.

The first useful reduction does not require partial server residency, per-document versions, cache eviction, or automatic loading. Gambol needs a smaller coherent phase in which the server remains authoritative and fully resident while each client starts with only the Workspaces needed for ROOT and restored navigation, then grows its resident projection only through an explicit Load command.

This phase must preserve local-first editing, the existing Upload workflow under its new Load name, ordered single-flight synchronization, canonical server History, projected Undo and Redo, and predictable behavior when commands encounter unloaded child lists.

## Solution

Give every client graph an explicit distinction between an authoritative loaded child list and an unloaded child list. Bootstrap the complete ROOT Workspace and, when needed, the complete Workspace containing the saved zoom target. Keep client residency monotonic by complete Workspace for the rest of the webpage session.

Rename Upload to Load. Bootstrap and explicit Load are the only residency-producing actions. Load keeps the existing source synchronization stages and additionally asks the fully resident server for complete owning-Workspace snapshots only for selected targets that require them; already-resident targets receive normal poll catch-up only. Ordered HistoryAction tails and snapshots are captured at one response revision and applied through the existing single-flight synchronization flow.

Treat the client graph as a resident projection of the canonical server graph. Shared behavior applies non-structural facts from ordered HistoryActions to resident Node headers, applies structural effects only where the child list is already Loaded, retains complete History, and guards local structural plans from modifying unloaded child lists. MoveSelected is the one deliberate exception: it may move into an unloaded destination and disappear from the resident projection until that Workspace is loaded.

## User Stories

1. As a user opening Gambol, I want startup cost to depend on the Workspaces needed for my initial view rather than the entire graph, so that large unrelated Workspaces do not delay the page.
2. As a user without a saved zoom target, I want startup to load exactly the complete ROOT Workspace, so that startup has a deterministic minimum scope.
3. As a user whose saved zoom target belongs to ROOT, I want startup to load only ROOT, so that restoration does not request duplicate residency.
4. As a user whose saved zoom target belongs to another Workspace, I want startup to load complete ROOT plus that complete Workspace, so that my saved location can be restored immediately.
5. As a user whose saved zoom target no longer exists in the canonical graph, I want startup to install only complete ROOT and fall back to the default in-ROOT view, so that stale session state does not request a second Workspace.
6. As a user with saved fold preferences, I want those preferences restored only where their nodes are resident, so that folds never cause implicit loading.
7. As a user, I want every canonical ROOT child and the complete SYSTEM and TRASH subtrees available after bootstrap, so that core navigation and deletion behavior remain available.
8. As a user, I want named Workspace nodes encountered inside ROOT to appear as ordinary resident headers with unloaded children, so that I can see available Workspaces without downloading their contents.
9. As a user, I want a loaded empty child list to remain distinguishable from an unloaded child list, so that a true leaf is not confused with missing content.
10. As a user, I want loading one Workspace to retain every Workspace already loaded in this webpage session, so that navigating back never causes a second load.
11. As a user, I want a page refresh to begin a new residency session, so that the browser can reclaim prior session state without an eviction subsystem.
12. As a user, I want external and Ref targets reached by loaded content to be present as ordinary Node headers without automatically loading their children, so that references remain meaningful without expanding residency.
13. As a user, I want every resident header to retain its canonical owner even when that owner's edge is unloaded while derived parent and owner indexes contain only authoritative edges from loaded child lists, so that projected graph queries neither invent relationships nor replace known identity.
14. As a user, I want unloaded nodes to behave like leaves for operations that do not require complete children, so that navigation remains simple and deterministic.
15. As a user, I want only bootstrap and Load to obtain new resident content, so that Zoom, Find, Move, edit, traversal, restoration, and folding never trigger surprising network work.
16. As a user, I want the existing Upload command to be named Load while retaining `Ctrl+Shift+>`, so that the broader behavior has one clear name without changing its shortcut.
17. As a user, I want Unloaded and Unparsed to remain distinct states even though both use a hollow-circle affordance and the Load command, so that source freshness is not confused with client residency.
18. As a user clicking a hollow circle on an unselected occurrence, I want that occurrence to become the sole selection before Load runs, so that the clicked target is loaded.
19. As a user clicking a hollow circle on an already selected occurrence, I want the full current selection preserved, so that I can Load several selected targets together.
20. As a user invoking Load, I want all existing Upload filters, stages, ordering rules, and source-side effects preserved, so that desktop push, parse, and reconciliation continue to work.
21. As a user loading a target whose children are already Loaded, I want its normal source synchronization stages and poll catch-up to run without another Workspace snapshot, so that Load remains useful for Unparsed content without redundant transfer.
22. As a user loading a target whose children are Unloaded, I want its complete owning Workspace added at the same revision as poll catch-up, so that the projection becomes coherent in one synchronization response.
23. As a user selecting targets from several Workspaces, I want one Load to resolve and deduplicate every owning Workspace, so that multi-selection does not duplicate snapshots.
24. As a user zooming into an unloaded node, I want it treated as an ordinary leaf, so that Zoom never loads implicitly.
25. As a user using Find, I want results limited to the resident projection, so that search is synchronous and does not hydrate unrelated Workspaces.
26. As a user committing a Find result, I want exactly the normal Zoom behavior, so that search navigation does not introduce a hidden loading path.
27. As a keyboard user, I want unfold and range commands to consume the resident child lists and naturally no-op or continue at unloaded nodes, so that only an explicit hollow-circle click or Load command causes loading.
28. As a user, I want Poll, submit, bootstrap, and Load HistoryAction responses serialized through one global single-flight synchronization state, so that responses cannot race or apply out of order.
29. As a user requesting Load while synchronization is busy, I want it queued and released through the existing synchronization planner, so that no parallel compare-and-swap or replay mechanism is required.
30. As a user, I want a Load response to contain the ordered Change, Undo, and Redo HistoryActions from my base revision through one atomically captured response revision, so that no canonical action is skipped or reinterpreted.
31. As a user, I want every requested complete Workspace snapshot captured at the same revision as its ordered HistoryAction tail, so that snapshot content and catch-up agree.
32. As a user with pending local HistoryActions, I want the existing optimistic conflict behavior retained, including full reload when the response cannot be applied, so that this phase does not introduce a second merge policy.
33. As a user with unloaded content, I want incoming child-list operations for an unloaded parent skipped in my projection, so that an incremental fragment is never mistaken for a complete child list.
34. As a user with resident Node headers, I want incoming non-structural changes applied to those headers even when their children are unloaded, so that visible metadata stays current.
35. As a user, I want receipt of a complete authoritative child list, including an empty list, to be the event that marks those children Loaded, so that incremental operations cannot accidentally promote residency.
36. As a user, I want my projected client to retain every complete canonical HistoryAction even when some effects are skipped in the projection, so that explicit Change, Undo, and Redo actions stay aligned with the server.
37. As a user planning a structural edit, I want the entire local Change to silently no-op when any planned operation would modify an unloaded child list, so that commands never partially commit.
38. As a user, I want Add Child, Paste, ordinary structural moves, and other structural commands to share one residency guard, so that their unloaded behavior is consistent without command-specific rules.
39. As a user invoking MoveSelected into an unloaded destination, I want the complete move submitted without loading that destination, so that explicit loading remains the only residency mechanism.
40. As a user after MoveSelected into an unloaded destination, I want the source edge removed from my projection while destination insertion is skipped, so that the moved node disappears until I explicitly load the destination Workspace.
41. As a user moving a selection, I want normal command feedback to name the destination even when the node disappears from my projection, so that the result is understandable.
42. As a user undoing a move into an unloaded destination, I want an explicit Undo HistoryAction to restore the source in my projection while removing the hidden destination canonically, so that projected Undo matches canonical Undo without planning an inverted Change.
43. As a user redoing that move, I want an explicit Redo HistoryAction to remove the source again without forcing destination residency, so that projected Redo matches canonical Redo without reissuing the original Change.
44. As a user invoking any structural move other than MoveSelected when its destination is unloaded, I want the command to no-op before commit if it intends to retain or focus the moved node, so that MoveSelected remains the only disappearance exception.
45. As a user deleting or permanently deleting content, I want existing ROOT, SYSTEM, and TRASH behavior and its Undo behavior preserved without a bootstrap exception, so that selective loading does not require a server-only delete command.
46. As a maintainer, I want client residency invariants and projected graph transitions concentrated behind a Shared interface, so that callers and tests do not each reproduce partial-graph rules.
47. As a maintainer, I want selection, hollow-circle rendering, command dispatch, and synchronization effects owned by the client, so that pure graph rules remain independent of the DOM.
48. As a maintainer, I want selected-target resolution and atomic poll-plus-snapshot capture owned by the server, so that clients cannot assemble inconsistent Workspace packages.
49. As a maintainer, I want the delivered client-only residency baseline documented as current behavior and later partial-server residency left in the roadmap, so that documentation authority matches what the system actually implements.
50. As a maintainer upgrading existing data, I want durable Change-only History entries to decode as Change HistoryActions, so that existing logs replay without migration or ambiguity.

## Implementation Decisions

- This specification is the sole current decision for selective client loading. It replaces conflicting portions of decision tickets 01–13; those tickets remain historical deliberation.
- This is a client-partial-residency phase, not a partial-server-residency phase. The server remains fully resident and authoritative even though it gains bootstrap and Load response behavior.
- Client residency is monotonic by complete Workspace during one webpage session. Do not add eviction, re-unloading, or a configurable loading policy.
- ROOT is a complete Workspace. Its package includes the complete SYSTEM and TRASH subtrees and stops at nested named Workspace boundaries, whose headers are resident while their children are Unloaded.
- Startup installs complete ROOT plus at most one additional complete Workspace: the owner of a valid saved zoom target outside ROOT. Saved fold state does not affect the bootstrap request.
- Node retains its ordinary ordered child list and adds a separate `childrenStatus` with exactly `Unloaded` and `Loaded`. `Unloaded` requires an empty resident child list; source state such as Unparsed remains independent.
- `Node.owner` remains a plain canonical `NodeId` and is supplied for every resident header. Graph rebuild preserves that canonical owner when its owner edge or list is Unloaded rather than defaulting it to ROOT. `ownerParentByChild` includes only authoritative Owner edges from Loaded lists. ROOT self-owns. Do not add an Unknown owner state, wrapper, or sentinel.
- Shared graph queries that do not require completeness operate on the resident projection. Only Loaded child lists contribute edges and derived indexes; rebuilding those indexes does not discard canonical owner identity.
- Complete Workspace packages contain ordinary Node headers needed by loaded child lists, including external and Ref targets. There is no separate stub domain type.
- Rename Upload to Load and retain its shortcut, filters, stages, ordering, and source-side effects. Load targets carry the selected Node identity and whether the target requires its owning Workspace.
- The server resolves targets to owning Workspaces and deduplicates packages. A target that does not require its Workspace receives only normal poll catch-up; a target that requires it additionally receives the complete owning-Workspace snapshot.
- There are no Direct, ArtifactClosure, or alternate load modes.
- Retire the existing queued Upload and Uploading states in favor of `QueuedLoad` and global `Loading`. Load's source-synchronization stages run under the same global Loading state that keeps Poll, submit, and Load remote responses single-flight.
- `HistoryAction` has exactly `Change`, `Undo`, and `Redo` cases and is the ordered messaging and persistence unit for submit, Poll, bootstrap and Load catch-up tails, durable log and replay, server apply, the client pending queue, and resident-projection transitions. Undo and Redo are transmitted, queued, stored, replayed, and applied as explicit actions; the client does not turn them into inverted or reissued Changes. Existing durable entries that encode only a Change decode as `HistoryAction.Change`.
- A Load response atomically captures one response revision, every ordered HistoryAction after the request base through that revision, and every requested complete Workspace package at that revision.
- Preserve the current optimistic pending-action and Poll conflict behavior. Do not add edit blocking, response compare-and-swap, retries, a second replay path, or per-node Loading state.
- Put residency-aware projected HistoryAction application behind one Shared interface. It skips structural child-list effects for Unloaded parents, applies non-structural facts to resident headers, modifies child lists incrementally only when already Loaded, and retains complete History with every revision and action.
- Projected Undo and Redo apply their canonical HistoryActions through that interface; they do not plan new local Changes.
- Put one Shared pre-commit guard in front of every local command that plans a Change except MoveSelected. If any planned operation modifies an Unloaded child list, commit nothing and silently no-op.
- MoveSelected is the sole exception. It may submit to an unloaded destination; projected apply removes the source edge and skips destination insertion while the server applies and records the complete move.
- Other structural moves that need to retain or focus the moved node no-op before commit when their destination is unloaded.
- ROOT is always Loaded, so existing SYSTEM, TRASH, ordinary delete, permanent delete, and related Undo behavior require no special remote delete operation.
- Shared owns residency invariants, resident-projection HistoryAction transitions, derived-index rebuilding, History projection, and the structural guard. Client owns selection, affordances, commands, the pending HistoryAction queue, session restoration, and single-flight effects. Server owns canonical HistoryAction application, owning-Workspace resolution, and atomic response capture.
- The implementation uses three high test seams: the Shared resident-graph transition interface, the client synchronization planner, and the server Load/bootstrap handler. Serialization is verified through those interfaces rather than becoming a fourth behavioral seam.

## Testing Decisions

- Tests assert externally visible graph, command, synchronization, and response behavior rather than private helper calls or internal data-flow steps.
- The primary seam is the Shared resident-graph transition interface. Its tests cover complete Workspace closure, ROOT boundaries, child-list status invariants, canonical owner preservation across unloaded owner edges, loaded-only indexes, projected Change application, full HistoryAction and revision retention, projected Undo and Redo, the common structural guard, MoveSelected disappearance, and resident-only search.
- Workspace closure tests reuse the existing document-partition behavior as prior art and prove that ROOT contains its complete core subtrees while a nested Workspace contributes only its header until loaded.
- Graph and History tests reuse current graph-build, large-change, operation-application, and sync-logic cases as prior art. They prove an unloaded child list is never treated as an authoritative empty list, an incremental child operation never promotes it to Loaded, graph rebuild never replaces a resident header's canonical owner when the owner list is unloaded, ROOT self-owns, and old Change-only durable entries decode and replay as Change HistoryActions.
- Structural command tests reuse existing move-planning tests as prior art. They prove all guarded commands commit nothing when any planned child-list mutation is unloaded and prove MoveSelected alone may disappear into an unloaded destination with canonical Undo and Redo parity.
- Search tests reuse existing synchronous search cases and prove only resident headers and loaded closures participate, with no network effect on commit.
- The second seam is the client synchronization planner. Its tests rename and extend the existing queued Upload and Uploading state-machine cases as `QueuedLoad` and `Loading`, proving that Load queues, releases, and serializes with Poll and submit without concurrent response paths.
- Client planner tests also prove explicit Undo and Redo enqueue HistoryActions rather than inverted or reissued Changes, plus selection-to-target intent, Loaded versus Unloaded request flags, full-selection hollow-circle behavior, startup scope, saved zoom restoration, and folds that never request residency. Thin rendering checks may cover the hollow-circle predicate; browser automation is not required for this phase.
- Existing Upload workflow tests remain authoritative prior art for filters, stage ordering, desktop push, parse, and reconciliation. Renaming to Load must not reduce their behavioral coverage.
- The third seam is the server Load/bootstrap handler over fully resident authoritative state. Its tests prove canonical Change, Undo, and Redo application, owning-Workspace resolution, deduplication across selected targets, complete package boundaries, ROOT bootstrap scope, ordered HistoryAction catch-up, and one shared response revision.
- Response codec coverage is included at the Shared/server seam and proves `Unloaded` and `Loaded`, selected target intent, ordered HistoryAction tails, Workspace packages, and response revision round-trip without inventing separate behavioral abstractions.
- Focused endpoint tests prove that an already-loaded target receives only ordered HistoryAction catch-up, an unloaded target receives its complete owning Workspace at the same revision, and several targets in one Workspace produce one package.
- Completion requires all focused Shared, client synchronization-planner, and server Load/bootstrap tests to pass and the client to compile through Fable. Existing unrelated full suites are not the specification's behavioral seam.

## Out of Scope

- Partial server residency, lazy server cache admission, startup without a fully resident server graph, or server cache eviction.
- Durable document descriptors, node-to-document membership tables, per-document versions, scoped SQL document loaders, or document projection patches.
- Server-backed or hybrid search across unloaded Workspaces, remote search-result hydration, or unparsed source-file content search.
- Client eviction, re-unloading, passive reclamation, memory budgets, pin sets, or IndexedDB/offline startup caches.
- Automatic loading from Zoom, Find, Move, edit, traversal, fold restoration, viewport lookahead, prefetch, or any action other than bootstrap and explicit Load.
- Loading modes other than the selected target's complete owning Workspace.
- Parallel Poll/submit/Load response handling, mutation-epoch compare-and-swap, response retry/replay, or a configurable load coordinator.
- New conflict resolution, server-authoritative merge, edit blocking during Load, or a full projection refresh after every response.
- Breaking the specification into implementation issues; that is the following `/to-tickets` step.

## Further Notes

- Revisit complete-Workspace client granularity only if loading any one Workspace takes more than 10 seconds.
- On completion, extract the implemented client-only selective-loading baseline from [[doc/roadmap/on-demand-graph-residency.md]] into a new authoritative document under [[doc/current/]]. Leave only still-unimplemented server-residency work in the roadmap: bounded SQL loading, per-document versions, projection patches, hybrid search, and reclamation. Do not promote server-cache admission, `NeedsDocuments`, or partial-server startup as current behavior.
- Update [[doc/index.md]] so selective client loading appears as a current baseline while later server residency remains planned. Reconcile the current sync, persistence, workspace-graph, architecture, API, and command references with the new baseline, linking to one authoritative home instead of duplicating it.
- The roadmap currently uses `Unknown | Loaded`; this specification standardizes the client phase on `Unloaded | Loaded`. The current document and remaining roadmap must use those terms deliberately rather than presenting them as the same implemented model.
