# 20 — State axes on special nodes

**Type:** coding
**Status:** coded
**Blocked by:** None — can start immediately
Actual: 2h5m

## Context

A person works a Graph that already has Workspace, Directory, and File special nodes. Later Parse (disk → Graph) and Persist (Graph → disk) need two independent markers on those nodes. Today `DocumentState` is `Current` | `Unparsed` | `NoServerFile`. Parsed is the other pole of Unparsed (`Current` is today's name). Persisted | Unpersisted is not present. This ticket adds the axes as Graph state that can be set and read. It does not start workers.

## What to build

Special nodes carry **Parsed | Unparsed** and **Persisted | Unpersisted**. Code can set and read each axis on a Workspace Node, a Directory Node, and a File Node. No Parse actor, no Persist stack, no git Load retarget, no Upload or selection Parse, no path-control change, and no retirement of old hops.

### 1. Special-node state axes

Add the two axes as Graph markers on special nodes only. Point of lock: [19 — Parsed/Unparsed and Persisted/Unpersisted](19-file-newer-graph-newer.md). Migration home: [Here→There — step 1 locked](../here-to-there.md).

- [x] 1.1 Carry Parsed | Unparsed — Each Workspace Node, Directory Node, and File Node has this axis. Parsed is the other pole of Unparsed.
- [x] 1.2 Carry Persisted | Unpersisted — Each Workspace Node, Directory Node, and File Node has this axis. The axis is independent of Parsed | Unparsed.
- [x] 1.3 Set and read both axes — Tests prove a special node can set and read each axis. No Parse or Persist work starts.
- [x] 1.4 Leave workers unbuilt — Do not stand up the Parse actor or stack, the Core Persist stack, git Load retarget, Upload or selection Parse, path-control migration, or retirement of old hops.

## Notes

Locked 2026-09-28 (Alan). Axis-write mechanics live on [Here→There — step 1 locked](../here-to-there.md) §4 and [[../map.md]] decision 23. This ticket’s acceptance stays markers set and read only. It does not start workers.

**Approach (Mikado)** — Locked 2026-09-28 (Alan). Create the new state axes. Set both the new axes and the old `DocumentState` wherever state changes. Migrate old uses over step by step. Finally remove the old.

1. **Writer target** — Core / mailbox only. Until that lands, set the axes at today’s file-edit sites and graph-edit sites.
2. **Graph edit** — Nearest owning special (File Node, Directory Node, or Workspace Node) Unpersisted only. Do not mark ancestors.
3. **Discovery** — Whoever finds a disk change writes the axis. Parse is not the discovery tool. New or deleted member → Directory Node Unparsed. Modified file → File Node Unparsed.
4. **git pull finish** — Notes the same discoveries. No separate axis path.
5. **Persist done** — That node Persisted only.
6. **Directory Parse done** — That Directory Node Parsed only.
7. **Create special** — Unparsed + Persisted.
8. **Client Load on Directory** — Mark the Directory Node Unparsed (re-process). Reconciliation with no extra info can spot disk members the Graph lacks.
9. **Client Load on File** — Mark the File Node Unparsed. Push onto the Parse stack is deferred (needs the Parse loop).
10. **Directory Parse body** (deferred) — Walks all nodes tied to that `.amb`, not only immediate children. Create missing File Nodes. Disk-newer → File Node Unparsed (and push when the stack exists).
11. **Parse stack pop** (deferred) — If the node is already Parsed, skip. Real work arrives Unparsed.

## See also

- [19 — Parsed/Unparsed and Persisted/Unpersisted](19-file-newer-graph-newer.md)
- [Here→There — step 1 locked](../here-to-there.md)
- [[../map.md]] decision 23

## Comments

- 2026-09-28: Alan locked axis-write mechanics. Notes record who writes each axis. Acceptance stays markers set and read only. No workers.
- 2026-09-28: Alan locked Mikado. Create the new axes; set both new and old wherever state changes; migrate old uses step by step; finally remove the old. Acceptance stays markers set and read only.
- 2026-09-28: Coded. `ParseState` and `PersistState` live on `Node`. `Graph.setParseState` / `Graph.setPersistState` set and read. `Op.SetDocumentState` and `DocumentAssembly.seedUnparsedStub` dual-write the parse axis. `Op.NewSpecialNode` starts Unparsed + Persisted. Graph edits (`setText` / `setName` / `setClasses` / `replace`) mark the nearest owning special Unpersisted only.
- 2026-09-28: Merged `origin/ready` (`fb83e3ce`). Ready added git Load after-step / directory-match helpers and `LazyLoadReconciliation.currentDiscoveredAsModified` (reads `DocumentState` Current only). No new DocumentState write sites. Dual-write stays on `Op.SetDocumentState` apply.
- 2026-09-28: Review must-fixes: create special now dual-writes `DocumentState` Unparsed with the parse axis. Reverted the ready Load/reconciliation merge so the PR vs staging is Ticket 20 axes only.

## Time

- 2026-09-28 15m — recorded axis-write mechanics Notes from Alan lock (from chat)
- 2026-09-28 5m — recorded Mikado approach from Alan lock (from chat)
- 2026-09-28 1h — Shared axes, dual-write at existing state-change sites, Shared.Tests (from chat)
- 2026-09-28 45m — review must-fixes: create dual-write + strip ready Load slice (from chat)
